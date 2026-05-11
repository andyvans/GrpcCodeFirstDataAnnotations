using System;
using System.Collections.Generic;

namespace Codify.GrpcCodeFirstDataAnnotations.Exceptions;

[Serializable]
public class DataAnnotationValidationTrailers
{
    public required IEnumerable<string> PropertyNames { get; set; }
    public required string? ErrorMessage { get; set; }
}