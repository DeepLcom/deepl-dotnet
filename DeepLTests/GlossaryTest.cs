// Copyright 2021 DeepL GmbH (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeepL;
using Xunit;

namespace DeepLTests {
  public sealed class GlossaryTest : BaseDeepLTest {
    private const string InvalidGlossaryId = "invalid_glossary_id";
    private const string GlossaryNamePrefix = "deepl-dotnet-test-glossary";
    private const string NonexistentGlossaryId = "96ab91fd-e715-41a1-adeb-5d701f84a483";
    private const string SourceLang = "en";
    private const string TargetLang = "de";
    private readonly Dictionary<string, string> testEntries = new Dictionary<string, string> { { "Hello", "Hallo" } };

    [Fact]
    public async Task TestGlossaryCreate() {
      var translator = CreateTestTranslator();
      var glossaryCleanup = new GlossaryCleanupUtility(translator, nameof(TestGlossaryCreate));
      var glossaryName = glossaryCleanup.GlossaryName;
      try {
        var glossary = glossaryCleanup.Capture(
              await translator.CreateGlossaryAsync(
                    glossaryName,
                    SourceLang,
                    TargetLang,
                    new Dictionary<string, string> { { "Hello", "Hallo" } }));
        Assert.Equal(glossaryName, glossary.Name);
        Assert.Equal(SourceLang, glossary.SourceLanguage);
        Assert.Equal(TargetLang, glossary.TargetLanguage);
        Assert.Equal(1, glossary.EntryCount);

        var getResult = await translator.GetGlossaryAsync(glossary.GlossaryId);
        Assert.Equal(getResult.Name, glossary.Name);
        Assert.Equal(getResult.SourceLanguage, glossary.SourceLanguage);
        Assert.Equal(getResult.TargetLanguage, glossary.TargetLanguage);
        Assert.Equal(getResult.CreationTime, glossary.CreationTime);
        Assert.Equal(getResult.EntryCount, glossary.EntryCount);
      } finally {
        await glossaryCleanup.Cleanup();
      }
    }

    [Fact]
    public async Task TestGlossaryCreateInvalid() {
      var translator = CreateTestTranslator();
      var glossaryCleanup = new GlossaryCleanupUtility(translator, nameof(TestGlossaryCreateInvalid));
      var glossaryName = glossaryCleanup.GlossaryName;
      try {
        await Assert.ThrowsAsync<DeepLException>(
              () => translator.CreateGlossaryAsync("", SourceLang, TargetLang, testEntries));
        await Assert.ThrowsAsync<DeepLException>(
              () => translator.CreateGlossaryAsync(glossaryName, "EN", "JA", testEntries));
        await Assert.ThrowsAsync<DeepLException>(
              () => translator.CreateGlossaryAsync(glossaryName, "JA", "EN", testEntries));
        await Assert.ThrowsAsync<DeepLException>(
              () => translator.CreateGlossaryAsync(glossaryName, "EN", "XX", testEntries));
        await Assert.ThrowsAsync<DeepLException>(
              () => translator.CreateGlossaryAsync(
                    glossaryName,
                    SourceLang,
                    TargetLang,
                    new Dictionary<string, string>()));
      } finally {
        await glossaryCleanup.Cleanup();
      }
    }

    [Fact]
    public async Task TestGlossaryGet() {
      var translator = CreateTestTranslator();
      var glossaryCleanup = new GlossaryCleanupUtility(translator, nameof(TestGlossaryGet));
      var glossaryName = glossaryCleanup.GlossaryName;
      try {
        var createdGlossary = glossaryCleanup.Capture(
              await translator.CreateGlossaryAsync(glossaryName, SourceLang, TargetLang, testEntries));
        var glossary = await translator.GetGlossaryAsync(createdGlossary.GlossaryId);
        Assert.Equal(createdGlossary.GlossaryId, glossary.GlossaryId);
        Assert.Equal(glossaryName, glossary.Name);
        Assert.Equal(SourceLang, glossary.SourceLanguage);
        Assert.Equal(TargetLang, glossary.TargetLanguage);
        Assert.Equal(createdGlossary.CreationTime, glossary.CreationTime);
        Assert.Equal(testEntries.Count, glossary.EntryCount);

        await Assert.ThrowsAsync<DeepLException>(() => translator.GetGlossaryAsync(InvalidGlossaryId));
        await Assert.ThrowsAsync<GlossaryNotFoundException>(() => translator.GetGlossaryAsync(NonexistentGlossaryId));
      } finally {
        await glossaryCleanup.Cleanup();
      }
    }

