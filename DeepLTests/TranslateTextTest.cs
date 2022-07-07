// Copyright 2022 DeepL SE (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DeepL;
using DeepL.Model;
using Xunit;

namespace DeepLTests {
  public sealed class TranslateTextTest : BaseDeepLTest {
    [Fact]
    public async Task TestSingleText() {
      var translator = CreateTestTranslator();
      var result = await translator.TranslateTextAsync(ExampleText("en"), null, LanguageCode.German);
      Assert.Equal(ExampleText("de"), result.Text);
      Assert.Equal("en", result.DetectedSourceLanguageCode);
      Assert.Equal(ExampleText("de"), $"{result}");
    }

    [Fact]
    public async Task TestTextArray() {
      var translator = CreateTestTranslator();
      var texts = new[] { ExampleText("fr"), ExampleText("en") };
      var result = await translator.TranslateTextAsync(texts, null, "DE");
      Assert.Equal(ExampleText("de"), result[0].Text);
      Assert.Equal(ExampleText("de"), result[1].Text);
    }

    [Fact]
    public async Task TestSourceLang() {
      void CheckResult(TextResult result) {
        Assert.Equal(ExampleText("de"), result.Text);
        Assert.Equal("en", result.DetectedSourceLanguageCode);
      }

      var translator = CreateTestTranslator();
      CheckResult(await translator.TranslateTextAsync(ExampleText("en"), null, "DE"));
      CheckResult(await translator.TranslateTextAsync(ExampleText("en"), "En", "DE"));
      CheckResult(await translator.TranslateTextAsync(ExampleText("en"), "en", "DE"));

      var sourceLanguages = await translator.GetSourceLanguagesAsync();
      var sourceLanguageEn = sourceLanguages.First(language => language.Code == "en");
      var sourceLanguageDe = sourceLanguages.First(language => language.Code == "de");

      CheckResult(await translator.TranslateTextAsync(ExampleText("en"), sourceLanguageEn, sourceLanguageDe));
    }

    [Fact]
    public async Task TestTargetLang() {
      void CheckResult(TextResult result) {
        Assert.Equal(ExampleText("de"), result.Text);
        Assert.Equal("en", result.DetectedSourceLanguageCode);
      }

      var translator = CreateTestTranslator();
      CheckResult(await translator.TranslateTextAsync(ExampleText("en"), null, "De"));
      CheckResult(await translator.TranslateTextAsync(ExampleText("en"), null, "de"));
      CheckResult(await translator.TranslateTextAsync(ExampleText("en"), null, "DE"));

      var targetLanguages = await translator.GetTargetLanguagesAsync();
      var targetLanguageDe = targetLanguages.First(language => language.Code == "de");

      CheckResult(await translator.TranslateTextAsync(ExampleText("en"), null, targetLanguageDe));

      // Check that EN and PT as target languages throw an exception
      await Assert.ThrowsAsync<ArgumentException>(() => translator.TranslateTextAsync(ExampleText("de"), null, "EN"));
      await Assert.ThrowsAsync<ArgumentException>(() => translator.TranslateTextAsync(ExampleText("de"), null, "PT"));
    }

    [Fact]
    public async Task TestInvalidLanguage() {
      var translator = CreateTestTranslator();
      var exception = await Assert.ThrowsAsync<DeepLException>(
            () =>
                  translator.TranslateTextAsync(ExampleText("en"), null, "XX"));
      Assert.Contains("target_lang", exception.Message);

      exception = await Assert.ThrowsAsync<DeepLException>(
            () =>
                  translator.TranslateTextAsync(ExampleText("en"), "XX", "DE"));
      Assert.Contains("source_lang", exception.Message);
    }

