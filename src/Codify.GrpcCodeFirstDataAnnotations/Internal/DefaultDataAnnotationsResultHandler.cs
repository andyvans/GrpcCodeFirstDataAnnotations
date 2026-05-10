using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Codify.GrpcCodeFirstDataAnnotations.Contracts;
using Codify.GrpcCodeFirstDataAnnotations.Exceptions;
using Codify.GrpcCodeFirstDataAnnotations.Models;

namespace Codify.GrpcCodeFirstDataAnnotations.Internal;

internal class DefaultDataAnnotationsResultHandler : IDataAnnotationsResultHandler
{
    public Task<DataAnnotationValidationResult> HandleAsync(IList<ValidationResult> failures)
    {
        var message = string.Join("\n", failures.Select(f => f.ErrorMessage));

        var trailers = failures.Select(f => new DataAnnotationValidationTrailers
        {
            PropertyNames = f.MemberNames,
            ErrorMessage = f.ErrorMessage
        }).ToList();

        var result = new DataAnnotationValidationResult
        {
            Message = message,
            Trailers = trailers
        };

        return Task.FromResult(result);
    }
}