using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Grpc.Core;

namespace Codify.GrpcCodeFirstDataAnnotations.Exceptions;

public static class RpcExceptionExtensions
{
    public const string TrailerKey = "data-annotation-validation-errors";

    public static IList<DataAnnotationValidationTrailers> GetValidationErrors(this RpcException exception)
    {
        var validationTrailer = exception.Trailers.FirstOrDefault(x => x.Key == TrailerKey);
        return validationTrailer?.Value.FromBase64<IList<DataAnnotationValidationTrailers>>();
    }

    private static T FromBase64<T>(this string base64)
    {
        var bytes = Convert.FromBase64String(base64);
        var json = Encoding.Default.GetString(bytes);
        return JsonSerializer.Deserialize<T>(json);
    }
}