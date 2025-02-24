// Copyright 2025 DeepL SE (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using DeepL.Internal;
using DeepL.Model;

namespace DeepL {
  public interface IWriter : IDisposable {

    /// <summary>Rephrase specified texts, improving them by fixing grammar and spelling errors.</summary>
    /// <param name="texts">Texts to improve; must not be empty.</param>
    /// <param name="targetLanguageCode">Language code of the desired output language.</param>
    /// <param name="options">Extra <see cref="TextRephraseOptions" /> influencing rephrasing.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns>Texts without grammatical or spelling errors.</returns>
    /// <exception cref="ArgumentException">If any argument is invalid.</exception>
    /// <exception cref="DeepLException">
    ///   If any error occurs while communicating with the DeepL API, a
    ///   <see cref="DeepLException" /> or a derived class will be thrown.
    /// </exception>
    Task<WriteResult[]> RephraseTextAsync(
          IEnumerable<string> texts,
          string? targetLanguageCode,
          TextRephraseOptions? options = null,
          CancellationToken cancellationToken = default);

    /// <summary>Rephrase specified text, improving them by fixing grammar and spelling errors.</summary>
    /// <param name="text">Text to improve; must not be empty.</param>
    /// <param name="targetLanguageCode">Language code of the desired output language.</param>
    /// <param name="options">Extra <see cref="TextRephraseOptions" /> influencing rephrasing.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns>Texts without grammatical or spelling errors.</returns>
    /// <exception cref="ArgumentException">If any argument is invalid.</exception>
    /// <exception cref="DeepLException">
    ///   If any error occurs while communicating with the DeepL API, a
    ///   <see cref="DeepLException" /> or a derived class will be thrown.
    /// </exception>
    Task<WriteResult> RephraseTextAsync(
          string text,
          string? targetLanguageCode,
          TextRephraseOptions? options = null,
          CancellationToken cancellationToken = default);
  }

  /// <summary>
  ///   Client for the DeepL API. To use the DeepL API, initialize an instance of this class using your DeepL
  ///   Authentication Key. All functions are thread-safe, aside from <see cref="DeepLClient.Dispose" />.
  /// </summary>
  public sealed class DeepLClient : Translator, IWriter {
    /// <summary>Initializes a new instance of the <see cref="AuthorizationException" /> class.</summary>
    /// <param name="message">The message that describes the error.</param>
    public DeepLClient(string authKey, DeepLClientOptions? options = null) : base(authKey, options) { }


    /// <inheritdoc />
    public async Task<WriteResult> RephraseTextAsync(
           string text,
           string? targetLanguageCode,
           TextRephraseOptions? options = null,
           CancellationToken cancellationToken = default) => (await RephraseTextAsync(
                   new[] { text },
                   targetLanguageCode,
                   options,
                   cancellationToken)
             .ConfigureAwait(false))[0];

    /// <inheritdoc />
    public async Task<WriteResult[]> RephraseTextAsync(
           IEnumerable<string> texts,
           string? targetLanguageCode,
           TextRephraseOptions? options = null,
           CancellationToken cancellationToken = default) {
      var bodyParams = new List<(string Key, string Value)>();
      if (targetLanguageCode != null) {
        CheckValidLanguages(null, targetLanguageCode);
        bodyParams.Add(("target_lang", targetLanguageCode));
      }
      var textParams = texts
            .Where(text => text.Length > 0 ? true : throw new ArgumentException("text must not be empty"))
            .Select(text => ("text", text));
      if (options != null && options.WritingStyle != null) {
        bodyParams.Add(("writing_style", options.WritingStyle));
      }
      if (options != null && options.WritingTone != null) {
        bodyParams.Add(("tone", options.WritingTone));
      }
      // TODO add `show_billed_characters` once write API supports it.

      using var responseMessage = await _client
            .ApiPostAsync("/v2/write/rephrase", cancellationToken, bodyParams.Concat(textParams))
            .ConfigureAwait(false);

      await DeepLHttpClient.CheckStatusCodeAsync(responseMessage).ConfigureAwait(false);
      var rephrasedTexts =
            await JsonUtils.DeserializeAsync<TextRephraseResult>(responseMessage).ConfigureAwait(false);
      return rephrasedTexts.Improvements;
    }

    /// <summary>Class used for JSON-deserialization of text rephrase results.</summary>
    private readonly struct TextRephraseResult {
      /// <summary>Initializes a new instance of <see cref="TextRephraseResult" />, used for JSON deserialization.</summary>
      [JsonConstructor]
      public TextRephraseResult(WriteResult[] improvements) {
        Improvements = improvements;
      }

      /// <summary>Array of <see cref="WriteResult" /> objects holding text rephrase results.</summary>
      public WriteResult[] Improvements { get; }
    }
  }
}
