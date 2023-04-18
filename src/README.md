# DeepL .NET Library

[![NuGet](https://img.shields.io/nuget/v/deepl.net.svg)](https://www.nuget.org/packages/DeepL.net/)
[![License: MIT](https://img.shields.io/badge/license-MIT-blueviolet.svg)](https://github.com/DeepLcom/deepl-dotnet/blob/main/LICENSE)
[![.NET](https://github.com/DeepLcom/deepl-dotnet/actions/workflows/build_default.yml/badge.svg)](https://github.com/DeepLcom/deepl-dotnet/actions/workflows/build_default.yml)

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
With a DeepL API Free account you can translate up to 500,000 characters/month for free.

## Installation

Using the .NET Core command-line interface (CLI) tools:

```
dotnet add package DeepL.net
```

Using the NuGet Command Line Interface (CLI):

```
nuget install DeepL.net
```

## Usage

All entities in the DeepL .NET library are in the `DeepL` namespace:

```c#
using DeepL;
```

Create a `Translator` object providing your DeepL API authentication key.

Be careful not to expose your key, for example when sharing source code.

```c#
var authKey = "f63c02c5-f056-..."; // Replace with your key
var translator = new Translator(authKey);
```

This example is for demonstration purposes only. In production code, the authentication key should not be hard-coded,
but instead fetched from a configuration file or environment variable.

`Translator` accepts options as the second argument, see [Configuration](#configuration) for more information.

### Translating text

To translate text, call `TranslateTextAsync()` with the text and the source and target language codes.

The source and target language arguments accept strings containing the language codes, for example `"DE"`, `"FR"`.
The `LanguageCode` static class defines constants for the currently supported languages, for example
`LanguageCode.German`, `LanguageCode.French`. To auto-detect the input text language, specify `null` as the source
language.

The returned `TextResult` contains the translated text and detected source language code.
Additional `TextTranslateOptions` can also be provided.

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
Console.WriteLine(translations[0].DetectedSourceLanguageCode); // "JA"
Console.WriteLine(translations[1].Text); // "How are you?"
Console.WriteLine(translations[1].DetectedSourceLanguageCode); // "ES"

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

To translate documents, call `TranslateDocumentAsync()` with the input and output files as either `FileInfo` or `Stream`
objects, and provide the source and target language as above. Additional `DocumentTranslateOptions` are also available.
Note that file paths are not accepted as strings, to avoid mixing up the file and language arguments.

```c#
// Translate a formal document from English to German
try {
  await translator.TranslateDocumentAsync(
        new FileInfo("Instruction Manual.docx"),
        new FileInfo("Bedienungsanleitung.docx"),
        "EN",
        "DE",
        new DocumentTranslateOptions { Formality = Formality.More });
} catch (DocumentTranslationException exception) {
  // If the error occurs *after* upload, the DocumentHandle will contain the document ID and key
  if (exception.DocumentHandle != null) {
    var handle = exception.DocumentHandle.Value;
    Console.WriteLine($"Document ID: {handle.DocumentId}, Document key: {handle.DocumentKey}");
  } else {
    Console.WriteLine($"Error occurred during document upload: {exception.Message}");
  }
}
```

`TranslateDocumentAsync()` manages the upload, wait until translation is complete, and download steps. If your
application needs to execute these steps individually, you can instead use the following functions directly:

- `TranslateDocumentUploadAsync()`,
- `TranslateDocumentStatusAsync()` (or `TranslateDocumentWaitUntilDoneAsync()`), and
- `TranslateDocumentDownloadAsync()`

### Glossaries

Glossaries allow you to customize your translations using defined terms. Create a glossary containing the desired terms
and then use it in translations. Multiple glossaries can be stored with your account.

```c#
// Create an English to German glossary with two terms:
var entriesDictionary = new Dictionary<string, string>{{"artist", "Maler"}, {"prize", "Gewinn"}};
var glossaryEnToDe = await translator.CreateGlossaryAsync(
    "My glossary", "EN", "DE",
    new GlossaryEntries(entriesDictionary));

// Functions to get, list, and delete glossaries from DeepL servers are also provided
var glossaries = await translator.ListGlossariesAsync();
Console.WriteLine($"{glossaries.Length} glossaries found.");

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
if (usage.AnyLimitReached) {
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

### Writing a Plugin

If you use this library in an application, please identify the application with
`TranslatorOptions.appInfo`, which needs the name and version of the app:

```c#
var options = new TranslatorOptions {
  appInfo =  new AppInfo { AppName = "my-dotnet-test-app", AppVersion = "1.2.3"}
};
var translator = new Translator(AuthKey, options);
```

This information is passed along when the library makes calls to the DeepL API.
Both name and version are required. Please note that setting the `User-Agent` header
via `TranslatorOptions.Headers` will override this setting, if you need to use this,
please manually identify your Application in the `User-Agent` header.

### Configuration

The `Translator` constructor accepts `TranslatorOptions` as a second argument,
for example:

```c#
var options = new TranslatorOptions {
      MaximumNetworkRetries = 5,
      PerRetryConnectionTimeout = TimeSpan.FromSeconds(10),
};
var translator = new Translator(authKey, options);
```

See the `TranslatorOptions` class for details about the available options.

#### Proxy configuration

To use the library with a proxy, override the `ClientFactory` with a function returning a custom `HttpClient`:

```c#
var proxyUrl = "http://localhost:3001";
var handler = new System.Net.Http.HttpClientHandler {
      Proxy = new System.Net.WebProxy(proxyUrl), UseProxy = true,
};
var options = new TranslatorOptions {
      ClientFactory = () => new HttpClientAndDisposeFlag {
            HttpClient = new HttpClient(handler), DisposeClient = true,
      }
};
var translator = new Translator(authKey, options);
```

#### Anonymous platform information

By default, we send some basic information about the platform the client library is running
on with each request, see [here for an explanation](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/User-Agent).
This data is completely anonymous and only used to improve our product, not track any
individual users. If you do not wish to send this data, you can opt-out when creating
your `Translator` object by setting the `sendPlatformInfo` flag in the
`TranslatorOptions` to `false` like so:

```c#
var options = new TranslatorOptions { sendPlatformInfo = false };
var translator = new Translator(authKey, options);
```

## Issues

If you experience problems using the library, or would like to request a new feature, please open an
[issue][issues].

## Development

We welcome Pull Requests, please read the [contributing guidelines](CONTRIBUTING.md).

### Tests

Execute the tests using `dotnet test`. The tests communicate with the DeepL API using the auth key defined by the
`DEEPL_AUTH_KEY` environment variable.

Be aware that the tests make DeepL API requests that contribute toward your API usage.

The test suite may instead be configured to communicate with the mock-server provided by
[deepl-mock](https://www.github.com/DeepLcom/deepl-mock). Although most test cases work for either, some test cases work
only with the DeepL API or the mock-server and will be otherwise skipped. The test cases that require the mock-server
trigger server errors and test the client error-handling. To execute the tests using deepl-mock, run it in another
terminal while executing the tests. Execute the tests using `dotnet test` with the `DEEPL_MOCK_SERVER_PORT` and
`DEEPL_SERVER_URL` environment variables defined referring to the mock-server.

[issues]: https://www.github.com/DeepLcom/deepl-dotnet/issues
