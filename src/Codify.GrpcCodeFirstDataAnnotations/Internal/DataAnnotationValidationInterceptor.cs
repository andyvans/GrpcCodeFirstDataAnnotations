using Codify.GrpcCodeFirstDataAnnotations.Contracts;
using Codify.GrpcCodeFirstDataAnnotations.Models;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;

namespace Codify.GrpcCodeFirstDataAnnotations.Internal;

internal class DataAnnotationValidationInterceptor(
    ILogger<DataAnnotationValidationInterceptor> logger,
    IOptions<DataAnnotationValidationOptions> options,
    IServiceProvider serviceProvider,
    IDataAnnotationsResultHandler handler) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        await ValidateRequest(request);
        return await continuation(request, context);
    }

    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        await ValidateRequest(request);
        await continuation(request, responseStream, context);
    }

    public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream,
        ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
        var validatingRequestStream = new ValidatingAsyncStreamReader<TRequest>(requestStream, request => ValidateRequest(request));
        return await continuation(validatingRequestStream, context);
    }

    public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        DuplexStreamingServerMethod<TRequest, TResponse> continuation)
    {
        var validatingRequestStream = new ValidatingAsyncStreamReader<TRequest>(requestStream, request => ValidateRequest(request));
        await continuation(validatingRequestStream, responseStream, context);
    }

    /// <summary>
    ///     Perform the validation using the Data Annotations ValidationContext.
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <param name="request"></param>
    /// <exception cref="ValidationRpcException"></exception>
    private async Task ValidateRequest<TRequest>(TRequest request) where TRequest : class
    {
        var validationFailures = new List<ValidationResult>();

        // First perform standard Data Annotations validation
        var context = new ValidationContext(request, serviceProvider, null);
        Validator.TryValidateObject(request, context, validationFailures, true);

        // Then perform the required properties validation, which checks for properties that are non-nullable reference types but don't have a [Required] attribute
        if (options.Value.ValidateRequiredNullableProperties)
        {
            var requiredFailures = RequiredPropertiesValidatorShared.GetOrCreate<TRequest>(options.Value.MaxRequiredValidationDepth).Validate(request);
            validationFailures.AddRange(requiredFailures);
        }

        // If there are any validation failures, handle them and throw a ValidationRpcException with the appropriate status code and trailers
        var valid = validationFailures.Count == 0;
        if (!valid)
        {
            var result = await handler.HandleAsync(validationFailures);

            logger.Log(
                options.Value.ValidationFailureLogLevel,
                "Validation failed for {RequestType}. {Message}. {PropertyNames}",
                typeof(TRequest).Name,
                result.Message,
                string.Join(", ", result.Trailers.Select(s => string.Join(", ", s.PropertyNames))));

            throw new ValidationRpcException(new Status(StatusCode.InvalidArgument, result.Message), result.Trailers.ToValidationMetadata());
        }
    }
}
