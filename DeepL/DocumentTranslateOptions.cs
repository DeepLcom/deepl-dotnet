// Copyright 2022 DeepL SE (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

using DeepL.Model;

namespace DeepL {
  /// <summary>
  ///   Options to control document translation behaviour. These options may be provided to <see cref="Translator" />
  ///   document translate functions.
  /// </summary>
  public sealed class DocumentTranslateOptions {
    /// <summary>Initializes a new <see cref="DocumentTranslateOptions" /> object.</summary>
    public DocumentTranslateOptions() { }

    /// <summary>Initializes a new <see cref="DocumentTranslateOptions" /> object including the given glossary.</summary>
    /// <param name="glossary">Glossary to use in translation.</param>
    public DocumentTranslateOptions(GlossaryInfo glossary) : this() {
      GlossaryId = glossary.GlossaryId;
    }

    /// <summary>Controls whether translations should lean toward formal or informal language.</summary>
    /// This option is only applicable for target languages that support the formality option.
    /// <seealso cref="TargetLanguage.SupportsFormality" />
    public Formality Formality { get; set; } = Formality.Default;

    /// <summary>Specifies the ID of a glossary to use with the translation.</summary>
    public string? GlossaryId { get; set; }

    /// <summary> Controls whether to use Document Minification for translation, if available.</summary>
    public bool EnableDocumentMinification { get; set; }
  }
}
