// Copyright 2022 DeepL SE (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

using System;

namespace DeepL.Model.Exceptions;

/// <summary>
///   Exception thrown when an error occurs related to a document translation. If the error occurs after the
///   document was successfully uploaded, the <see cref="DocumentHandle" /> for the associated document is included,
///   to allow later retrieval of the document.
/// </summary>
public sealed class DocumentTranslationException : DeepLException {
  /// <summary>Initializes a new instance of the <see cref="DocumentTranslationException" /> class.</summary>
  /// <param name="message">The message that describes the error.</param>
  /// <param name="innerException">The exception representing the connection error.</param>
  /// <param name="documentHandle">
  ///   Handle to the in-progress document translation, or null if an error occurred before
  ///   uploading the document.
  /// </param>
  internal DocumentTranslationException(string message, Exception innerException, DocumentHandle? documentHandle) :
        base(message, innerException) {
    DocumentHandle = documentHandle;
  }

  /// <summary>Handle to the in-progress document translation, or null if an error occurred before uploading the document.</summary>
  /// The handle can be used to later retrieve the document or to contact DeepL support.
  public DocumentHandle? DocumentHandle { get; }
}
