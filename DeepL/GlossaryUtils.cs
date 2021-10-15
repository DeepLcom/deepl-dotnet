// Copyright 2021 DeepL GmbH (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeepL {
  /// <summary>Class containing utility functions related to glossaries.</summary>
  public static class GlossaryUtils {
    /// <summary>
    ///   Converts the given tab-separated-value (TSV) string of glossary entries into a dictionary of source-target
    ///   entry pairs. Whitespace is trimmed from the start and end of each term.
    /// </summary>
    /// <param name="tsv">String containing the entries in TSV format.</param>
    /// <param name="skipChecks">If <c>true</c>, validity checks on the entries are skipped, defaults to <c>false</c>.</param>
    /// <returns>Dictionary containing the source-target entry pairs.</returns>
    /// <exception cref="DeepLException">If the entries fail any validity check.</exception>
    public static Dictionary<string, string> ConvertToDictionary(
          string tsv,
          bool skipChecks = false) {
      string[] lineSeparators = { "\r\n", "\n", "\r" };
      const char termSeparator = '\t';
      char[] termSeparatorArray = { termSeparator };

      var entries = new Dictionary<string, string>();
      var lineNumber = 0;
      foreach (var line in tsv.Split(lineSeparators, StringSplitOptions.None)) {
        lineNumber += 1;
        var lineTrimmed = line.Trim();
        if (lineTrimmed.Length == 0) {
          continue;
        }

        var termSeparatorPos = lineTrimmed.IndexOf(termSeparator);
        if (termSeparatorPos == -1) {
          throw new DeepLException($"Entry on line {lineNumber} does not contain separator: {line}");
        }

        var source = lineTrimmed.Substring(0, termSeparatorPos).Trim();
        var target = lineTrimmed.Substring(termSeparatorPos + 1).Trim();
        if (entries.ContainsKey(source)) {
          throw new DeepLException($"Entry on line {lineNumber} duplicates source term \"{source}\"");
        }

        if (target.Contains(termSeparator)) {
          throw new DeepLException($"Entry on line {lineNumber} contains more than one term separator: {line}");
        }

        if (!skipChecks) {
          ValidateGlossaryTerm(source);
          ValidateGlossaryTerm(target);
        }

        entries.Add(source, target);
      }

      return entries;
    }

    /// <summary>
    ///   Checks the validity of the given glossary term, for example that it contains no invalid characters. Whitespace
    ///   is trimmed from the start and end of each term.
    /// </summary>
    /// <param name="term">String containing term to check.</param>
    /// <exception cref="DeepLException">If the term is invalid.</exception>
    public static void ValidateGlossaryTerm(string term) {
      var termTrimmed = term.Trim();

      if (termTrimmed.Length == 0) {
        throw new DeepLException($"String {term} is not a valid term");
      }

      foreach (var ch in termTrimmed) {
        if ((0 <= ch && ch <= 31) || // C0 control characters
            (128 <= ch && ch <= 159) || // C1 control characters
            ch == '\u2028' || ch == '\u2029' // Unicode newlines
        ) {
          throw new DeepLException($"Term {term} contains invalid character: '{ch}' (U+{Convert.ToInt32(ch):X4})");
        }
      }
    }

    /// <summary>
    ///   Converts the given source-target entry pairs to a string containing the entries in tab-separated-value (TSV)
    ///   format.
    /// </summary>
    /// <param name="entries">Dictionary of source-target entry pairs to convert.</param>
    /// <param name="skipChecks">If <c>true</c>, validity checks on the entries are skipped, defaults to <c>false</c>.</param>
    /// <returns>String containing the entries in TSV format.</returns>
    public static string ConvertToTsv(IEnumerable<KeyValuePair<string, string>> entries, bool skipChecks = false) {
      var builder = new StringBuilder();
      foreach (var pair in entries) {
        var source = pair.Key!.Trim();
        var target = pair.Value!.Trim();
        if (!skipChecks) {
          ValidateGlossaryTerm(source);
          ValidateGlossaryTerm(target);
        }

        if (builder.Length > 0) {
          builder.Append("\n");
        }

        builder.Append($"{source}\t{target}");
      }

      return builder.ToString();
    }
  }
}
