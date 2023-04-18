// Copyright 2022 DeepL SE (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

using System.Text.Json.Serialization;

namespace DeepL.Model;

/// <summary>Information about a language pair supported for glossaries.</summary>
public sealed class GlossaryLanguagePair {
  /// <summary>
  ///   Initializes a new <see cref="GlossaryLanguagePair" /> containing information about a language pair supported
  ///   for glossaries.
  /// </summary>
  /// <param name="sourceLanguageCode">Language code of the source terms in the glossary.</param>
  /// <param name="targetLanguageCode">Language code of the target terms in the glossary.</param>
  /// <remarks>
  ///   The constructor for this class (and all other Model classes) should not be used by library users. Ideally it
  ///   would be marked <see langword="internal" />, but needs to be <see langword="public" /> for JSON deserialization.
  ///   In future this function may have backwards-incompatible changes.
  /// </remarks>
  [JsonConstructor]
  public GlossaryLanguagePair(string sourceLanguageCode, string targetLanguageCode) {
    (SourceLanguageCode, TargetLanguageCode) = (sourceLanguageCode, targetLanguageCode);
  }

  /// <summary>Language code of the source terms in the glossary.</summary>
  [JsonPropertyName("source_lang")]
  public string SourceLanguageCode { get; }

  /// <summary>Language code of the target terms in the glossary.</summary>
  [JsonPropertyName("target_lang")]
  public string TargetLanguageCode { get; }
}
