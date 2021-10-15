// Copyright 2021 DeepL GmbH (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

using System.Text.Json.Serialization;

namespace DeepL {
  /// <summary>Information about a language pair supported for glossaries.</summary>
  public sealed class GlossaryLanguagePair {
    /// <summary>
    ///   Initializes a new <see cref="GlossaryLanguagePair" /> containing information about a language pair supported
    ///   for glossaries.
    /// </summary>
    /// <param name="sourceLanguage">Language code of the source terms in the glossary.</param>
    /// <param name="targetLanguage">Language code of the target terms in the glossary.</param>
    [JsonConstructor]
    public GlossaryLanguagePair(string sourceLanguage, string targetLanguage) {
      (SourceLanguage, TargetLanguage) = (sourceLanguage, targetLanguage);
    }

    /// <summary>Language code of the source terms in the glossary.</summary>
    [JsonPropertyName("source_lang")]
    public string SourceLanguage { get; }

    /// <summary>Language code of the target terms in the glossary.</summary>
    [JsonPropertyName("target_lang")]
    public string TargetLanguage { get; }
  }
}
