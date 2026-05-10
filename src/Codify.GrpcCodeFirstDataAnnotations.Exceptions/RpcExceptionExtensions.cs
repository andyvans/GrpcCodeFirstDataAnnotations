using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Grpc.Core;

namespace Codify.GrpcCodeFirstDataAnnotations.Exceptions;

public static class RpcExceptionExtensions
{
    public const string TrailerKey = "data-annotation-validation-errors";

    public static IList<DataAnnotationValidationTrailers> GetValidationErrors(this RpcException exception)
    {
        var validationTrailer = exception.Trailers.FirstOrDefault(x => x.Key == TrailerKey);
        return validationTrailer != null
            ? JsonSerializer.Deserialize<IList<DataAnnotationValidationTrailers>>(validationTrailer.Value)
            : new List<DataAnnotationValidationTrailers>();
    }
}