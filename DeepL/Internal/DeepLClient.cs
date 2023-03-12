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
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using DeepL.Model;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Timeout;

namespace DeepL.Internal {
  /// <summary>Internal class implementing HTTP requests.</summary>
  internal class DeepLClient : IDisposable {
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

    /// <summary>Base URL for DeepL API Pro accounts.</summary>
    private const string DeepLServerUrl = "https://api.deepl.com";

    /// <summary>Base URL for DeepL API Free accounts.</summary>
    private const string DeepLServerUrlFree = "https://api-free.deepl.com";



    /// <inheritdoc />
    public async Task<Usage> GetUsageAsync(CancellationToken cancellationToken = default) {
      using var responseMessage = await ApiGetAsync("/v2/usage", cancellationToken).ConfigureAwait(false);
      await CheckStatusCodeAsync(responseMessage).ConfigureAwait(false);
      var usageFields = await JsonUtils.DeserializeAsync<Usage.JsonFieldsStruct>(responseMessage)
            .ConfigureAwait(false);
      return new Usage(usageFields);
    }

    /// <summary>An internal constructor used for DI.</summary>
    internal DeepLClient(HttpClient client, IOptions<TranslatorOptions> optionsFactory) {
      var options = optionsFactory.Value;

      if (options.AuthKey == null) {
        throw new ArgumentNullException(nameof(options.AuthKey));
      }

      var authKey = options.AuthKey.Trim();

      if (authKey.Length == 0) {
        throw new ArgumentException($"{nameof(authKey)} is empty");
      }

      var serverUrl = new Uri(
            options.ServerUrl ?? (AuthKeyIsFreeAccount(authKey) ? DeepLServerUrlFree : DeepLServerUrl));

      var headers = new Dictionary<string, string?>(options.Headers, StringComparer.OrdinalIgnoreCase);

      if (!headers.ContainsKey("User-Agent")) {
        headers.Add("User-Agent", ConstructUserAgentString(options.sendPlatformInfo, options.appInfo));
      }

      if (!headers.ContainsKey("Authorization")) {
        headers.Add("Authorization", $"DeepL-Auth-Key {authKey}");
      }

      if (serverUrl == null) {
        throw new ArgumentNullException($"{nameof(serverUrl)}");
      }

      _serverUrl = serverUrl;
      _httpClient = client;
      _disposeClient = true;

      _headers = headers.ToArray();
    }

    /// <summary>
    ///   Builds the user-agent string we want to send to the API with every request. This string contains
    ///   basic information about the client environment, such as the deepl client library version, operating
    ///   system and language runtime version.
    /// </summary>
    /// <param name="sendPlatformInfo">
    ///   <c>true</c> to send platform information with every API request (default), 
    ///   <c>false</c> to only send the library version.
    /// </param>
    /// <param name="AppInfo">
    ///   Name and version of the application using this library. Ignored if null.
    /// </param>
    /// <returns>Enumerable of tuples containing the parameters to include in HTTP request.</returns>
    private string ConstructUserAgentString(bool sendPlatformInfo = true, AppInfo? appInfo = null) {
      var platformInfoString = $"deepl-dotnet/{Version()}";
      if (sendPlatformInfo) {
        var osDescription = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
        var clrVersion = Environment.Version.ToString();
        platformInfoString += $" ({osDescription}) dotnet-clr/{clrVersion}";
      }
      if (appInfo != null) {
        platformInfoString += $" {appInfo?.AppName}/{appInfo?.AppVersion}";
      }
      return platformInfoString;
    }

    /// <summary>
    ///   Determines if the given DeepL Authentication Key belongs to an API Free account or an API Pro account.
    /// </summary>
    /// <param name="authKey">
    ///   DeepL Authentication Key as found in your
    ///   <a href="https://www.deepl.com/pro-account/">DeepL API account</a>.
    /// </param>
    /// <returns>
    ///   <c>true</c> if the Authentication Key belongs to an API Free account, <c>false</c> if it belongs to an API Pro
    ///   account.
    /// </returns>
    public static bool AuthKeyIsFreeAccount(string authKey) => authKey.TrimEnd().EndsWith(":fx");

    /// <summary>Retrieves the version string, with format MAJOR.MINOR.BUGFIX.</summary>
    /// <returns>String containing the library version.</returns>
    public static string Version() {
      var version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "";
      return version;
    }