    [Fact]
    public async Task TestGlossaryGetEntries() {
      var translator = CreateTestTranslator();
      var glossaryCleanup = new GlossaryCleanupUtility(translator, nameof(TestGlossaryGetEntries));
      var glossaryName = glossaryCleanup.GlossaryName;
      try {
        var entries = new Dictionary<string, string> { { "Apple", "Apfel" }, { "Banana", "Banane" } };
        var createdGlossary = glossaryCleanup.Capture(
              await translator.CreateGlossaryAsync(glossaryName, SourceLang, TargetLang, entries));
        Assert.Equal(entries, await translator.GetGlossaryEntriesAsync(createdGlossary));
        Assert.Equal(entries, await translator.GetGlossaryEntriesAsync(createdGlossary.GlossaryId));

        await Assert.ThrowsAsync<DeepLException>(() => translator.GetGlossaryEntriesAsync(InvalidGlossaryId));
        await Assert.ThrowsAsync<GlossaryNotFoundException>(
              () => translator.GetGlossaryEntriesAsync(NonexistentGlossaryId));
      } finally {
        await glossaryCleanup.Cleanup();
      }
    }

    [Fact]
    public async Task TestGlossaryList() {
      var translator = CreateTestTranslator();
      var glossaryCleanup = new GlossaryCleanupUtility(translator, nameof(TestGlossaryList));
      var glossaryName = glossaryCleanup.GlossaryName;
      try {
        glossaryCleanup.Capture(
              await translator.CreateGlossaryAsync(glossaryName, SourceLang, TargetLang, testEntries));
        var glossaries = await translator.ListGlossariesAsync();
        Assert.Contains(glossaries, glossary => glossary.Name == glossaryName);
      } finally {
        await glossaryCleanup.Cleanup();
      }
    }

    [Fact]
    public async Task TestGlossaryDelete() {
      var translator = CreateTestTranslator();
      var glossaryCleanup = new GlossaryCleanupUtility(translator, nameof(TestGlossaryDelete));
      var glossaryName = glossaryCleanup.GlossaryName;
      try {
        var glossary = glossaryCleanup.Capture(
              await translator.CreateGlossaryAsync(glossaryName, SourceLang, TargetLang, testEntries));
        await translator.DeleteGlossaryAsync(glossary);
        await Assert.ThrowsAsync<GlossaryNotFoundException>(() => translator.DeleteGlossaryAsync(glossary));

        await Assert.ThrowsAsync<DeepLException>(() => translator.DeleteGlossaryAsync(InvalidGlossaryId));
        await Assert.ThrowsAsync<GlossaryNotFoundException>(
              () => translator.DeleteGlossaryAsync(NonexistentGlossaryId));
      } finally {
        await glossaryCleanup.Cleanup();
      }
    }

    [Fact]
    public async Task TestGlossaryTranslateTextSentence() {
      var translator = CreateTestTranslator();
      var glossaryCleanup = new GlossaryCleanupUtility(translator, nameof(TestGlossaryTranslateTextSentence));
      var glossaryName = glossaryCleanup.GlossaryName;
      try {
        var sourceLang = "EN";
        var targetLang = "DE";
        var inputText = "The artist was awarded a prize.";
        var entries = new Dictionary<string, string> { { "artist", "Maler" }, { "prize", "Gewinn" } };
        var glossary = glossaryCleanup.Capture(
              await translator.CreateGlossaryAsync(glossaryName, sourceLang, targetLang, entries));
        var result = await translator.TranslateTextAsync(
              inputText,
              sourceLang,
              targetLang,
              new TextTranslateOptions { GlossaryId = glossary.GlossaryId });
        if (!IsMockServer) {
          Assert.Contains("Maler", result.Text);
          Assert.Contains("Gewinn", result.Text);
        }

        // It is also possible to specify GlossaryInfo
        result = await translator.TranslateTextAsync(
              inputText,
              sourceLang,
              targetLang,
              new TextTranslateOptions(glossary) { Formality = Formality.More });
        if (!IsMockServer) {
          Assert.Contains("Maler", result.Text);
          Assert.Contains("Gewinn", result.Text);
        }
      } finally {
        await glossaryCleanup.Cleanup();
      }
    }

