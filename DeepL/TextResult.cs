// Copyright 2021 DeepL GmbH (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

using System.Text.Json.Serialization;

namespace DeepL {
  /// <summary>The result of a text translation.</summary>
  public sealed class TextResult {
    /// <summary>Initializes a new instance of <see cref="TextResult" />.</summary>
    /// <param name="text">Translated text.</param>
    /// <param name="detectedSourceLanguage">The detected language of the input text.</param>
    [JsonConstructor]
    public TextResult(string text, string detectedSourceLanguage) {
      Text = text;
      DetectedSourceLanguage = LanguageCode.Standardize(detectedSourceLanguage);
    }

    /// <summary>The translated text.</summary>
    public string Text { get; }

    /// <summary>The language code of the source text detected by DeepL.</summary>
    public string DetectedSourceLanguage { get; }

    /// <summary>Returns the translated text.</summary>
    /// <returns>The translated text.</returns>
    public override string ToString() => Text;
  }
}
