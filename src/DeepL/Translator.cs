// Copyright 2022 DeepL SE (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using DeepL.Internal;
using DeepL.Model;
using DeepL.Model.Exceptions;
using DeepL.Model.Interfaces;
using DeepL.Model.Options;

namespace DeepL;

/// <summary>
///   Client for the DeepL API. To use the DeepL API, initialize an instance of this class using your DeepL
///   Authentication Key. All functions are thread-safe, aside from <see cref="Translator.Dispose" />.
/// </summary>
public sealed class Translator : ITranslator {
  /// <summary>Base URL for DeepL API Pro accounts.</summary>
  private const string DeepLServerUrl = "https://api.deepl.com";

  /// <summary>Base URL for DeepL API Free accounts.</summary>
  private const string DeepLServerUrlFree = "https://api-free.deepl.com";

  /// <summary>Internal class implementing HTTP requests.</summary>
  private readonly DeepLClient _client;

  /// <summary>Initializes a new <see cref="Translator" /> object using your authentication key.</summary>
  /// <param name="authKey">
  ///   Authentication Key as found in your
  ///   <a href="https://www.deepl.com/pro-account/">DeepL API account</a>.
  /// </param>
  /// <param name="options">Additional options controlling Translator behaviour.</param>
  /// <exception cref="ArgumentNullException">If authKey argument is null.</exception>
  /// <exception cref="ArgumentException">If authKey argument is empty.</exception>
  /// <remarks>
  ///   This function does not establish a connection to the DeepL API. To check connectivity, use
  ///   <see cref="GetUsageAsync" />.
  /// </remarks>
  public Translator(string authKey, TranslatorOptions? options = null) {
    options ??= new TranslatorOptions();

    if (authKey == null) {
      throw new ArgumentNullException(nameof(authKey));
    }

    authKey = authKey.Trim();

    if (authKey.Length == 0) {
      throw new ArgumentException($"{nameof(authKey)} is empty");
    }

    var serverUrl = new Uri(
          options.ServerUrl ?? (AuthKeyIsFreeAccount(authKey) ? DeepLServerUrlFree : DeepLServerUrl));

    var headers = new Dictionary<string, string?>(options.Headers, StringComparer.OrdinalIgnoreCase);

    if (!headers.ContainsKey("User-Agent")) {
      headers.Add("User-Agent", ConstructUserAgentString(options.sendPlatformInfo, options.appInfo));
    }

    if (!headers.ContainsKey("Authorization")) {
      headers.Add("Authorization", $"DeepL-Auth-Key {authKey}");
    }

    var clientFactory = options.ClientFactory ?? (() =>
          DeepLClient.CreateDefaultHttpClient(
                options.PerRetryConnectionTimeout,
                options.OverallConnectionTimeout,
                options.MaximumNetworkRetries));

    _client = new DeepLClient(
          serverUrl,
          clientFactory,
          headers);
  }

  /// <summary>Releases the unmanaged resources and disposes of the managed resources used by the <see cref="Translator" />.</summary>
  public void Dispose() => _client.Dispose();

  /// <summary>Retrieves the version string, with format MAJOR.MINOR.BUGFIX.</summary>
  /// <returns>String containing the library version.</returns>
  public static string Version() {
    var version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()
          ?.InformationalVersion ?? "";
    return version;
  }

  /// <summary>
  ///   Determines if the given DeepL Authentication Key belongs to an API Free account or an API Pro account.
  /// </summary>
  /// <param name="authKey">
  ///   DeepL Authentication Key as found in your
  ///   <a href="https://www.deepl.com/pro-account/">DeepL API account</a>.
  /// </param>
  /// <returns>
  ///   <c>true</c> if the Authentication Key belongs to an API Free account, <c>false</c> if it belongs to an API Pro
  ///   account.
  /// </returns>
  public static bool AuthKeyIsFreeAccount(string authKey) => authKey.TrimEnd().EndsWith(":fx");

