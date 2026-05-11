using System.Collections.Generic;
using Codify.GrpcCodeFirstDataAnnotations.Exceptions;

namespace Codify.GrpcCodeFirstDataAnnotations.Models;

/// <summary>
///     Represents the result of a data annotation validation, including the validation message and associated trailers.
/// </summary>
public record DataAnnotationValidationResult
{
    /// <summary>
    ///     The message content associated with this instance.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    ///     The collection of validation trailers associated with the data annotation process.
    /// </summary>    
    public required IList<DataAnnotationValidationTrailers> Trailers { get; init; }
};