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
    public LogLevel ValidationFailureLogLevel { get; set; } = LogLevel.Debug;

    /// <summary>
    ///     Gets or sets a value indicating whether <c>required</c> non-nullable properties are validated.
    /// </summary>    
    public bool ValidateRequiredNonNullableProperties { get; set; } = false;

    /// <summary>
    ///     Gets or sets the maximum depth to recurse into nested properties when validating
    ///     <c>required</c> non-nullable reference properties.
    /// </summary>
    public int MaxRequiredValidationDepth { get; set; } = 20;
}