  /// <inheritdoc />
  public async Task<Usage> GetUsageAsync(CancellationToken cancellationToken = default) {
    using var responseMessage = await _client.ApiGetAsync("/v2/usage", cancellationToken).ConfigureAwait(false);
    await DeepLClient.CheckStatusCodeAsync(responseMessage).ConfigureAwait(false);
    var usageFields = await JsonUtils.DeserializeAsync<Usage.JsonFieldsStruct>(responseMessage)
          .ConfigureAwait(false);
    return new Usage(usageFields);
  }


  /// <inheritdoc />
  public async Task<TextResult[]> TranslateTextAsync(
        IEnumerable<string> texts,
        string? sourceLanguageCode,
        string targetLanguageCode,
        TextTranslateOptions? options = null,
        CancellationToken cancellationToken = default) {
    var bodyParams = CreateHttpParams(sourceLanguageCode, targetLanguageCode, options);
    var textParams = texts
          .Where(text => text.Length > 0 ? true : throw new ArgumentException("text must not be empty"))
          .Select(text => ("text", text));

    using var responseMessage = await _client
          .ApiPostAsync("/v2/translate", cancellationToken, bodyParams.Concat(textParams)).ConfigureAwait(false);

    await DeepLClient.CheckStatusCodeAsync(responseMessage).ConfigureAwait(false);
    var translatedTexts =
          await JsonUtils.DeserializeAsync<TextTranslateResult>(responseMessage).ConfigureAwait(false);
    return translatedTexts.Translations;
  }


  /// <inheritdoc />
  public async Task<TextResult> TranslateTextAsync(
        string text,
        string? sourceLanguageCode,
        string targetLanguageCode,
        TextTranslateOptions? options = null,
        CancellationToken cancellationToken = default)
    => (await TranslateTextAsync(
                new[] { text },
                sourceLanguageCode,
                targetLanguageCode,
                options,
                cancellationToken)
          .ConfigureAwait(false))[0];


  /// <inheritdoc />
  public async Task TranslateDocumentAsync(
        FileInfo inputFileInfo,
        FileInfo outputFileInfo,
        string? sourceLanguageCode,
        string targetLanguageCode,
        DocumentTranslateOptions? options = null,
        CancellationToken cancellationToken = default) {
    using var inputFile = inputFileInfo.OpenRead();
    using var outputFile = outputFileInfo.Open(FileMode.CreateNew, FileAccess.Write);
    try {
      await TranslateDocumentAsync(
            inputFile,
            inputFileInfo.Name,
            outputFile,
            sourceLanguageCode,
            targetLanguageCode,
            options,
            cancellationToken).ConfigureAwait(false);
    } catch {
      try {
        outputFileInfo.Delete();
      } catch {
        // ignored
      }

      throw;
    }
  }


  /// <inheritdoc />
  public async Task TranslateDocumentAsync(
        Stream inputFile,
        string inputFileName,
        Stream outputFile,
        string? sourceLanguageCode,
        string targetLanguageCode,
        DocumentTranslateOptions? options = null,
        CancellationToken cancellationToken = default) {
    DocumentHandle? handle = null;
    try {
      handle = await TranslateDocumentUploadAsync(
                  inputFile,
                  inputFileName,
                  sourceLanguageCode,
                  targetLanguageCode,
                  options,
                  cancellationToken)
            .ConfigureAwait(false);
      await TranslateDocumentWaitUntilDoneAsync(handle.Value, cancellationToken).ConfigureAwait(false);
      await TranslateDocumentDownloadAsync(handle.Value, outputFile, cancellationToken).ConfigureAwait(false);
    } catch (Exception exception) {
      throw new DocumentTranslationException(
            $"Error occurred during document translation: {exception.Message}",
            exception,
            handle);
    }
  }

  /// <inheritdoc />
  public async Task<DocumentHandle> TranslateDocumentUploadAsync(
        FileInfo inputFileInfo,
        string? sourceLanguageCode,
        string targetLanguageCode,
        DocumentTranslateOptions? options = null,
        CancellationToken cancellationToken = default) {
    using var inputFileStream = inputFileInfo.OpenRead();
    return await TranslateDocumentUploadAsync(
          inputFileStream,
          inputFileInfo.Name,
          sourceLanguageCode,
          targetLanguageCode,
          options,
          cancellationToken).ConfigureAwait(false);
  }

