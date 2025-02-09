// Copyright 2022 DeepL SE (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

using DeepL.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeepL.Extensions;

public class QuotaException : DeepLException {
  public QuotaException(string message) : base(message) {

  }
}

public static class SafeGuardTranslateExtensions {


  /// <summary>
  /// Translate text while making sure that the quota is not exceeded.
  /// This call will check the available quota before making the translation request,
  /// if the remaining quote given the quota limit is less than the number of characters to be translated,
  /// this call will fail with a QuotaException.
  /// This is useful to set custom limits for specific translations. 
  /// </summary>
  /// <param name="translator"></param>
  /// <param name="texts"></param>
  /// <param name="sourceLanguage"></param>
  /// <param name="targetLanguage"></param>
  /// <param name="options"></param>
  /// <param name="quotaLimit"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public static async Task<TextResult[]> TranslateAsyncWithSafeGuard(
    this Translator translator,
    string[] texts,
    string sourceLanguage,
    string targetLanguage,
    TextTranslateOptions options,
    int quotaLimit,
    CancellationToken cancellationToken = new CancellationToken()) {

    var quote = await translator.GetUsageAsync(cancellationToken);
    if (quote.Character != null && quote.Character.Count + texts.Sum(t => t.Length) <= quotaLimit) {
      return await translator.TranslateTextAsync(texts, sourceLanguage, targetLanguage, options, cancellationToken);
    }

    throw new QuotaException($"Quota exceeded, current usage is {quote.Character.Count} and limit is {quotaLimit}");
  }


  /// <summary>
  /// Translate a document while making sure that the quota is not exceeded.
  /// DocumentLimit sets a unique limit for this function, if the limit is exceeded a QuotaException is thrown.
  /// THis can be useful to prioritize documents and only allow documents to be translated if the quota is available.
  /// </summary>
  /// <param name="translator"></param>
  /// <param name="inputFileInfo"></param>
  /// <param name="outputFileInfo"></param>
  /// <param name="sourceLanguageCode"></param>
  /// <param name="targetLanguageCode"></param>
  /// <param name="documentLimit"></param>
  /// <param name="options"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <exception cref="QuotaException"></exception>
  public static async Task TranslateDocumentAsync(
          this Translator translator,
          FileInfo inputFileInfo,
          FileInfo outputFileInfo,
          string? sourceLanguageCode,
          string targetLanguageCode,
          int documentLimit,
          DocumentTranslateOptions? options = null,
          CancellationToken cancellationToken = default) {

    var quote = await translator.GetUsageAsync(cancellationToken);
    if (quote.Character != null && quote.Document.Count + 1 <= documentLimit) {
      await translator.TranslateDocumentAsync(inputFileInfo, outputFileInfo, sourceLanguageCode, targetLanguageCode, options, cancellationToken);
    }

    throw new QuotaException($"Quota exceeded, current usage is {quote.Document.Count} and limit is {documentLimit}");


  }

}

