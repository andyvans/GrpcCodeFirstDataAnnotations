using Microsoft.Extensions.Logging;

namespace Codify.GrpcCodeFirstDataAnnotations.Models;

/// <summary>
///     Represents configuration options for data annotation validation behavior.
/// </summary>
public record DataAnnotationValidationOptions
{
    /// <summary>
    ///     Gets or sets the log level used when a validation failure occurs.
    /// </summary>
    public LogLevel ValidationFailureLogLevel { get; set; } = LogLevel.Information;
}
