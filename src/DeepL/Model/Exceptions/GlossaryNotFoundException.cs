// Copyright 2022 DeepL SE (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

namespace DeepL.Model.Exceptions;

/// <summary>Exception thrown when no glossary could be found with the specified ID.</summary>
public sealed class GlossaryNotFoundException : NotFoundException {
  /// <summary>Initializes a new instance of the <see cref="GlossaryNotFoundException" /> class.</summary>
  /// <param name="message">The message that describes the error.</param>
  internal GlossaryNotFoundException(string message) : base(message) {
  }
}
