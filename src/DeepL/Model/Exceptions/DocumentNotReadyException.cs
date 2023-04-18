// Copyright 2022 DeepL SE (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

namespace DeepL.Model.Exceptions;

/// <summary>Exception thrown when attempting to download a translated document before it is ready.</summary>
public sealed class DocumentNotReadyException : DeepLException {
  /// <summary>Initializes a new instance of the <see cref="DocumentNotReadyException" /> class.</summary>
  /// <param name="message">The message that describes the error.</param>
  internal DocumentNotReadyException(string message) : base(message) { }
}
