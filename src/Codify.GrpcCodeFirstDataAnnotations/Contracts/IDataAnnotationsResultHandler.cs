using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Codify.GrpcCodeFirstDataAnnotations.Models;

namespace Codify.GrpcCodeFirstDataAnnotations.Contracts;

/// <summary>
///     Defines a handler for processing the results of data annotation validation failures asynchronously.
/// </summary>
public interface IDataAnnotationsResultHandler
{
    /// <summary>
    ///     Processes a collection of validation failures and returns a result representing the outcome of data annotation validation.
    /// </summary>    
    Task<DataAnnotationValidationResult> HandleAsync(IList<ValidationResult> failures);
}