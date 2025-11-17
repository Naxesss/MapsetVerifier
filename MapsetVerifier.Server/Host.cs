using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;

namespace MapsetVerifier.Server
{
    public abstract class Host
    {
        private const string hubUrl = "/mapsetverifier/signalr";

        public static void Initialize()
        {
            var myHost = new WebHostBuilder().UseKestrel().UseStartup<Startup>().Build();

            myHost.Run();
        }

        public class Startup
        {
            public Startup(IHostingEnvironment env) => Console.WriteLine("Startup.");

            // This method gets called by the runtime. Use this method to add services to the container.
            public void ConfigureServices(IServiceCollection services)
            {
                Console.WriteLine("Configure Services.");

                // Add framework services.
                services.AddControllers();
                services.AddHostedService<Worker>();
                services.AddSignalR();
            }

            // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
            public void Configure(IApplicationBuilder app)
            {
                Console.WriteLine("Configure.");

                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapHub<SignalHub>(hubUrl);
                    endpoints.MapControllers();
                });
            }
        }
    }
}