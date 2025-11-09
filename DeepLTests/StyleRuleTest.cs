// Copyright 2025 DeepL SE (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

using System.Threading.Tasks;
using DeepL;
using Xunit;

namespace DeepLTests {
  public sealed class StyleRuleTest : BaseDeepLTest {
    private const string DefaultStyleId = "dca2e053-8ae5-45e6-a0d2-881156e7f4e4";

    [MockServerOnlyFact]
    public async Task TestGetAllStyleRules() {
      var client = CreateTestClient();
      var styleRules = await client.GetAllStyleRulesAsync(0, 10, true);
      Assert.NotNull(styleRules);
      Assert.True(styleRules.Length > 0);
      Assert.Equal(DefaultStyleId, styleRules[0].StyleId);
      Assert.Equal("Default Style Rule", styleRules[0].Name);
      Assert.Equal("en", styleRules[0].Language);
      Assert.Equal(1, styleRules[0].Version);
      Assert.NotNull(styleRules[0].ConfiguredRules);
      Assert.NotNull(styleRules[0].CustomInstructions);
    }

    [MockServerOnlyFact]
    public async Task TestGetAllStyleRulesWithoutDetailed() {
      var client = CreateTestClient();
      var styleRules = await client.GetAllStyleRulesAsync();

      Assert.NotNull(styleRules);
      Assert.True(styleRules.Length > 0);
      Assert.Equal(DefaultStyleId, styleRules[0].StyleId);
      Assert.Null(styleRules[0].ConfiguredRules);
      Assert.Null(styleRules[0].CustomInstructions);
    }

    [MockServerOnlyFact]
    public async Task TestTranslateTextWithStyleId() {
      // Note: this test may use the mock server that will not translate the text,
      // therefore we do not check the translated result.
      var client = CreateTestClient();
      const string exampleText = "Hallo, Welt!";

      var result = await client.TranslateTextAsync(
            exampleText,
            "de",
            "en-US",
            new TextTranslateOptions { StyleId = DefaultStyleId });

      Assert.NotNull(result);
    }

    [MockServerOnlyFact]
    public async Task TestTranslateTextWithStyleRuleInfo() {
      var client = CreateTestClient();
      var styleRules = await client.GetAllStyleRulesAsync();
      var styleRule = styleRules[0];
      const string exampleText = "Hallo, Welt!";

      var result = await client.TranslateTextAsync(
            exampleText,
            "de",
            "en-US",
            new TextTranslateOptions(styleRule));

      Assert.NotNull(result);
    }
  }
}