  /// <inheritdoc />
  public async Task<DocumentHandle> TranslateDocumentUploadAsync(
        Stream inputFile,
        string inputFileName,
        string? sourceLanguageCode,
        string targetLanguageCode,
        DocumentTranslateOptions? options = null,
        CancellationToken cancellationToken = default) {
    var bodyParams = CreateCommonHttpParams(
          sourceLanguageCode,
          targetLanguageCode,
          options?.Formality,
          options?.GlossaryId);

    using var responseMessage = await _client.ApiUploadAsync(
                "/v2/document/",
                cancellationToken,
                bodyParams,
                inputFile,
                inputFileName)
          .ConfigureAwait(false);
    await DeepLClient.CheckStatusCodeAsync(responseMessage).ConfigureAwait(false);
    return await JsonUtils.DeserializeAsync<DocumentHandle>(responseMessage).ConfigureAwait(false);
  }

  /// <inheritdoc />
  public async Task<DocumentStatus> TranslateDocumentStatusAsync(
        DocumentHandle handle,
        CancellationToken cancellationToken = default) {
    var bodyParams = new (string Key, string Value)[] { ("document_key", handle.DocumentKey) };
    using var responseMessage =
          await _client.ApiPostAsync(
                      $"/v2/document/{handle.DocumentId}",
                      cancellationToken,
                      bodyParams)
                .ConfigureAwait(false);
    await DeepLClient.CheckStatusCodeAsync(responseMessage).ConfigureAwait(false);
    return await JsonUtils.DeserializeAsync<DocumentStatus>(responseMessage).ConfigureAwait(false);
  }

  /// <inheritdoc />
  public async Task TranslateDocumentWaitUntilDoneAsync(
        DocumentHandle handle,
        CancellationToken cancellationToken = default) {
    var status = await TranslateDocumentStatusAsync(handle, cancellationToken).ConfigureAwait(false);
    while (status.Ok && !status.Done) {
      await Task.Delay(CalculateDocumentWaitTime(status.SecondsRemaining), cancellationToken).ConfigureAwait(false);
      status = await TranslateDocumentStatusAsync(handle, cancellationToken).ConfigureAwait(false);
    }

    if (!status.Ok) {
      throw new DeepLException(status.ErrorMessage ?? "Unknown error");
    }
  }

  /// <inheritdoc />
  public async Task TranslateDocumentDownloadAsync(
        DocumentHandle handle,
        FileInfo outputFileInfo,
        CancellationToken cancellationToken = default) {
    using var outputFileStream = outputFileInfo.Open(FileMode.CreateNew, FileAccess.Write);
    try {
      await TranslateDocumentDownloadAsync(handle, outputFileStream, cancellationToken).ConfigureAwait(false);
    } catch {
      try {
        outputFileInfo.Delete();
      } catch {
        // ignored
      }

      throw;
    }
  }

  /// <inheritdoc />
  public async Task TranslateDocumentDownloadAsync(
        DocumentHandle handle,
        Stream outputFile,
        CancellationToken cancellationToken = default) {
    var bodyParams = new (string Key, string Value)[] { ("document_key", handle.DocumentKey) };
    using var responseMessage = await _client.ApiPostAsync(
                $"/v2/document/{handle.DocumentId}/result",
                cancellationToken,
                bodyParams)
          .ConfigureAwait(false);

    await DeepLClient.CheckStatusCodeAsync(responseMessage, downloadingDocument: true).ConfigureAwait(false);
    await responseMessage.Content.CopyToAsync(outputFile).ConfigureAwait(false);
  }

  /// <inheritdoc />
  public async Task<SourceLanguage[]> GetSourceLanguagesAsync(CancellationToken cancellationToken = default) =>
        await GetLanguagesAsync<SourceLanguage>(false, cancellationToken).ConfigureAwait(false);

