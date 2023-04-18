// Copyright 2022 DeepL SE (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

using System.Text.Json.Serialization;

namespace DeepL.Model;

/// <summary>Handle to an in-progress document translation.</summary>
public readonly struct DocumentHandle {


  /// <summary>Initializes a new <see cref="DocumentHandle" /> object to resume an in-progress document translation.</summary>
  /// <param name="documentId">Document ID returned by document upload.</param>
  /// <param name="documentKey">Document Key returned by document upload.</param>
  /// <remarks>
  ///   The constructor for this class (and all other Model classes) should not be used by library users. Ideally it
  ///   would be marked <see langword="internal" />, but needs to be <see langword="public" /> for JSON deserialization.
  ///   In future this function may have backwards-incompatible changes.
  /// </remarks>
  [JsonConstructor]
  public DocumentHandle(string documentId, string documentKey) {
    (DocumentId, DocumentKey) = (documentId, documentKey);
  }

  /// <summary>ID of associated document request.</summary>
  public string DocumentId { get; }

  /// <summary>Key of associated document request.</summary>
  public string DocumentKey { get; }
}
