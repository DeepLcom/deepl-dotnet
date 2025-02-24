// Copyright 2024 DeepL SE (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

using System;

namespace DeepL {
  /// <summary>
  /// Enum controlling whether the translation engine should use a newer model type
  /// that offers higher quality translations at the cost of translation time.
  /// </summary>
  public enum ModelType {
    /// <summary>
    /// Use a translation model that maximizes translation quality, at the cost
    /// of response time. This option may be unavailable for some language pairs.
    /// </summary>
    QualityOptimized,

    /// <summary>
    /// Use a translation model that minimizes response time, at the cost of
    /// translation quality.
    /// </summary>
    LatencyOptimized,

    /// <summary>
    /// Use the highest-quality translation model for the given language pair.
    /// </summary>
    PreferQualityOptimized
  }

  public static class ModelTypeExtensions {
    /// <summary>
    /// Retrieves the string representation of the enum value used by the DeepL API.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">If an unknown enum value is passed.</exception>
    public static string ToApiValue(this ModelType modelType) {
      return modelType switch {
        ModelType.PreferQualityOptimized => "prefer_quality_optimized",
        ModelType.LatencyOptimized => "latency_optimized",
        ModelType.QualityOptimized => "quality_optimized",
        _ => throw new ArgumentOutOfRangeException(nameof(modelType), modelType, "Unrecognized model type value")
      };
    }
  }
}
