# GRPC Code-First Data Annotations

Data annotation validation for gRPC Code-First models in ASP.NET Core. Automatically validates request messages using `System.ComponentModel.DataAnnotations` attributes and 
`required` properties then returns structured validation errors via gRPC trailers.

## Packages

| Package | Description |
|---|---|
| [**Codify.GrpcCodeFirstDataAnnotations**](https://www.nuget.org/packages/Codify.GrpcCodeFirstDataAnnotations) | Server-side interceptor that validates incoming gRPC requests |
| [**Codify.GrpcCodeFirstDataAnnotations.Exceptions**](https://www.nuget.org/packages/Codify.GrpcCodeFirstDataAnnotations.Exceptions) | Client-side helper to read validation errors from gRPC `RpcException` trailers |

## Getting Started

### Server Setup

1. Install the NuGet package:

```shell
dotnet add package Codify.GrpcCodeFirstDataAnnotations
```

2. Register services and enable the validation interceptor:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpcDataAnnotationValidation();
builder.Services.AddCodeFirstGrpc(options =>
{
    options.EnableDataAnnotationValidation();
});
```

3. Add `DataAnnotations` attributes to your request models:

```csharp
[DataContract]
public class CreatePersonRequest
{
    [DataMember(Order = 1)]
    [Required]
    [StringLength(50)]
    public string FirstName { get; set; }

    [DataMember(Order = 2)]
    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [DataMember(Order = 3)]
    [Range(1, 120)]
    public int Age { get; set; }

    [DataMember(Order = 4)]
    [Range(typeof(TimeSpan), "00:30:00", "08:00:00")]
    public TimeSpan SessionDuration { get; set; }

    [DataMember(Order = 5)]    
    public required string Job { get; set; }
}
```

When a request fails validation, the interceptor throws an `RpcException` with `StatusCode.InvalidArgument` and includes the validation errors as Base64-encoded JSON in the gRPC trailers.

### Client Setup

1. Install the client package:

```shell
dotnet add package Codify.GrpcCodeFirstDataAnnotations.Exceptions
```

2. Catch and read validation errors:

```csharp
try
{
    var response = await client.CreatePersonAsync(request);
}
catch (RpcException ex)
{
    var errors = ex.GetValidationErrors();
    foreach (var error in errors)
    {
        Console.WriteLine($"{string.Join(", ", error.PropertyNames)}: {error.ErrorMessage}");
    }
}
```

## Custom Validation Handling

Implement `IDataAnnotationsResultHandler` to customize how validation failures are processed:

```csharp
public class MyCustomHandler : IDataAnnotationsResultHandler
{
    public Task<DataAnnotationValidationResult> HandleAsync(IList<ValidationResult> failures)
    {
        // Custom logic here
    }
}
```

Register it before calling `AddGrpcDataAnnotationValidation`:

```csharp
builder.Services.AddSingleton<IDataAnnotationsResultHandler, MyCustomHandler>();
```

## Supported gRPC Method Types

- Unary
- Server streaming
- Client streaming
- Duplex streaming

For streaming methods, each incoming message is validated as it is read.

## License

See [license.txt](license.txt) for details.
