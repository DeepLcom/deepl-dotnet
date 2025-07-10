# Migration Documentation for Newest Glossary Functionality

## 1. Overview of Changes

The newest version of the Glossary APIs is the `/v3` endpoints, which introduce enhanced functionality:

- **Support for Multilingual Glossaries**: The v3 endpoints allow for the creation of glossaries with multiple language
  pairs, enhancing flexibility and usability.
- **Editing Capabilities**: Users can now edit existing glossaries.

To support these new v3 APIs, we have created new methods to interact with these new multilingual glossaries. Users are
encouraged to transition to the new to take full advantage of these new features. The `v2` methods for monolingual
glossaries (e.g., `CreateGlossaryAsync()`, `GetGlossaryAsync()`, etc.) remain available, however users are encouraged
to update to use the new functions.

## 2. Endpoint Changes

| Monolingual glossary methods   | Multilingual glossary methods                | Changes Summary                                                                                                                                                                      |
|--------------------------------|----------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `CreateGlossaryAsync()`        | `CreateMultilingualGlossaryAsync()`          | Accepts a list of `MultilingualGlossaryDictionaryEntries` for multi-lingual support and now returns a `MultilingualGlossaryInfo` object.                                             |
| `CreateGlossaryFromCsvAsync()` | `CreateMultilingualGlossaryFromCsvAsync()`   | Similar functionality, but now returns a `MultilingualGlossaryInfo` object                                                                                                           |
| `GetGlossaryAsync()`           | `GetMultilingualGlossaryAsync()`             | Similar functionality, but now returns `MultilingualGlossaryInfo`. Also can accept a `MultilingualGlossaryInfo` object as the glossary parameter instead of a `GlossaryInfo` object. |
| `ListGlossariesAsync()`        | `ListMultilingualGlossariesAsync()`          | Similar functionality, but now returns a list of `MultilingualGlossaryInfo` objects.                                                                                                 |
| `GetGlossaryEntriesAsync()`    | `GetMultilingualGlossaryDictionaryEntries()` | Requires specifying source and target languages. Also returns a `MultilingualGlossaryDictionaryEntriesResponse` object as the response.                                              |
| `DeleteGlossaryAsync()`        | `DeleteMultilingualGlossaryAsync()`          | Similar functionality, but now can accept a `MultilingualGlossaryInfo` object instead of a `GlossaryInfo` object when specifying the glossary.                                       |

## 3. Model Changes

V2 glossaries are monolingual and the previous glossary objects could only have entries for one language pair (
`SourceLanguageCode` and `TargetLanguageCode`). Now we introduce the concept of "glossary dictionaries", where a
glossary dictionary specifies its own `SourceLanguageCode`, `TargetLanguageCode`, and has its own entries.

- **Glossary Information**:
  - **v2**: `GlossaryInfo` supports only mono-lingual glossaries, containing fields such as `SourceLanguageCode`,
    `TargetLanguageCode`, and `EntryCount`.
  - **v3**: `MultilingualGlossaryInfo` supports multi-lingual glossaries and includes a list of
    `MultilingualGlossaryDictionaryInfo`, which provides details about each glossary dictionary, each with its own
    `SourceLanguageCode`, `TargetLanguageCode`, and `EntryCount`.

- **Glossary Entries**:
  - **v3**: Introduces `MultilingualGlossaryDictionaryEntries`, which encapsulates a glossary dictionary with source
    and target languages along with its entries.

## 4. Code Examples

### Create a glossary

```c#
// monolingual glossary example
var entries = new Dictionary<string, string>() {{"hello", "hallo"}};
var glossaryInfo = await client.CreateGlossaryAsync("My Glossary", "EN", "DE", new GlossaryEntries(entries));

// multilingual glossary example
var entries = new Dictionary<string, string>() {{"hello", "hallo"}};
var glossaryDicts = new[] {new MultilingualGlossaryDictionaryEntries("EN", "DE", new GlossaryEntries(entries))};
var glossaryInfo = await client.CreateMultilingualGlossaryAsync("My Glossary", glossaryDicts);
```