  /// <inheritdoc />
  public async Task<TargetLanguage[]> GetTargetLanguagesAsync(CancellationToken cancellationToken = default) =>
        await GetLanguagesAsync<TargetLanguage>(true, cancellationToken).ConfigureAwait(false);

  /// <inheritdoc />
  public async Task<GlossaryLanguagePair[]> GetGlossaryLanguagesAsync(CancellationToken cancellationToken = default) {
    using var responseMessage = await _client
          .ApiGetAsync("/v2/glossary-language-pairs", cancellationToken).ConfigureAwait(false);

    await DeepLClient.CheckStatusCodeAsync(responseMessage).ConfigureAwait(false);
    var languages = await JsonUtils.DeserializeAsync<GlossaryLanguageListResult>(responseMessage)
          .ConfigureAwait(false);
    return languages.GlossaryLanguagePairs;
  }

  /// <inheritdoc />
  public async Task<GlossaryInfo> CreateGlossaryAsync(
        string name,
        string sourceLanguageCode,
        string targetLanguageCode,
        GlossaryEntries entries,
        CancellationToken cancellationToken = default) =>
        await CreateGlossaryInternalAsync(
              name,
              sourceLanguageCode,
              targetLanguageCode,
              "tsv",
              entries.ToTsv(),
              cancellationToken).ConfigureAwait(false);

  /// <inheritdoc />
  public async Task<GlossaryInfo> CreateGlossaryFromCsvAsync(
        string name,
        string sourceLanguageCode,
        string targetLanguageCode,
        Stream csvFile,
        CancellationToken cancellationToken = default) =>
        await CreateGlossaryInternalAsync(
              name,
              sourceLanguageCode,
              targetLanguageCode,
              "csv",
              await new StreamReader(csvFile).ReadToEndAsync().ConfigureAwait(false),
              cancellationToken).ConfigureAwait(false);

  /// <inheritdoc />
  public async Task<GlossaryInfo> GetGlossaryAsync(
        string glossaryId,
        CancellationToken cancellationToken = default) {
    using var responseMessage =
          await _client.ApiGetAsync($"/v2/glossaries/{glossaryId}", cancellationToken)
                .ConfigureAwait(false);

    await DeepLClient.CheckStatusCodeAsync(responseMessage, true).ConfigureAwait(false);
    return await JsonUtils.DeserializeAsync<GlossaryInfo>(responseMessage).ConfigureAwait(false);
  }

  /// <inheritdoc />
  public async Task<GlossaryInfo> WaitUntilGlossaryReadyAsync(
        string glossaryId,
        CancellationToken cancellationToken = default) {
    var info = await GetGlossaryAsync(glossaryId, cancellationToken).ConfigureAwait(false);
    while (!info.Ready) {
      await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
      info = await GetGlossaryAsync(glossaryId, cancellationToken).ConfigureAwait(false);
    }

    return info;
  }

  /// <inheritdoc />
  public async Task<GlossaryInfo[]> ListGlossariesAsync(CancellationToken cancellationToken = default) {
    using var responseMessage =
          await _client.ApiGetAsync("/v2/glossaries", cancellationToken).ConfigureAwait(false);

    await DeepLClient.CheckStatusCodeAsync(responseMessage, true).ConfigureAwait(false);
    return (await JsonUtils.DeserializeAsync<GlossaryListResult>(responseMessage).ConfigureAwait(false)).Glossaries;
  }

  /// <inheritdoc />
  public async Task<GlossaryEntries> GetGlossaryEntriesAsync(
        string glossaryId,
        CancellationToken cancellationToken = default) {
    using var responseMessage = await _client.ApiGetAsync(
          $"/v2/glossaries/{glossaryId}/entries",
          cancellationToken,
          acceptHeader: "text/tab-separated-values").ConfigureAwait(false);

    await DeepLClient.CheckStatusCodeAsync(responseMessage, true).ConfigureAwait(false);
    var contentTsv = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
    return GlossaryEntries.FromTsv(contentTsv, true);
  }

