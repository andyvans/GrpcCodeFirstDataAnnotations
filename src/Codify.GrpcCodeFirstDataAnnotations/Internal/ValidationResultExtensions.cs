using System.Collections.Generic;
using System.Text.Json;
using Codify.GrpcCodeFirstDataAnnotations.Exceptions;
using Grpc.Core;

namespace Codify.GrpcCodeFirstDataAnnotations.Internal;

internal static class ValidationResultExtensions
{
    public static Metadata ToValidationMetadata(this IList<DataAnnotationValidationTrailers> trailers)
    {
        var metadata = new Metadata();
        var json = JsonSerializer.Serialize(trailers);
        metadata.Add(new Metadata.Entry(RpcExceptionExtensions.TrailerKey, json));
        return metadata;
    }
}