### Get a glossary

```c#
// monolingual glossary example
var entries = new Dictionary<string, string>() {{"hello", "hallo"}};
var createdGlossary = await client.CreateGlossaryAsync("My Glossary", "EN", "DE", new GlossaryEntries(entries));
var glossaryInfo = await client.GetGlossaryAsync(createdGlossary) // GlossaryInfo object

// multilingual glossary example
var entries = new Dictionary<string, string>() {{"hello", "hallo"}};
var glossaryDicts = new[] {new MultilingualGlossaryDictionaryEntries("EN", "DE", new GlossaryEntries(entries))};
var createdGlossary = await client.CreateMultilingualGlossaryAsync("My Glossary", glossaryDicts);
var glossaryInfo = await client.GetMultilingualGlossaryAsync(createdGlossary); // MultilingualGlossaryInfo object
```

### Get glossary entries

```c#
// monolingual glossary example
var entries = new Dictionary<string, string>() {{"hello", "hallo"}};
var createdGlossary = await client.CreateGlossaryAsync("My Glossary", "EN", "DE", new GlossaryEntries(entries));
var entries = await client.GetGlossaryEntriesAsync(createdGlossary);
Console.WriteLine(entries.ToTsv()); // 'hello\thallo'

// multilingual glossary example
var entries = new Dictionary<string, string>() {{"hello", "hallo"}};
var glossaryDicts = new[] {new MultilingualGlossaryDictionaryEntries("EN", "DE", new GlossaryEntries(entries))};
var createdGlossary = await client.CreateMultilingualGlossaryAsync("My Glossary", glossaryDicts);
var dictEntries = await client.GetMultilingualGlossaryDictionaryEntriesAsync(createdGlossary, "EN", "DE");
Console.WriteLine(dictEntries.Dictionaries[0].Entries.ToTsv()); // 'hello\thallo'
```

### List and delete glossaries

```c#
// monolingual glossary example
var glossaries = await client.ListGlossariesAsync();
foreach (var glossary in glossaries) {
    if (glossary.Name == "Old glossary") {
        await client.DeleteGlossaryAsync(glossary);
    }
}

// multilingual glossary example
var glossaries = await client.ListMultilingualGlossariesAsync();
foreach (var glossary in glossaries) {
    if (glossary.Name == "Old glossary") {
        await client.DeleteMultilingualGlossaryAsync(glossary);
    }
}
```

## 5. New Multilingual Glossary Methods

In addition to introducing multilingual glossaries, we introduce several new methods that enhance the functionality for
managing glossaries. Below are the details for each new method:

### Update Multilingual Glossary Dictionary

- **Method Overloads**:
  - `Task<MultilingualGlossaryInfo> UpdateMultilingualGlossaryDictionaryAsync(string glossaryId, string
  sourceLanguageCode, string targetLanguageCode, GlossaryEntries entries, CancellationToken cancellationToken = default)`
  - `Task<MultilingualGlossaryInfo> UpdateMultilingualGlossaryDictionaryAsync(MultilingualGlossaryInfo glossary, string
  sourceLanguageCode, string targetLanguageCode, GlossaryEntries entries, CancellationToken cancellationToken = default)`
  - `Task<MultilingualGlossaryInfo> UpdateMultilingualGlossaryDictionaryAsync(string glossaryId,
  MultilingualGlossaryDictionaryEntries glossaryDict, CancellationToken cancellationToken = default)`
  - `Task<MultilingualGlossaryInfo> UpdateMultilingualGlossaryDictionaryAsync(MultilingualGlossaryInfo glossary,
  MultilingualGlossaryDictionaryEntries glossaryDict, CancellationToken cancellationToken = default)`
