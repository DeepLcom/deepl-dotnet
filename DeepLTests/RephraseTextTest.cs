// Copyright 2025 DeepL SE (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

using System.Threading.Tasks;
using DeepL;
using DeepL.Model;
using Xunit;

namespace DeepLTests {
  public sealed class RephraseTextTest : BaseDeepLTest {
    [Fact]
    public async Task TestSingleText() {
      var client = CreateTestClient();
      var inputText = ExampleText("en");
      var result = await client.RephraseTextAsync(ExampleText("en"), LanguageCode.EnglishAmerican);
      checkSanityOfImprovements(inputText, result);
    }

    [Fact]
    public async Task TestTextArray() {
      var client = CreateTestClient();
      var texts = new[] { ExampleText("en"), ExampleText("en") };
      var inputText = ExampleText("en");
      var results = await client.RephraseTextAsync(texts, LanguageCode.EnglishAmerican);
      foreach (var result in results) {
        checkSanityOfImprovements(inputText, result);
      }
    }

    [RealServerOnlyFact]
    public async Task TestBusinessStyle() {
      var client = CreateTestClient();
      var inputText = "As Gregor Samsa awoke one morning from uneasy dreams he found himself transformed in his bed into a gigantic insect.";
      var result = await client.RephraseTextAsync(
        inputText, LanguageCode.EnglishAmerican, new TextRephraseOptions { WritingStyle = "business" }
      );
      checkSanityOfImprovements(inputText, result);
    }

    private void checkSanityOfImprovements(
        string inputText,
        WriteResult result,
        string expectedSourceLangUppercase="EN",
        string expectedTargetLangUppercase="EN-US",
        float epsilon=0.2f) {
      Assert.Equal(expectedSourceLangUppercase, result.DetectedSourceLanguageCode.ToUpper());
      Assert.Equal(expectedTargetLangUppercase, result.TargetLanguageCode.ToUpper());
      var ratio = ((float) result.Text.Length) / inputText.Length;
      Assert.True(1 / (1.0 + epsilon) <= ratio, $"Rephrased text is too short compared to input text.\n{inputText}\n{result.Text}");
      Assert.True(ratio <= (1.0 + epsilon), $"Rephrased text is too long compared to input text.\n{inputText}\n{result.Text}");
    }
  }
}
