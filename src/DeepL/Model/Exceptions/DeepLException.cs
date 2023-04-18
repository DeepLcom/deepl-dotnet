// Copyright 2022 DeepL SE (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

using System;
using DeepL.Model;

namespace DeepL.Model.Exceptions;

/// <summary>Base class for all exceptions thrown by this library.</summary>
public class DeepLException : Exception {
  /// <summary>Initializes a new instance of the <see cref="AuthorizationException" /> class.</summary>
  /// <param name="message">The message that describes the error.</param>
  internal DeepLException(string message) : base(message) {
  }

  /// <summary>
  ///   Initializes a new instance of the <see cref="AuthorizationException" /> class including the exception that
  ///   caused this one.
  /// </summary>
  /// <param name="message">The message that describes the error.</param>
  /// <param name="innerException">Exception that is the cause of this exception.</param>
  internal DeepLException(string message, Exception innerException) : base(message, innerException) {
  }
}
