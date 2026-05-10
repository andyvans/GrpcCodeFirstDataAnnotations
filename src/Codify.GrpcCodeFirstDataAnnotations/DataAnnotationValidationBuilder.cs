using Codify.GrpcCodeFirstDataAnnotations.Contracts;
using Codify.GrpcCodeFirstDataAnnotations.Internal;
using Grpc.AspNetCore.Server;
using Microsoft.Extensions.DependencyInjection;

namespace Codify.GrpcCodeFirstDataAnnotations;

public static class DataAnnotationValidationBuilder
{
    public static IServiceCollection AddGrpcDataAnnotationValidation(this IServiceCollection services)
    {
        services.AddSingleton<IDataAnnotationsResultHandler, DefaultDataAnnotationsResultHandler>();
        return services;
    }

    public static GrpcServiceOptions EnableDataAnnotationValidation(this GrpcServiceOptions options)
    {
        options.Interceptors.Add<DataAnnotationValidationInterceptor>();
        return options;
    }
}