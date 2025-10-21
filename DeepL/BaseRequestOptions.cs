// Copyright 2025 DeepL SE (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

using System.Collections.Generic;

namespace DeepL {
  /// <summary>
  ///   Base options that apply to all request endpoints.
  /// </summary>
  public class BaseRequestOptions {
    /// <summary>
    ///   Additional key-value pairs to include in the JSON request body sent to the API. If provided, keys in this
    ///   dictionary will be added to the request body and can override built-in parameters. This can be used to access
    ///   beta features or override built-in parameters for testing purposes. Mostly used by DeepL employees to test
    ///   functionality, or for beta programs.
    /// </summary>
    public Dictionary<string, string>? ExtraBodyParameters { get; set; }
  }
}
