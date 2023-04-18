// Copyright 2022 DeepL SE (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DeepL.Model;
using DeepL.Model.Exceptions;
using DeepL.Model.Options;

namespace DeepL.Model.Interfaces {
  public interface ITranslator : IDisposable {

    /// <summary>Retrieves the usage in the current billing period for this DeepL account.</summary>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns><see cref="Usage" /> object containing account usage information.</returns>
    /// <exception cref="DeepLException">
    ///   If any error occurs while communicating with the DeepL API, a
    ///   <see cref="DeepLException" /> or a derived class will be thrown.
    /// </exception>
    Task<Usage> GetUsageAsync(CancellationToken cancellationToken = default);

    /// <summary>Translate specified texts from source language into target language.</summary>
    /// <param name="texts">Texts to translate; must not be empty.</param>
    /// <param name="sourceLanguageCode">Language code of the input language, or null to use auto-detection.</param>
    /// <param name="targetLanguageCode">Language code of the desired output language.</param>
    /// <param name="options">Extra <see cref="TextTranslateOptions" /> influencing translation.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns>Texts translated into specified target language.</returns>
    /// <exception cref="ArgumentException">If any argument is invalid.</exception>
    /// <exception cref="DeepLException">
    ///   If any error occurs while communicating with the DeepL API, a
    ///   <see cref="DeepLException" /> or a derived class will be thrown.
    /// </exception>
    Task<TextResult[]> TranslateTextAsync(
          IEnumerable<string> texts,
          string? sourceLanguageCode,
          string targetLanguageCode,
          TextTranslateOptions? options = null,
          CancellationToken cancellationToken = default);

    /// <summary>Translate specified text from source language into target language.</summary>
    /// <param name="text">Text to translate; must not be empty.</param>
    /// <param name="sourceLanguageCode">Language code of the input language, or null to use auto-detection.</param>
    /// <param name="targetLanguageCode">Language code of the desired output language.</param>
    /// <param name="options"><see cref="TextTranslateOptions" /> influencing translation.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns>Text translated into specified target language.</returns>
    /// <exception cref="ArgumentException">If any argument is invalid.</exception>
    /// <exception cref="DeepLException">
    ///   If any error occurs while communicating with the DeepL API, a
    ///   <see cref="DeepLException" /> or a derived class will be thrown.
    /// </exception>
    Task<TextResult> TranslateTextAsync(
          string text,
          string? sourceLanguageCode,
          string targetLanguageCode,
          TextTranslateOptions? options = null,
          CancellationToken cancellationToken = default);

    /// <summary>
    ///   Translate document at specified input <see cref="FileInfo" /> from source language to target language and
    ///   store the translated document at specified output <see cref="FileInfo" />.
    /// </summary>
    /// <param name="inputFileInfo"><see cref="FileInfo" /> object containing path to input document.</param>
    /// <param name="outputFileInfo"><see cref="FileInfo" /> object containing path to store translated document.</param>
    /// <param name="sourceLanguageCode">Language code of the input language, or null to use auto-detection.</param>
    /// <param name="targetLanguageCode">Language code of the desired output language.</param>
    /// <param name="options"><see cref="DocumentTranslateOptions" /> influencing translation.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <exception cref="IOException">If the output path is occupied.</exception>
    /// <exception cref="DocumentTranslationException">
    ///   If cancellation was requested, or an error occurs while translating the document. If the document was uploaded
    ///   successfully, then the <see cref="DocumentTranslationException.DocumentHandle" /> contains the document ID and
    ///   key that may be used to retrieve the document.
    ///   If cancellation was requested, the <see cref="DocumentTranslationException.InnerException" /> will be a
    ///   <see cref="TaskCanceledException" />.
    /// </exception>
    Task TranslateDocumentAsync(
          FileInfo inputFileInfo,
          FileInfo outputFileInfo,
          string? sourceLanguageCode,
          string targetLanguageCode,
          DocumentTranslateOptions? options = null,
          CancellationToken cancellationToken = default);

