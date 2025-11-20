using System.Text.Json;
using MapsetVerifier.Server.Service;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace MapsetVerifier.Server
{
    public abstract class Host
    {
        private const string hubUrl = "/mapsetverifier/signalr";

        public static void Initialize()
        {
            var myHost = new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://localhost:5005") // TODO find a better way to expose the API and avoid clashing with the old API
                .UseStartup<Startup>()
                .Build();

            myHost.Run();
        }

        public class Startup
        {
            public Startup(IWebHostEnvironment env) => Console.WriteLine("Startup.");

            // This method gets called by the runtime. Use this method to add services to the container.
            public void ConfigureServices(IServiceCollection services)
            {
                Console.WriteLine("Configure Services.");

                // Add framework services.
                services.AddControllers()
                    .AddJsonOptions(options =>
                    {
                        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                    });
                
                services.AddHostedService<Worker>();
                services.AddSignalR();
                services.AddSwaggerGen();
            }

            // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
            public void Configure(IApplicationBuilder app)
            {
                Console.WriteLine("Configure.");
                
                // General exception handler
                app.UseExceptionHandler(errorApp =>
                {
                    errorApp.Run(async context =>
                    {
                        context.Response.ContentType = "application/json";
                        var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                        if (error != null)
                        {
                            var apiError = ExceptionService.GetApiError(error);
                            var result = JsonSerializer.Serialize(apiError);
                            await context.Response.WriteAsync(result);
                        }
                    });
                });

                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapHub<SignalHub>(hubUrl);
                    endpoints.MapControllers();
                });
                
                // Enable middleware to serve generated Swagger as a JSON endpoint and the Swagger UI
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1");
                });
            }
        }
    }
}