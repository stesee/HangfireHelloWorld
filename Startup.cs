using Hangfire;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using Hangfire.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;

namespace WebApplicationHangfire
{
  public class Startup
  {
    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      services.AddSingleton<IStuff, Stuff>();
      services.AddSingleton<IBackgroundProcessingServer, BackgroundProcessingServer>();
      services.AddSingleton<IServiceCollection, ServiceCollection>();
      services.AddSingleton<IApplicationBuilder, ApplicationBuilder>();

      var mongoUrlBuilder = new MongoUrlBuilder("mongodb://localhost/jobs");
      var mongoClient = new MongoClient(mongoUrlBuilder.ToMongoUrl());

      // Add Hangfire services. Hangfire.AspNetCore nuget required
      services.AddHangfire(configuration => configuration
          .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
          .UseSimpleAssemblyNameTypeSerializer()
          .UseRecommendedSerializerSettings()
          .UseMongoStorage(mongoClient, mongoUrlBuilder.DatabaseName, new MongoStorageOptions
          {
            MigrationOptions = new MongoMigrationOptions
            {
              MigrationStrategy = new MigrateMongoMigrationStrategy(),
              BackupStrategy = new CollectionMongoBackupStrategy()
            },
            Prefix = "hangfire.mongo",
            CheckConnection = true
          })
      );
      // Add the processing server as IHostedService
      services.AddHangfireServer(serverOptions =>
      {
        serverOptions.ServerName = "Hangfire.Mongo server 1";
        serverOptions.Queues = new[] { "alpha", "beta", "default" };
        // decreased to whatch things in human speed
        serverOptions.WorkerCount = 2;
      });

      services.AddControllers();
      services.AddSwaggerGen(c =>
      {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "WebApplicationHangfire", Version = "v1" });
      });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "WebApplicationHangfire v1"));
      }

      app.UseRouting();

      app.UseAuthorization();

      app.UseEndpoints(endpoints =>
      {
        endpoints.MapControllers();
        endpoints.MapHangfireDashboard();
      });
    }
  }
}