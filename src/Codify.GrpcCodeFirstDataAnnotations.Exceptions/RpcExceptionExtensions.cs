using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Grpc.Core;

namespace Codify.GrpcCodeFirstDataAnnotations.Exceptions;

public static class RpcExceptionExtensions
{
    public const string TrailerKey = "data-annotation-validation-errors";

    /// <summary>
    ///     Retrieves a list of validation errors from the trailers of the specified gRPC exception.
    /// </summary>
    /// <remarks>
    ///     This method is used to extract data annotation validation errors that were sent as
    ///     trailers in a gRPC response. 
    /// </remarks>
    /// <param name="exception">The <see cref="RpcException"/> instance from which to extract validation error information. Cannot be null.</param>
    /// <returns>A list of <see cref="DataAnnotationValidationTrailers"/> objects representing validation errors.</returns>
    public static IList<DataAnnotationValidationTrailers> GetValidationErrors(this RpcException exception)
    {
        var validationTrailer = exception.Trailers.FirstOrDefault(x => x.Key == TrailerKey);
        var validationTrailers = validationTrailer != null
            ? JsonSerializer.Deserialize<IList<DataAnnotationValidationTrailers>>(validationTrailer.Value)
            : null;
        return validationTrailers ?? [];
    }

    public static string? GetFormattedValidationErrors(this RpcException exception)
    {
        var validationErrors = exception.GetValidationErrors();
        if (!validationErrors.Any()) return null;

        var formattedErrors = validationErrors
            .Select(error => $"{string.Join(", ", error.PropertyNames)}: {error.ErrorMessage}")
            .ToList();
        return string.Join("\n", formattedErrors);
    }   
}