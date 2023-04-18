// Copyright 2022 DeepL SE (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

using DeepL.Service;
using Microsoft.AspNetCore.Mvc;

namespace ASP.NET.Controllers;

public class TranslateController : Controller {

  private readonly DeepLService deepl;

  public TranslateController(DeepLService deepLService) {
    this.deepl = deepLService;
  }

  [HttpGet]
  public async Task<IActionResult> Index(string translateMe, string targetLanguage) {
    var result = await deepl.client.TranslateTextAsync(translateMe, string.Empty, targetLanguage);
    return Ok(result.Text);
  }
}
