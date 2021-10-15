// Copyright 2021 DeepL GmbH (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

using System.Text.Json.Serialization;

namespace DeepL {
  /// <summary>Handle to an in-progress document translation.</summary>
  public readonly struct DocumentHandle {
    /// <summary>Initializes a new <see cref="DocumentHandle" /> object to resume an in-progress document translation.</summary>
    /// <param name="documentId">Document ID returned by document upload.</param>
    /// <param name="documentKey">Document Key returned by document upload.</param>
    [JsonConstructor]
    public DocumentHandle(string documentId, string documentKey) {
      (DocumentId, DocumentKey) = (documentId, documentKey);
    }

    /// <summary>ID of associated document request.</summary>
    public string DocumentId { get; }

    /// <summary>Key of associated document request.</summary>
    public string DocumentKey { get; }
  }
}
