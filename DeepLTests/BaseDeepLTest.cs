// Copyright 2022 DeepL SE (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

using System;
using System.Collections.Generic;
using System.IO;
using DeepL;
using Xunit;

namespace DeepLTests {
  public class BaseDeepLTest {
    protected static readonly bool IsMockServer = Environment.GetEnvironmentVariable("DEEPL_MOCK_SERVER_PORT") != null;
    protected static readonly string AuthKey;
    protected static readonly string? ServerUrl;
    protected static readonly string? ProxyUrl;

    static BaseDeepLTest() {
      if (IsMockServer) {
        AuthKey = "mock_server";
        ServerUrl = Environment.GetEnvironmentVariable("DEEPL_SERVER_URL") ?? throw new Exception(
              "DEEPL_SERVER_URL environment variable must be set when using mock server.");
      } else {
        AuthKey = Environment.GetEnvironmentVariable("DEEPL_AUTH_KEY") ?? throw new Exception(
              "DEEPL_AUTH_KEY environment variable must be set unless using mock server.");
        ServerUrl = Environment.GetEnvironmentVariable("DEEPL_SERVER_URL");
      }
      ProxyUrl = Environment.GetEnvironmentVariable("DEEPL_PROXY_URL");
    }

    protected static Translator CreateTestTranslator(bool randomAuthKey = false) {
      var authKey = randomAuthKey ? Guid.NewGuid().ToString() : AuthKey;
      return ServerUrl == null
            ? new Translator(authKey)
            : new Translator(authKey, new TranslatorOptions { ServerUrl = ServerUrl });
    }

    protected static Translator CreateTestTranslatorWithMockSession(
          string testName,
          SessionOptions sessionOptions,
          TranslatorOptions? translatorOptions = null,
          bool randomAuthKey = false) {
      if (!IsMockServer) {
        return CreateTestTranslator();
      }

      var authKey = randomAuthKey ? Guid.NewGuid().ToString() : AuthKey;
      var sessionHeaders = CreateSessionHeaders(testName, sessionOptions);
      translatorOptions = translatorOptions ?? new TranslatorOptions();
      translatorOptions.ServerUrl = ServerUrl;
      translatorOptions.Headers = sessionHeaders;
      return new Translator(authKey, translatorOptions);
    }

    protected static string ExampleText(string language) {
      switch (language) {
        case "bg":
          return "протонен лъч";
        case "cs":
          return "protonový paprsek";
        case "da":
          return "protonstråle";
        case "de":
          return "Protonenstrahl";
        case "el":
          return "δέσμη πρωτονίων";
        case "en":
        case "en-GB":
        case "en-US":
          return "proton beam";
        case "es":
          return "haz de protones";
        case "et":
          return "prootonikiirgus";
        case "fi":
          return "protonisäde";
        case "fr":
          return "faisceau de protons";
        case "hu":
          return "protonnyaláb";
        case "id":
          return "berkas proton";
        case "it":
          return "fascio di protoni";
        case "ja":
          return "陽子ビーム";
        case "lt":
          return "protonų spindulys";
        case "lv":
          return "protonu staru kūlis";
        case "nl":
          return "protonenbundel";
        case "pl":
          return "wiązka protonów";
        case "pt":
        case "pt-BR":
        case "pt-PT":
          return "feixe de prótons";
        case "ro":
          return "fascicul de protoni";
        case "ru":
          return "протонный луч";
        case "sk":
          return "protónový lúč";
        case "sl":
          return "protonski žarek";
        case "sv":
          return "protonstråle";
        case "tr":
          return "proton ışını";
        case "zh":
          return "质子束";
        default:
          throw new Exception("no example text for language " + language);
      }
    }

    protected static string[] ExpectedSourceLanguages() =>
          new[] {
                LanguageCode.Bulgarian, LanguageCode.Czech, LanguageCode.Danish, LanguageCode.German,
                LanguageCode.Greek, LanguageCode.English, LanguageCode.Spanish, LanguageCode.Estonian,
                LanguageCode.Finnish, LanguageCode.French, LanguageCode.Hungarian, LanguageCode.Indonesian,
                LanguageCode.Italian, LanguageCode.Japanese, LanguageCode.Lithuanian, LanguageCode.Latvian,
                LanguageCode.Dutch, LanguageCode.Polish, LanguageCode.Portuguese, LanguageCode.Romanian,
                LanguageCode.Russian, LanguageCode.Slovak, LanguageCode.Slovenian, LanguageCode.Swedish,
                LanguageCode.Turkish, LanguageCode.Chinese
          };

