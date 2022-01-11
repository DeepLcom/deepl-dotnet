// Copyright 2022 DeepL SE (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

namespace DeepL {
  /// <summary>
  ///   Language codes for the languages currently supported by DeepL translation. New languages may be added in
  ///   future; to retrieve the currently supported languages use <see cref="Translator.GetSourceLanguagesAsync" /> and
  ///   <see cref="Translator.GetTargetLanguagesAsync" />.
  /// </summary>
  public static class LanguageCode {
    /// <summary>Bulgarian language code, may be used as source or target language.</summary>
    public const string Bulgarian = "bg";

    /// <summary>Czech language code, may be used as source or target language.</summary>
    public const string Czech = "cs";

    /// <summary>Danish language code, may be used as source or target language.</summary>
    public const string Danish = "da";

    /// <summary>German language code, may be used as source or target language.</summary>
    public const string German = "de";

    /// <summary>Greek language code, may be used as source or target language.</summary>
    public const string Greek = "el";

    /// <summary>English language code, may only be used as a source language.</summary>
    public const string English = "en";

    /// <summary>British English language code, may only be used as a target language.</summary>
    public const string EnglishBritish = "en-GB";

    /// <summary>American English language code, may only be used as a target language.</summary>
    public const string EnglishAmerican = "en-US";

    /// <summary>Spanish language code, may be used as source or target language.</summary>
    public const string Spanish = "es";

    /// <summary>Estonian language code, may be used as source or target language.</summary>
    public const string Estonian = "et";

    /// <summary>Finnish language code, may be used as source or target language.</summary>
    public const string Finnish = "fi";

    /// <summary>French language code, may be used as source or target language.</summary>
    public const string French = "fr";

    /// <summary>Hungarian language code, may be used as source or target language.</summary>
    public const string Hungarian = "hu";

    /// <summary>Italian language code, may be used as source or target language.</summary>
    public const string Italian = "it";

    /// <summary>Japanese language code, may be used as source or target language.</summary>
    public const string Japanese = "ja";

    /// <summary>Lithuanian language code, may be used as source or target language.</summary>
    public const string Lithuanian = "lt";

    /// <summary>Latvian language code, may be used as source or target language.</summary>
    public const string Latvian = "lv";

    /// <summary>Dutch language code, may be used as source or target language.</summary>
    public const string Dutch = "nl";

    /// <summary>Polish language code, may be used as source or target language.</summary>
    public const string Polish = "pl";

    /// <summary>Portuguese language code, may only be used as a source language.</summary>
    public const string Portuguese = "pt";

    /// <summary>Brazilian Portuguese language code, may only be used as a target language.</summary>
    public const string PortugueseBrazilian = "pt-BR";

    /// <summary>European Portuguese language code, may only be used as a target language.</summary>
    public const string PortugueseEuropean = "pt-PT";

    /// <summary>Romanian language code, may be used as source or target language.</summary>
    public const string Romanian = "ro";

    /// <summary>Russian language code, may be used as source or target language.</summary>
    public const string Russian = "ru";

    /// <summary>Slovak language code, may be used as source or target language.</summary>
    public const string Slovak = "sk";

    /// <summary>Slovenian language code, may be used as source or target language.</summary>
    public const string Slovenian = "sl";

    /// <summary>Swedish language code, may be used as source or target language.</summary>
    public const string Swedish = "sv";

    /// <summary>Chinese language code, may be used as source or target language.</summary>
    public const string Chinese = "zh";

    /// <summary>Removes the regional variant (if any) from the given language code</summary>
    /// <param name="code">Language code possibly containing a regional variant.</param>
    /// <returns>The language code without a regional variant.</returns>
    public static string RemoveRegionalVariant(string code) => code.Split(new[] { '-' }, 2)[0].ToLowerInvariant();

    /// <summary>
    ///   Changes the upper- and lower-casing of the given language code to match ISO 639-1 with an optional regional
    ///   code from ISO 3166-1.
    /// </summary>
    /// <param name="code">String containing language code to standardize.</param>
    /// <returns>String containing the standardized language code.</returns>
    internal static string Standardize(string code) {
      var parts = code.Split(new[] { '-' }, 2);
      return parts.Length == 1
            ? parts[0].ToLowerInvariant()
            : $"{parts[0].ToLowerInvariant()}-{parts[1].ToUpperInvariant()}";
    }
  }
}
