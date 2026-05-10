using System.Collections.Generic;
using Codify.GrpcCodeFirstDataAnnotations.Exceptions;

namespace Codify.GrpcCodeFirstDataAnnotations.Models;

public record DataAnnotationValidationResult
{
    public required string Message { get; init; }
    public required IList<DataAnnotationValidationTrailers> Trailers { get; init; }
};