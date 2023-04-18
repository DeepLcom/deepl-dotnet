// Copyright 2022 DeepL SE (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

using DeepL.Model.Interfaces;
using DeepL.Model.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DeepL.Service;

public class DeepLConfiguration {

  public TranslatorOptions TranslatorOptions { get; set; }
  public ServiceLifetime ServiceLifetime { get; set; } = ServiceLifetime.Scoped;
  public string ApiKey { get; set; }

  public Type? customTranslator { get; set; } = null;

}

public class DeepLService {

  public Translator client { get; set; }

  public DeepLService(IOptions<DeepLConfiguration> options) {
    if(options.Value == null) throw new ArgumentNullException(nameof(options.Value));
    this.client = new Translator(options.Value.ApiKey, options.Value.TranslatorOptions);
  }
}


public static class DeepLExtension {

  /// <summary>
  /// Adds DeepL to DI
  /// </summary>
  /// <param name="services">Service Collection</param>
  /// <param name="lifetime">Service Lifetime</param>
  /// <param name="option" cref="TranslatorOptions">Translator Options</param>
  /// <returns></returns>
  public static IServiceCollection AddDeepL(this IServiceCollection services, Action<DeepLConfiguration> options) {

    if (services == null) throw new ArgumentNullException(nameof(services));
    if (options == null) throw new ArgumentNullException(nameof(options));

    //Apply Config
    services.AddOptions();
    services.Configure(options);

    var deeplConfig = new DeepLConfiguration();
    options?.Invoke(deeplConfig);

    // Lets see if we want to use a custom translator
    var translatorType = typeof(Translator);
    if (deeplConfig.customTranslator != null) {
      if (deeplConfig.customTranslator.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ITranslator))) {
        translatorType = typeof(Translator);
      } else {
        throw new ArgumentException("CustomTranslator must implement ITranslator Interface");
      }
    }

    services.Add(new ServiceDescriptor(typeof(ITranslator), translatorType, deeplConfig.ServiceLifetime));
    return services;
  }
}

