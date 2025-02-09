// Copyright 2022 DeepL SE (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

namespace DeepL.Extensions.BatchTranslate;

public class BatchTranslateTargetLanguage {
  public string TargetLanguage { get; set; } = string.Empty;
  public TextTranslateOptions Options { get; set; } = new TextTranslateOptions();
}
