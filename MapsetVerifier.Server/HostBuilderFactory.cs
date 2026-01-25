using System.Text.Json;
using MapsetVerifier.Server.Filter;
using MapsetVerifier.Server.Service;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MapsetVerifier.Server.Middleware;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace MapsetVerifier.Server;

public static class HostBuilderFactory
{
    public static IHost Build(string[] args)
    {
        try
        {
            Log.Information("Building host...");
            var host = Host.CreateDefaultBuilder(args)
                .UseSerilog(Log.Logger)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureServices(services =>
                    {
                        services.AddControllers(options =>
                            {
                                options.Filters.Add<ApiExceptionFilter>();
                            })
                            .AddJsonOptions(options =>
                            {
                                options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                            });
                        services.AddSwaggerGen();
                        services.AddCors(options =>
                        {
                            options.AddDefaultPolicy(builder =>
                            {
                                builder.AllowAnyOrigin()
                                    .AllowAnyHeader()
                                    .AllowAnyMethod();
                            });
                        });
                    });

                    webBuilder.Configure(app =>
                    {
                        app.UseRequestResponseLogging();

                        app.UseCors();

                        app.UseSwagger();
                        app.UseSwaggerUI(c =>
                        {
                            c.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1");
                        });

                        app.UseRouting();

                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
                        });
                    });
                })
                .Build();

            // Register application lifetime logging to know when host actually starts and stops.
            var lifetime = host.Services.GetService<IHostApplicationLifetime>();
            if (lifetime != null)
            {
                lifetime.ApplicationStarted.Register(() => Log.Information("Host started"));
                lifetime.ApplicationStopping.Register(() => Log.Information("Host stopping..."));
                lifetime.ApplicationStopped.Register(() => Log.Information("Host stopped"));
            }

            Log.Information("Host build complete.");
            return host;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly");
            throw;
        }
    }
}