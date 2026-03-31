// Copyright 2022 DeepL SE (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Http;
using Polly;
using Polly.Timeout;

namespace DeepL.Internal {
  /// <summary>Identifies the type of resource being accessed, used for contextual error messages.</summary>
  internal enum ResourceType {
    Glossary,
    StyleRule
  }

  internal static class ResourceTypeExtensions {
    internal static string ToDisplayString(this ResourceType resourceType) =>
          resourceType switch {
            ResourceType.Glossary => "Glossary",
            ResourceType.StyleRule => "Style rule",
            _ => resourceType.ToString()
          };
  }

  /// <summary>Internal class implementing HTTP requests.</summary>
  internal class DeepLHttpClient : IDisposable {
    /// <summary>HTTP status code returned by DeepL API to indicate servers are currently under high load.</summary>
    private const HttpStatusCode HttpStatusCodeTooManyRequests = (HttpStatusCode)429;

    /// <summary>HTTP status code returned by DeepL API to indicate account translation quota has been exceeded.</summary>
    private const HttpStatusCode HttpStatusCodeQuotaExceeded = (HttpStatusCode)456;

    /// <summary><c>true</c> if <see cref="_httpClient" /> should be disposed, otherwise <c>false</c>.</summary>
    private readonly bool _disposeClient;

    /// <summary>HTTP headers attached to every request.</summary>
    private readonly KeyValuePair<string, string?>[] _headers;

    /// <summary><see cref="HttpClient" /> used for requests to DeepL API.</summary>
    private readonly HttpClient _httpClient;

    /// <summary>The base URL for DeepL's API.</summary>
    private readonly Uri _serverUrl;

    /// <summary>Whether to preserve the path component of the server URL when resolving API request URIs.</summary>
    private readonly bool _preserveServerUrlPath;

    /// <summary>Initializes a new <see cref="DeepLHttpClient" />.</summary>
    /// <param name="serverUrl">Base server URL to apply to all relative URLs in requests.</param>
    /// <param name="clientFactory">Factory function to obtain <see cref="HttpClient" /> used for requests.</param>
    /// <param name="headers">HTTP headers applied to all requests.</param>
    /// <param name="preserveServerUrlPath">
    ///   When <c>true</c>, the path component of <paramref name="serverUrl" /> is preserved in request URIs.
    ///   When <c>false</c>, API paths are resolved from the server root, ignoring any base path.
    /// </param>
    /// <exception cref="ArgumentNullException">If any argument is null.</exception>
    internal DeepLHttpClient(
          Uri serverUrl,
          Func<HttpClientAndDisposeFlag> clientFactory,
          IEnumerable<KeyValuePair<string, string?>> headers,
          bool preserveServerUrlPath = true) {
      if (serverUrl == null) {
        throw new ArgumentNullException($"{nameof(serverUrl)}");
      }

      _preserveServerUrlPath = preserveServerUrlPath;

      // Ensure the server URL ends with a trailing slash so that relative URI resolution
      // (RFC 3986 §5.2.2) appends path segments rather than replacing the last segment.
      // This is important when ServerUrl contains a path prefix such as a reverse-proxy base path.
      var serverUrlStr = serverUrl.ToString();
      _serverUrl = serverUrlStr.EndsWith("/") ? serverUrl : new Uri(serverUrlStr + "/");
      var clientAndDisposeFlag = clientFactory();
      _httpClient = clientAndDisposeFlag.HttpClient;
      _disposeClient = clientAndDisposeFlag.DisposeClient;

      if (_httpClient == null) {
        throw new ArgumentNullException(
              $"{nameof(clientAndDisposeFlag.HttpClient)}",
              $"HttpClient returned by {nameof(clientFactory)} was null");
      }

      _headers = headers.ToArray();
    }

    /// <summary>
    ///   Resolves a relative URI against the server URL.
    ///   When <see cref="_preserveServerUrlPath" /> is <c>true</c>, the path component of the server URL is preserved.
    ///   When <c>false</c>, a leading slash is prepended to the relative URI so that it is resolved from the server root.
    /// </summary>
    /// <param name="relativeUri">Relative URI to resolve against the server URL.</param>
    /// <returns>Resolved absolute <see cref="Uri" />.</returns>
    private Uri ResolveUri(string relativeUri) {
      if (_preserveServerUrlPath) {
        return new Uri(_serverUrl, relativeUri);
      }

      // Prepend "/" so that RFC 3986 resolution treats it as an absolute path,
      // effectively ignoring any base path in _serverUrl.
      var absolutePath = relativeUri.StartsWith("/") ? relativeUri : "/" + relativeUri;
      return new Uri(_serverUrl, absolutePath);
    }