    [Fact]
    public async Task TestGlossaryTranslateTextBasic() {
      var translator = CreateTestTranslator();
      var glossaryCleanupEnDe = new GlossaryCleanupUtility(translator, nameof(TestGlossaryTranslateTextBasic) + "EnDe");
      var glossaryCleanupDeEn = new GlossaryCleanupUtility(translator, nameof(TestGlossaryTranslateTextBasic) + "DeEn");
      var glossaryNameEnDe = glossaryCleanupEnDe.GlossaryName;
      var glossaryNameDeEn = glossaryCleanupDeEn.GlossaryName;
      try {
        var textsEn = new[] { "Apple", "Banana" };
        var textsDe = new[] { "Apfel", "Banane" };
        var entriesEnDe = new Dictionary<string, string>();
        var entriesDeEn = new Dictionary<string, string>();
        for (var i = 0; i < textsEn.Length; i++) {
          entriesDeEn[textsDe[i]] = textsEn[i];
          entriesEnDe[textsEn[i]] = textsDe[i];
        }

        var glossaryEnDe = glossaryCleanupEnDe.Capture(
              await translator.CreateGlossaryAsync(glossaryNameEnDe, "EN", "DE", entriesEnDe));
        var glossaryDeEn = glossaryCleanupDeEn.Capture(
              await translator.CreateGlossaryAsync(glossaryNameDeEn, "DE", "EN", entriesDeEn));

        var result = await translator.TranslateTextAsync(textsEn, "en", "de", new TextTranslateOptions(glossaryEnDe));
        Assert.Equal(textsDe, result.Select(textResult => textResult.Text));

        result = await translator.TranslateTextAsync(
              textsDe,
              "DE",
              "EN-US",
              new TextTranslateOptions { GlossaryId = glossaryDeEn.GlossaryId });
        Assert.Equal(textsEn, result.Select(textResult => textResult.Text));

        result = await translator.TranslateTextAsync(
              textsDe,
              "DE",
              "EN-US",
              new TextTranslateOptions(glossaryDeEn));
        Assert.Equal(textsEn, result.Select(textResult => textResult.Text));
      } finally {
        await glossaryCleanupEnDe.Cleanup();
        await glossaryCleanupDeEn.Cleanup();
      }
    }

    [Fact]
    public async Task TestGlossaryTranslateTextInvalid() {
      var translator = CreateTestTranslator();
      var glossaryCleanupEnDe = new GlossaryCleanupUtility(
            translator,
            nameof(TestGlossaryTranslateTextInvalid) + "EnDe");
      var glossaryCleanupDeEn = new GlossaryCleanupUtility(
            translator,
            nameof(TestGlossaryTranslateTextInvalid) + "DeEn");
      var glossaryNameEnDe = glossaryCleanupEnDe.GlossaryName;
      var glossaryNameDeEn = glossaryCleanupDeEn.GlossaryName;
      try {
        var glossaryEnDe = glossaryCleanupEnDe.Capture(
              await translator.CreateGlossaryAsync(glossaryNameEnDe, "EN", "DE", testEntries));
        var glossaryDeEn = glossaryCleanupDeEn.Capture(
              await translator.CreateGlossaryAsync(glossaryNameDeEn, "DE", "EN", testEntries));
        var exception = await Assert.ThrowsAsync<DeepLException>(
              () => translator.TranslateTextAsync(
                    "test",
                    null,
                    "DE",
                    new TextTranslateOptions { GlossaryId = glossaryEnDe.GlossaryId }));
        Assert.Contains("sourceLang is required", exception.Message);

        exception = await Assert.ThrowsAsync<DeepLException>(
              () => translator.TranslateTextAsync(
                    "test",
                    "DE",
                    "EN",
                    new TextTranslateOptions { GlossaryId = glossaryEnDe.GlossaryId }));
        Assert.Contains("targetLang=\"en\" is deprecated", exception.Message);
      } finally {
        await glossaryCleanupEnDe.Cleanup();
        await glossaryCleanupDeEn.Cleanup();
      }
    }

    // Utility class for labelling test glossaries and deleting them at test completion
    private sealed class GlossaryCleanupUtility {
      private readonly Translator _translator;
      public readonly string GlossaryName;
      private string? _glossaryId;

      public GlossaryCleanupUtility(Translator translator, string testName) {
        _translator = translator;
        var uuid = Guid.NewGuid();
        GlossaryName = $"{GlossaryNamePrefix}: {testName} {uuid}";
      }

      public GlossaryInfo Capture(GlossaryInfo glossary) {
        _glossaryId = glossary.GlossaryId;
        return glossary;
      }

      public async Task Cleanup() {
        try {
          if (_glossaryId != null) {
            await _translator.DeleteGlossaryAsync(_glossaryId);
          }
        } catch {
          // All exceptions ignored
        }
      }
    }
  }
}
