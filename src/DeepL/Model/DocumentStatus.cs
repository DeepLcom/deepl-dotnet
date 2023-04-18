// Copyright 2022 DeepL SE (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace DeepL.Model {
  /// <summary>Status of an in-progress document translation.</summary>
  public sealed class DocumentStatus {
    /// <summary>Status code indicating status of the document translation.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum StatusCode {
      /// <summary>Document translation has not yet started, but will begin soon.</summary>
      [EnumMember(Value = "queued")] Queued,

      /// <summary>Document translation is in progress.</summary>
      [EnumMember(Value = "translating")] Translating,

      /// <summary>Document translation completed successfully, and the translated document may be downloaded.</summary>
      [EnumMember(Value = "done")] Done,

      /// <summary>An error occurred during document translation.</summary>
      [EnumMember(Value = "error")] Error
    }

    /// <summary>Initializes a new <see cref="DocumentStatus" /> object for an in-progress document translation.</summary>
    /// <param name="documentId">Document ID of the associated document.</param>
    /// <param name="status">Status of the document translation.</param>
    /// <param name="secondsRemaining">
    ///   Number of seconds remaining until translation is complete, only included while
    ///   document is in translating state.
    /// </param>
    /// <param name="billedCharacters">
    ///   Number of characters billed for the translation of this document, only included
    ///   after document translation is finished and the state is done.
    /// </param>
    /// <param name="errorMessage">Short description of the error, if available.</param>
    /// <remarks>
    ///   The constructor for this class (and all other Model classes) should not be used by library users. Ideally it
    ///   would be marked <see langword="internal" />, but needs to be <see langword="public" /> for JSON deserialization.
    ///   In future this function may have backwards-incompatible changes.
    /// </remarks>
    [JsonConstructor]
    public DocumentStatus(string documentId, StatusCode status, int? secondsRemaining, int? billedCharacters, string? errorMessage) {
      (DocumentId, Status, SecondsRemaining, BilledCharacters, ErrorMessage) =
            (documentId, status, secondsRemaining, billedCharacters, errorMessage);
    }

    /// <summary>Document ID of the associated document.</summary>
    public string DocumentId { get; }

    /// <summary>Status of the document translation.</summary>
    public StatusCode Status { get; }

    /// <summary>
    ///   Number of seconds remaining until translation is complete, only included while
    ///   document is in translating state.
    /// </summary>
    public int? SecondsRemaining { get; }

    /// <summary>
    ///   Number of characters billed for the translation of this document, only included
    ///   after document translation is finished and the state is done.
    /// </summary>
    public int? BilledCharacters { get; }

    /// <summary>Short description of the error, if available.</summary>
    public string? ErrorMessage { get; }

    /// <summary><c>true</c> if no error has occurred during document translation, otherwise <c>false</c>.</summary>
    public bool Ok => Status != StatusCode.Error;

    /// <summary><c>true</c> if document translation has completed successfully, otherwise <c>false</c>.</summary>
    public bool Done => Status == StatusCode.Done;
  }
}