    /// <summary>
    ///   Translate specified document content from source language to target language and store the translated document
    ///   content to specified stream.
    /// </summary>
    /// <param name="inputFile"><see cref="Stream" /> containing input document content.</param>
    /// <param name="inputFileName">Name of the input file. The file extension is used to determine file type.</param>
    /// <param name="outputFile"><see cref="Stream" /> to store translated document content.</param>
    /// <param name="sourceLanguageCode">Language code of the input language, or null to use auto-detection.</param>
    /// <param name="targetLanguageCode">Language code of the desired output language.</param>
    /// <param name="options"><see cref="DocumentTranslateOptions" /> influencing translation.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <exception cref="DocumentTranslationException">
    ///   If cancellation was requested, or an error occurs while translating the document. If the document was uploaded
    ///   successfully, then the <see cref="DocumentTranslationException.DocumentHandle" /> contains the document ID and
    ///   key that may be used to retrieve the document.
    ///   If cancellation was requested, the <see cref="DocumentTranslationException.InnerException" /> will be a
    ///   <see cref="TaskCanceledException" />.
    /// </exception>
    Task TranslateDocumentAsync(
          Stream inputFile,
          string inputFileName,
          Stream outputFile,
          string? sourceLanguageCode,
          string targetLanguageCode,
          DocumentTranslateOptions? options = null,
          CancellationToken cancellationToken = default);

    /// <summary>
    ///   Upload document at specified input <see cref="FileInfo" /> for translation from source language to target
    ///   language.
    /// </summary>
    /// <param name="inputFileInfo"><see cref="FileInfo" /> object containing path to input document.</param>
    /// <param name="sourceLanguageCode">Language code of the input language, or null to use auto-detection.</param>
    /// <param name="targetLanguageCode">Language code of the desired output language.</param>
    /// <param name="options"><see cref="DocumentTranslateOptions" /> influencing translation.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns><see cref="DocumentHandle" /> object associated with the in-progress document translation.</returns>
    /// <exception cref="ArgumentException">If any argument is invalid.</exception>
    /// <exception cref="DeepLException">
    ///   If any error occurs while communicating with the DeepL API, a
    ///   <see cref="DeepLException" /> or a derived class will be thrown.
    /// </exception>
    Task<DocumentHandle> TranslateDocumentUploadAsync(
          FileInfo inputFileInfo,
          string? sourceLanguageCode,
          string targetLanguageCode,
          DocumentTranslateOptions? options = null,
          CancellationToken cancellationToken = default);

    /// <summary>Upload document content with specified filename for translation from source language to target language.</summary>
    /// <param name="inputFile"><see cref="Stream" /> containing input document content.</param>
    /// <param name="inputFileName">Name of the input file. The file extension is used to determine file type.</param>
    /// <param name="sourceLanguageCode">Language code of the input language, or null to use auto-detection.</param>
    /// <param name="targetLanguageCode">Language code of the desired output language.</param>
    /// <param name="options"><see cref="DocumentTranslateOptions" /> influencing translation.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns><see cref="DocumentHandle" /> object associated with the in-progress document translation.</returns>
    /// <exception cref="ArgumentException">If any argument is invalid.</exception>
    /// <exception cref="DeepLException">
    ///   If any error occurs while communicating with the DeepL API, a
    ///   <see cref="DeepLException" /> or a derived class will be thrown.
    /// </exception>
    Task<DocumentHandle> TranslateDocumentUploadAsync(
          Stream inputFile,
          string inputFileName,
          string? sourceLanguageCode,
          string targetLanguageCode,
          DocumentTranslateOptions? options = null,
          CancellationToken cancellationToken = default);

    /// <summary>
    ///   Retrieve the status of in-progress document translation associated with specified
    ///   <see cref="DocumentHandle" />.
    /// </summary>
    /// <param name="handle"><see cref="DocumentHandle" /> associated with an in-progress document translation.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns><see cref="DocumentStatus" /> object containing the document translation status.</returns>
    /// <exception cref="DeepLException">
    ///   If any error occurs while communicating with the DeepL API, a
    ///   <see cref="DeepLException" /> or a derived class will be thrown.
    /// </exception>
    Task<DocumentStatus> TranslateDocumentStatusAsync(
          DocumentHandle handle,
          CancellationToken cancellationToken = default);

    /// <summary>
    ///   Checks document translation status and asynchronously-waits until document translation is complete or fails
    ///   due to an error.
    /// </summary>
    /// <param name="handle"><see cref="DocumentHandle" /> associated with an in-progress document translation.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <exception cref="DeepLException">
    ///   If any error occurs while communicating with the DeepL API, a
    ///   <see cref="DeepLException" /> or a derived class will be thrown.
    /// </exception>
    Task TranslateDocumentWaitUntilDoneAsync(
          DocumentHandle handle,
          CancellationToken cancellationToken = default);

