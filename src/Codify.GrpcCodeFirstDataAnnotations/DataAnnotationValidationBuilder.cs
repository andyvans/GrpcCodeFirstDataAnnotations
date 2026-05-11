using Codify.GrpcCodeFirstDataAnnotations.Contracts;
using Codify.GrpcCodeFirstDataAnnotations.Internal;
using Codify.GrpcCodeFirstDataAnnotations.Models;
using Grpc.AspNetCore.Server;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Codify.GrpcCodeFirstDataAnnotations;

public static class DataAnnotationValidationBuilder
{
    public static IServiceCollection AddGrpcDataAnnotationValidation(this IServiceCollection services, Action<DataAnnotationValidationOptions>? configure = null)
    {
        var options = new DataAnnotationValidationOptions();
        configure?.Invoke(options);
        services
            .AddOptions<DataAnnotationValidationOptions>()
            .Configure(opt => configure?.Invoke(opt))
            .ValidateDataAnnotations();

        services.AddSingleton<IDataAnnotationsResultHandler, DefaultDataAnnotationsResultHandler>();
        return services;
    }

    public static GrpcServiceOptions EnableDataAnnotationValidation(this GrpcServiceOptions options)
    {
        options.Interceptors.Add<DataAnnotationValidationInterceptor>();
        return options;
    }
}