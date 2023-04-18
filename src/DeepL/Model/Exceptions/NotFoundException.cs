// Copyright 2022 DeepL SE (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

namespace DeepL.Model.Exceptions;

/// <summary>Exception thrown when the specified resource could not be found.</summary>
public class NotFoundException : DeepLException {
  /// <summary>Initializes a new instance of the <see cref="NotFoundException" /> class.</summary>
  /// <param name="message">The message that describes the error.</param>
  internal NotFoundException(string message) : base(message) {
  }
}
