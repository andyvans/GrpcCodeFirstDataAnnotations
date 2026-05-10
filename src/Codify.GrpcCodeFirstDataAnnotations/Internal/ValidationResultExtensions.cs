using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Codify.GrpcCodeFirstDataAnnotations.Exceptions;
using Grpc.Core;

namespace Codify.GrpcCodeFirstDataAnnotations.Internal;

internal static class ValidationResultExtensions
{
    public static Metadata ToValidationMetadata(this IList<DataAnnotationValidationTrailers> trailers)
    {
        var metadata = new Metadata();
        if (trailers.Any())
        {
            metadata.Add(new Metadata.Entry(RpcExceptionExtensions.TrailerKey, ToBase64(trailers)));
        }
        return metadata;
    }

    internal static string ToBase64(IList<DataAnnotationValidationTrailers> trailers)
    {
        var json = JsonSerializer.Serialize(trailers);
        var bytes = Encoding.Default.GetBytes(json);
        return Convert.ToBase64String(bytes);
    }
}