using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Codify.GrpcCodeFirstDataAnnotations.Models;

namespace Codify.GrpcCodeFirstDataAnnotations.Contracts;

public interface IDataAnnotationsResultHandler
{
    Task<DataAnnotationValidationResult> HandleAsync(IList<ValidationResult> failures);
}