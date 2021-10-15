// Copyright 2021 DeepL GmbH (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

using System;
using System.Text.Json.Serialization;

namespace DeepL {
  /// <summary>Information about a glossary, excluding the entry list.</summary>
  public sealed class GlossaryInfo {
    /// <summary>Initializes a new <see cref="GlossaryInfo" /> containing information about a glossary.</summary>
    /// <param name="glossaryId">ID of the associated glossary.</param>
    /// <param name="name">Name of the glossary chosen during creation.</param>
    /// <param name="ready"><c>true</c> if the glossary may be used for translations, otherwise <c>false</c>.</param>
    /// <param name="sourceLanguage">Language code of the source terms in the glossary.</param>
    /// <param name="targetLanguage">Language code of the target terms in the glossary.</param>
    /// <param name="creationTime"><see cref="DateTime" /> when the glossary was created.</param>
    /// <param name="entryCount">The number of source-target entry pairs in the glossary.</param>
    [JsonConstructor]
    public GlossaryInfo(
          string glossaryId,
          string name,
          bool ready,
          string sourceLanguage,
          string targetLanguage,
          DateTime creationTime,
          int entryCount) {
      (GlossaryId, Name, Ready, SourceLanguage, TargetLanguage, CreationTime, EntryCount) =
            (glossaryId, name, ready, sourceLanguage, targetLanguage, creationTime, entryCount);
    }

    /// <summary>ID of the associated glossary.</summary>
    public string GlossaryId { get; }

    /// <summary>Name of the glossary chosen during creation.</summary>
    public string Name { get; }

    /// <summary><c>true</c> if the glossary may be used for translations, otherwise <c>false</c>.</summary>
    public bool Ready { get; }

    /// <summary>Language code of the source terms in the glossary.</summary>
    [JsonPropertyName("source_lang")]
    public string SourceLanguage { get; }

    /// <summary>Language code of the target terms in the glossary.</summary>
    [JsonPropertyName("target_lang")]
    public string TargetLanguage { get; }

    /// <summary><see cref="DateTime" /> when the glossary was created.</summary>
    public DateTime CreationTime { get; }

    /// <summary>The number of source-target entry pairs in the glossary.</summary>
    public int EntryCount { get; }

    /// <summary>Creates a string containing the name and ID of the current <see cref="GlossaryInfo" /> object.</summary>
    /// <returns>A string containing the name and ID of the <see cref="GlossaryInfo" /> object.</returns>
    /// <remarks>
    ///   This function is for diagnostic purposes only; the content of the returned string is exempt from backwards
    ///   compatibility.
    /// </remarks>
    public override string ToString() => $"Glossary \"{Name}\" ({GlossaryId})";
  }
}
