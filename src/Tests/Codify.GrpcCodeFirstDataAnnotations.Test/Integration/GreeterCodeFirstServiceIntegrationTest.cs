using AwesomeAssertions;
using Codify.GrpcCodeFirstDataAnnotations.Exceptions;
using Codify.GrpcCodeFirstDataAnnotations.TestServer;
using Codify.GrpcCodeFirstDataAnnotations.TestServer.Models;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Grpc.Client;
using ProtoBuf.Grpc.Server;
using Xunit;

namespace Codify.GrpcCodeFirstDataAnnotations.Test.Integration;

public class GreeterCodeFirstServiceIntegrationTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GreeterCodeFirstServiceIntegrationTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory
            .WithWebHostBuilder(builder => builder
                .ConfigureTestServices(services =>
                {
                    // Add Microsoft code first gRPC services
                    services.AddCodeFirstGrpc();

                    // Add Codify.GrpcCodeFirstDataAnnotations services and enable validation
                    services.AddGrpcDataAnnotationValidation(options =>
                    {
                        options.ValidateRequiredNullableProperties = true;
                    });

                    // Enable validation in gRPC options
                    services.AddGrpc(options => options.EnableDataAnnotationValidation());
                }));
    }

    [Fact]
    public void Should_ResponseMessage_When_MessageIsValid()
    {
        var client = CreateClient();

        // Run test
        var response = client.SayHello(new HelloCodeFirstRequest
        {
            Name = "Alice",
            Action = "greet",
            Age = 30,
            Duration = TimeSpan.FromHours(1),
            AdditionalInfo = "extra",
            MoreInfo = new MoreInfo { Info = "info", ArrayData = ["a"] },
            MoreInfoArray = [new MoreInfo { Info = "info", ArrayData = ["a"] }]
        }, default);

        // Verify
        response.Name.Should().Be("Alice");
    }

    [Fact]
    public void Should_ThrowInvalidArgument_When_NameIsEmpty()
    {
        var client = CreateClient();

        // Run test
        void Action()
        {
            client.SayHello(new HelloCodeFirstRequest
            {
                Name = "",
                Action = "greet",
                Age = 30,
                Duration = TimeSpan.FromHours(1),
                AdditionalInfo = "extra",
                MoreInfo = new MoreInfo { Info = "info", ArrayData = ["a"] },
                MoreInfoArray = [new MoreInfo { Info = "info", ArrayData = ["a"] }]
            }, default);
        }

        // Verify
        var rpcException = FluentActions.Invoking(Action).Should().Throw<RpcException>().Which;
        rpcException.Status.StatusCode.Should().Be(StatusCode.InvalidArgument);
        rpcException.Status.Detail.Should().Be("The Name field is required.");

        var errors = rpcException.GetValidationErrors();
        errors.Should().BeEquivalentTo(
        [
            new DataAnnotationValidationTrailers
            {
                PropertyNames = ["Name"],
                ErrorMessage = "The Name field is required."
            }
        ]);
    }

    [Fact]
    public void Should_ThrowInvalidArgument_When_ActionIsTooShort()
    {
        var client = CreateClient();

        // Run test
        void Action()
        {
            client.SayHello(new HelloCodeFirstRequest
            {
                Name = "Alice",
                Action = "run",
                Age = 30,
                Duration = TimeSpan.FromHours(1),
                AdditionalInfo = "extra",
                MoreInfo = new MoreInfo { Info = "info", ArrayData = ["a"] },
                MoreInfoArray = [new MoreInfo { Info = "info", ArrayData = ["a"] }]
            }, default);
        }

        // Verify
        var rpcException = FluentActions.Invoking(Action).Should().Throw<RpcException>().Which;
        rpcException.Status.StatusCode.Should().Be(StatusCode.InvalidArgument);
        rpcException.Status.Detail.Should().Be("The field Action must be a string or array type with a minimum length of '4'.");

        var errors = rpcException.GetValidationErrors();
        errors.Should().BeEquivalentTo(
        [
            new DataAnnotationValidationTrailers
            {
                PropertyNames = ["Action"],
                ErrorMessage = "The field Action must be a string or array type with a minimum length of '4'."
            }
        ]);
    }

    [Fact]
    public void Should_ThrowInvalidArgument_When_AgeIsOutOfRange()
    {
        var client = CreateClient();

        // Run test
        void Action()
        {
            client.SayHello(new HelloCodeFirstRequest
            {
                Name = "Alice",
                Action = "greet",
                Age = 150,
                Duration = TimeSpan.FromHours(1),
                AdditionalInfo = "extra",
                MoreInfo = new MoreInfo { Info = "info", ArrayData = ["a"] },
                MoreInfoArray = [new MoreInfo { Info = "info", ArrayData = ["a"] }]
            }, default);
        }

        // Verify
        var rpcException = FluentActions.Invoking(Action).Should().Throw<RpcException>().Which;
        rpcException.Status.StatusCode.Should().Be(StatusCode.InvalidArgument);
        rpcException.Status.Detail.Should().Be("The field Age must be between 0 and 120.");

        var errors = rpcException.GetValidationErrors();
        errors.Should().BeEquivalentTo(
        [
            new DataAnnotationValidationTrailers
            {
                PropertyNames = ["Age"],
                ErrorMessage = "The field Age must be between 0 and 120."
            }
        ]);
    }

    [Fact]
    public async Task Should_StreamResponses_When_ServerStreamingWithValidRequest()
    {
        var client = CreateClient();

        // Run test
        var responses = new List<HelloCodeFirstResponse>();
        await foreach (var response in client.SayHelloServerStream(new HelloCodeFirstRequest
        {
            Name = "Alice",
            Action = "greet",
            Age = 30,
            Duration = TimeSpan.FromHours(1),
            AdditionalInfo = "extra",
            MoreInfo = new MoreInfo { Info = "info", ArrayData = ["a"] },
            MoreInfoArray = [new MoreInfo { Info = "info", ArrayData = ["a"] }]
        }, default))
        {
            responses.Add(response);
        }

        // Verify
        responses.Count.Should().Be(5);
        responses[0].Name.Should().Be("Alice #0");
        responses[4].Name.Should().Be("Alice #4");
    }

    [Fact]
    public async Task Should_ThrowInvalidArgument_When_ServerStreamingWithInvalidRequest()
    {
        var client = CreateClient();

        // Run test
        async Task Action()
        {
            await foreach (var _ in client.SayHelloServerStream(new HelloCodeFirstRequest
            {
                Name = "",
                Action = "greet",
                Age = 30,
                Duration = TimeSpan.FromHours(1),
                AdditionalInfo = "extra",
                MoreInfo = new MoreInfo { Info = "info", ArrayData = ["a"] },
                MoreInfoArray = [new MoreInfo { Info = "info", ArrayData = ["a"] }]
            }, default))
            {
            }
        }

        // Verify
        var rpcException = await FluentActions.Invoking(Action).Should().ThrowAsync<RpcException>();
        rpcException.Which.Status.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task Should_AggregateNames_When_ClientStreaming()
    {
        var client = CreateClient();

        async IAsyncEnumerable<HelloCodeFirstRequest> GetRequests()
        {
            yield return new HelloCodeFirstRequest { Name = "Alice", Action = "greet", Age = 30, Duration = TimeSpan.FromHours(1), AdditionalInfo = "extra", MoreInfo = new MoreInfo { Info = "info", ArrayData = ["a"] }, MoreInfoArray = [new MoreInfo { Info = "info", ArrayData = ["a"] }] };
            yield return new HelloCodeFirstRequest { Name = "Bob", Action = "wave", Age = 25, Duration = TimeSpan.FromHours(2), AdditionalInfo = "extra", MoreInfo = new MoreInfo { Info = "info", ArrayData = ["a"] }, MoreInfoArray = [new MoreInfo { Info = "info", ArrayData = ["a"] }] };
            await Task.CompletedTask;
        }

        // Run test
        var response = await client.SayHelloClientStream(GetRequests(), default);

        // Verify
        response.Name.Should().Be("Alice, Bob");
    }

    [Fact]
    public async Task Should_EchoResponses_When_DuplexStreaming()
    {
        var client = CreateClient();

        async IAsyncEnumerable<HelloCodeFirstRequest> GetRequests()
        {
            yield return new HelloCodeFirstRequest { Name = "Alice", Action = "greet", Age = 30, Duration = TimeSpan.FromHours(1), AdditionalInfo = "extra", MoreInfo = new MoreInfo { Info = "info", ArrayData = ["a"] }, MoreInfoArray = [new MoreInfo { Info = "info", ArrayData = ["a"] }] };
            yield return new HelloCodeFirstRequest { Name = "Bob", Action = "wave", Age = 25, Duration = TimeSpan.FromHours(2), AdditionalInfo = "extra", MoreInfo = new MoreInfo { Info = "info", ArrayData = ["a"] }, MoreInfoArray = [new MoreInfo { Info = "info", ArrayData = ["a"] }] };
            await Task.CompletedTask;
        }

        // Run test
        var responses = new List<HelloCodeFirstResponse>();
        await foreach (var response in client.SayHelloDuplexStream(GetRequests(), default))
        {
            responses.Add(response);
        }

        // Verify
        responses.Count.Should().Be(2);
        responses[0].Name.Should().Be("Hello Alice");
        responses[1].Name.Should().Be("Hello Bob");
    }

    [Fact]
    public void Should_ThrowInvalidArgument_When_DurationIsOutOfRange()
    {
        var client = CreateClient();

        // Run test
        void Action()
        {
            client.SayHello(new HelloCodeFirstRequest
            {
                Name = "Alice",
                Action = "greet",
                Age = 30,
                Duration = TimeSpan.FromMinutes(10),
                AdditionalInfo = "extra",
                MoreInfo = new MoreInfo { Info = "info", ArrayData = ["a"] },
                MoreInfoArray = [new MoreInfo { Info = "info", ArrayData = ["a"] }]
            }, default);
        }

        // Verify
        var rpcException = FluentActions.Invoking(Action).Should().Throw<RpcException>().Which;
        rpcException.Status.StatusCode.Should().Be(StatusCode.InvalidArgument);
        rpcException.Status.Detail.Should().Be("The field Duration must be between 00:30:00 and 08:00:00.");

        var errors = rpcException.GetValidationErrors();
        errors.Should().BeEquivalentTo(
        [
            new DataAnnotationValidationTrailers
            {
                PropertyNames = ["Duration"],
                ErrorMessage = "The field Duration must be between 00:30:00 and 08:00:00."
            }
        ]);
    }

    [Fact]
    public void Should_ThrowInvalidArgument_When_AdditionalInfoIsNull()
    {
        var client = CreateClient();

        // Run test
        void Action()
        {
            client.SayHello(new HelloCodeFirstRequest
            {
                Name = "Alice",
                Action = "greet",
                Age = 30,
                Duration = TimeSpan.FromHours(1),
                AdditionalInfo = null,
                MoreInfo = new MoreInfo { Info = "info", ArrayData = ["a"] },
                MoreInfoArray = [new MoreInfo { Info = "info", ArrayData = ["a"] }]
            }, default);
        }

        // Verify
        var rpcException = FluentActions.Invoking(Action).Should().Throw<RpcException>().Which;
        rpcException.Status.StatusCode.Should().Be(StatusCode.InvalidArgument);
        rpcException.Status.Detail.Should().Be("The AdditionalInfo field is required.");

        var errors = rpcException.GetValidationErrors();
        errors.Should().BeEquivalentTo(
        [
            new DataAnnotationValidationTrailers
            {
                PropertyNames = ["AdditionalInfo"],
                ErrorMessage = "The AdditionalInfo field is required."
            }
        ]);
    }

    [Fact]
    public void Should_ThrowInvalidArgument_When_InfoIsNull()
    {
        var client = CreateClient();

        // Run test
        void Action()
        {
            client.SayHello(new HelloCodeFirstRequest
            {
                Name = "Alice",
                Action = "greet",
                Age = 30,
                Duration = TimeSpan.FromHours(1),
                AdditionalInfo = "more",
                MoreInfo = new MoreInfo { Info = null!, ArrayData = ["a"] },
                MoreInfoArray = [new MoreInfo { Info = "info", ArrayData = ["a"] }]
            }, default);
        }

        // Verify
        var rpcException = FluentActions.Invoking(Action).Should().Throw<RpcException>().Which;
        rpcException.Status.StatusCode.Should().Be(StatusCode.InvalidArgument);
        rpcException.Status.Detail.Should().Be("MoreInfo.Info must not be null.");

        var errors = rpcException.GetValidationErrors();
        errors.Should().BeEquivalentTo(
        [
            new DataAnnotationValidationTrailers
            {
                PropertyNames = ["MoreInfo.Info"],
                ErrorMessage = "MoreInfo.Info must not be null."
            }
        ]);
    }

    [Fact]
    public void Should_ThrowInvalidArgument_When_MoreInfoIsNull()
    {
        var client = CreateClient();

        // Run test
        void Action()
        {
            client.SayHello(new HelloCodeFirstRequest
            {
                Name = "Alice",
                Action = "greet",
                Age = 30,
                Duration = TimeSpan.FromHours(1),
                AdditionalInfo = "extra",
                MoreInfo = null,
                MoreInfoArray = [new MoreInfo { Info = "info", ArrayData = ["a"] }]
            }, default);
        }

        // Verify
        var rpcException = FluentActions.Invoking(Action).Should().Throw<RpcException>().Which;
        rpcException.Status.StatusCode.Should().Be(StatusCode.InvalidArgument);
        rpcException.Status.Detail.Should().Be("The MoreInfo field is required.");

        var errors = rpcException.GetValidationErrors();
        errors.Should().BeEquivalentTo(
        [
            new DataAnnotationValidationTrailers
            {
                PropertyNames = ["MoreInfo"],
                ErrorMessage = "The MoreInfo field is required."
            }
        ]);
    }

    [Fact]
    public void Should_ThrowInvalidArgument_When_MoreInfoArrayIsNull()
    {
        var client = CreateClient();

        // Run test
        void Action()
        {
            client.SayHello(new HelloCodeFirstRequest
            {
                Name = "Alice",
                Action = "greet",
                Age = 30,
                Duration = TimeSpan.FromHours(1),
                AdditionalInfo = "extra",
                MoreInfo = new MoreInfo { Info = "info", ArrayData = ["a"] },
                MoreInfoArray = null
            }, default);
        }

        // Verify
        var rpcException = FluentActions.Invoking(Action).Should().Throw<RpcException>().Which;
        rpcException.Status.StatusCode.Should().Be(StatusCode.InvalidArgument);
        rpcException.Status.Detail.Should().Be("The MoreInfoArray field is required.");

        var errors = rpcException.GetValidationErrors();
        errors.Should().BeEquivalentTo(
        [
            new DataAnnotationValidationTrailers
            {
                PropertyNames = ["MoreInfoArray"],
                ErrorMessage = "The MoreInfoArray field is required."
            }
        ]);
    }

    private IGreeterCodeFirst CreateClient()
    {
        return CreateGrpcChannel().CreateGrpcService<IGreeterCodeFirst>();
    }

    private GrpcChannel CreateGrpcChannel()
    {
        var client = _factory.CreateDefaultClient();
        ArgumentNullException.ThrowIfNull(client.BaseAddress);

        return GrpcChannel.ForAddress(client.BaseAddress, new GrpcChannelOptions
        {
            HttpClient = client
        });
    }
}

