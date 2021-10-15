// Copyright 2021 DeepL GmbH (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

using System.Text.Json.Serialization;

namespace DeepL {
  /// <summary>
  ///   Information about DeepL account usage for the current billing period, for example the number of characters
  ///   translated.
  /// </summary>
  /// <remarks>
  ///   Depending on the account type, some usage types will be omitted. See the
  ///   <a href="https://www.deepl.com/docs-api/">API documentation</a> for more information.
  /// </remarks>
  public sealed class Usage {
    /// <summary>Initializes a new <see cref="Usage" /> object from the given fields object.</summary>
    /// <param name="fieldsStruct"><see cref="JsonFieldsStruct" /> object containing fields read from JSON data.</param>
    internal Usage(JsonFieldsStruct fieldsStruct) {
      Detail? DetailOrNull(long? count, long? limit) {
        return count != null && limit != null ? new Detail((long)count, (long)limit) : null;
      }

      Character = DetailOrNull(fieldsStruct.CharacterCount, fieldsStruct.CharacterLimit);
      Document = DetailOrNull(fieldsStruct.DocumentCount, fieldsStruct.DocumentLimit);
      TeamDocument = DetailOrNull(fieldsStruct.TeamDocumentCount, fieldsStruct.TeamDocumentLimit);
    }

    /// <summary>The character usage if included for the account type, or null.</summary>
    public Detail? Character { get; }

    /// <summary>The document usage if included for the account type, or null.</summary>
    public Detail? Document { get; }

    /// <summary>The team document usage if included for the account type, or null.</summary>
    public Detail? TeamDocument { get; }

    /// <summary><c>true</c> if any of the usage types included for the account type have been reached.</summary>
    public bool AnyLimitReached => (Character?.LimitReached ?? false) || (Document?.LimitReached ?? false) ||
                                   (TeamDocument?.LimitReached ?? false);

    /// <summary>Returns a string representing the usage.</summary>
    /// <returns>A string containing the usage for this billing period.</returns>
    /// <remarks>
    ///   This function is for diagnostic purposes only; the content of the returned string is exempt from backwards
    ///   compatibility.
    /// </remarks>
    public override string ToString() {
      static string LabelledDetail(string label, Detail? detail) {
        return detail == null ? "" : $"\n{label}: {detail}";
      }

      return "Usage this billing period:" +
             LabelledDetail("Characters", Character) +
             LabelledDetail("Documents", Document) +
             LabelledDetail("Team documents", TeamDocument);
    }

    /// <summary>
    ///   Stores the amount used and maximum amount for one usage type.
    /// </summary>
    public sealed class Detail {
      /// <summary>Initializes a new <see cref="Detail" /> object.</summary>
      /// <param name="count">Amount used of one usage type.</param>
      /// <param name="limit">Maximum amount allowed for one usage type.</param>
      internal Detail(long count, long limit) {
        (Count, Limit) = (count, limit);
      }

      /// <summary>The currently used number of items for this usage type.</summary>
      public long Count { get; }

      /// <summary>The maximum permitted number of items for this usage type.</summary>
      public long Limit { get; }

      /// <summary><c>true</c> if the amount used meets or exceeds the limit, otherwise <c>false</c>.</summary>
      public bool LimitReached => Count >= Limit;

      /// <summary>The usage detail as a string.</summary>
      /// <returns>A string containing the amount used and the limit.</returns>
      /// <remarks>
      ///   This function is for diagnostic purposes only; the content of the returned string is exempt from backwards
      ///   compatibility.
      /// </remarks>
      public override string ToString() => $"{Count} of {Limit}";
    }

    /// <summary>Internal struct used for JSON deserialization of <see cref="Usage" />.</summary>
    internal readonly struct JsonFieldsStruct {
      [JsonConstructor]
      public JsonFieldsStruct(
            long? characterCount,
            long? characterLimit,
            long? documentCount,
            long? documentLimit,
            long? teamDocumentCount,
            long? teamDocumentLimit) {
        (CharacterCount, CharacterLimit, DocumentCount, DocumentLimit, TeamDocumentCount, TeamDocumentLimit) =
              (characterCount, characterLimit, documentCount, documentLimit, teamDocumentCount, teamDocumentLimit);
      }

      public long? CharacterCount { get; }
      public long? CharacterLimit { get; }
      public long? DocumentCount { get; }
      public long? DocumentLimit { get; }
      public long? TeamDocumentCount { get; }
      public long? TeamDocumentLimit { get; }
    }
  }
}