    /// <summary>
    ///   Releases the unmanaged resources and disposes of the managed resources used by the
    ///   <see cref="DeepLHttpClient" />.
    /// </summary>
    public void Dispose() {
      if (_disposeClient) {
        _httpClient.Dispose();
      }
    }

    /// <summary>
    ///   Creates a policy to retry failed HTTP requests wrapping the given handler.
    /// </summary>
    /// <param name="innerHandler"><see cref="HttpMessageHandler" /> on which requests should be retried.</param>
    /// <param name="perRetryConnectionTimeout">Maximum time for each attempted request.</param>
    /// <param name="maximumNetworkRetries">Maximum number of retried requests.</param>
    /// <returns>An <see cref="HttpMessageHandler" /> comprising the inner handler wrapped with the retry policy.</returns>
    private static HttpMessageHandler CreateHttpMessageHandlerWithRetryPolicy(
          HttpMessageHandler innerHandler,
          TimeSpan perRetryConnectionTimeout,
          int maximumNetworkRetries) {
      var rnd = new Random();
      var getSleepDuration = new Func<int, TimeSpan>(
            retryCount => {
              const double backoffInitial = 1.0;
              const double backoffMaximum = 120.0;
              const double backoffJitter = 0.23;
              const double backoffMultiplier = 1.6;
              var backoff = backoffInitial * Math.Pow(backoffMultiplier, retryCount - 1);
              backoff = Math.Min(backoff, backoffMaximum);
              lock (rnd) {
                backoff *= 1.0 + (backoffJitter * ((rnd.NextDouble() * 2.0) - 1.0));
              }

              return TimeSpan.FromSeconds(backoff);
            });

      var timeout = Policy.TimeoutAsync<HttpResponseMessage>(perRetryConnectionTimeout);
      var waitAndRetry = Policy.Handle<TaskCanceledException>()
            .Or<TimeoutRejectedException>()
            .Or<HttpRequestException>(_ => false)
            .Or<Exception>()
            .OrResult<HttpResponseMessage>(
                  responseMessage => responseMessage.StatusCode == HttpStatusCodeTooManyRequests ||
                                     responseMessage.StatusCode >= HttpStatusCode.InternalServerError)
            .WaitAndRetryAsync(maximumNetworkRetries, getSleepDuration);
      var policy = Policy.WrapAsync(waitAndRetry, timeout);
      return new PolicyHttpMessageHandler(policy) { InnerHandler = innerHandler };
    }

    /// <summary>Creates a default HttpClient with exponential-backoff policy for retrying failed requests.</summary>
    /// <param name="perRetryConnectionTimeout">Connection timeout for each HTTP request.</param>
    /// <param name="overallConnectionTimeout">Timeout including all request-retries.</param>
    /// <param name="maximumNetworkRetries">Maximum number of failed requests that may be retried.</param>
    /// <returns>Newly initialized <see cref="HttpClient" /> object.</returns>
    public static HttpClientAndDisposeFlag CreateDefaultHttpClient(
          TimeSpan perRetryConnectionTimeout,
          TimeSpan overallConnectionTimeout,
          int maximumNetworkRetries) {
      var handler = CreateHttpMessageHandlerWithRetryPolicy(
            new HttpClientHandler(),
            perRetryConnectionTimeout,
            maximumNetworkRetries);
      return new HttpClientAndDisposeFlag {
        DisposeClient = true,
        HttpClient = new HttpClient(handler) { Timeout = overallConnectionTimeout }
      };
    }

