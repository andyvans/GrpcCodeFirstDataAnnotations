using Codify.GrpcCodeFirstDataAnnotations.Contracts;
using Codify.GrpcCodeFirstDataAnnotations.Internal;
using Codify.GrpcCodeFirstDataAnnotations.Models;
using Grpc.AspNetCore.Server;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Codify.GrpcCodeFirstDataAnnotations;

/// <summary>
///     Provides extension methods for configuring and enabling data annotation-based validation in gRPC services.
/// </summary>
public static class DataAnnotationValidationBuilder
{
    /// <summary>
    ///     Adds support for validating gRPC request messages using data annotation attributes and registers the required services for data annotation validation.
    /// </summary>
    /// <remarks>
    ///     This method enables automatic validation of gRPC request messages decorated with data annotation attributes. 
    ///     It should be called during service configuration in the application's startup code.
    /// </remarks>
    /// <param name="services">The service collection to which the data annotation validation services are added.</param>
    /// <param name="configure">An optional delegate to configure the data annotation validation.</param>
    /// <returns>The same instance of <see cref="IServiceCollection"/> to allow for method chaining.</returns>
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

    /// <summary>
    ///     Enables data annotation-based validation for gRPC service requests by adding the
    ///     DataAnnotationValidationInterceptor to the specified options.
    /// </summary>
    /// <remarks>
    ///     This method allows gRPC services to automatically validate incoming requests using data
    ///     annotation attributes defined on request message types. Requests that fail validation will be rejected before
    ///     reaching the service implementation.
    /// </remarks>
    /// <param name="options">The GrpcServiceOptions instance to configure. Cannot be null.</param>
    /// <returns>The same GrpcServiceOptions instance with data annotation validation enabled.</returns>
    public static GrpcServiceOptions EnableDataAnnotationValidation(this GrpcServiceOptions options)
    {
        options.Interceptors.Add<DataAnnotationValidationInterceptor>();
        return options;
    }
}