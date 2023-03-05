// Copyright 2022 DeepL SE (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.
using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DeepL {
  public static class ServiceCollectionExtensions {
    public static IServiceCollection AddDeepL(
          this IServiceCollection services,
          IConfiguration configuration,
          Action<TranslatorOptions>? configureOptions = null) {
      if (services is null) throw new ArgumentNullException(nameof(services));
      if (configuration is null) throw new ArgumentNullException(nameof(configuration));

      services.Configure<TranslatorOptions>(options =>
      {
        configuration.GetSection("DeepL").Bind(options);
        configureOptions?.Invoke(options);
      });

      services.AddScoped<ITranslator, Translator>();

      return services;
    }
  }
}
