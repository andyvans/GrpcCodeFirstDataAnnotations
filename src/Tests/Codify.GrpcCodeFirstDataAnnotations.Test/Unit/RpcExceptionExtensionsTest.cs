using System.Text.Json;
using AwesomeAssertions;
using Codify.GrpcCodeFirstDataAnnotations.Exceptions;
using Grpc.Core;
using Xunit;

namespace Codify.GrpcCodeFirstDataAnnotations.Test.Unit;

public class RpcExceptionExtensionsTest
{
    [Fact]
    public void GetFormattedValidationErrors_NoTrailers()
    {
        var exception = new RpcException(new Status(StatusCode.InvalidArgument, "bad"), new Metadata());

        var result = exception.GetFormattedValidationErrors();
        result.Should().BeNull();
    }

    [Fact]
    public void GetFormattedValidationErrors()
    {
        var errors = new List<DataAnnotationValidationTrailers>
        {
            new() { PropertyNames = ["FirstName"], ErrorMessage = "The FirstName field is required." },
            new() { PropertyNames = ["Email", "ContactEmail"], ErrorMessage = "Invalid email address." }
        };

        var metadata = new Metadata
        {
            { RpcExceptionExtensions.TrailerKey, JsonSerializer.Serialize(errors) }
        };
        var exception = new RpcException(new Status(StatusCode.InvalidArgument, "bad"), metadata);

        var result = exception.GetFormattedValidationErrors();
        result.Should().Be("FirstName: The FirstName field is required.\nEmail, ContactEmail: Invalid email address.");
    }

    [Fact]
    public void EmptyList_When_NoTrailers()
    {
        var exception = new RpcException(new Status(StatusCode.InvalidArgument, "bad"), new Metadata());
        
        var result = exception.GetValidationErrors();
        result.Should().BeEmpty();
    }
}
