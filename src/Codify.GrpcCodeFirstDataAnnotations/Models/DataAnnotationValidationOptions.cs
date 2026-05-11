using Microsoft.Extensions.Logging;

namespace Codify.GrpcCodeFirstDataAnnotations.Models;

public record DataAnnotationValidationOptions
{
    public LogLevel ValidationFailureLogLevel { get; set; } = LogLevel.Information;
}