- **Description**: Updates a glossary or glossary dictionary with new entries or names.
- **Parameters:**
  - `string glossaryId`: ID of the glossary to update.
  - `string sourceLanguageCode`: Source language code for the glossary dictionary.
  - `string targetLanguageCode`: Target language code for the glossary dictionary.
  - `GlossaryEntries entries`: The source-target entry pairs.
  - `MultilingualGlossaryDictionaryEntries glossaryDict`: The glossary dictionary to update.
  - `CancellationToken cancellationToken`: An optional parameter for the cancellation token.
- **Returns**: `Task<MultilingualGlossaryInfo>` containing details about the updated glossary.
- **Exceptions**:
  - `ArgumentException`: Thrown if any argument is invalid.
  - `DeepLException`: Thrown if any error occurs while communicating with the DeepL API.
- **Example**:

```c#
var entries = new Dictionary<string,string>{{"artist", "Maler"}, {"hello", "guten tag"}};
var dictionaries = new[] {new MultilingualGlossaryDictionaryEntries("EN", "DE", new GlossaryEntries(entries))};
var myGlossary = await client.CreateMultilingualGlossaryAsync(
        "My glossary",
        dictionaries
);
var newEntries = new Dictionary<string,string>{{"hello", "hallo"}, {"prize": "Gewinn"}};
var glossaryDict = new MultilingualGlossaryDictionaryEntries("EN", "DE", new GlossaryEntries(newEntries));
var updatedGlossary = await client.UpdateMultilingualGlossaryDictionaryAsync(
        myGlossary,
        glossaryDict
);

var entriesResponse = await client.GetMultilingualGlossaryDictionaryEntriesAsync(myGlossary, "EN", "DE");
foreach (KeyValuePair<string, string> entry in entriesResponse.Dictionaries[0].Entries.ToDictionary()) {
  Console.WriteLine($"{entry.Key}: {entry.Value}");
}
// prints:
//   artist: Maler
//   hello: hallo
//   prize: Gewinn
```

### Update Multilingual Glossary Dictionary from CSV

- **Method**:
  - `Task<MultilingualGlossaryInfo> UpdateMultilingualGlossaryDictionaryFromCsvAsync(string glossaryId, string
  sourceLanguageCode, string targetLanguageCode, Stream csvFile, CancellationToken cancellationToken = default)`
  - `Task<MultilingualGlossaryInfo> UpdateMultilingualGlossaryDictionaryFromCsvAsync(MultilingualGlossaryInfo glossary,
  string sourceLanguageCode, string targetLanguageCode, Stream csvFile, CancellationToken cancellationToken = default)`
- **Description**: This method allows you to update or create a glossary dictionary using entries in CSV format.
- **Parameters**:
  - `string glossaryId`: The ID of the glossary to update.
  - `MultilingualGlossaryInfo glossary`: The `MultilingualGlossaryInfo` object representing the glossary to update
  - `string sourceLanguageCode`: Language of source entries.
  - `string targetLanguageCode`: Language of target entries.
  - `Stream csvFile`: The CSV data containing glossary entries as a stream.
  - `CancellationToken cancellationToken`: An optional parameter for the cancellation token.
- **Returns**: `Task<MultilingualGlossaryInfo>` containing information about the updated glossary.
- **Exceptions**:
  - `ArgumentException`: Thrown if any argument is invalid.
  - `DeepLException`: Thrown if any error occurs while communicating with the DeepL API.
- **Example**:
  ```c#
  var csvStream =  File.OpenRead("myGlossary.csv");
  var myCsvGlossary = await client.UpdateMultilingualGlossaryDictionaryFromCsvAsync(
    glossary="4c81ffb4-2e...",
    sourceLanguageCode="EN",
    targetLanguageCode="DE",
    csvFile=csvStream
  );
  ```

### Update Multilingual Glossary Name