  /// <inheritdoc />
  public async Task<GlossaryEntries> GetGlossaryEntriesAsync(
        GlossaryInfo glossary,
        CancellationToken cancellationToken = default) =>
        await GetGlossaryEntriesAsync(glossary.GlossaryId, cancellationToken).ConfigureAwait(false);

  /// <inheritdoc />
  public async Task DeleteGlossaryAsync(
        string glossaryId,
        CancellationToken cancellationToken = default) {
    using var responseMessage =
          await _client.ApiDeleteAsync($"/v2/glossaries/{glossaryId}", cancellationToken)
                .ConfigureAwait(false);

    await DeepLClient.CheckStatusCodeAsync(responseMessage, true).ConfigureAwait(false);
  }

  /// <inheritdoc />
  public async Task DeleteGlossaryAsync(
        GlossaryInfo glossary,
        CancellationToken cancellationToken = default) =>
        await DeleteGlossaryAsync(glossary.GlossaryId, cancellationToken).ConfigureAwait(false);

  /// <summary>Internal function to retrieve available languages.</summary>
  /// <param name="target"><c>true</c> to retrieve target languages, <c>false</c> to retrieve source languages.</param>
  /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
  /// <returns>Array of <see cref="Language" /> objects containing information about the available languages.</returns>
  /// <exception cref="DeepLException">
  ///   If any error occurs while communicating with the DeepL API, a
  ///   <see cref="DeepLException" /> or a derived class will be thrown.
  /// </exception>
  private async Task<TValue[]> GetLanguagesAsync<TValue>(
        bool target,
        CancellationToken cancellationToken = default) {
    var queryParams = new (string Key, string Value)[] { ("type", target ? "target" : "source") };
    using var responseMessage =
          await _client.ApiGetAsync("/v2/languages", cancellationToken, queryParams)
                .ConfigureAwait(false);

    await DeepLClient.CheckStatusCodeAsync(responseMessage).ConfigureAwait(false);
    return await JsonUtils.DeserializeAsync<TValue[]>(responseMessage).ConfigureAwait(false);
  }

  /// <summary>
  ///   Builds the user-agent string we want to send to the API with every request. This string contains
  ///   basic information about the client environment, such as the deepl client library version, operating
  ///   system and language runtime version.
  /// </summary>
  /// <param name="sendPlatformInfo">
  ///   <c>true</c> to send platform information with every API request (default), 
  ///   <c>false</c> to only send the library version.
  /// </param>
  /// <param name="AppInfo">
  ///   Name and version of the application using this library. Ignored if null.
  /// </param>
  /// <returns>Enumerable of tuples containing the parameters to include in HTTP request.</returns>
  private String ConstructUserAgentString(bool sendPlatformInfo = true, AppInfo? appInfo = null) {
    var platformInfoString = $"deepl-dotnet/{Version()}";
    if (sendPlatformInfo) {
    var osDescription = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
    var clrVersion = Environment.Version.ToString();
      platformInfoString += $" ({osDescription}) dotnet-clr/{clrVersion}";
    }
    if (appInfo != null) {
      platformInfoString += $" {appInfo?.AppName}/{appInfo?.AppVersion}";
    }
    return platformInfoString;
  }

