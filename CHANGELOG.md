# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).


## [Unreleased]
### Fixed
* Fix CI build for netcoreapp3.1.
* Fix `Formality` options `PreferLess` and `PreferMore` for document
  translation.


## [1.5.0] - 2022-09-30
### Added
* Add new `Formality` options: `PreferLess` and `PreferMore`.
### Changed
* Requests resulting in `503 Service Unavailable` errors are now retried.
  Attempting to download a document before translation is completed will now
  wait and retry (up to 5 times by default), rather than throwing an exception.


## [1.4.0] - 2022-09-09
### Added
* New language available: Ukrainian (`'uk'`). Add language code constant and
  tests.

  Note: older library versions also support new languages, this update only
  adds new code constant.


## [1.3.0] - 2022-08-02
### Added
* Add `Translator.createGlossaryFromCsvAsync()` allowing glossaries downloaded
  from website to be easily uploaded to API.
### Changed
* Strong name DeepL.net assembly.


## [1.2.1] - 2022-06-29
### Changed
* Update contributing guidelines, we can now accept Pull Requests.
### Fixed
* Fix bug in OutlineDetection option for text translation.


## [1.2.0] - 2022-05-18
### Added
* New languages available: Indonesian (`'id'`) and Turkish (`'tr'`). Add
  language code constants and tests.

  Note: older library versions also support the new languages, this update only
  adds new code constants.
* Added `ITranslator` interface implemented by `Translator` class, to achieve a
  better mock-ability in conjunction with common mocking frameworks.
### Changed
* Improve package icon, tags and project URLs.
### Fixed
* Test fix: tests should still succeed after new languages are added.


## [1.1.0] - 2022-04-13
### Added
* Add `ErrorMessage` property to `DocumentStatus` that contains a short
  description of document translation error, if available.


## [1.0.5] - 2022-04-12
### Added
* Add support for `TextTranslateOptions.TagHandling = "html"`. No code changes
  were needed, only comments and tests were changed.
* Add test for proxy support.
### Fixed
* Fix some tests that intermittently failed.


## [1.0.4] - 2022-01-27
### Fixed
* Fix issue in .NET versions earlier than 5.0, when creating large glossaries or
 translating texts larger than 64 KiB.


## [1.0.3] - 2022-01-19
### Added
* Add contribution guidelines -- currently we are unable to accept Pull Requests.
### Changed
* Improve README tests section.


## [1.0.2] - 2022-01-03
### Changed
- Include additional information with error status codes.


## [1.0.1] - 2021-12-07
### Fixed
- Use default HTTP version in requests rather than always using HTTP/2.


## [1.0.0] - 2021-11-18
### Changed
- Add missing properties in package, e.g. icon.
- Introduce GlossaryEntries class to encapsulate glossary entries.
- SplitSentences enum renamed to SentenceSplittingMode.
- Exceptions thrown by some functions were changed to use standard exceptions.
- Improvements to tests, README and documentation.


## [0.1.0] - 2021-11-05
Initial release.


[Unreleased]: https://github.com/DeepLcom/deepl-dotnet/compare/v1.5.0...HEAD
[1.5.0]: https://github.com/DeepLcom/deepl-dotnet/compare/v1.4.0...v1.5.0
[1.4.0]: https://github.com/DeepLcom/deepl-dotnet/compare/v1.3.0...v1.4.0
[1.3.0]: https://github.com/DeepLcom/deepl-dotnet/compare/v1.2.1...v1.3.0
[1.2.1]: https://github.com/DeepLcom/deepl-dotnet/compare/v1.2.0...v1.2.1
[1.2.0]: https://github.com/DeepLcom/deepl-dotnet/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/DeepLcom/deepl-dotnet/compare/v1.0.5...v1.1.0
[1.0.5]: https://github.com/DeepLcom/deepl-dotnet/compare/v1.0.4...v1.0.5
[1.0.4]: https://github.com/DeepLcom/deepl-dotnet/compare/v1.0.3...v1.0.4
[1.0.3]: https://github.com/DeepLcom/deepl-dotnet/compare/v1.0.2...v1.0.3
[1.0.2]: https://github.com/DeepLcom/deepl-dotnet/compare/v1.0.1...v1.0.2
[1.0.1]: https://github.com/DeepLcom/deepl-dotnet/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/DeepLcom/deepl-dotnet/compare/v0.1.0...v1.0.0
[0.1.0]: https://github.com/DeepLcom/deepl-dotnet/releases/tag/v0.1.0