- **Method**:
  `Task<MultilingualGlossaryInfo> UpdateMultilingualGlossaryNameAsync(string glossaryId, string name, CancellationToken cancellationToken = default)`
- **Description**: This method allows you to update the name of an existing glossary.
- **Parameters**:
  - `glossary`: The ID of the glossary to update.
  - `name`: The new name for the glossary.
  - `CancellationToken cancellationToken`: An optional parameter for the cancellation token.
- **Returns**: `Task<MultilingualGlossaryInfo>` containing information about the updated glossary.
- **Exceptions**:
  - `ArgumentException`: Thrown if any argument is invalid.
  - `DeepLException`: Thrown if any error occurs while communicating with the DeepL API.
- **Example**:
  ```c#
  var updatedGlossary = await client.UpdateMultilingualGlossaryNameAsync("4c81ffb4-2e...", "New Glossary Name");
  ```

### Replace a Multilingual Glossary Dictionary

- **Method**:
  - `Task<MultilingualGlossaryDictionaryInfo> ReplaceMultilingualGlossaryDictionaryAsync(string glossaryId, string
  sourceLanguageCode, string targetLanguageCode, GlossaryEntries entries, CancellationToken cancellationToken = default)`
  - `Task<MultilingualGlossaryDictionaryInfo> ReplaceMultilingualGlossaryDictionaryAsync(MultilingualGlossaryInfo
  glossary, string sourceLanguageCode, string targetLanguageCode, GlossaryEntries entries, CancellationToken cancellationToken = default)`
  - `Task<MultilingualGlossaryDictionaryInfo> ReplaceMultilingualGlossaryDictionaryAsync(string glossaryId,
  MultilingualGlossaryDictionaryEntries glossaryDict, CancellationToken cancellationToken = default)`
  - `Task<MultilingualGlossaryDictionaryInfo> ReplaceMultilingualGlossaryDictionaryAsync(MultilingualGlossaryInfo
  glossary, MultilingualGlossaryDictionaryEntries glossaryDict, CancellationToken cancellationToken = default)`
- **Description**: This method replaces the existing glossary dictionary with a new set of entries.
- **Parameters**:
  - `string glossaryId`: ID of the glossary whose dictionary will be replaced.
  - `string sourceLanguageCode`: Source language code for the glossary dictionary.
  - `string targetLanguageCode`: Target language code for the glossary dictionary.
  - `GlossaryEntries entries`: The source-target entries that will replace any existing ones for that language pair.
  - `MultilingualGlossaryDictionaryEntries glossaryDict`: The glossary dictionary to update.
  - `CancellationToken cancellationToken`: An optional parameter for the cancellation token.
- **Returns**: `Task<MultilingualGlossaryDictionaryInfo>` containing information about the replaced glossary dictionary.
- **Exceptions**:
  - `ArgumentException`: Thrown if any argument is invalid.
  - `DeepLException`: Thrown if any error occurs while communicating with the DeepL API.
- **Note**: Ensure that the new dictionary entries are complete and valid, as this method will completely overwrite the
  existing entries. It will also create a new glossary dictionary if one did not exist for the given language pair.
- **Example**:
  ```c#
  var entries = new Dictionary<string,string>{{"goodbye", "auf Wiedersehen"}};
  var newGlossaryDict = new MultilingualGlossaryDictionaryEntries("EN", "DE", new GlossaryEntries(entries));
  var replacedGlossaryDictionary = await client.ReplaceMultilingualGlossaryDictionaryAsync("4c81ffb4-2e...", newGlossaryDict);
  ```

### Replace Multilingual Glossary Dictionary from CSV

- **Method**:
  - `Task<MultilingualGlossaryDictionaryInfo> ReplaceMultilingualGlossaryDictionaryFromCsvAsync(string glossaryId, string
  sourceLanguageCode, string targetLanguageCode, Stream csvFile, CancellationToken cancellationToken = default)`
  - `Task<MultilingualGlossaryDictionaryInfo> ReplaceMultilingualGlossaryDictionaryFromCsvAsync(MultilingualGlossaryInfo
  glossary, string sourceLanguageCode, string targetLanguageCode, Stream csvFile, CancellationToken cancellationToken = default)`