  /// <summary>
  ///   Checks the specified languages and options are valid, and returns an enumerable of tuples containing the parameters
  ///   to include in HTTP request.
  /// </summary>
  /// <param name="sourceLanguageCode">
  ///   Language code of translation source language, or null if auto-detection should be
  ///   used.
  /// </param>
  /// <param name="targetLanguageCode">Language code of translation target language.</param>
  /// <param name="options">Extra <see cref="TextTranslateOptions" /> influencing translation.</param>
  /// <returns>Enumerable of tuples containing the parameters to include in HTTP request.</returns>
  /// <exception cref="ArgumentException">If the specified languages or options are invalid.</exception>
  private static IEnumerable<(string Key, string Value)> CreateHttpParams(
        string? sourceLanguageCode,
        string targetLanguageCode,
        TextTranslateOptions? options) {

    var bodyParams = CreateCommonHttpParams(
          sourceLanguageCode,
          targetLanguageCode,
          options?.Formality,
          options?.GlossaryId);

    if (options == null) {
      return bodyParams;
    }

    if (options.SentenceSplittingMode != SentenceSplittingMode.All) {
      bodyParams.Add(
            ("split_sentences", options.SentenceSplittingMode == SentenceSplittingMode.Off ? "0" : "nonewlines"));
    }

    if (options.PreserveFormatting) {
      bodyParams.Add(("preserve_formatting", "1"));
    }

    if (options.TagHandling != null) {
      bodyParams.Add(("tag_handling", options.TagHandling));
    }

    if (!options.OutlineDetection) {
      bodyParams.Add(("outline_detection", "0"));
    }

    if (options.NonSplittingTags.Count > 0) {
      bodyParams.Add(("non_splitting_tags", string.Join(",", options.NonSplittingTags)));
    }

    if (options.SplittingTags.Count > 0) {
      bodyParams.Add(("splitting_tags", string.Join(",", options.SplittingTags)));
    }

    if (options.IgnoreTags.Count > 0) {
      bodyParams.Add(("ignore_tags", string.Join(",", options.IgnoreTags)));
    }

    return bodyParams;
  }

  /// <summary>
  ///   Checks the specified languages and options are valid, and returns a list of tuples containing the parameters
  ///   to include in HTTP request.
  /// </summary>
  /// <param name="sourceLanguageCode">
  ///   Language code of translation source language, or null if auto-detection should be
  ///   used.
  /// </param>
  /// <param name="targetLanguageCode">Language code of translation target language.</param>
  /// <param name="formality">Formality option for translation.</param>
  /// <param name="glossaryId">Optional ID of glossary to use for translation.</param>
  /// <returns>List of tuples containing the parameters to include in HTTP request.</returns>
  /// <exception cref="ArgumentException">If the specified languages or options are invalid.</exception>
  private static List<(string Key, string Value)> CreateCommonHttpParams(
        string? sourceLanguageCode,
        string targetLanguageCode,
        Formality? formality,
        string? glossaryId) {
    targetLanguageCode = LanguageCode.Standardize(targetLanguageCode);
    sourceLanguageCode = sourceLanguageCode == null ? null : LanguageCode.Standardize(sourceLanguageCode);

    CheckValidLanguages(sourceLanguageCode, targetLanguageCode);

    var bodyParams = new List<(string Key, string Value)> { ("target_lang", targetLanguageCode) };
    if (sourceLanguageCode != null) {
      bodyParams.Add(("source_lang", sourceLanguageCode));
    }

    if (glossaryId != null) {
      if (sourceLanguageCode == null) {
        throw new ArgumentException($"{nameof(sourceLanguageCode)} is required if using a glossary");
      }

      bodyParams.Add(("glossary_id", glossaryId));
    }

    switch (formality) {
      case null:
      case Formality.Default:
        break;
      case Formality.Less:
        bodyParams.Add(("formality", "less"));
        break;
      case Formality.More:
        bodyParams.Add(("formality", "more"));
        break;
      case Formality.PreferLess:
        bodyParams.Add(("formality", "prefer_less"));
        break;
      case Formality.PreferMore:
        bodyParams.Add(("formality", "prefer_more"));
        break;
      default:
        throw new ArgumentException($"{nameof(formality)} value is out of range");
    }

    return bodyParams;
  }

  /// <summary>Checks the specified source and target language are valid, and throws an exception if not.</summary>
  /// <param name="sourceLanguageCode">Language code of translation source language, or null if auto-detection is used.</param>
  /// <param name="targetLanguageCode">Language code of translation target language.</param>
  /// <exception cref="ArgumentException">If source or target language code are not valid.</exception>
  private static void CheckValidLanguages(string? sourceLanguageCode, string targetLanguageCode) {
    if (sourceLanguageCode is { Length: 0 }) {
      throw new ArgumentException($"{nameof(sourceLanguageCode)} must not be empty");
    }

    if (targetLanguageCode.Length == 0) {
      throw new ArgumentException($"{nameof(targetLanguageCode)} must not be empty");
    }

    switch (targetLanguageCode) {
      case "en":
        throw new ArgumentException(
              $"{nameof(targetLanguageCode)}=\"en\" is deprecated, please use \"en-GB\" or \"en-US\" instead");
      case "pt":
        throw new ArgumentException(
              $"{nameof(targetLanguageCode)}=\"pt\" is deprecated, please use \"pt-PT\" or \"pt-BR\" instead");
    }
  }

