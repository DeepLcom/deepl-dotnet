// Copyright 2022 DeepL SE (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

using DeepL.Model;

namespace DeepL {
  /// <summary>
  ///   Options to control text translation behaviour. These options may be provided to <see cref="Translator" /> text
  ///   translate functions.
  /// </summary>
  public sealed class TextRephraseOptions {
    /// <summary>Initializes a new <see cref="TextRephraseOptions" /> object.</summary>
    public TextRephraseOptions() { }

    /// <summary>Controls the style the rephrasing should be in.</summary>
    /// This option is only applicable for target languages that support styles, and only a tone
    /// OR a style can be chosen in the same request.
    public string? WritingStyle { get; set; } = null;

    /// <summary>Controls the tone the rephrasing should be in.</summary>
    /// This option is only applicable for target languages that support tones, and only a tone
    /// OR a style can be chosen in the same request.
    public string? WritingTone { get; set; } = null;
  }
}
