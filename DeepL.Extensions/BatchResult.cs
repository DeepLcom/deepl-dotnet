// Copyright 2022 DeepL SE (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

using DeepL.Model;

namespace DeepL.Extensions;

public class BatchResult {

  public string TargetLanguage { get; set; }
  public bool Completed { get; set; } = false;
  public string Error { get; set; } = string.Empty;
  public TextResult[] Results { get; set; } = new TextResult[0];
  public int Cost { get; set; }
}
