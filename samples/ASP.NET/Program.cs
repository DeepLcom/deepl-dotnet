// Copyright 2022 DeepL SE (https://www.deepl.com)
// Use of this source code is governed by an MIT
// license that can be found in the LICENSE file.

using DeepL.Model.Options;
using DeepL.Service;

namespace ASP.NET;

public class Program {
  public static void Main(string[] args) {
    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.

    builder.Services.AddControllers();

    // Add DeepL Services
    builder.Services.AddDeepL(options => {
      options.ServiceLifetime = ServiceLifetime.Singleton;
      options.ApiKey = "test";
      options.TranslatorOptions = new TranslatorOptions() {
        appInfo = new AppInfo() {
          AppName = "test",
          AppVersion = "1.0.0",
        }
      };
    });




    var app = builder.Build();

    // Configure the HTTP request pipeline.

    app.UseHttpsRedirection();

    app.UseAuthorization();


    app.MapControllers();

    app.Run();
  }
}
