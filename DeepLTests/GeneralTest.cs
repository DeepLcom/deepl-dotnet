// Copyright 2021 DeepL GmbH (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using DeepL;
using Xunit;

namespace DeepLTests {
  public sealed class GeneralTest : BaseDeepLTest {
    /// <summary>
    ///   Tests assembly version begins with current version number.
    /// </summary>
    [Fact]
    public void TestVersion() {
      Assert.Equal("1.0.2", Translator.Version());

      // Note the assembly version must remain unchanged for binary compatibility, excepting the major version.
      Assert.Equal("1.0.0.0", typeof(Translator).Assembly.GetName().Version?.ToString());
    }

    /// <summary>
    ///   Tests translations of pre-prepared example texts to ensure translation is working.
    ///   The texts are translations of "proton beam".
    /// </summary>
    [Fact]
    public async Task TestExampleTranslation() {
      var translator = CreateTestTranslator();
      foreach (var sourceLanguage in ExpectedSourceLanguages()) {
        var inputText = ExampleText(sourceLanguage);
        var resultText = (await translator.TranslateTextAsync(inputText, null, "en-US")).Text.ToLowerInvariant();
        Assert.Contains("proton", resultText);
      }
    }

    [Fact]
    public async Task TestInvalidAuthKey() {
      var translator = new Translator("invalid", new TranslatorOptions { ServerUrl = ServerUrl });
      await Assert.ThrowsAsync<AuthorizationException>(() => translator.GetUsageAsync());
    }

    [Fact]
    public void TestInvalidServerUrl() =>
          Assert.ThrowsAny<Exception>(
                () => new Translator(AuthKey, new TranslatorOptions { ServerUrl = "http:/api.deepl.com" }));

    [Fact]
    public async Task TestUsage() {
      var translator = CreateTestTranslator();
      var usage = await translator.GetUsageAsync();
      Assert.Contains("Usage this billing period", usage.ToString()!);
    }

    [Fact]
    public async Task TestGetSourceLanguages() {
      var translator = CreateTestTranslator();
      var sourceLanguages = await translator.GetSourceLanguagesAsync();
      Assert.Equal(sourceLanguages.Length, ExpectedSourceLanguages().Length);
      foreach (var sourceLanguage in ExpectedSourceLanguages()) {
        Assert.Contains(sourceLanguages, language => language.Code == sourceLanguage);
      }

      Assert.Equal(sourceLanguages[0], sourceLanguages[0]);
      Assert.NotEqual(sourceLanguages[0], sourceLanguages[1]);
    }

    [Fact]
    public async Task TestGetTargetLanguages() {
      var translator = CreateTestTranslator();
      var targetLanguages = await translator.GetTargetLanguagesAsync();
      Assert.Equal(targetLanguages.Length, ExpectedTargetLanguages().Length);
      foreach (var targetLanguage in ExpectedTargetLanguages()) {
        Assert.Contains(targetLanguages, language => language.Code == targetLanguage);
      }

      Assert.Equal(targetLanguages[0], targetLanguages[0]);
      Assert.NotEqual(targetLanguages[0], targetLanguages[1]);
    }

    [Fact]
    public async Task TestGetGlossaryLanguages() {
      var translator = CreateTestTranslator();
      var glossaryLanguages = await translator.GetGlossaryLanguagesAsync();
      Assert.True(glossaryLanguages.Length > 0);
      foreach (var glossaryLanguagePair in glossaryLanguages) {
        Assert.True(glossaryLanguagePair.SourceLanguageCode.Length > 0);
        Assert.True(glossaryLanguagePair.TargetLanguageCode.Length > 0);
      }
    }

    [Fact]
    public void TestServerUrlSelectedBasedOnAuthKey() {
      Assert.True(Translator.AuthKeyIsFreeAccount("ABCD:fx "));
      Assert.False(Translator.AuthKeyIsFreeAccount("ABCD "));
    }

    [MockServerOnlyFact]
    public async Task TestUsageNoResponse() {
      // Lower the retry count and timeout for this test
      var translator = CreateTestTranslatorWithMockSession(
            nameof(TestUsageNoResponse),
            new SessionOptions { NoResponse = 2 },
            new TranslatorOptions {
                  PerRetryConnectionTimeout = TimeSpan.FromMilliseconds(1), MaximumNetworkRetries = 0
            });

      await Assert.ThrowsAsync<ConnectionException>(() => translator.GetUsageAsync());
    }

    [MockServerOnlyFact]
    public async Task TestTranslateTooManyRequests() {
      // Lower the retry count and timeout for this test
      var translator = CreateTestTranslatorWithMockSession(
            nameof(TestTranslateTooManyRequests),
            new SessionOptions { RespondWith429 = 2 },
            new TranslatorOptions { PerRetryConnectionTimeout = TimeSpan.FromSeconds(1), MaximumNetworkRetries = 1 });

      await Assert.ThrowsAsync<TooManyRequestsException>(
            () =>
                  translator.TranslateTextAsync(ExampleText("en"), null, "DE"));
    }

