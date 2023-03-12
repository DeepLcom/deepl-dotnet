// Copyright 2022 DeepL SE (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

using DeepL.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DeepL.Extensions.DependencyInjection;
public static class ServiceCollectionExtensions {
  public static IServiceCollection AddDeepL(
    this IServiceCollection services,
    Action<TranslatorOptions>? translatorOptionsAction = null,
    Action<DeepLDependencyInjectionOptions>? dependencyInjectionOptionsAction = null,
    Action<IHttpClientBuilder>? clientBuilderAction = null) {

    var dependencyInjectionOptions = new DeepLDependencyInjectionOptions();
    dependencyInjectionOptionsAction?.Invoke(dependencyInjectionOptions);

    //This solves the constructor issue detailed here - https://github.com/dotnet/extensions/issues/3132#issuecomment-606960048
    var clientBuilder = services.AddHttpClient(nameof(DeepLClient));
    clientBuilderAction?.Invoke(clientBuilder);


    services.AddTransient(provider => {
      var options = provider.GetRequiredService<IOptions<TranslatorOptions>>();
      var factory = provider.GetRequiredService<IHttpClientFactory>();
      return new DeepLClient(factory.CreateClient(nameof(DeepLClient)), options);
    });

    var translatorOptions = new TranslatorOptions();
    translatorOptionsAction?.Invoke(translatorOptions);

    services.Configure<TranslatorOptions>(options => {
      translatorOptionsAction?.Invoke(options);
    });

    if (dependencyInjectionOptions.UseDefaultResiliencyOptions) {
      clientBuilder.AddPolicyHandler(DeepLClient.CreateDefaultPolicy(translatorOptions.PerRetryConnectionTimeout, translatorOptions.MaximumNetworkRetries));
    }

    services.Add(ServiceDescriptor.Describe(typeof(ITranslator), provider => {
      var client = provider.GetRequiredService<DeepLClient>();
      return new Translator(client);
    }, dependencyInjectionOptions.ServiceLifetime));

    return services;
  }


  public class DeepLDependencyInjectionOptions {
    public ServiceLifetime ServiceLifetime { get; set; } = ServiceLifetime.Transient;
    public bool UseDefaultResiliencyOptions { get; set; }
  }
}