    /// <summary>Checks the response HTTP status is OK, otherwise throws corresponding exception.</summary>
    /// <param name="responseMessage"><see cref="HttpResponseMessage" /> received from DeepL API.</param>
    /// <param name="usingGlossary"><c>true</c> if a glossary function is used, otherwise <c>false</c>.</param>
    /// <param name="downloadingDocument"><c>true</c> if document download function is used, otherwise <c>false</c>.</param>
    /// <exception cref="AuthorizationException">If authorization failed.</exception>
    /// <exception cref="QuotaExceededException">If the translation quota has been exceeded.</exception>
    /// <exception cref="GlossaryNotFoundException">If the specified glossary was not found.</exception>
    /// <exception cref="GlossaryDictionaryNotFoundException">If the specified glossary dictionary was not found.</exception>
    /// <exception cref="TooManyRequestsException">If the DeepL servers are currently receiving too many requests.</exception>
    /// <exception cref="DeepLException">If some other error occurred.</exception>
    internal static async Task CheckStatusCodeAsync(
          HttpResponseMessage responseMessage,
          ResourceType? resourceType = null,
          bool downloadingDocument = false) {
      var statusCode = responseMessage.StatusCode;
      if (statusCode >= HttpStatusCode.OK && statusCode < HttpStatusCode.BadRequest) {
        return;
      }

      string message;
      try {
        var errorResult = await JsonUtils.DeserializeAsync<ErrorResult>(responseMessage).ConfigureAwait(false);
        message = (errorResult.Message != null ? $", message: {errorResult.Message}" : "") +
                  (errorResult.Detail != null ? $", detail: {errorResult.Detail}" : "");
      } catch (JsonException) {
        message = string.Empty;
      }

      switch (statusCode) {
        case HttpStatusCode.Forbidden:
          throw new AuthorizationException("Authorization failure, check AuthKey" + message);
        case HttpStatusCodeQuotaExceeded:
          throw new QuotaExceededException("Quota for this billing period has been exceeded" + message);
        case HttpStatusCode.NotFound:
          var notFoundMessage = resourceType != null
                ? $"{resourceType.Value.ToDisplayString()} not found" + message
                : "Not found" + message;
          if (resourceType == ResourceType.Glossary) {
            throw new GlossaryNotFoundException(notFoundMessage);
          }
          throw new NotFoundException(notFoundMessage);
        case HttpStatusCode.BadRequest:
          throw new DeepLException("Bad request" + message);
        case HttpStatusCodeTooManyRequests:
          throw new TooManyRequestsException(
                "Too many requests, DeepL servers are currently experiencing high load" + message);
        case HttpStatusCode.ServiceUnavailable:
          if (downloadingDocument) {
            throw new DocumentNotReadyException("Document not ready" + message);
          } else {
            throw new DeepLException("Service unavailable" + message);
          }
        default:
          throw new DeepLException("Unexpected status code: " + statusCode + message);
      }
    }

    /// <summary>Internal function to perform HTTP GET requests.</summary>
    /// <param name="relativeUri">Endpoint URL relative to server base URL.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <param name="queryParams">Parameters to embed in the HTTP request query string.</param>
    /// <param name="acceptHeader">String to use as Accept header.</param>
    /// <returns><see cref="HttpResponseMessage" /> received from DeepL API.</returns>
    /// <exception cref="ConnectionException">If any failure occurs while sending the request.</exception>
    public async Task<HttpResponseMessage> ApiGetAsync(
          string relativeUri,
          CancellationToken cancellationToken,
          IEnumerable<(string Key, string Value)>? queryParams = null,
          string? acceptHeader = null) {
      var queryString = queryParams == null
            ? string.Empty
            : "?" + string.Join(
                  "&",
                  queryParams.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));

      using var requestMessage = new HttpRequestMessage {
        RequestUri = ResolveUri(relativeUri + queryString),
        Method = HttpMethod.Get,
        Headers = { Accept = { new MediaTypeWithQualityHeaderValue(acceptHeader ?? "application/json") } }
      };
      return await ApiCallAsync(requestMessage, cancellationToken);
    }

