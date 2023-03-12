// Copyright 2022 DeepL SE (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

using DeepL;
using DeepL.Model;
using Microsoft.AspNetCore.Mvc;

namespace ApiTest.Controllers;
[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase {
  private readonly ITranslator _translatr;

  public WeatherForecastController(ITranslator translatr) {
    _translatr = translatr;
  }

  [HttpGet]
  public async Task<Usage> Test() => await _translatr.GetUsageAsync();
}
