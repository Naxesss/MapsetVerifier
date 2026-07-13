using MapsetVerifier.Server.Filter;
using MapsetVerifier.Server.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace MapsetVerifier.Server;

public static class HostBuilderFactory
{
    public static IHost Build(string[] args)
    {
        try
        {
            Log.Information("Host build initiated. Args: {Args}", args);

            var host = Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    Log.Information("Configuring WebHost...");

                    webBuilder.ConfigureServices(services =>
                    {
                        Log.Information("Registering services...");

                        services
                            .AddControllers(options =>
                            {
                                options.Filters.Add<ApiExceptionFilter>();
                            })
                            .AddJsonOptions(options =>
                            {
                                options.JsonSerializerOptions.Converters.Add(
                                    new System.Text.Json.Serialization.JsonStringEnumConverter()
                                );
                            });

                        Log.Information("Controllers configured");

                        services.AddSwaggerGen();
                        Log.Information("Swagger registered");

                        services.AddCors(options =>
                        {
                            options.AddDefaultPolicy(builder =>
                            {
                                builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                            });
                        });

                        Log.Information("CORS policy registered");
                    });

                    webBuilder.Configure(app =>
                    {
                        app.UseRequestResponseLogging();
                        Log.Information("Request/Response logging middleware enabled");

                        app.UseCors();
                        Log.Information("CORS middleware enabled");

                        app.UseSwagger();
                        app.UseSwaggerUI(c =>
                        {
                            c.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1");
                        });

                        Log.Information("Swagger middleware enabled");

                        app.UseRouting();

                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
                            Log.Information("Controllers mapped");
                        });

                        Log.Information("Middleware pipeline configured");
                    });
                })
                .Build();

            Log.Information("Host built");

            var lifetime = host.Services.GetService<IHostApplicationLifetime>();
            if (lifetime != null)
            {
                lifetime.ApplicationStarted.Register(() =>
                    Log.Information("Host started successfully")
                );
                lifetime.ApplicationStopping.Register(() => Log.Warning("Host is stopping..."));
                lifetime.ApplicationStopped.Register(() => Log.Warning("Host has stopped"));
            }
            else
            {
                Log.Warning("IHostApplicationLifetime not available from DI container");
            }

            return host;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated during build");
            throw;
        }
    }
}
