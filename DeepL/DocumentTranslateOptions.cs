// Copyright 2021 DeepL GmbH (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

namespace DeepL {
  /// <summary>
  ///   Options to control document translation behaviour. These options may be provided to <see cref="Translator" />
  ///   document translate functions.
  /// </summary>
  public sealed class DocumentTranslateOptions {
    /// <summary>Controls whether translations should lean toward formal or informal language.</summary>
    /// This option is only applicable for target languages that support the formality option.
    /// <seealso cref="TargetLanguage.SupportsFormality" />
    public Formality Formality { get; set; } = Formality.Default;
  }
}
