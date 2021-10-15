// Copyright 2021 DeepL GmbH (https://www.deepl.com)
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

namespace DeepL {
  /// <summary>
  ///   Client for the DeepL API. To use the DeepL API, initialize an instance of this class using your DeepL
  ///   Authentication Key. All functions are thread-safe, aside from <see cref="Translator.Dispose" />.
  /// </summary>
  public sealed class Translator : IDisposable {
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
    public Translator(string authKey, TranslatorOptions? options = null) {
      options ??= new TranslatorOptions();

      if (authKey == null) {
        throw new ArgumentNullException(nameof(authKey));
      }

      authKey = authKey.Trim();

      var serverUrl = new Uri(
            options.ServerUrl ?? (AuthKeyIsFreeAccount(authKey) ? DeepLServerUrlFree : DeepLServerUrl));

      var headers = new Dictionary<string, string?>(options.Headers, StringComparer.OrdinalIgnoreCase);

      if (!headers.ContainsKey("User-Agent")) {
        headers.Add("User-Agent", $"deepl-dotnet/{Version()}");
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
    public static bool AuthKeyIsFreeAccount(string authKey) {
      if (authKey == null) {
        throw new ArgumentNullException(nameof(authKey));
      }

      authKey = authKey.Trim();

      if (authKey.Length == 0) {
        throw new ArgumentException($"{nameof(authKey)} argument must not be empty", nameof(authKey));
      }

      return authKey.EndsWith(":fx");
    }

    /// <summary>Retrieves the usage in the current billing period for this DeepL account.</summary>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns><see cref="Usage" /> object containing account usage information.</returns>
    public async Task<Usage> GetUsageAsync(CancellationToken cancellationToken = default) {
      using var responseMessage = await _client.ApiGetAsync("/v2/usage", cancellationToken).ConfigureAwait(false);
      await DeepLClient.CheckStatusCodeAsync(responseMessage).ConfigureAwait(false);
      var usageFields = await JsonUtils.DeserializeAsync<Usage.JsonFieldsStruct>(responseMessage)
            .ConfigureAwait(false);
      return new Usage(usageFields);
    }

    /// <summary>Translate specified texts from source language into target language.</summary>
    /// <param name="texts">Texts to translate.</param>
    /// <param name="sourceLanguage">Language code of the input language, or null to use auto-detection.</param>
    /// <param name="targetLanguage">Language code of the desired output language.</param>
    /// <param name="options">Extra <see cref="TextTranslateOptions" /> influencing translation.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns>Texts translated into specified target language.</returns>
    /// <exception cref="DeepLException">
    ///   If any error occurs, a <see cref="DeepLException" /> or a derived class will be
    ///   thrown.
    /// </exception>
    public async Task<TextResult[]> TranslateTextAsync(
          IEnumerable<string> texts,
          string? sourceLanguage,
          string targetLanguage,
          TextTranslateOptions? options = null,
          CancellationToken cancellationToken = default) {
      var bodyParams = CreateHttpParams(sourceLanguage, targetLanguage, options);
      var textParams = texts
            .Where(text => text.Length > 0 ? true : throw new DeepLException("text must not be empty"))
            .Select(text => ("text", text));

      using var responseMessage = await _client
            .ApiPostAsync("/v2/translate", cancellationToken, bodyParams.Concat(textParams)).ConfigureAwait(false);

      await DeepLClient.CheckStatusCodeAsync(responseMessage).ConfigureAwait(false);
      var translatedTexts =
            await JsonUtils.DeserializeAsync<TextTranslateResult>(responseMessage).ConfigureAwait(false);
      return translatedTexts.Translations;
    }

    /// <summary>Translate specified text from source language into target language.</summary>
    /// <param name="text">Text to translate.</param>
    /// <param name="sourceLanguage">Language code of the input language, or null to use auto-detection.</param>
    /// <param name="targetLanguage">Language code of the desired output language.</param>
    /// <param name="options"><see cref="TextTranslateOptions" /> influencing translation.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns>Text translated into specified target language.</returns>
    /// <exception cref="DeepLException">
    ///   If any error occurs, a <see cref="DeepLException" /> or a derived class will be thrown.
    /// </exception>
    public async Task<TextResult> TranslateTextAsync(
          string text,
          string? sourceLanguage,
          string targetLanguage,
          TextTranslateOptions? options = null,
          CancellationToken cancellationToken = default)
      => (await TranslateTextAsync(
                  new[] { text },
                  sourceLanguage,
                  targetLanguage,
                  options,
                  cancellationToken)
            .ConfigureAwait(false))[0];

    /// <summary>
    ///   Translate document at specified input <see cref="FileInfo" /> from source language to target language and
    ///   store the translated document at specified output <see cref="FileInfo" />.
    /// </summary>
    /// <param name="inputFileInfo"><see cref="FileInfo" /> object containing path to input document.</param>
    /// <param name="outputFileInfo"><see cref="FileInfo" /> object containing path to store translated document.</param>
    /// <param name="sourceLanguage">Language code of the input language, or null to use auto-detection.</param>
    /// <param name="targetLanguage">Language code of the desired output language.</param>
    /// <param name="options"><see cref="DocumentTranslateOptions" /> influencing translation.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <exception cref="System.IO.IOException">If the output path is occupied.</exception>
    /// <exception cref="DocumentTranslationException">
    ///   If the document is successfully uploaded, but an error occurs during
    ///   translation. This exception includes the <see cref="DocumentHandle" /> that may be used to retrieve the document.
    /// </exception>
    /// <exception cref="DeepLException">
    ///   If an error occurs while uploading, or there is a problem with the request, a
    ///   <see cref="DeepLException" /> or a derived class will be thrown.
    /// </exception>
    public async Task TranslateDocumentAsync(
          FileInfo inputFileInfo,
          FileInfo outputFileInfo,
          string? sourceLanguage,
          string targetLanguage,
          DocumentTranslateOptions? options = null,
          CancellationToken cancellationToken = default) {
      using var inputFile = inputFileInfo.OpenRead();
      using var outputFile = outputFileInfo.Open(FileMode.CreateNew, FileAccess.Write);
      try {
        await TranslateDocumentAsync(
              inputFile,
              inputFileInfo.Name,
              outputFile,
              sourceLanguage,
              targetLanguage,
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

    /// <summary>
    ///   Translate specified document content from source language to target language and store the translated document
    ///   content to specified stream.
    /// </summary>
    /// <param name="inputFile"><see cref="Stream" /> containing input document content.</param>
    /// <param name="inputFileName">Name of the input file. The file extension is used to determine file type.</param>
    /// <param name="outputFile"><see cref="Stream" /> to store translated document content.</param>
    /// <param name="sourceLanguage">Language code of the input language, or null to use auto-detection.</param>
    /// <param name="targetLanguage">Language code of the desired output language.</param>
    /// <param name="options"><see cref="DocumentTranslateOptions" /> influencing translation.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <exception cref="DocumentTranslationException">
    ///   If the document is successfully uploaded, but an error occurs during
    ///   translation. This exception includes the <see cref="DocumentHandle" /> that may be used to retrieve the document.
    /// </exception>
    /// <exception cref="DeepLException">
    ///   If an error occurs while uploading, or there is a problem with the request, a
    ///   <see cref="DeepLException" /> or a derived class will be thrown.
    /// </exception>
    public async Task TranslateDocumentAsync(
          Stream inputFile,
          string inputFileName,
          Stream outputFile,
          string? sourceLanguage,
          string targetLanguage,
          DocumentTranslateOptions? options = null,
          CancellationToken cancellationToken = default) {
      var handle =
            await TranslateDocumentUploadAsync(
                        inputFile,
                        inputFileName,
                        sourceLanguage,
                        targetLanguage,
                        options,
                        cancellationToken)
                  .ConfigureAwait(false);
      try {
        await TranslateDocumentWaitUntilDoneAsync(handle, cancellationToken).ConfigureAwait(false);
        await TranslateDocumentDownloadAsync(handle, outputFile, cancellationToken).ConfigureAwait(false);
      } catch (Exception exception) {
        throw new DocumentTranslationException(
              $"Error occurred during document translation: {exception.Message}",
              exception,
              handle);
      }
    }

    /// <summary>
    ///   Upload document at specified input <see cref="FileInfo" /> for translation from source language to target
    ///   language.
    /// </summary>
    /// <param name="inputFileInfo"><see cref="FileInfo" /> object containing path to input document.</param>
    /// <param name="sourceLanguage">Language code of the input language, or null to use auto-detection.</param>
    /// <param name="targetLanguage">Language code of the desired output language.</param>
    /// <param name="options"><see cref="DocumentTranslateOptions" /> influencing translation.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns><see cref="DocumentHandle" /> object associated with the in-progress document translation.</returns>
    /// <exception cref="DeepLException">
    ///   If any error occurs, a <see cref="DeepLException" /> or a derived class will be
    ///   thrown.
    /// </exception>
    public async Task<DocumentHandle> TranslateDocumentUploadAsync(
          FileInfo inputFileInfo,
          string? sourceLanguage,
          string targetLanguage,
          DocumentTranslateOptions? options = null,
          CancellationToken cancellationToken = default) {
      using var inputFileStream = inputFileInfo.OpenRead();
      return await TranslateDocumentUploadAsync(
            inputFileStream,
            inputFileInfo.Name,
            sourceLanguage,
            targetLanguage,
            options,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Upload document content with specified filename for translation from source language to target language.</summary>
    /// <param name="inputFile"><see cref="Stream" /> containing input document content.</param>
    /// <param name="inputFileName">Name of the input file. The file extension is used to determine file type.</param>
    /// <param name="sourceLanguage">Language code of the input language, or null to use auto-detection.</param>
    /// <param name="targetLanguage">Language code of the desired output language.</param>
    /// <param name="options"><see cref="DocumentTranslateOptions" /> influencing translation.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns><see cref="DocumentHandle" /> object associated with the in-progress document translation.</returns>
    /// <exception cref="DeepLException">
    ///   If any error occurs, a <see cref="DeepLException" /> or a derived class will be
    ///   thrown.
    /// </exception>
    public async Task<DocumentHandle> TranslateDocumentUploadAsync(
          Stream inputFile,
          string inputFileName,
          string? sourceLanguage,
          string targetLanguage,
          DocumentTranslateOptions? options = null,
          CancellationToken cancellationToken = default) {
      var bodyParams = CreateHttpParams(
            sourceLanguage,
            targetLanguage,
            options);

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

    /// <summary>
    ///   Retrieve the status of in-progress document translation associated with specified
    ///   <see cref="DocumentHandle" />.
    /// </summary>
    /// <param name="handle"><see cref="DocumentHandle" /> associated with an in-progress document translation.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns><see cref="DocumentStatus" /> object containing the document translation status.</returns>
    /// <exception cref="DeepLException">
    ///   If any error occurs, a <see cref="DeepLException" /> or a derived class will be
    ///   thrown.
    /// </exception>
    public async Task<DocumentStatus> TranslateDocumentStatusAsync(
          DocumentHandle handle,
          CancellationToken cancellationToken = default) {
      var bodyParams = new (string key, string value)[] { ("document_key", handle.DocumentKey) };
      using var responseMessage =
            await _client.ApiPostAsync(
                        $"/v2/document/{handle.DocumentId}",
                        cancellationToken,
                        bodyParams)
                  .ConfigureAwait(false);
      await DeepLClient.CheckStatusCodeAsync(responseMessage).ConfigureAwait(false);
      return await JsonUtils.DeserializeAsync<DocumentStatus>(responseMessage).ConfigureAwait(false);
    }

    /// <summary>
    ///   Checks document translation status and asynchronously-waits until document translation is complete or fails
    ///   due to an error.
    /// </summary>
    /// <param name="handle"><see cref="DocumentHandle" /> associated with an in-progress document translation.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <exception cref="DeepLException">If an error occurred during document translation.</exception>
    public async Task TranslateDocumentWaitUntilDoneAsync(
          DocumentHandle handle,
          CancellationToken cancellationToken = default) {
      var status = await TranslateDocumentStatusAsync(handle, cancellationToken).ConfigureAwait(false);
      while (status.Ok && !status.Done) {
        var secs = Math.Max(((status.SecondsRemaining ?? 0) / 2.0) + 1.0, 1.0);
        await Task.Delay(TimeSpan.FromSeconds(secs), cancellationToken).ConfigureAwait(false);
        status = await TranslateDocumentStatusAsync(handle, cancellationToken).ConfigureAwait(false);
      }

      if (!status.Ok) {
        throw new DeepLException("Document translation resulted in an error");
      }
    }

    /// <summary>
    ///   Downloads the resulting translated document associated with specified <see cref="DocumentHandle" /> to the
    ///   specified <see cref="FileInfo" />. The <see cref="DocumentStatus" /> for the document must have
    ///   <see cref="DocumentStatus.Done" /> == <c>true</c>.
    /// </summary>
    /// <param name="handle"><see cref="DocumentHandle" /> associated with an in-progress document translation.</param>
    /// <param name="outputFileInfo"><see cref="FileInfo" /> object containing path to store translated document.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <exception cref="System.IO.IOException">If the output path is occupied.</exception>
    /// <exception cref="DocumentNotReadyException">
    ///   If the document is not ready to be downloaded, that is, the document status
    ///   is not <see cref="DocumentStatus.Done" />.
    /// </exception>
    /// <exception cref="DeepLException">
    ///   If any error occurs, a <see cref="DeepLException" /> or a derived class will be
    ///   thrown.
    /// </exception>
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

    /// <summary>
    ///   Downloads the resulting translated document associated with specified <see cref="DocumentHandle" /> to the
    ///   specified <see cref="Stream" />. The <see cref="DocumentStatus" /> for the document must have
    ///   <see cref="DocumentStatus.Done" /> == <c>true</c>.
    /// </summary>
    /// <param name="handle"><see cref="DocumentHandle" /> associated with an in-progress document translation.</param>
    /// <param name="outputFile"><see cref="Stream" /> to store translated document content.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <exception cref="DocumentNotReadyException">
    ///   If the document is not ready to be downloaded, that is, the document status
    ///   is not <see cref="DocumentStatus.Done" />.
    /// </exception>
    /// <exception cref="DeepLException">
    ///   If any error occurs, a <see cref="DeepLException" /> or a derived class will be
    ///   thrown.
    /// </exception>
    public async Task TranslateDocumentDownloadAsync(
          DocumentHandle handle,
          Stream outputFile,
          CancellationToken cancellationToken = default) {
      var bodyParams = new (string key, string value)[] { ("document_key", handle.DocumentKey) };
      using var responseMessage = await _client.ApiPostAsync(
                  $"/v2/document/{handle.DocumentId}/result",
                  cancellationToken,
                  bodyParams)
            .ConfigureAwait(false);

      await DeepLClient.CheckStatusCodeAsync(responseMessage, downloadingDocument: true).ConfigureAwait(false);
      await responseMessage.Content.CopyToAsync(outputFile).ConfigureAwait(false);
    }

    /// <summary>Retrieves the list of supported translation source languages.</summary>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns>Array of <see cref="Language" /> objects representing the available translation source languages.</returns>
    /// <exception cref="DeepLException">
    ///   If a connection error occurs, a <see cref="DeepLException" /> or a derived class will
    ///   be thrown.
    /// </exception>
    public async Task<SourceLanguage[]> GetSourceLanguagesAsync(CancellationToken cancellationToken = default) =>
          await GetLanguagesAsync<SourceLanguage>(false, cancellationToken).ConfigureAwait(false);

    /// <summary>Retrieves the list of supported translation target languages.</summary>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns>Array of <see cref="Language" /> objects representing the available translation target languages.</returns>
    /// <exception cref="DeepLException">
    ///   If a connection error occurs, a <see cref="DeepLException" /> or a derived class will
    ///   be thrown.
    /// </exception>
    public async Task<TargetLanguage[]> GetTargetLanguagesAsync(CancellationToken cancellationToken = default) =>
          await GetLanguagesAsync<TargetLanguage>(true, cancellationToken).ConfigureAwait(false);

    /// <summary>
    ///   Retrieves the list of supported glossary language pairs. When creating glossaries, the source and target
    ///   language pair must match one of the available language pairs.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns>Array of <see cref="GlossaryLanguagePair" /> objects representing the available glossary language pairs.</returns>
    /// <exception cref="DeepLException">
    ///   If a connection error occurs, a <see cref="DeepLException" /> or a derived class will
    ///   be thrown.
    /// </exception>
    public async Task<GlossaryLanguagePair[]> GetGlossaryLanguagesAsync(CancellationToken cancellationToken = default) {
      using var responseMessage = await _client
            .ApiGetAsync("/v2/glossary-language-pairs", cancellationToken).ConfigureAwait(false);

      await DeepLClient.CheckStatusCodeAsync(responseMessage).ConfigureAwait(false);
      var languages = await JsonUtils.DeserializeAsync<GlossaryLanguageListResult>(responseMessage)
            .ConfigureAwait(false);
      return languages.GlossaryLanguagePairs;
    }

    /// <summary>
    ///   Creates a glossary in your DeepL account with the specified details and returns a <see cref="GlossaryInfo" />
    ///   object with details about the newly created glossary. The glossary can be used in translations to override
    ///   translations for specific terms (words). The glossary source and target languages must match the languages of
    ///   translations for which it will be used.
    /// </summary>
    /// <param name="name">User-defined name to assign to the glossary.</param>
    /// <param name="sourceLang">Language code of the source terms language.</param>
    /// <param name="targetLang">Language code of the target terms language.</param>
    /// <param name="entries">Dictionary of source-target entry pairs.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns><see cref="GlossaryInfo" /> object with details about the newly created glossary.</returns>
    /// <exception cref="DeepLException">
    ///   If any error occurs, a <see cref="DeepLException" /> or a derived class will be
    ///   thrown.
    /// </exception>
    public async Task<GlossaryInfo> CreateGlossaryAsync(
          string name,
          string sourceLang,
          string targetLang,
          IEnumerable<KeyValuePair<string, string>> entries,
          CancellationToken cancellationToken = default) {
      if (name.Length == 0) {
        throw new DeepLException($"Parameter {nameof(name)} must not be empty");
      }

      sourceLang = LanguageCode.RemoveRegionalVariant(sourceLang);
      targetLang = LanguageCode.RemoveRegionalVariant(targetLang);

      var entriesTsv = GlossaryUtils.ConvertToTsv(entries);
      if (entriesTsv.Length == 0) {
        throw new DeepLException($"Parameter {nameof(entries)} must not be empty");
      }

      var bodyParams = new (string key, string value)[] {
            ("name", name), ("source_lang", sourceLang), ("target_lang", targetLang), ("entries_format", "tsv"),
            ("entries", entriesTsv)
      };
      using var responseMessage =
            await _client.ApiPostAsync("/v2/glossaries", cancellationToken, bodyParams).ConfigureAwait(false);

      await DeepLClient.CheckStatusCodeAsync(responseMessage, true).ConfigureAwait(false);
      return await JsonUtils.DeserializeAsync<GlossaryInfo>(responseMessage).ConfigureAwait(false);
    }

    /// <summary>
    ///   Retrieves information about the glossary with the specified ID and returns a <see cref="GlossaryInfo" />
    ///   object containing details. This does not retrieve the glossary entries; to retrieve entries use
    ///   <see cref="GetGlossaryEntriesAsync(string, CancellationToken)" />
    /// </summary>
    /// <param name="glossaryId">ID of glossary to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns><see cref="GlossaryInfo" /> object with details about the specified glossary.</returns>
    /// <exception cref="DeepLException">
    ///   If any error occurs, a <see cref="DeepLException" /> or a derived class will be
    ///   thrown.
    /// </exception>
    public async Task<GlossaryInfo> GetGlossaryAsync(
          string glossaryId,
          CancellationToken cancellationToken = default) {
      using var responseMessage =
            await _client.ApiGetAsync($"/v2/glossaries/{glossaryId}", cancellationToken)
                  .ConfigureAwait(false);

      await DeepLClient.CheckStatusCodeAsync(responseMessage, true).ConfigureAwait(false);
      return await JsonUtils.DeserializeAsync<GlossaryInfo>(responseMessage).ConfigureAwait(false);
    }

    /// <summary>Asynchronously waits until the given glossary is ready to be used for translations.</summary>
    /// <param name="glossaryId">ID of glossary to ensure ready.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <exception cref="DeepLException">
    ///   If any error occurs, a <see cref="DeepLException" /> or a derived class will be
    ///   thrown.
    /// </exception>
    public async Task<GlossaryInfo> WaitUntilGlossaryReadyAsync(
          string glossaryId,
          CancellationToken cancellationToken = default) {
      var info = await GetGlossaryAsync(glossaryId, cancellationToken);
      while (!info.Ready) {
        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
        info = await GetGlossaryAsync(glossaryId, cancellationToken);
      }

      return info;
    }

    /// <summary>
    ///   Retrieves information about all glossaries and returns an array of <see cref="GlossaryInfo" /> objects
    ///   containing details. This does not retrieve the glossary entries; to retrieve entries use
    ///   <see cref="GetGlossaryEntriesAsync(string, CancellationToken)" />
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns>Array of <see cref="GlossaryInfo" /> objects with details about each glossary.</returns>
    /// <exception cref="DeepLException">
    ///   If any error occurs, a <see cref="DeepLException" /> or a derived class will be
    ///   thrown.
    /// </exception>
    public async Task<GlossaryInfo[]> ListGlossariesAsync(CancellationToken cancellationToken = default) {
      using var responseMessage =
            await _client.ApiGetAsync("/v2/glossaries", cancellationToken).ConfigureAwait(false);

      await DeepLClient.CheckStatusCodeAsync(responseMessage, true).ConfigureAwait(false);
      return (await JsonUtils.DeserializeAsync<GlossaryListResult>(responseMessage).ConfigureAwait(false)).Glossaries;
    }

    /// <summary>
    ///   Retrieves the entries containing within the glossary with the specified ID and returns them as a Dictionary of
    ///   source-target entry-pairs.
    /// </summary>
    /// <param name="glossaryId">ID of glossary for which to retrieve entries.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns>Dictionary containing source-target entry pairs of the glossary.</returns>
    /// <exception cref="DeepLException">
    ///   If any error occurs, a <see cref="DeepLException" /> or a derived class will be
    ///   thrown.
    /// </exception>
    public async Task<Dictionary<string, string>> GetGlossaryEntriesAsync(
          string glossaryId,
          CancellationToken cancellationToken = default) {
      using var responseMessage = await _client.ApiGetAsync(
            $"/v2/glossaries/{glossaryId}/entries",
            cancellationToken,
            acceptHeader: "text/tab-separated-values").ConfigureAwait(false);

      await DeepLClient.CheckStatusCodeAsync(responseMessage, true).ConfigureAwait(false);
      var content = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
      return GlossaryUtils.ConvertToDictionary(content);
    }

    /// <summary>
    ///   Retrieves the entries containing within the glossary and returns them as a Dictionary of source-target
    ///   entry-pairs.
    /// </summary>
    /// <param name="glossary"><see cref="GlossaryInfo" /> object corresponding to glossary for which to retrieve entries.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns>Dictionary containing source-target entry pairs of the glossary.</returns>
    /// <exception cref="DeepLException">
    ///   If any error occurs, a <see cref="DeepLException" /> or a derived class will be
    ///   thrown.
    /// </exception>
    public async Task<Dictionary<string, string>> GetGlossaryEntriesAsync(
          GlossaryInfo glossary,
          CancellationToken cancellationToken = default) =>
          await GetGlossaryEntriesAsync(glossary.GlossaryId, cancellationToken).ConfigureAwait(false);

    /// <summary>Deletes the glossary with the specified ID.</summary>
    /// <param name="glossaryId">ID of glossary to delete.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <exception cref="DeepLException">
    ///   If any error occurs, a <see cref="DeepLException" /> or a derived class will be
    ///   thrown.
    /// </exception>
    public async Task DeleteGlossaryAsync(
          string glossaryId,
          CancellationToken cancellationToken = default) {
      using var responseMessage =
            await _client.ApiDeleteAsync($"/v2/glossaries/{glossaryId}", cancellationToken)
                  .ConfigureAwait(false);

      await DeepLClient.CheckStatusCodeAsync(responseMessage, true).ConfigureAwait(false);
    }

    /// <summary>Deletes the specified glossary.</summary>
    /// <param name="glossary"><see cref="GlossaryInfo" /> object corresponding to glossary to delete.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <exception cref="DeepLException">
    ///   If any error occurs, a <see cref="DeepLException" /> or a derived class will be
    ///   thrown.
    /// </exception>
    public async Task DeleteGlossaryAsync(
          GlossaryInfo glossary,
          CancellationToken cancellationToken = default) =>
          await DeleteGlossaryAsync(glossary.GlossaryId, cancellationToken).ConfigureAwait(false);

    /// <summary>Internal function to retrieve available languages.</summary>
    /// <param name="target"><c>true</c> to retrieve target languages, <c>false</c> to retrieve source languages.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns>Array of <see cref="Language" /> objects containing information about the available languages.</returns>
    private async Task<TValue[]> GetLanguagesAsync<TValue>(
          bool target,
          CancellationToken cancellationToken = default) {
      var queryParams = new (string key, string value)[] { ("type", target ? "target" : "source") };
      using var responseMessage =
            await _client.ApiGetAsync("/v2/languages", cancellationToken, queryParams)
                  .ConfigureAwait(false);

      await DeepLClient.CheckStatusCodeAsync(responseMessage).ConfigureAwait(false);
      return await JsonUtils.DeserializeAsync<TValue[]>(responseMessage).ConfigureAwait(false);
    }

    /// <summary>
    ///   Checks the specified languages and options are valid, and returns an enumerable of tuples containing the parameters
    ///   to include in HTTP request.
    /// </summary>
    /// <param name="sourceLang">Language code of translation source language, or null if auto-detection should be used.</param>
    /// <param name="targetLang">Language code of translation target language.</param>
    /// <param name="options">Extra <see cref="TextTranslateOptions" /> influencing translation.</param>
    /// <returns>Enumerable of tuples containing the parameters to include in HTTP request.</returns>
    /// <exception cref="DeepLException">If the specified languages, or options are invalid.</exception>
    private IEnumerable<(string key, string value)> CreateHttpParams(
          string? sourceLang,
          string targetLang,
          TextTranslateOptions? options) {
      targetLang = LanguageCode.Standardize(targetLang);
      sourceLang = sourceLang == null ? null : LanguageCode.Standardize(sourceLang);

      CheckValidLanguages(sourceLang, targetLang);

      var bodyParams = new List<(string key, string value)> { ("target_lang", targetLang) };
      if (sourceLang != null) {
        bodyParams.Add(("source_lang", sourceLang));
      }

      if (options == null) {
        return bodyParams;
      }

      if (options.GlossaryId != null) {
        if (sourceLang == null) {
          throw new DeepLException($"{nameof(sourceLang)} is required if using a glossary");
        }

        bodyParams.Add(("glossary_id", options.GlossaryId));
      }

      if (options.Formality != Formality.Default) {
        bodyParams.Add(("formality", options.Formality.ToString().ToLowerInvariant()));
      }

      if (options.SplitSentences != SplitSentences.All) {
        bodyParams.Add(("split_sentences", options.SplitSentences == SplitSentences.Off ? "0" : "nonewlines"));
      }

      if (options.PreserveFormatting) {
        bodyParams.Add(("preserve_formatting", "1"));
      }

      if (options.TagHandling != null) {
        bodyParams.Add(("tag_handling", options.TagHandling));
      }

      if (options.OutlineDetection) {
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
    ///   Checks the specified languages and options are valid, and returns an enumerable of tuples containing the parameters
    ///   to include in HTTP request.
    /// </summary>
    /// <param name="sourceLang">Language code of translation source language, or null if auto-detection should be used.</param>
    /// <param name="targetLang">Language code of translation target language.</param>
    /// <param name="options">Extra <see cref="DocumentTranslateOptions" /> influencing translation.</param>
    /// <returns>Enumerable of tuples containing the parameters to include in HTTP request.</returns>
    /// <exception cref="DeepLException">If the specified languages, or options are invalid.</exception>
    private IEnumerable<(string, string)> CreateHttpParams(
          string? sourceLang,
          string targetLang,
          DocumentTranslateOptions? options) {
      targetLang = LanguageCode.Standardize(targetLang);
      sourceLang = sourceLang == null ? null : LanguageCode.Standardize(sourceLang);

      CheckValidLanguages(sourceLang, targetLang);

      var bodyParams = new List<(string key, string value)> { ("target_lang", targetLang) };
      if (sourceLang != null) {
        bodyParams.Add(("source_lang", sourceLang));
      }

      if (options == null) {
        return bodyParams;
      }

      if (options.Formality != Formality.Default) {
        bodyParams.Add(("formality", options.Formality.ToString().ToLowerInvariant()));
      }

      return bodyParams;
    }

    /// <summary>Checks the specified target language is valid, and throws an exception if not.</summary>
    /// <param name="sourceLang">Language code of translation source language, or null if auto-detection is used.</param>
    /// <param name="targetLang">Language code of translation target language.</param>
    /// <exception cref="DeepLException">If target language code is not valid.</exception>
    private static void CheckValidLanguages(string? sourceLang, string targetLang) {
      if (sourceLang is { Length: 0 }) {
        throw new DeepLException($"{nameof(sourceLang)} must not be empty");
      }

      if (targetLang.Length == 0) {
        throw new DeepLException($"{nameof(targetLang)} must not be empty");
      }

      switch (targetLang) {
        case "en":
          throw new DeepLException(
                $"{nameof(targetLang)}=\"en\" is deprecated, please use \"en-GB\" or \"en-US\" instead");
        case "pt":
          throw new DeepLException(
                $"{nameof(targetLang)}=\"pt\" is deprecated, please use \"pt-PT\" or \"pt-BR\" instead");
      }
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
}
