# DeepL .NET Library
[![NuGet](https://img.shields.io/nuget/v/deepl.net.svg)](https://www.nuget.org/packages/DeepL.net/)
[![License: MIT](https://img.shields.io/badge/license-MIT-blueviolet.svg)](https://github.com/DeepLcom/deepl-dotnet/blob/main/LICENSE)

The [DeepL API](https://www.deepl.com/docs-api?utm_source=github&utm_medium=github-dotnet-readme) is a language
translation API that allows other computer programs to send texts and documents to DeepL's servers and receive
high-quality translations. This opens a whole universe of opportunities for developers: any translation product you can
imagine can now be built on top of DeepL's best-in-class translation technology.

The DeepL .NET library offers a convenient way for applications written in .NET to interact with the DeepL API. We
intend to support all API functions with the library, though support for new features may be added to the library after
they’re added to the API.

## Getting an authentication key

To use the DeepL .NET Library, you'll need an API authentication key. To get a key,
[please create an account here](https://www.deepl.com/pro?utm_source=github&utm_medium=github-dotnet-readme#developer).
You can translate up to 500,000 characters/month for free.

## Installation
Using the .NET Core command-line interface (CLI) tools:
```
dotnet add package DeepL.net
```

Using the NuGet Command Line Interface (CLI):
```
nuget install DeepL.net
```

### Requirements
The library used for JSON deserialization is built-in as part of the shared framework for .NET Core 3.0 and later versions.
Older versions may need to install the [System.Text.Json](https://www.nuget.org/packages/System.Text.Json) NuGet package.

[Polly](https://github.com/App-vNext/Polly) v5.0.1 is used for retrying failed HTTP requests.

## Usage
All entities in the DeepL .NET library are in the `DeepL` namespace:
```c#
using DeepL;
```

Create a `Translator` object providing your DeepL API authentication key.

To avoid writing your key in source code, you can set it in an environment
variable `DEEPL_AUTH_KEY`, then read the variable in your C# code:
```c#
var authKey = Environment.GetEnvironmentVariable("DEEPL_AUTH_KEY");
var translator = new Translator(authKey);
```

### Translating text
To translate text, call `TranslateTextAsync()` with the text and the source and
target language codes.

The source and target language arguments accept strings containing the language
codes, for example `"DE"`, `"FR"`. The `LanguageCode` static class defines
constants for the currently supported languages, for example
`LanguageCode.German`, `LanguageCode.French`.
To auto-detect the input text language, specify `null` as the source language.

The returned `TextResult` contains the translated text and detected source
language code. Additional `TextTranslateOptions` can also be provided.
```c#
// Translate text into a target language, in this case, French:
var translatedText = await translator.TranslateTextAsync(
      "Hello, world!",
      LanguageCode.English,
      LanguageCode.French);
Console.WriteLine(translatedText); // "Bonjour, le monde !"
// Note: printing or converting the result to a string uses the output text.

// Translate multiple texts into British English:
var translations = await translator.TranslateTextAsync(
      new[] { "お元気ですか？", "¿Cómo estás?" }, null, "EN-GB");
Console.WriteLine(translations[0].Text); // "How are you?"
Console.WriteLine(translations[0].DetectedSourceLanguage); // "JA"
Console.WriteLine(translations[1].Text); // "How are you?"
Console.WriteLine(translations[1].DetectedSourceLanguage); // "ES"

// Translate into German with less and more Formality:
foreach (var formality in new[] { Formality.Less, Formality.More }) {
  Console.WriteLine(
        await translator.TranslateTextAsync(
              "How are you?",
              null,
              LanguageCode.German,
              new TextTranslateOptions { Formality = formality }));
}
// Will print: "Wie geht es dir?" "Wie geht es Ihnen?"
```

### Translating documents
To translate documents, specify the input and output files as `FileInfo`
objects, or `Stream` objects, and provide the source and target language as
above. Additional `DocumentTranslateOptions` are also available. Note that file
paths are not accepted as strings, to avoid interchanging the file and language
arguments.
```c#
// Translate a formal document from English to German
await translator.TranslateDocumentAsync(
      new FileInfo("Instruction Manual.docx"),
      new FileInfo("Bedienungsanleitung.docx"),
      "EN",
      "DE",
      new DocumentTranslateOptions { Formality = Formality.More });
```

### Glossaries
Glossaries allow you to customize your translations using defined terms.
Create a glossary containing the desired terms and then use it in translations.
Multiple glossaries can be stored with your account.
```c#
// Create an English to German glossary with two terms:
var glossaryEnToDe = await translator.CreateGlossaryAsync(
    "My glossary", "EN", "DE",
    new Dictionary<string, string>{{"artist", "Maler"}, {"prize", "Gewinn"}});

var glossaries = await translator.ListGlossariesAsync();

var resultWithGlossary = await translator.TranslateTextAsync(
    "The artist was awarded a prize.",
    "EN",
    "DE",
    new TextTranslateOptions { GlossaryId = glossaryEnToDe.GlossaryId });
// resultWithGlossary.Text == "Der Maler wurde mit einem Gewinn ausgezeichnet."
// Without using a glossary: "Der Künstler wurde mit einem Preis ausgezeichnet."
```

### Check account usage
```c#
var usage = await translator.GetUsageAsync();
if (usage.AnyLimitExceeded) {
  Console.WriteLine("Translation limit exceeded.");
} else if (usage.Character != null) {
  Console.WriteLine($"Character usage: {usage.Character}");
} else {
  Console.WriteLine($"{usage}");
}
```

### Listing available languages
```c#
// Source and target languages
var sourceLanguages = await translator.GetSourceLanguagesAsync();
foreach (var lang in sourceLanguages) {
  Console.WriteLine($"{lang.Name} ({lang.Code})"); // Example: "English (EN)"
}
var targetLanguages = await translator.GetTargetLanguagesAsync();
foreach (var lang in targetLanguages) {
  if (lang.SupportsFormality ?? false) {
    Console.WriteLine($"{lang.Name} ({lang.Code}) supports formality");
     // Example: "German (DE) supports formality"
  }
}

// Glossary languages
var glossaryLanguages = await translator.GetGlossaryLanguagesAsync();
foreach (var languagePair in glossaryLanguages) {
  Console.WriteLine($"{languagePair.SourceLanguage} to {languagePair.TargetLanguage}");
  // Example: "EN to DE", "DE to EN"
}
```

### Exceptions
All library exceptions are derived from `DeepL.DeepLException`.

## Development
The test suite depends on [deepl-mock](https://www.github.com/DeepLcom/deepl-mock). Run it in another terminal
while executing the tests, using port 3000. Set the mock-server listening port using the environment variable
`DEEPL_MOCK_SERVER_PORT`.

Execute the tests using `dotnet test`.

### Issues
If you experience problems using the library, or would like to request a new feature, please create an
[issue](https://www.github.com/DeepLcom/deepl-dotnet/issues).
