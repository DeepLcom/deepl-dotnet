// Copyright 2025 DeepL SE (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

using System.Text.Json.Serialization;

namespace DeepL.Model {
  /// <summary>Information about a glossary dictionary, excluding the entry list.</summary>
  public sealed class MultilingualGlossaryDictionaryInfo {
    /// <summary>
    ///   Initializes a new <see cref="MultilingualGlossaryDictionaryInfo" /> containing information about a glossary
    ///   dictionary.
    /// </summary>
    /// <param name="sourceLanguageCode">Language code of the source terms in the glossary dictionary.</param>
    /// <param name="targetLanguageCode">Language code of the target terms in the glossary dictionary.</param>
    /// <param name="entryCount">The number of source-target entry pairs in the glossary dictionary.</param>
    /// <remarks>
    ///   The constructor for this class (and all other Model classes) should not be used by library users. Ideally it
    ///   would be marked <see langword="internal" />, but needs to be <see langword="public" /> for JSON deserialization.
    ///   In future this function may have backwards-incompatible changes.
    /// </remarks>
    [JsonConstructor]
    public MultilingualGlossaryDictionaryInfo(
          string sourceLanguageCode,
          string targetLanguageCode,
          int entryCount) {
      (SourceLanguageCode, TargetLanguageCode, EntryCount) =
            (sourceLanguageCode, targetLanguageCode, entryCount);
    }

    /// <summary>Language code of the source terms in the glossary.</summary>
    [JsonPropertyName("source_lang")]
    public string SourceLanguageCode { get; }

    /// <summary>Language code of the target terms in the glossary.</summary>
    [JsonPropertyName("target_lang")]
    public string TargetLanguageCode { get; }

    /// <summary>The number of source-target entry pairs in the glossary dictionary.</summary>
    [JsonPropertyName("entry_count")]
    public int EntryCount { get; }

    /// <summary>
    ///   Creates a string containing the source and target language pair of the current
    ///   <see cref="MultilingualGlossaryDictionaryInfo" /> object.
    /// </summary>
    /// <returns>
    ///   A string containing the source and target language pair of the
    ///   <see cref="MultilingualGlossaryDictionaryInfo" /> object.
    /// </returns>
    /// <remarks>
    ///   This function is for diagnostic purposes only; the content of the returned string is exempt from backwards
    ///   compatibility.
    /// </remarks>
    public override string ToString() =>
          $"Glossary dictionary \"{SourceLanguageCode}\"->\"{TargetLanguageCode}\"";
  }
}
