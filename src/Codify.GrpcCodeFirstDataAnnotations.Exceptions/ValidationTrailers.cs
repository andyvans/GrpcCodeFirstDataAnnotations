using System;
using System.Collections.Generic;

namespace Codify.GrpcCodeFirstDataAnnotations.Exceptions;

[Serializable]
public class DataAnnotationValidationTrailers
{
    public IEnumerable<string> PropertyNames { get; set; }
    public string ErrorMessage { get; set; }
}