    /// <summary>Initializes a new <see cref="DeepLClient" />.</summary>
    /// <param name="serverUrl">Base server URL to apply to all relative URLs in requests.</param>
    /// <param name="clientFactory">Factory function to obtain <see cref="HttpClient" /> used for requests.</param>
    /// <param name="headers">HTTP headers applied to all requests.</param>
    /// <exception cref="ArgumentNullException">If any argument is null.</exception>
    internal DeepLClient(
          Uri serverUrl,
          Func<HttpClientAndDisposeFlag> clientFactory,
          IEnumerable<KeyValuePair<string, string?>> headers) {
      _serverUrl = serverUrl ?? throw new ArgumentNullException($"{nameof(serverUrl)}");
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

    /// <summary>Releases the unmanaged resources and disposes of the managed resources used by the <see cref="DeepLClient" />.</summary>
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
    internal static IAsyncPolicy<HttpResponseMessage> CreateDefaultPolicy(
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
      return Policy.WrapAsync(waitAndRetry, timeout);
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
      var policy = CreateDefaultPolicy(
            perRetryConnectionTimeout,
            maximumNetworkRetries);

#if NET5_0_OR_GREATER
      var handler = new SocketsHttpHandler() { PooledConnectionLifetime = TimeSpan.FromMinutes(2) };
#else
      var handler = new HttpClientHandler() { };
#endif

      var policyHandler = new PolicyHttpMessageHandler(policy) { InnerHandler = handler };
      return new HttpClientAndDisposeFlag {
        DisposeClient = true,
        HttpClient = new HttpClient(policyHandler) { Timeout = overallConnectionTimeout }
      };
    }

    /// <summary>Checks the response HTTP status is OK, otherwise throws corresponding exception.</summary>
    /// <param name="responseMessage"><see cref="HttpResponseMessage" /> received from DeepL API.</param>
    /// <param name="usingGlossary"><c>true</c> if a glossary function is used, otherwise <c>false</c>.</param>
    /// <param name="downloadingDocument"><c>true</c> if document download function is used, otherwise <c>false</c>.</param>
    /// <exception cref="AuthorizationException">If authorization failed.</exception>
    /// <exception cref="QuotaExceededException">If the translation quota has been exceeded.</exception>
    /// <exception cref="GlossaryNotFoundException">If the specified glossary was not found.</exception>
    /// <exception cref="TooManyRequestsException">If the DeepL servers are currently receiving too many requests.</exception>
    /// <exception cref="DeepLException">If some other error occurred.</exception>
    internal static async Task CheckStatusCodeAsync(
          HttpResponseMessage responseMessage,
          bool usingGlossary = false,
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
          if (usingGlossary) {
            throw new GlossaryNotFoundException("Glossary not found" + message);
          } else {
            throw new NotFoundException("Not found, check ServerUrl" + message);
          }
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
        RequestUri = new Uri(_serverUrl, relativeUri + queryString),
        Method = HttpMethod.Get,
        Headers = { Accept = { new MediaTypeWithQualityHeaderValue(acceptHeader ?? "application/json") } }
      };
      return await ApiCallAsync(requestMessage, cancellationToken);
    }

    /// <summary>Internal function to perform HTTP DELETE requests.</summary>
    /// <param name="relativeUri">Endpoint URL relative to server base URL.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns><see cref="HttpResponseMessage" /> received from DeepL API.</returns>
    /// <exception cref="ConnectionException">If any failure occurs while sending the request.</exception>
    public async Task<HttpResponseMessage> ApiDeleteAsync(string relativeUri, CancellationToken cancellationToken) {
      using var requestMessage = new HttpRequestMessage {
        RequestUri = new Uri(_serverUrl, relativeUri),
        Method = HttpMethod.Delete
      };
      return await ApiCallAsync(requestMessage, cancellationToken);
    }

    /// <summary>Internal function to perform HTTP POST requests.</summary>
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
        RequestUri = new Uri(_serverUrl, relativeUri),
        Method = HttpMethod.Post,
        Content = bodyParams != null
                  ? new LargeFormUrlEncodedContent(
                        bodyParams.Select(pair => new KeyValuePair<string, string>(pair.Key, pair.Value)))
                  : null
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
        RequestUri = new Uri(_serverUrl, relativeUri),
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