    [MockServerOnlyFact]
    public async Task TestTranslateWithRetries() {
      var translator = CreateTestTranslatorWithMockSession(
            nameof(TestTranslateWithRetries),
            new SessionOptions { RespondWith429 = 2 });

      var timeBefore = DateTime.Now;
      var result = await translator.TranslateTextAsync(new[] { ExampleText("en"), ExampleText("ja") }, null, "DE");
      var timeAfter = DateTime.Now;
      Assert.Equal(2, result.Length);
      Assert.Equal(ExampleText("de"), result[0].Text);
      Assert.Equal("en", result[0].DetectedSourceLanguageCode);
      Assert.Equal(ExampleText("de"), result[1].Text);
      Assert.Equal("ja", result[1].DetectedSourceLanguageCode);
      Assert.True(timeAfter - timeBefore > TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task TestFormality() {
      var translator = CreateTestTranslator();
      TextResult result;
      result = await translator.TranslateTextAsync(
            "How are you?",
            null,
            "DE",
            new TextTranslateOptions { Formality = Formality.Less });
      if (!IsMockServer) {
        Assert.Equal("Wie geht es dir?", result.Text);
      }

      result = await translator.TranslateTextAsync(
            "How are you?",
            null,
            "DE",
            new TextTranslateOptions { Formality = Formality.Default });
      if (!IsMockServer) {
        Assert.Equal("Wie geht es Ihnen?", result.Text);
      }

      result = await translator.TranslateTextAsync(
            "How are you?",
            null,
            "DE",
            new TextTranslateOptions { Formality = Formality.More });
      if (!IsMockServer) {
        Assert.Equal("Wie geht es Ihnen?", result.Text);
      }
    }

    [Fact]
    public async Task TestSplitSentences() {
      var translator = CreateTestTranslator();
      const string text = "If the implementation is hard to explain, it's a bad idea.\n" +
                          "If the implementation is easy to explain, it may be a good idea.";

      await translator.TranslateTextAsync(
            text,
            null,
            "DE",
            new TextTranslateOptions { SentenceSplittingMode = SentenceSplittingMode.Off });
      await translator.TranslateTextAsync(
            text,
            null,
            "DE",
            new TextTranslateOptions { SentenceSplittingMode = SentenceSplittingMode.All });
      await translator.TranslateTextAsync(
            text,
            null,
            "DE",
            new TextTranslateOptions { SentenceSplittingMode = SentenceSplittingMode.NoNewlines });
    }

    [MockServerOnlyFact]
    public async Task TestPreserveFormatting() {
      var translator = CreateTestTranslator();
      const string text = "Test";
      await translator.TranslateTextAsync(text, null, "DE", new TextTranslateOptions { PreserveFormatting = false });
      await translator.TranslateTextAsync(text, null, "DE", new TextTranslateOptions { PreserveFormatting = true });
    }

    [Fact]
    public async Task TestTagHandlingBasic() {
      const string text = "<!DOCTYPE html>" +
                          "<html>" +
                          "<body>" +
                          "<p>This is an example sentence.</p>" +
                          "</body>" +
                          "</html>";
      var translator = CreateTestTranslator();
      // Note: this test may use the mock server that will not translate the text, therefore we do not check the
      // translated result.
      await translator.TranslateTextAsync(
            text,
            null,
            "DE",
            new TextTranslateOptions { TagHandling = "xml" });
      await translator.TranslateTextAsync(
            text,
            null,
            "DE",
            new TextTranslateOptions { TagHandling = "html" });
    }

    [RealServerOnlyFact]
    public async Task TestTagHandlingXml() {
      var translator = CreateTestTranslator();
      const string text = "<document><meta><title>A document's title</title></meta>" +
                          "<content><par>" +
                          "<span>This is a sentence split</span>" +
                          "<span>across two &lt;span&gt; tags that should be treated as one." +
                          "</span>" +
                          "</par>" +
                          "<par>Here is a sentence. Followed by a second one.</par>" +
                          "<raw>This sentence will not be translated.</raw>" +
                          "</content>" +
                          "</document>";

      var result = await translator.TranslateTextAsync(
            text,
            null,
            "DE",
            new TextTranslateOptions {
                  TagHandling = "xml",
                  OutlineDetection = false,
                  NonSplittingTags = { "span" },
                  SplittingTags = { "title", "par" },
                  IgnoreTags = { "raw" }
            });
      Assert.Contains("<raw>This sentence will not be translated.</raw>", result.Text);
      Assert.Matches("<title>.*Der Titel.*</title>", result.Text);
    }

    [RealServerOnlyFact]
    public async Task TestTagHandlingHtml() {
      var translator = CreateTestTranslator();
      const string text = "<!DOCTYPE html>" +
                          "<html>" +
                          "<body>" +
                          "<h1>My First Heading</h1>" +
                          "<p translate=\"no\">My first paragraph.</p>" +
                          "</body>" +
                          "</html>";

      var result = await translator.TranslateTextAsync(
            text,
            null,
            "DE",
            new TextTranslateOptions { TagHandling = "html" });
      Assert.Contains("<h1>Meine erste Überschrift</h1>", result.Text);
      Assert.Contains("<p translate=\"no\">My first paragraph.</p>", result.Text);
    }

    [Fact]
    public void TextEmptyAuthKey() => Assert.Throws<ArgumentException>(() => new Translator(""));

    [Fact]
    public async Task TestInvalidAuthKey() {
      var translator = new Translator("invalid", new TranslatorOptions { ServerUrl = ServerUrl });
      await Assert.ThrowsAsync<AuthorizationException>(
            () => translator.TranslateTextAsync("Hello, world!", null, "DE"));
    }

    [Fact]
    public async Task TestTextCancellation() {
      var translator = CreateTestTranslator();
      var cancellationTokenSource = new CancellationTokenSource();
      var task = translator.TranslateTextAsync(
            ExampleText("en"),
            "EN",
            "DE",
            cancellationToken: cancellationTokenSource.Token);

      cancellationTokenSource.Cancel();
      await Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
    }

    [Fact]
    public async Task TestEmptyText() {
      var translator = CreateTestTranslator();
      await Assert.ThrowsAsync<ArgumentException>(() => translator.TranslateTextAsync("", null, "DE"));
    }

    [Fact]
    public async Task TestMixedCaseLanguages() {
      var translator = CreateTestTranslator();
      TextResult result;
      result = await translator.TranslateTextAsync(ExampleText("de"), null, "en-us");
      Assert.Equal(ExampleText("en-US"), result.Text.ToLower());
      Assert.Equal("de", result.DetectedSourceLanguageCode);

      result = await translator.TranslateTextAsync(ExampleText("de"), null, "EN-us");
      Assert.Equal(ExampleText("en-US"), result.Text.ToLower());
      Assert.Equal("de", result.DetectedSourceLanguageCode);

      result = await translator.TranslateTextAsync(ExampleText("de"), "de", "EN-US");
      Assert.Equal(ExampleText("en-US"), result.Text.ToLower());
      Assert.Equal("de", result.DetectedSourceLanguageCode);

      result = await translator.TranslateTextAsync(ExampleText("de"), "dE", "EN-US");
      Assert.Equal(ExampleText("en-US"), result.Text.ToLower());
      Assert.Equal("de", result.DetectedSourceLanguageCode);
    }
  }
}