    [MockServerOnlyFact]
    public async Task TestUsageOverrun() {
      const int characterLimit = 20;
      const int documentLimit = 1;
      var translator = CreateTestTranslatorWithMockSession(
            nameof(TestUsageOverrun),
            new SessionOptions { InitCharacterLimit = characterLimit, InitDocumentLimit = documentLimit },
            randomAuthKey: true);

      var usage = await translator.GetUsageAsync();
      Assert.Equal(0, usage.Character?.Count);
      Assert.Equal(0, usage.Document?.Count);
      Assert.Equal(characterLimit, usage.Character?.Limit);
      Assert.Equal(documentLimit, usage.Document?.Limit);
      Assert.Contains("Characters: 0 of 20", usage.ToString()!);
      Assert.Contains("Documents: 0 of 1", usage.ToString()!);

      var tempDir = TempDir();
      var inputPath = Path.Combine(tempDir, "example_document.txt");
      File.WriteAllText(inputPath, new string('a', characterLimit));
      var outputPath = Path.Combine(tempDir, "example_document_output.txt");
      File.Delete(outputPath);

      await translator.TranslateDocumentAsync(new FileInfo(inputPath), new FileInfo(outputPath), null, "DE");

      usage = await translator.GetUsageAsync();
      Assert.True(usage.AnyLimitReached);
      Assert.True(usage.Document?.LimitReached ?? false);
      Assert.True(usage.Character?.LimitReached ?? false);
      Assert.False(usage.TeamDocument?.LimitReached ?? false);

      await Assert.ThrowsAsync<IOException>(
            async () => await translator.TranslateDocumentAsync(
                  new FileInfo(inputPath),
                  new FileInfo(outputPath),
                  null,
                  "DE"));
      File.Delete(outputPath);

      var exception = await Assert.ThrowsAsync<DocumentTranslationException>(
            () =>
                  translator.TranslateDocumentAsync(new FileInfo(inputPath), new FileInfo(outputPath), null, "DE"));
      Assert.Null(exception.DocumentHandle);
      Assert.Equal(typeof(QuotaExceededException), exception.InnerException!.GetType());

      await Assert.ThrowsAsync<QuotaExceededException>(
            () => translator.TranslateTextAsync(ExampleText("en"), null, "DE"));
    }

    [MockServerOnlyFact]
    public async Task TestUsageTeamDocumentLimit() {
      const int teamDocumentLimit = 1;
      var translator = CreateTestTranslatorWithMockSession(
            nameof(TestUsageOverrun),
            new SessionOptions {
                  InitCharacterLimit = 0, InitDocumentLimit = 0, InitTeamDocumentLimit = teamDocumentLimit
            },
            randomAuthKey: true);


      var usage = await translator.GetUsageAsync();
      Assert.False(usage.AnyLimitReached);
      Assert.Equal(0, usage.TeamDocument?.Count);
      Assert.Equal(teamDocumentLimit, usage.TeamDocument?.Limit);
      Assert.DoesNotContain("Characters", usage.ToString()!);
      Assert.DoesNotContain("Documents", usage.ToString()!);
      Assert.Contains("Team documents: 0 of 1", usage.ToString()!);

      var tempDir = TempDir();
      var inputPath = Path.Combine(tempDir, "example_document.txt");
      File.WriteAllText(inputPath, "a");
      var outputPath = Path.Combine(tempDir, "example_document_output.txt");
      File.Delete(outputPath);
      await translator.TranslateDocumentAsync(new FileInfo(inputPath), new FileInfo(outputPath), null, "DE");

      usage = await translator.GetUsageAsync();
      Assert.True(usage.AnyLimitReached);
      Assert.False(usage.Document?.LimitReached ?? false);
      Assert.False(usage.Character?.LimitReached ?? false);
      Assert.True(usage.TeamDocument?.LimitReached ?? false);
    }

    [Fact]
    public void TestClientFactory() {
      var clientWrapper = new HttpClientWrapper();

      HttpClientAndDisposeFlag Factory() {
        return new HttpClientAndDisposeFlag { DisposeClient = false, HttpClient = clientWrapper };
      }

      var translator = new Translator(AuthKey, new TranslatorOptions { ClientFactory = Factory });
      translator.Dispose();
      Assert.False(clientWrapper.WasDisposed);
    }

    private class HttpClientWrapper : HttpClient {
      public bool WasDisposed { get; private set; }

      protected override void Dispose(bool disposing) {
        WasDisposed = true;
        base.Dispose(disposing);
      }
    }
  }
}
