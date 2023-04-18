// Copyright 2022 DeepL SE (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

namespace DeepL.Model.Exceptions;

/// <summary>Exception thrown when the specified authentication key was invalid.</summary>
public sealed class AuthorizationException : DeepLException {
  /// <summary>Initializes a new instance of the <see cref="AuthorizationException" /> class.</summary>
  /// <param name="message">The message that describes the error.</param>
  internal AuthorizationException(string message) : base(message) {
  }
}