    protected static string[] ExpectedTargetLanguages() =>
          new[] {
                LanguageCode.Bulgarian, LanguageCode.Czech, LanguageCode.Danish, LanguageCode.German,
                LanguageCode.Greek, LanguageCode.EnglishBritish, LanguageCode.EnglishAmerican, LanguageCode.Spanish,
                LanguageCode.Estonian, LanguageCode.Finnish, LanguageCode.French, LanguageCode.Hungarian,
                LanguageCode.Indonesian, LanguageCode.Italian, LanguageCode.Japanese, LanguageCode.Lithuanian,
                LanguageCode.Latvian, LanguageCode.Dutch, LanguageCode.Polish, LanguageCode.PortugueseBrazilian,
                LanguageCode.PortugueseEuropean, LanguageCode.Romanian, LanguageCode.Russian, LanguageCode.Slovak,
                LanguageCode.Slovenian, LanguageCode.Swedish, LanguageCode.Turkish, LanguageCode.Chinese
          };

    private static Dictionary<string, string?> CreateSessionHeaders(string testName, SessionOptions options) {
      if (!IsMockServer) {
        return new Dictionary<string, string?>();
      }

      var uuid = Guid.NewGuid();
      var headers = new Dictionary<string, string?> {
            { "mock-server-session", $"deepl-dotnet-test/{testName}/{uuid}" }
      };

      if (options.NoResponse != null) {
        headers["mock-server-session-no-response-count"] = options.NoResponse.ToString();
      }

      if (options.RespondWith429 != null) {
        headers["mock-server-session-429-count"] = options.RespondWith429.ToString();
      }

      if (options.InitCharacterLimit != null) {
        headers["mock-server-session-init-character-limit"] = options.InitCharacterLimit.ToString();
      }

      if (options.InitDocumentLimit != null) {
        headers["mock-server-session-init-document-limit"] = options.InitDocumentLimit.ToString();
      }

      if (options.InitTeamDocumentLimit != null) {
        headers["mock-server-session-init-team-document-limit"] =
              options.InitTeamDocumentLimit.ToString();
      }

      if (options.DocumentFailure != null) {
        headers["mock-server-session-doc-failure"] = options.DocumentFailure.ToString();
      }

      if (options.DocumentQueueTime != null) {
        headers["mock-server-session-doc-queue-time"] =
              ((int)options.DocumentQueueTime.Value.TotalMilliseconds).ToString();
      }

      if (options.DocumentTranslateTime != null) {
        headers["mock-server-session-doc-translate-time"] =
              ((int)options.DocumentTranslateTime.Value.TotalMilliseconds).ToString();
      }

      if (options.ExpectProxy != null) {
        headers["mock-server-session-expect-proxy"] = options.ExpectProxy.Value ? "1" : "0";
      }

      return headers;
    }

    protected static string TempDir() {
      var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
      Directory.CreateDirectory(path);
      return path;
    }

    protected struct SessionOptions {
      public int? NoResponse;
      public int? RespondWith429;
      public int? InitCharacterLimit;
      public int? InitDocumentLimit;
      public int? InitTeamDocumentLimit;
      public int? DocumentFailure;
      public TimeSpan? DocumentQueueTime;
      public TimeSpan? DocumentTranslateTime;
      public bool? ExpectProxy;
    }

    protected sealed class MockServerOnlyFact : FactAttribute {
      public MockServerOnlyFact() {
        if (!IsMockServer) {
          Skip = "Only run if using mock server";
        }
      }
    }

    protected sealed class MockProxyServerOnlyFact : FactAttribute {
      public MockProxyServerOnlyFact() {
        if (!IsMockServer || ProxyUrl == null) {
          Skip = "Only run if using mock server with proxy";
        }
      }
    }

    protected sealed class RealServerOnlyFact : FactAttribute {
      public RealServerOnlyFact() {
        if (IsMockServer) {
          Skip = "Only run if using real server";
        }
      }
    }
  }
}
