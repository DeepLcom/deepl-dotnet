// Copyright 2021 DeepL GmbH (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

using System;
using System.Collections.Generic;
using System.Net.Http;

namespace DeepL {
  /// <summary>Class containing containing options controlling <see cref="Translator" /> behaviour.</summary>
  public sealed class TranslatorOptions {
    /// <summary>
    ///   HTTP headers attached to every HTTP request. By default no extra headers are used. Note that during
    ///   <see cref="Translator" /> initialization headers for Authorization and User-Agent are added, unless they are
    ///   overridden in this option.
    /// </summary>
    public Dictionary<string, string?> Headers { get; set; } = new Dictionary<string, string?>();

    /// <summary>
    ///   The maximum number of failed attempts that <see cref="Translator" /> will retry, per request. By default 5 retries
    ///   are made. Note: only errors due to transient conditions are retried. Only used if <see cref="ClientFactory" /> is
    ///   unset.
    /// </summary>
    public int MaximumNetworkRetries { get; set; } = 5;

    /// <summary>
    ///   Connection timeout for HTTP requests, including all retries, the default is 100 seconds. Only used if
    ///   <see cref="ClientFactory" /> is unset.
    /// </summary>
    public TimeSpan OverallConnectionTimeout { get; set; } = TimeSpan.FromSeconds(100);

    /// <summary>
    ///   Connection timeout used for each HTTP request retry, the default is 10 seconds. Only used if
    ///   <see cref="ClientFactory" /> is unset.
    /// </summary>
    public TimeSpan PerRetryConnectionTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    ///   The base URL for DeepL's API that may be overridden for testing purposes. By default the correct DeepL API URL
    ///   is selected based on the user account type (free or paid).
    /// </summary>
    public string? ServerUrl { get; set; }

    /// <summary>
    ///   Factory function returning an <see cref="HttpClient" /> to be used by <see cref="Translator" /> for HTTP requests,
    ///   and a flag whether to call <see cref="HttpClient.Dispose" /> in <see cref="Translator.Dispose" />.
    ///   Override this function to provide your own <see cref="HttpClient" />. Note that overriding this function will disable
    ///   the built-in retrying of failed-requests, so you must provide your own retry policy.
    /// </summary>
    public Func<HttpClientAndDisposeFlag>? ClientFactory { get; set; }
  }

  /// <summary>
  ///   Struct containing an <see cref="HttpClient" /> to be used by <see cref="Translator" /> and flag whether it
  ///   should be disposed in <see cref="Translator.Dispose" />.
  /// </summary>
  public struct HttpClientAndDisposeFlag {
    /// <summary>
    ///   <see cref="HttpClient" /> used by <see cref="Translator" /> for all requests to DeepL API.
    /// </summary>
    public HttpClient HttpClient { get; set; }

    /// <summary>
    ///   <c>true</c> if the provided <see cref="HttpClient" /> should be disposed of by <see cref="Translator.Dispose" />;
    ///   <c>false</c> if you
    ///   intend to reuse it.
    /// </summary>
    public bool DisposeClient { get; set; }
  }
}
