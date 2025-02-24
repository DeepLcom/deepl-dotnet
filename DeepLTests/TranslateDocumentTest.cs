// Copyright 2022 DeepL SE (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeepL;
using DeepL.Model;
using Xunit;

namespace DeepLTests {
  public sealed class TranslateDocumentTest : BaseDeepLTest {
    private readonly string _tempDir = TempDir();

    private static string ExampleDocumentInput => ExampleText("en");
    private static string ExampleDocumentTranslation => ExampleText("de");
    private static string ExampleLargeDocumentInput => string.Join("\n", Enumerable.Repeat(ExampleDocumentInput, 1000));

    private static string ExampleLargeDocumentTranslation => string.Join(
          "\n",
          Enumerable.Repeat(ExampleDocumentTranslation, 1000));

    private string ExampleDocumentPath(string? content = null) {
      var filePath = Path.Combine(_tempDir, "example_document.txt");
      File.Delete(filePath);
      File.WriteAllText(filePath, content ?? ExampleDocumentInput);
      return filePath;
    }

    private string ExampleLargeDocumentPath() {
      var filePath = Path.Combine(_tempDir, "example_large_document.txt");
      File.Delete(filePath);
      File.WriteAllText(filePath, ExampleLargeDocumentInput);
      return filePath;
    }

    private string OutputDocumentPath() {
      var path = Path.Combine(_tempDir, "output", "example_document.txt");
      Directory.CreateDirectory(Path.Combine(_tempDir, "output"));
      File.Delete(path);
      return path;
    }

    [Fact]
    public async Task TestTranslateDocumentWithFilepath() {
      var translator = CreateTestTranslator();
      var outputDocumentPath = OutputDocumentPath();
      await translator.TranslateDocumentAsync(
            new FileInfo(ExampleDocumentPath()),
            new FileInfo(outputDocumentPath),
            "EN",
            "DE");
      Assert.Equal(ExampleDocumentTranslation, File.ReadAllText(outputDocumentPath));
    }

    [MockServerOnlyFact]
    public async Task TestTranslateDocumentWithRetry() {
      var translator = CreateTestTranslatorWithMockSession(
            nameof(TestTranslateDocumentWithRetry),
            new SessionOptions { NoResponse = 1 },
            new TranslatorOptions {
              PerRetryConnectionTimeout = TimeSpan.FromSeconds(1),
              OverallConnectionTimeout = TimeSpan.FromSeconds(10)
            });
      var outputDocumentPath = OutputDocumentPath();

      await translator.TranslateDocumentAsync(
            new FileInfo(ExampleDocumentPath()),
            new FileInfo(outputDocumentPath),
            "EN",
            "DE");
      Assert.Equal(ExampleDocumentTranslation, File.ReadAllText(outputDocumentPath));
    }

    [MockServerOnlyFact]
    public async Task TestTranslateDocumentWithWaiting() {
      var translator = CreateTestTranslatorWithMockSession(
            nameof(TestTranslateDocumentWithWaiting),
            new SessionOptions {
              DocumentQueueTime = TimeSpan.FromSeconds(2),
              DocumentTranslateTime = TimeSpan.FromSeconds(2)
            });
      var outputDocumentPath = OutputDocumentPath();
      await translator.TranslateDocumentAsync(
            new FileInfo(ExampleDocumentPath()),
            new FileInfo(outputDocumentPath),
            "EN",
            "DE");
      Assert.Equal(ExampleDocumentTranslation, File.ReadAllText(outputDocumentPath));
    }

    [MockServerOnlyFact]
    public async Task TestTranslateLargeDocument() {
      var translator = CreateTestTranslator();
      using var outputStream = new MemoryStream();
      var inputFile = new FileInfo(ExampleLargeDocumentPath());
      using var inputStream = inputFile.OpenRead();
      await translator.TranslateDocumentAsync(inputStream, inputFile.Name, outputStream, "EN", "DE");
      Assert.Equal(ExampleLargeDocumentTranslation, Encoding.UTF8.GetString(outputStream.GetBuffer()));
    }