- **Description**: This method allows you to replace or create a glossary dictionary using entries in CSV format.
- **Parameters**:
  - `string glossaryId`: The ID of the glossary whose dictionary will be replaced.
  - `MultilingualGlossaryInfo glossary`: The `MultilingualGlossaryInfo` object representing the glossary whose
    dictionary will be replaced.
  - `string sourceLanguageCode`: Language of source entries.
  - `string targetLanguageCode`: Language of target entries.
  - `Stream csvFile`: The CSV data containing glossary entries as a stream.
  - `CancellationToken cancellationToken`: An optional parameter for the cancellation token.
- **Returns**: `Task<MultilingualGlossaryDictionaryInfo>` containing information about the replaced glossary dictionary.
- **Exceptions**:
  - `ArgumentException`: Thrown if any argument is invalid.
  - `DeepLException`: Thrown if any error occurs while communicating with the DeepL API.
- **Example**:
  ```c#
  var csvStream =  File.OpenRead("myGlossary.csv");
  var myCsvGlossaryDictionary = await client.ReplaceMultilingualGlossaryDictionaryFromCsvAsync(
    glossary="4c81ffb4-2e...",
    sourceLanguageCode="EN",
    targetLanguageCode="DE",
    csvFile=csvFile
  );
  ```

### Delete a Multilingual Glossary Dictionary

- **Method**:
  - `Task DeleteMultilingualGlossaryDictionaryAsync(MultilingualGlossaryInfo glossary, string sourceLanguageCode, string
  targetLanguageCode, CancellationToken cancellationToken = default)`
  - `Task DeleteMultilingualGlossaryDictionaryAsync(string glossaryId, string sourceLanguageCode, string
  targetLanguageCode, CancellationToken cancellationToken = default)`
- **Description**: This method deletes a specified glossary dictionary from a given glossary.
- **Parameters**:
  - `string glossaryId`: The ID of the glossary containing the dictionary to delete.
  - `MultilingualGlossaryInfo glossary`: The `MultilingualGlossaryInfo` object of the glossary containing the dictionary to delete.
  - `MultilingualGlossaryDictionaryInfo dictionary`: The `MultilingualGlossaryDictionaryInfo` object that specifies the
  - dictionary to delete.
  - `string sourceLanguageCode`: The source language of the glossary dictionary.
  - `string targetLanguageCode`: The target language of the glossary dictionary.
- **Returns**: A `Task`

- **Migration Note**: Ensure that your application logic correctly identifies the dictionary to delete. If using
  `sourceLanguageCode` and `targetLanguageCode`, both must be provided to specify the dictionary.

- **Example**:
  ```c#
  var entriesEnde = new Dictionary<string,string>{{"hello", "hallo"}};
  var entriesDeen = new Dictionary<string,string>{{"hallo", "hello"}};
  var glossaryDictDeen = new MultilingualGlossaryDictionaryEntries("EN", "DE", new GlossaryEntries(entriesDeen));
  var glossaryDictEnde = new MultilingualGlossaryDictionaryEntries("DE", "EN", new GlossaryEntries(entriesEnde));
  var glossaryDicts = new [] {glossaryDictDeen, glossaryDictEnde};
  var createdGlossary = await client.CreateMultilingualGlossaryAsync("My Glossary", glossaryDicts);

  // Delete via specifying the glossary dictionary
  await client.DeleteMultilingualGlossaryDictionaryAsync(createdGlossary, createdGlossary.Dictionaries[0])

  // Delete via specifying the language pair
  await client.DeleteMultilingualGlossaryDictionaryAsync(createdGlossary, "DE", "EN");
  ```
