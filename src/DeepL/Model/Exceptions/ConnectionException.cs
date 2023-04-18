// Copyright 2022 DeepL SE (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

using System;

namespace DeepL.Model.Exceptions;

/// <summary>Exception thrown when a connection error occurs while accessing the DeepL API.</summary>
public sealed class ConnectionException : DeepLException {
  /// <summary>Initializes a new instance of the <see cref="ConnectionException" /> class.</summary>
  /// <param name="message">The message that describes the error.</param>
  /// <param name="innerException">The exception representing the connection error.</param>
  internal ConnectionException(string message, Exception innerException) : base(
        message,
        innerException) {
  }
}