    [Fact]
    public async Task TestTranslateDocumentFormality() {
      var translator = CreateTestTranslator();
      var exampleDocumentPath = ExampleDocumentPath("How are you?");
      var outputDocumentPath = OutputDocumentPath();
      await translator.TranslateDocumentAsync(
            new FileInfo(exampleDocumentPath),
            new FileInfo(outputDocumentPath),
            "EN",
            "DE",
            new DocumentTranslateOptions { Formality = Formality.More });
      if (!IsMockServer) {
        Assert.Equal("Wie geht es Ihnen?", File.ReadAllText(outputDocumentPath));
      }

      await Assert.ThrowsAsync<IOException>(
            async () => await translator.TranslateDocumentAsync(
                  new FileInfo(exampleDocumentPath),
                  new FileInfo(outputDocumentPath),
                  "EN",
                  "DE"));

      File.Delete(outputDocumentPath);
      await translator.TranslateDocumentAsync(
            new FileInfo(exampleDocumentPath),
            new FileInfo(outputDocumentPath),
            "EN",
            "DE",
            new DocumentTranslateOptions { Formality = Formality.Less });
      if (!IsMockServer) {
        Assert.Equal("Wie geht es dir?", File.ReadAllText(outputDocumentPath));
      }

      File.Delete(outputDocumentPath);
      await translator.TranslateDocumentAsync(
            new FileInfo(exampleDocumentPath),
            new FileInfo(outputDocumentPath),
            "EN",
            "DE",
            new DocumentTranslateOptions { Formality = Formality.PreferMore });
      if (!IsMockServer) {
        Assert.Equal("Wie geht es Ihnen?", File.ReadAllText(outputDocumentPath));
      }

      File.Delete(outputDocumentPath);
      await translator.TranslateDocumentAsync(
            new FileInfo(exampleDocumentPath),
            new FileInfo(outputDocumentPath),
            "EN",
            "DE",
            new DocumentTranslateOptions { Formality = Formality.PreferLess });
      if (!IsMockServer) {
        Assert.Equal("Wie geht es dir?", File.ReadAllText(outputDocumentPath));
      }
    }

    [Fact]
    public async Task TestDocumentFailureDuringTranslation() {
      var translator = CreateTestTranslator();
      var outputDocumentPath = OutputDocumentPath();

      // Translating text from DE to DE will trigger error
      var inputDocumentPath = ExampleDocumentPath(ExampleText(LanguageCode.German));

      var exception = await Assert.ThrowsAsync<DocumentTranslationException>(
            async () => await translator.TranslateDocumentAsync(
                  new FileInfo(inputDocumentPath),
                  new FileInfo(outputDocumentPath),
                  null,
                  "DE"));

      Assert.Contains("Source and target language", exception.Message);
    }

    [Fact]
    public async Task TestDocumentCancellation() {
      var translator = CreateTestTranslator();
      var outputDocumentPath = OutputDocumentPath();
      var cancellationTokenSource = new CancellationTokenSource();
      var task = translator.TranslateDocumentAsync(
            new FileInfo(ExampleDocumentPath()),
            new FileInfo(outputDocumentPath),
            "EN",
            "DE",
            cancellationToken: cancellationTokenSource.Token);

      cancellationTokenSource.Cancel();
      var exception = await Assert.ThrowsAsync<DocumentTranslationException>(async () => await task);
      Assert.Equal(typeof(TaskCanceledException), exception.InnerException!.GetType());
    }

    [Fact]
    public async Task TestInvalidDocument() {
      var translator = CreateTestTranslator();
      var inputFilePath = Path.Combine(_tempDir, "document.xyz");
      Directory.CreateDirectory(Path.Combine(_tempDir, "output"));
      var outputFilePath = Path.Combine(_tempDir, "output", "document.xyz");
      File.WriteAllText(inputFilePath, ExampleText("en"));
      File.Delete(outputFilePath);
      var exception = await Assert.ThrowsAsync<DocumentTranslationException>(
            () => translator.TranslateDocumentAsync(
                  new FileInfo(inputFilePath),
                  new FileInfo(outputFilePath),
                  "EN",
                  "DE"));
      Assert.Null(exception.DocumentHandle);
    }