    /// <summary>
    ///   Downloads the resulting translated document associated with specified <see cref="DocumentHandle" /> to the
    ///   specified <see cref="FileInfo" />. The <see cref="DocumentStatus" /> for the document must have
    ///   <see cref="DocumentStatus.Done" /> == <c>true</c>.
    /// </summary>
    /// <param name="handle"><see cref="DocumentHandle" /> associated with an in-progress document translation.</param>
    /// <param name="outputFileInfo"><see cref="FileInfo" /> object containing path to store translated document.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <exception cref="IOException">If the output path is occupied.</exception>
    /// <exception cref="DocumentNotReadyException">
    ///   If the document is not ready to be downloaded, that is, the document status
    ///   is not <see cref="DocumentStatus.Done" />.
    /// </exception>
    /// <exception cref="DeepLException">
    ///   If any other error occurs while communicating with the DeepL API, a
    ///   <see cref="DeepLException" /> or a derived class will be thrown.
    /// </exception>
    Task TranslateDocumentDownloadAsync(
          DocumentHandle handle,
          FileInfo outputFileInfo,
          CancellationToken cancellationToken = default);

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
    ///   If any other error occurs while communicating with the DeepL API, a
    ///   <see cref="DeepLException" /> or a derived class will be thrown.
    /// </exception>
    Task TranslateDocumentDownloadAsync(
          DocumentHandle handle,
          Stream outputFile,
          CancellationToken cancellationToken = default);

    /// <summary>Retrieves the list of supported translation source languages.</summary>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns>Array of <see cref="Language" /> objects representing the available translation source languages.</returns>
    /// <exception cref="DeepLException">
    ///   If any error occurs while communicating with the DeepL API, a
    ///   <see cref="DeepLException" /> or a derived class will be thrown.
    /// </exception>
    Task<SourceLanguage[]> GetSourceLanguagesAsync(CancellationToken cancellationToken = default);

    /// <summary>Retrieves the list of supported translation target languages.</summary>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns>Array of <see cref="Language" /> objects representing the available translation target languages.</returns>
    /// <exception cref="DeepLException">
    ///   If any error occurs while communicating with the DeepL API, a
    ///   <see cref="DeepLException" /> or a derived class will be thrown.
    /// </exception>
    Task<TargetLanguage[]> GetTargetLanguagesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///   Retrieves the list of supported glossary language pairs. When creating glossaries, the source and target
    ///   language pair must match one of the available language pairs.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns>Array of <see cref="GlossaryLanguagePair" /> objects representing the available glossary language pairs.</returns>
    /// <exception cref="DeepLException">
    ///   If any error occurs while communicating with the DeepL API, a
    ///   <see cref="DeepLException" /> or a derived class will be thrown.
    /// </exception>
    Task<GlossaryLanguagePair[]> GetGlossaryLanguagesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///   Creates a glossary in your DeepL account with the specified details and returns a <see cref="GlossaryInfo" />
    ///   object with details about the newly created glossary. The glossary can be used in translations to override
    ///   translations for specific terms (words). The glossary source and target languages must match the languages of
    ///   translations for which it will be used.
    /// </summary>
    /// <param name="name">User-defined name to assign to the glossary; must not be empty.</param>
    /// <param name="sourceLanguageCode">Language code of the source terms language.</param>
    /// <param name="targetLanguageCode">Language code of the target terms language.</param>
    /// <param name="entries">Glossary entry list.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns><see cref="GlossaryInfo" /> object with details about the newly created glossary.</returns>
    /// <exception cref="ArgumentException">If any argument is invalid.</exception>
    /// <exception cref="DeepLException">
    ///   If any error occurs while communicating with the DeepL API, a
    ///   <see cref="DeepLException" /> or a derived class will be thrown.
    /// </exception>
    Task<GlossaryInfo> CreateGlossaryAsync(
          string name,
          string sourceLanguageCode,
          string targetLanguageCode,
          GlossaryEntries entries,
          CancellationToken cancellationToken = default);

    /// <summary>
    ///   Creates a glossary in your DeepL account with the specified details and returns a <see cref="GlossaryInfo" />
    ///   object with details about the newly created glossary. The glossary can be used in translations to override
    ///   translations for specific terms (words). The glossary source and target languages must match the languages of
    ///   translations for which it will be used.
    /// </summary>
    /// <param name="name">User-defined name to assign to the glossary; must not be empty.</param>
    /// <param name="sourceLanguageCode">Language code of the source terms language.</param>
    /// <param name="targetLanguageCode">Language code of the target terms language.</param>
    /// <param name="csvFile"><see cref="Stream" /> containing CSV content.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns><see cref="GlossaryInfo" /> object with details about the newly created glossary.</returns>
    /// <exception cref="ArgumentException">If any argument is invalid.</exception>
    /// <exception cref="DeepLException">
    ///   If any error occurs while communicating with the DeepL API, a
    ///   <see cref="DeepLException" /> or a derived class will be thrown.
    /// </exception>
    Task<GlossaryInfo> CreateGlossaryFromCsvAsync(
          string name,
          string sourceLanguageCode,
          string targetLanguageCode,
          Stream csvFile,
          CancellationToken cancellationToken = default);

