// Copyright 2025 DeepL SE (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.
using System.Threading;
using System.Threading.Tasks;
using DeepL.Model;

namespace DeepL {
  public interface IStyleRuleManager {
    /// <summary>Retrieves the list of all available style rules.</summary>
    /// <param name="page">Optional page number for pagination, 0-indexed.</param>
    /// <param name="pageSize">Optional number of items per page.</param>
    /// <param name="detailed">
    ///   Optional flag indicating whether to include detailed configuration rules in the configuredRules property.
    /// </param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns>Array of <see cref="StyleRuleInfo" /> objects representing the available style rules.</returns>
    /// <exception cref="DeepLException">
    ///   If any error occurs while communicating with the DeepL API, a
    ///   <see cref="DeepLException" /> or a derived class will be thrown.
    /// </exception>
    Task<StyleRuleInfo[]> GetAllStyleRulesAsync(
          int? page = null,
          int? pageSize = null,
          bool? detailed = null,
          CancellationToken cancellationToken = default);
  }
}
