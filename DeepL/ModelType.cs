// Copyright 2024 DeepL SE (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

using System;
using System.Reflection;

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
    [ApiValue("quality_optimized")]
    QualityOptimized,

    /// <summary>
    /// Use a translation model that minimizes response time, at the cost of
    /// translation quality.
    /// </summary>
    [ApiValue("latency_optimized")]
    LatencyOptimized,

    /// <summary>
    /// Use the highest-quality translation model for the given language pair.
    /// </summary>
    [ApiValue("prefer_quality_optimized")]
    PreferQualityOptimized
  }

  public static class ModelTypeExtensions {
    public static string ToApiValue(this ModelType modelType) {
      var fieldInfo = modelType.GetType().GetField(modelType.ToString());
      var attribute = fieldInfo?.GetCustomAttribute<ApiValueAttribute>();
      if (attribute == null) {
        throw new ArgumentOutOfRangeException(nameof(modelType), modelType, "Unrecognized model type value");
      }

      return attribute.Value;
    }
  }


  [AttributeUsage(AttributeTargets.Field)]
  internal sealed class ApiValueAttribute : Attribute
  {
    public string Value { get; }

    public ApiValueAttribute(string value)
    {
      Value = value;
    }
  }
}