  /// <summary>
  ///   Determines recommended time to wait before checking document translation again, using an optional hint of
  ///   seconds remaining.
  /// </summary>
  /// <param name="hintSecondsRemaining">Optional hint of the number of seconds remaining.</param>
  /// <returns><see cref="TimeSpan" /> to wait.</returns>
  private static TimeSpan CalculateDocumentWaitTime(int? hintSecondsRemaining) {
    // hintSecondsRemaining is currently unreliable, so just poll equidistantly
    const int POLLING_TIME_SECS = 5;
    return TimeSpan.FromSeconds(POLLING_TIME_SECS);
  }

  /// <summary>Creates a glossary with given details.</summary>
  private async Task<GlossaryInfo> CreateGlossaryInternalAsync(
        string name,
        string sourceLanguageCode,
        string targetLanguageCode,
        string entriesFormat,
        string entries,
        CancellationToken cancellationToken) {
    if (name.Length == 0) {
      throw new ArgumentException($"Parameter {nameof(name)} must not be empty");
    }

    sourceLanguageCode = LanguageCode.RemoveRegionalVariant(sourceLanguageCode);
    targetLanguageCode = LanguageCode.RemoveRegionalVariant(targetLanguageCode);

    var bodyParams = new (string Key, string Value)[] {
          ("name", name), ("source_lang", sourceLanguageCode), ("target_lang", targetLanguageCode),
          ("entries_format", entriesFormat), ("entries", entries)
    };
    using var responseMessage =
          await _client.ApiPostAsync("/v2/glossaries", cancellationToken, bodyParams).ConfigureAwait(false);

    await DeepLClient.CheckStatusCodeAsync(responseMessage, true).ConfigureAwait(false);
    return await JsonUtils.DeserializeAsync<GlossaryInfo>(responseMessage).ConfigureAwait(false);
  }

  /// <summary>Class used for JSON-deserialization of text translate results.</summary>
  private readonly struct TextTranslateResult {
    /// <summary>Initializes a new instance of <see cref="TextTranslateResult" />, used for JSON deserialization.</summary>
    [JsonConstructor]
    public TextTranslateResult(TextResult[] translations) {
      Translations = translations;
    }

    /// <summary>Array of <see cref="TextResult" /> objects holding text translation results.</summary>
    public TextResult[] Translations { get; }
  }

  /// <summary>Class used for JSON-deserialization of glossary list results.</summary>
  private readonly struct GlossaryListResult {
    /// <summary>Initializes a new instance of <see cref="GlossaryListResult" />, used for JSON deserialization.</summary>
    [JsonConstructor]
    public GlossaryListResult(GlossaryInfo[] glossaries) {
      Glossaries = glossaries;
    }

    /// <summary>Array of <see cref="GlossaryInfo" /> objects holding glossary information.</summary>
    public GlossaryInfo[] Glossaries { get; }
  }

  /// <summary>Class used for JSON-deserialization of results of supported languages for glossaries.</summary>
  private readonly struct GlossaryLanguageListResult {
    /// <summary>Initializes a new instance of <see cref="GlossaryLanguageListResult" />, used for JSON deserialization.</summary>
    [JsonConstructor]
    public GlossaryLanguageListResult(GlossaryLanguagePair[] glossaryLanguagePairs) {
      GlossaryLanguagePairs = glossaryLanguagePairs;
    }

    /// <summary>Array of <see cref="GlossaryLanguagePair" /> objects holding supported glossary language pairs.</summary>
    [JsonPropertyName("supported_languages")]
    public GlossaryLanguagePair[] GlossaryLanguagePairs { get; }
  }
}
