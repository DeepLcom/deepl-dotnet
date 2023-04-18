﻿// Copyright 2022 DeepL SE (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

namespace DeepL.Model.Exceptions;

/// <summary>Exception thrown when too many requests are made to the DeepL API too quickly.</summary>
public sealed class TooManyRequestsException : DeepLException {
  /// <summary>Initializes a new instance of the <see cref="TooManyRequestsException" /> class.</summary>
  /// <param name="message">The message that describes the error.</param>
  internal TooManyRequestsException(string message) : base(message) {
  }
}