    /// <summary>Internal function to perform HTTP DELETE requests.</summary>
    /// <param name="relativeUri">Endpoint URL relative to server base URL.</param>
    /// <param name="queryParams">Parameters to embed in the HTTP request query string.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns><see cref="HttpResponseMessage" /> received from DeepL API.</returns>
    /// <exception cref="ConnectionException">If any failure occurs while sending the request.</exception>
    public async Task<HttpResponseMessage> ApiDeleteAsync(
          string relativeUri,
          CancellationToken cancellationToken,
          IEnumerable<(string Key, string Value)>? queryParams = null) {
      var queryString = queryParams == null
            ? string.Empty
            : "?" + string.Join(
                  "&",
                  queryParams.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
      using var requestMessage = new HttpRequestMessage {
        RequestUri = ResolveUri(relativeUri + queryString),
        Method = HttpMethod.Delete
      };
      return await ApiCallAsync(requestMessage, cancellationToken);
    }

    /// <summary>Internal function to perform HTTP POST requests with form-encoded body.</summary>
    /// <param name="relativeUri">Endpoint URL relative to server base URL.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <param name="bodyParams">Parameters to embed in the HTTP request body.</param>
    /// <returns><see cref="HttpResponseMessage" /> received from DeepL API.</returns>
    /// <exception cref="ConnectionException">If any failure occurs while sending the request.</exception>
    public async Task<HttpResponseMessage> ApiPostAsync(
          string relativeUri,
          CancellationToken cancellationToken,
          IEnumerable<(string Key, string Value)>? bodyParams = null) {
      using var requestMessage = new HttpRequestMessage {
        RequestUri = ResolveUri(relativeUri),
        Method = HttpMethod.Post,
        Content = bodyParams != null
                  ? new LargeFormUrlEncodedContent(
                        bodyParams.Select(pair => new KeyValuePair<string, string>(pair.Key, pair.Value)))
                  : null
      };
      return await ApiCallAsync(requestMessage, cancellationToken);
    }

    /// <summary>Internal function to perform HTTP POST requests with JSON body.</summary>
    /// <param name="relativeUri">Endpoint URL relative to server base URL.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <param name="body">Object to serialize as JSON for the request body.</param>
    /// <param name="jsonOptions">Optional JSON serializer options.</param>
    /// <returns><see cref="HttpResponseMessage" /> received from DeepL API.</returns>
    /// <exception cref="ConnectionException">If any failure occurs while sending the request.</exception>
    public async Task<HttpResponseMessage> ApiPostJsonAsync(
          string relativeUri,
          CancellationToken cancellationToken,
          object body,
          JsonSerializerOptions? jsonOptions = null) {
      var jsonBody = JsonSerializer.Serialize(body, jsonOptions);
      using var requestMessage = new HttpRequestMessage {
        RequestUri = ResolveUri(relativeUri),
        Method = HttpMethod.Post,
        Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
      };
      return await ApiCallAsync(requestMessage, cancellationToken);
    }

    /// <summary>Internal function to perform HTTP PUT requests with form-encoded body.</summary>
    /// <param name="relativeUri">Endpoint URL relative to server base URL.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <param name="bodyParams">Parameters to embed in the HTTP request body.</param>
    /// <returns><see cref="HttpResponseMessage" /> received from DeepL API.</returns>
    /// <exception cref="ConnectionException">If any failure occurs while sending the request.</exception>
    public async Task<HttpResponseMessage> ApiPutAsync(
          string relativeUri,
          CancellationToken cancellationToken,
          IEnumerable<(string Key, string Value)>? bodyParams = null) {
      using var requestMessage = new HttpRequestMessage {
        RequestUri = ResolveUri(relativeUri),
        Method = HttpMethod.Put,
        Content = bodyParams != null
                  ? new LargeFormUrlEncodedContent(
                        bodyParams.Select(pair => new KeyValuePair<string, string>(pair.Key, pair.Value)))
                  : null
      };
      return await ApiCallAsync(requestMessage, cancellationToken);
    }

    /// <summary>Internal function to perform HTTP PUT requests with JSON body.</summary>
    /// <param name="relativeUri">Endpoint URL relative to server base URL.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <param name="body">Object to serialize as JSON for the request body.</param>
    /// <param name="jsonOptions">Optional JSON serializer options.</param>
    /// <returns><see cref="HttpResponseMessage" /> received from DeepL API.</returns>
    /// <exception cref="ConnectionException">If any failure occurs while sending the request.</exception>
    public async Task<HttpResponseMessage> ApiPutJsonAsync(
          string relativeUri,
          CancellationToken cancellationToken,
          object body,
          JsonSerializerOptions? jsonOptions = null) {
      var jsonBody = JsonSerializer.Serialize(body, jsonOptions);
      using var requestMessage = new HttpRequestMessage {
        RequestUri = ResolveUri(relativeUri),
        Method = HttpMethod.Put,
        Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
      };
      return await ApiCallAsync(requestMessage, cancellationToken);
    }

    /// <summary>Internal function to perform HTTP PATCH requests with form-encoded body.</summary>
    /// <param name="relativeUri">Endpoint URL relative to server base URL.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <param name="bodyParams">Parameters to embed in the HTTP request body.</param>
    /// <returns><see cref="HttpResponseMessage" /> received from DeepL API.</returns>
    /// <exception cref="ConnectionException">If any failure occurs while sending the request.</exception>
    public async Task<HttpResponseMessage> ApiPatchAsync(
          string relativeUri,
          CancellationToken cancellationToken,
          IEnumerable<(string Key, string Value)>? bodyParams = null) {
      using var requestMessage = new HttpRequestMessage {
        RequestUri = ResolveUri(relativeUri),
        Method = new HttpMethod("PATCH"),
        Content = bodyParams != null
                  ? new LargeFormUrlEncodedContent(
                        bodyParams.Select(pair => new KeyValuePair<string, string>(pair.Key, pair.Value)))
                  : null
      };
      return await ApiCallAsync(requestMessage, cancellationToken);
    }

    /// <summary>Internal function to perform HTTP PATCH requests with JSON body.</summary>
    /// <param name="relativeUri">Endpoint URL relative to server base URL.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <param name="body">Object to serialize as JSON for the request body.</param>
    /// <param name="jsonOptions">Optional JSON serializer options.</param>
    /// <returns><see cref="HttpResponseMessage" /> received from DeepL API.</returns>
    /// <exception cref="ConnectionException">If any failure occurs while sending the request.</exception>
    public async Task<HttpResponseMessage> ApiPatchJsonAsync(
          string relativeUri,
          CancellationToken cancellationToken,
          object body,
          JsonSerializerOptions? jsonOptions = null) {
      var jsonBody = JsonSerializer.Serialize(body, jsonOptions);
      using var requestMessage = new HttpRequestMessage {
        RequestUri = ResolveUri(relativeUri),
        Method = new HttpMethod("PATCH"),
        Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
      };
      return await ApiCallAsync(requestMessage, cancellationToken);
    }

    /// <summary>Internal function to upload files using an HTTP POST request.</summary>
    /// <param name="relativeUri">Endpoint URL relative to server base URL.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <param name="bodyParams">Parameters to embed in the HTTP request body.</param>
    /// <param name="file">Optional file content to upload in request.</param>
    /// <param name="fileName">If <see cref="file" /> is used, the name of file.</param>
    /// <returns><see cref="HttpResponseMessage" /> received from DeepL API.</returns>
    /// <exception cref="ConnectionException">If any failure occurs while sending the request.</exception>
    public async Task<HttpResponseMessage> ApiUploadAsync(
          string relativeUri,
          CancellationToken cancellationToken,
          IEnumerable<(string Key, string Value)> bodyParams,
          Stream file,
          string fileName) {
      var content = new MultipartFormDataContent();
      foreach (var (key, value) in bodyParams) {
        content.Add(new StringContent(value), key);
      }

      content.Add(new StreamContent(file), "file", fileName);

      using var requestMessage = new HttpRequestMessage {
        RequestUri = ResolveUri(relativeUri),
        Method = HttpMethod.Post,
        Content = content,
        Headers = { Accept = { new MediaTypeWithQualityHeaderValue("application/json") } }
      };
      return await ApiCallAsync(requestMessage, cancellationToken);
    }

    /// <summary>Sends given HTTP request, ensuring message uses HTTP 2.0 and includes configured HTTP headers.</summary>
    /// <param name="requestMessage"><see cref="HttpRequestMessage" /> to send to the DeepL API.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns><see cref="HttpResponseMessage" /> received from DeepL API.</returns>
    /// <exception cref="ConnectionException">If any failure occurs while sending the request.</exception>
    private async Task<HttpResponseMessage> ApiCallAsync(
          HttpRequestMessage requestMessage,
          CancellationToken cancellationToken) {
      try {
        foreach (var header in _headers) {
          requestMessage.Headers.Add(header.Key, header.Value);
        }

        return await _httpClient.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
        // Distinguish cancellation due to user-provided token or request time-out
      } catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested) {
        throw;
      } catch (TaskCanceledException ex) {
        throw new ConnectionException($"Request timed out: {ex.Message}", ex);
      } catch (HttpRequestException ex) {
        throw new ConnectionException($"Request failed: {ex.Message}", ex);
      } catch (Exception ex) {
        throw new ConnectionException($"Unexpected request failure: {ex.Message}", ex);
      }
    }

    /// <summary>Class used for JSON-deserialization of error results.</summary>
    private readonly struct ErrorResult {
      /// <summary>Initializes a new instance of <see cref="ErrorResult" />, used for JSON deserialization.</summary>
      [JsonConstructor]
      public ErrorResult(string? message, string? detail) {
        Message = message;
        Detail = detail;
      }

      /// <summary>Message describing the error, if it was included in response.</summary>
      public string? Message { get; }

      /// <summary>String explaining more detail the error, if it was included in response.</summary>
      public string? Detail { get; }
    }
  }
}
