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

To translate text, call `TranslateTextAsync()`. The first argument is a string containing the text to translate, or
`IEnumerable` of strings to translate multiple texts.

The second and third arguments are case-insensitive language codes for the source and target language respectively,
for example `"DE"`, `"FR"`.
The `LanguageCode` static class defines constants for the currently supported languages, for example
`LanguageCode.German`, `LanguageCode.French`.
To auto-detect the input text language, specify `null` as the source language.

Additional `TextTranslateOptions` can also be provided, see [Text translation options](#text-translation-options) below.

`TranslateTextAsync()` returns a `TextResult` or `TextResult` array corresponding to the input text(s).
The `TextResult` contains the translated text and detected source language code.

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

#### Text translation options

`TextTranslateOptions` has the following properties that impact text translation:

- `SentenceSplittingMode`: specifies how input text should be split into
  sentences, default: `'SentenceSplittingMode.All'`.
  - `SentenceSplittingMode.All`: input text will be split into sentences using
    both newlines and punctuation.
  - `SentenceSplittingMode.Off`: input text will not be split into sentences.
    Use this for applications where each input text contains only one
    sentence.
  - `SentenceSplittingMode.NoNewlines`: input text will be split into
    sentences using punctuation but not newlines.
- `PreserveFormatting`: controls automatic-formatting-correction. Set to
  `true` to prevent automatic-correction of formatting, default: `false`.
- `Formality`: controls whether translations should lean toward informal or
  formal language. This option is only available for some target languages, see
  [Listing available languages](#listing-available-languages).
  - `Formality.Less`: use informal language.
  - `Formality.More`: use formal, more polite language.
  - `Formality.Default`: standard level of formality.
  - `Formality.PreferLess`: less formality, if available for the specified target language, otherwise default.
  - `Formality.PreferMore`: more formality, if available for the specified target language, otherwise default.
- `GlossaryId`: specifies a glossary to use with translation, as a string
  containing the glossary ID.
- `Context`: specifies additional context to influence translations, that is not
  translated itself. Note this is an **alpha feature**: it may be deprecated at
  any time, or incur charges if it becomes generally available.
  See the [API documentation][api-docs-context-param] for more information and
  example usage.
- `TagHandling`: type of tags to parse before translation, options are
  `"html"` and `"xml"`.

The following options are only used if `TagHandling` is set to `'xml'`:

- `OutlineDetection`: set to `false` to disable automatic tag detection,
  default is `true`.
- `SplittingTags`: `List` of XML tags that should be used to split text into
  sentences. Tags may be specified individually (`['tag1', 'tag2']`),
  or a comma-separated list of strings (`'tag1,tag2'`). The default is an empty
  list.
- `NonSplittingTags`: `List` of XML tags that should not be used to split
  text into sentences. Format and default are the same as for splitting tags.
- `IgnoreTags`: `List` of XML tags that containing content that should not be
  translated. Format and default are the same as for splitting tags.

For a detailed explanation of the XML handling options, see the [API documentation][api-docs-xml-handling].

### Translating documents

To translate documents, call `TranslateDocumentAsync()` with the input and output files as either `FileInfo` or `Stream`
objects, and provide the source and target language as above.
Additional `DocumentTranslateOptions` are also available, see [Document translation options](#document-translation-options) below.
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

#### Document translation options

`DocumentTranslateOptions` has the following properties that impact text translation:

- `Formality`:  same as in [Text translation options](#text-translation-options).
- `GlossaryId`:  same as in [Text translation options](#text-translation-options).

### Glossaries

Glossaries allow you to customize your translations using defined terms.
Multiple glossaries can be stored with your account, each with a user-specified name and a uniquely-assigned ID.

#### Creating a glossary

You can create a glossary with your desired terms and name using
`CreateGlossaryAsync()`. Each glossary applies to a single source-target language
pair. Note: Glossaries are only supported for some language pairs, see
[Listing available glossary languages](#listing-available-glossary-languages)
for more information. The entries should be specified as a `Dictionary`.

If successful, the glossary is created and stored with your DeepL account, and
a `GlossaryInfo` object is returned including the ID, name, languages and entry
count.

```c#
// Create an English to German glossary with two terms:
var entriesDictionary = new Dictionary<string, string>{{"artist", "Maler"}, {"prize", "Gewinn"}};
var glossaryEnToDe = await translator.CreateGlossaryAsync(
    "My glossary", "EN", "DE",
    new GlossaryEntries(entriesDictionary));

Console.WriteLine($"Created {glossaryEnToDe.name}' ({glossaryEnToDe.GlossaryId}) " +
    $"{glossaryEnToDe.SourceLanguageCode}->{glossaryEnToDe.TargetLanguageCode} " +
    $"containing {glossaryEnToDe.EntryCount} entries"
)
// Example: Created 'My glossary' (559192ed-8e23-...) en->de containing 2 entries
 ```

You can also upload a glossary downloaded from the DeepL website using
`CreateGlossaryFromCsvAsync()`. Instead of supplying the entries as a dictionary,
specify the CSV data as a `Stream` containing file content:

```c#
var csvStream =  File.OpenRead("myGlossary.csv");
var csvGlossary = await translator.CreateGlossaryFromCsvAsync("My CSV glossary", "EN", "DE", csvStream);
```

The [API documentation][api-docs-csv-format] explains the expected CSV format in detail.

#### Getting, listing and deleting stored glossaries

Functions to get, list, and delete stored glossaries are also provided:

- `GetGlossaryAsync()` takes a glossary ID and returns a `GlossaryInfo` object for a
  stored glossary, or raises an exception if no such glossary is found.
- `ListGlossariesAsync()` returns a `List` of `GlossaryInfo` objects corresponding to
  all of your stored glossaries.
- `DeleteGlossaryAsync()` takes a glossary ID or `GlossaryInfo` object and deletes
  the stored glossary from the server, or raises an exception if no such glossary is found.

```c#
// Retrieve a stored glossary using the ID
var myGlossary = await translator.GetGlossaryAsync("559192ed-8e23-...");

// Find and delete glossaries named 'Old glossary'
var glossaries = await translator.ListGlossariesAsync();
foreach (var glossaryInfo in glossaries) {
  if (glossaryInfo.Name == "Old glossary")
    await translator.DeleteGlossaryAsync(glossaryInfo);
}
```

#### Listing entries in a stored glossary

The `GlossaryInfo` object does not contain the glossary entries, but instead
only the number of entries in the `EntryCount` property.

To list the entries contained within a stored glossary, use
`GetGlossaryEntriesAsync()` providing either the `GlossaryInfo` object or glossary ID:

```c#
var entries = translator.GetGlossaryEntriesAsync(myGlossary);

foreach (KeyValuePair<string, string> entry in entries.ToDictionary()) {
  Console.WriteLine($"{entry.Key}: {entry.Value}");
}
// prints:
//   artist: Maler
//   prize: Gewinn
```

#### Using a stored glossary

You can use a stored glossary for text (or document) translation by setting the
`TextTranslationOptions` (or `DocumentTranslationOptions`) `GlossaryId` property
to the glossary ID. You must also specify the `source_lang` argument (it is
required when using a glossary):

```c#
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

You can request the list of languages supported by DeepL for text and documents
using the `GetSourceLanguagesAsync()` and `GetTargetLanguagesAsync()` functions.
They both return a list of `Language` objects.

The `Name` property gives the name of the language in English, and the `Code`
property gives the language code. The `SupportsFormality` property only appears
for target languages, and indicates whether the target language supports the
optional `Formality` parameter.

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
```

#### Listing available glossary languages

Glossaries are supported for a subset of language pairs. To retrieve those
languages use the `GetGlossaryLanguagesAsync()` function, which returns an array
of `GlossaryLanguagePair` objects. Use the `SourceLanguage` and
`TargetLanguage` properties to check the pair of language codes supported.

```c#

// Glossary languages
var glossaryLanguages = await translator.GetGlossaryLanguagesAsync();
foreach (var languagePair in glossaryLanguages) {
  Console.WriteLine($"{languagePair.SourceLanguage} to {languagePair.TargetLanguage}");
  // Example: "EN to DE", "DE to EN", etc.
}
```

You can also find the list of supported glossary language pairs in the
[API documentation][api-docs-glossary-lang-list].

Note that glossaries work for all target regional-variants: a glossary for the
target language English (`"EN"`) supports translations to both American English
(`"EN-US"`) and British English (`"EN-GB"`).

### Exceptions

All library functions may raise `DeepLException` or one of its subclasses. If
invalid arguments are provided, they may raise the standard exceptions
`ArgumentException`.

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


[api-docs-context-param]: https://www.deepl.com/docs-api/translating-text/?utm_source=github&utm_medium=github-dotnet-readme

[api-docs-csv-format]: https://www.deepl.com/docs-api/managing-glossaries/supported-glossary-formats/?utm_source=github&utm_medium=github-dotnet-readme

[api-docs-glossary-lang-list]: https://www.deepl.com/docs-api/managing-glossaries/?utm_source=github&utm_medium=github-dotnet-readme

[api-docs-xml-handling]: https://www.deepl.com/docs-api/handling-xml/?utm_source=github&utm_medium=github-dotnet-readme

[issues]: https://www.github.com/DeepLcom/deepl-dotnet/issues