    /// <summary>
    ///   Retrieves information about the glossary with the specified ID and returns a <see cref="GlossaryInfo" />
    ///   object containing details. This does not retrieve the glossary entries; to retrieve entries use
    ///   <see cref="ITranslator.GetGlossaryEntriesAsync(string,CancellationToken)" />
    /// </summary>
    /// <param name="glossaryId">ID of glossary to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns><see cref="GlossaryInfo" /> object with details about the specified glossary.</returns>
    /// <exception cref="DeepLException">
    ///   If any error occurs while communicating with the DeepL API, a
    ///   <see cref="DeepLException" /> or a derived class will be thrown.
    /// </exception>
    Task<GlossaryInfo> GetGlossaryAsync(
          string glossaryId,
          CancellationToken cancellationToken = default);

    /// <summary>Asynchronously waits until the given glossary is ready to be used for translations.</summary>
    /// <param name="glossaryId">ID of glossary to ensure ready.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <exception cref="DeepLException">
    ///   If any error occurs while communicating with the DeepL API, a
    ///   <see cref="DeepLException" /> or a derived class will be thrown.
    /// </exception>
    Task<GlossaryInfo> WaitUntilGlossaryReadyAsync(
          string glossaryId,
          CancellationToken cancellationToken = default);

    /// <summary>
    ///   Retrieves information about all glossaries and returns an array of <see cref="GlossaryInfo" /> objects
    ///   containing details. This does not retrieve the glossary entries; to retrieve entries use
    ///   <see cref="ITranslator.GetGlossaryEntriesAsync(string,CancellationToken)" />
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns>Array of <see cref="GlossaryInfo" /> objects with details about each glossary.</returns>
    /// <exception cref="DeepLException">
    ///   If any error occurs while communicating with the DeepL API, a
    ///   <see cref="DeepLException" /> or a derived class will be thrown.
    /// </exception>
    Task<GlossaryInfo[]> ListGlossariesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///   Retrieves the entries containing within the glossary with the specified ID and returns them as a
    ///   <see cref="GlossaryEntries" />.
    /// </summary>
    /// <param name="glossaryId">ID of glossary for which to retrieve entries.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns><see cref="GlossaryEntries" /> containing entry pairs of the glossary.</returns>
    /// <exception cref="DeepLException">
    ///   If any error occurs while communicating with the DeepL API, a
    ///   <see cref="DeepLException" /> or a derived class will be thrown.
    /// </exception>
    Task<GlossaryEntries> GetGlossaryEntriesAsync(
          string glossaryId,
          CancellationToken cancellationToken = default);

    /// <summary>
    ///   Retrieves the entries containing within the glossary and returns them as a <see cref="GlossaryEntries" />.
    /// </summary>
    /// <param name="glossary"><see cref="GlossaryInfo" /> object corresponding to glossary for which to retrieve entries.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns><see cref="GlossaryEntries" /> containing entry pairs of the glossary.</returns>
    /// <exception cref="DeepLException">
    ///   If any error occurs while communicating with the DeepL API, a
    ///   <see cref="DeepLException" /> or a derived class will be thrown.
    /// </exception>
    Task<GlossaryEntries> GetGlossaryEntriesAsync(
          GlossaryInfo glossary,
          CancellationToken cancellationToken = default);

    /// <summary>Deletes the glossary with the specified ID.</summary>
    /// <param name="glossaryId">ID of glossary to delete.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <exception cref="DeepLException">
    ///   If any error occurs while communicating with the DeepL API, a
    ///   <see cref="DeepLException" /> or a derived class will be thrown.
    /// </exception>
    Task DeleteGlossaryAsync(
          string glossaryId,
          CancellationToken cancellationToken = default);

    /// <summary>Deletes the specified glossary.</summary>
    /// <param name="glossary"><see cref="GlossaryInfo" /> object corresponding to glossary to delete.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <exception cref="DeepLException">
    ///   If any error occurs while communicating with the DeepL API, a
    ///   <see cref="DeepLException" /> or a derived class will be thrown.
    /// </exception>
    Task DeleteGlossaryAsync(
          GlossaryInfo glossary,
          CancellationToken cancellationToken = default);
  }
}