    [MockServerOnlyFact]
    public async Task TestTranslateDocumentLowLevel() {
      // Set a small document queue time to attempt downloading a queued document
      var translator = CreateTestTranslatorWithMockSession(
            nameof(TestTranslateDocumentLowLevel),
            new SessionOptions { DocumentQueueTime = TimeSpan.FromMilliseconds(100) });
      var exampleDocumentPath = ExampleDocumentPath();
      var handle = await translator.TranslateDocumentUploadAsync(new FileInfo(exampleDocumentPath), "EN", "DE");

      var status = await translator.TranslateDocumentStatusAsync(handle);
      Assert.Equal(handle.DocumentId, status.DocumentId);
      Assert.True(status.Ok);
      Assert.False(status.Done);

      // Test recreating a document handle from id & key
      var (documentId, documentKey) = (handle.DocumentId, handle.DocumentKey);
      handle = new DocumentHandle(documentId, documentKey);
      status = await translator.TranslateDocumentStatusAsync(handle);
      Assert.True(status.Ok);

      while (status.Ok && !status.Done) {
        await Task.Delay(TimeSpan.FromMilliseconds(200));
        status = await translator.TranslateDocumentStatusAsync(handle);
      }

      Assert.True(status.Ok && status.Done);
      var outputDocumentPath = OutputDocumentPath();
      var outputStream = File.OpenWrite(outputDocumentPath);
      await translator.TranslateDocumentDownloadAsync(handle, outputStream);
      outputStream.Close();
      Assert.Equal(ExampleDocumentTranslation, File.ReadAllText(outputDocumentPath));
    }

    [MockServerOnlyFact]
    public async Task TestTranslateDocumentRequestFields() {
      var translator = CreateTestTranslatorWithMockSession(
            nameof(TestTranslateDocumentRequestFields),
            new SessionOptions {
              DocumentQueueTime = TimeSpan.FromSeconds(2),
              DocumentTranslateTime = TimeSpan.FromSeconds(2)
            });
      var exampleDocumentPath = ExampleDocumentPath();

      var timeBefore = DateTime.Now;
      var handle = await translator.TranslateDocumentUploadAsync(new FileInfo(exampleDocumentPath), "EN", "DE");
      var status = await translator.TranslateDocumentStatusAsync(handle);
      Assert.True(status.Ok);
      while (status.Ok && !status.Done) {
        await Task.Delay(TimeSpan.FromMilliseconds(200));
        Assert.True(status.Status == DocumentStatus.StatusCode.Queued || status.SecondsRemaining! >= 0);
        status = await translator.TranslateDocumentStatusAsync(handle);
      }

      var outputDocumentPath = OutputDocumentPath();
      await translator.TranslateDocumentDownloadAsync(handle, new FileInfo(outputDocumentPath));

      var timeAfter = DateTime.Now;
      Assert.Equal(ExampleDocumentInput.Length, status.BilledCharacters);
      Assert.True(timeAfter - timeBefore > TimeSpan.FromSeconds(4));
      Assert.Equal(ExampleDocumentTranslation, File.ReadAllText(outputDocumentPath));
    }

    [Fact]
    public async Task TestRecreateDocumentHandleInvalid() {
      var translator = CreateTestTranslator();
      string documentId = string.Concat(Enumerable.Repeat("12AB", 8)); // IDs are 32 hex characters
      string documentKey = string.Concat(Enumerable.Repeat("CD34", 16)); // Keys are 64 hex characters
      var handle = new DocumentHandle(documentId, documentKey);
      await Assert.ThrowsAsync<NotFoundException>(() => translator.TranslateDocumentStatusAsync(handle));
    }

    [RealServerOnlyFact]
    public async Task TestOutputFormatFailsOnInvalidFormat() {
      var deeplClient = CreateTestClient();
      var exception = await Assert.ThrowsAsync<DocumentTranslationException>(
        async () => await deeplClient.TranslateDocumentAsync(
            new FileInfo(ExampleDocumentPath()),
            new FileInfo(OutputDocumentPath()),
            "EN",
            "DE",
            new DocumentTranslateOptions() { OutputFormat = "pdf" }));
      var exceptionMessageLowercase = exception.Message.ToLower();
      Assert.Contains("not supported", exceptionMessageLowercase);
      Assert.Contains("conversion", exceptionMessageLowercase);
      Assert.Contains("different document types", exceptionMessageLowercase);
    }
  }
}
