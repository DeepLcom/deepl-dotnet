// Copyright 2022 DeepL SE (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace DeepL.Extensions;

public static class TranslatorExtensions {


  /// <summary>
  /// Translate into multiple target languages at the same time.
  /// This creates a single API Request for each target language.
  /// Each request is handled seperately and the results are returned in a list.
  /// NOTE: This consumes API credits for each target language.
  /// </summary>
  /// <param name="translator"></param>
  /// <param name="texts"></param>
  /// <param name="sourceLanguage"></param>
  /// <param name="targetLanguage"></param>
  /// <returns cref="List<BatchResult>">A list of batch result entries</returns>
  public static async Task<List<BatchResult>> TranslateBatchAsync(this Translator translator,
    string[] texts,
    string sourceLanguage,
    BatchTranslateTargetLanguage[] targetLanguage,
    bool handleInParallel = true,
    int maxParallelRequests = 10,
    CancellationToken cancellationToken = new CancellationToken()) {

    List<BatchResult> results = new List<BatchResult>();

    if (handleInParallel) {

      await Parallel.ForEachAsync(targetLanguage, new ParallelOptions { MaxDegreeOfParallelism = maxParallelRequests }, async (target, token) => { 
        try {
          var result = await translator.TranslateTextAsync(texts,
            sourceLanguage,
            target.TargetLanguage,
            target.Options,
            cancellationToken
            );
          results.Add(
            new BatchResult {
              TargetLanguage = target.TargetLanguage,
              Completed = true,
              Results = result,
              Cost = result.Sum(r => r.BilledCharacters)
            });
        } catch (Exception ex) {
          results.Add(
              new BatchResult {
                TargetLanguage = target.TargetLanguage,
                Completed = false,
                Error = ex.Message }
              );
        }
      });
    } else {
      foreach (var target in targetLanguage) {
        try {
          var result = await translator.TranslateTextAsync(texts,
            sourceLanguage,
            target.TargetLanguage,
            target.Options
          );
          results.Add(
            new BatchResult {
              TargetLanguage = target.TargetLanguage,
              Completed = true,
              Results = result,
              Cost = result.Sum(r => r.BilledCharacters)
            });
        } catch (Exception ex) {
          results.Add(
            new BatchResult {
              TargetLanguage = target.TargetLanguage,
              Completed = false,
              Error = ex.Message
            });
        }
      }
    }
    return results;
  }

}

public class BatchTranslateTargetLanguage {
  public string TargetLanguage { get; set; } = string.Empty;
  public TextTranslateOptions Options { get; set; } = new TextTranslateOptions();
}
