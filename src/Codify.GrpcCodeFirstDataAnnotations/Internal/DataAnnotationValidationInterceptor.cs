using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Codify.GrpcCodeFirstDataAnnotations.Contracts;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Codify.GrpcCodeFirstDataAnnotations.Internal;

internal class DataAnnotationValidationInterceptor(
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
        var context = new ValidationContext(request, serviceProvider, null);
        var validationFailures = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(request, context, validationFailures, true);
        if (!valid)
        {
            var result = await handler.HandleAsync(validationFailures);
            throw new ValidationRpcException(new Status(StatusCode.InvalidArgument, result.Message), result.Trailers.ToValidationMetadata());
        }
    }
}
