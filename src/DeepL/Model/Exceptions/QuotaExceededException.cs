// Copyright 2022 DeepL SE (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

namespace DeepL.Model.Exceptions;

/// <summary>Exception thrown when the DeepL translation quota has been reached.</summary>
public sealed class QuotaExceededException : DeepLException {
  /// <summary>Initializes a new instance of the <see cref="QuotaExceededException" /> class.</summary>
  /// <param name="message">The message that describes the error.</param>
  internal QuotaExceededException(string message) : base(message) {
  }
}
