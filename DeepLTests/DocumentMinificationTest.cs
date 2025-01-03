// Copyright 2022 DeepL SE (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using DeepL;
using Xunit;

namespace DeepLTests {
  public sealed class DocumentMinificationTest : BaseDeepLTest {
    private readonly string _tempDir = TempDir();

    private string OutputDocumentPath(string extension) {
      var path = Path.Combine(_tempDir, "output", Path.ChangeExtension("example_document", extension));
      Directory.CreateDirectory(Path.Combine(_tempDir, "output"));
      File.Delete(path);
      return path;
    }

    [Theory]
    [InlineData(".pptx")]
    [InlineData(".docx")]
    public void TestMinifyDocumentHappyPath(string extension) {
      var minifiedTestDocument = CreateMinifiedTestDocument(extension, _tempDir);
      var originalFileSize = new FileInfo(minifiedTestDocument).Length;
      var minifier = new DocumentMinifier(_tempDir);
      var minifiedDocumentPath = minifier.MinifyDocument(minifiedTestDocument, false);
      var minifiedFileSize = new FileInfo(minifiedDocumentPath).Length;

      Assert.True(minifiedFileSize < originalFileSize);
      Assert.InRange(minifiedFileSize, 100, 50000);

      // Cleanup
      Directory.Delete(minifier.GetExtractedDocDirectory(), true);
      Directory.Delete(minifier.GetOriginalMediaDirectory(), true);
      File.Delete(minifiedTestDocument);
      File.Delete(minifiedDocumentPath);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TestDocumentMinificationCleansUpProperly(bool shouldCleanUp) {
      var minifiedTestDocument = CreateMinifiedTestDocument(".pptx", _tempDir);
      var minifier = new DocumentMinifier(_tempDir);
      var minifiedDocumentPath = minifier.MinifyDocument(minifiedTestDocument, shouldCleanUp);

      Assert.Equal(shouldCleanUp, !Directory.Exists(minifier.GetExtractedDocDirectory()));

      // Cleanup
      if (!shouldCleanUp) Directory.Delete(minifier.GetExtractedDocDirectory(), true);
      Directory.Delete(minifier.GetOriginalMediaDirectory(), true);
      File.Delete(minifiedTestDocument);
      File.Delete(minifiedDocumentPath);
    }

    [Fact]
    public void TestDeminifyDocumentHappyPath() {
      var inputFile = CreateMinifiedTestDocument(".zip", _tempDir);
      var outputFile = Path.Combine(_tempDir, "example_zip_transformed.zip");
      var minifier = new DocumentMinifier(_tempDir);
      var minifiedFile = minifier.MinifyDocument(inputFile, true);
      minifier.DeminifyDocument(minifiedFile, outputFile, false);

      var inputExtractionDir = Path.Combine(_tempDir, "input_dir");
      var outputExtractionDir = Path.Combine(_tempDir, "output_dir");
      ZipFile.ExtractToDirectory(inputFile, inputExtractionDir);
      ZipFile.ExtractToDirectory(outputFile, outputExtractionDir);

      Assert.True(AssertDirectoriesAreEqual(inputExtractionDir, outputExtractionDir));

      // Cleanup
      Directory.Delete(_tempDir, true);
    }


    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TestDocumentDeminificationCleansUpProperly(bool shouldCleanUp) {
      var minifiedTestDocument = CreateMinifiedTestDocument(".zip", _tempDir);
      var outputFile = Path.Combine(_tempDir, "example_zip_transformed.zip");
      var minifier = new DocumentMinifier();
      var minifiedFile = minifier.MinifyDocument(minifiedTestDocument, true);
      minifier.DeminifyDocument(minifiedFile, outputFile, shouldCleanUp);

      Assert.Equal(shouldCleanUp, !Directory.Exists(minifier.GetExtractedDocDirectory()));

      // Cleanup
      if (!shouldCleanUp) {
        Directory.Delete(minifier.GetExtractedDocDirectory(), true);
        Directory.Delete(minifier.GetOriginalMediaDirectory(), true);
        File.Delete(minifiedFile);
      }
      File.Delete(minifiedTestDocument);
      File.Delete(outputFile);
    }

    [RealServerOnlyFact]
    public async Task TestMinifyAndTranslateDocuments() {
      var translator = CreateTestTranslator();
      var extensions = DocMinificationTestFilesMapping.Keys;
      foreach (var extension in extensions) {
        var exampleDocumentPath = CreateMinifiedTestDocument(extension, _tempDir);
        var outputDocumentPath = OutputDocumentPath(extension);

        await translator.TranslateDocumentAsync(
              new FileInfo(exampleDocumentPath),
              new FileInfo(outputDocumentPath),
              "EN",
              "DE",
              new DocumentTranslateOptions { EnableDocumentMinification = true });

        // If the output exists, the input document must have been minified as TranslateDocumentAsync
        // will not succeed for files over 30 MB
        Assert.True(File.Exists(outputDocumentPath));
        Assert.NotInRange(new FileInfo(exampleDocumentPath).Length, 0, 30000000);
      }

      // Cleanup
      Directory.Delete(_tempDir, true);
    }
  }
}
