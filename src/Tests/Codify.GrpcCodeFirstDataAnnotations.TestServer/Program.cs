using Codify.GrpcCodeFirstDataAnnotations.TestServer.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProtoBuf.Grpc.Server;

namespace Codify.GrpcCodeFirstDataAnnotations.TestServer;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder
                    .ConfigureServices(services =>
                    {
                        // Add Microsoft code first gRPC services
                        services.AddCodeFirstGrpc();

                        // Add Codify.GrpcCodeFirstDataAnnotations services and enable validation
                        services.AddGrpcDataAnnotationValidation();

                        // Enable validation in gRPC options
                        services.AddGrpc(options => options.EnableDataAnnotationValidation());
                    });

                webBuilder.Configure(app =>
                {
                    var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
                    if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

                    app.UseRouting();

                    app.UseEndpoints(endpoints =>
                    {
                        // Map gRPC services
                        endpoints.MapGrpcService<GreeterCodeFirstService>();
                    });
                });
            });
    }
}