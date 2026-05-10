using System.ComponentModel.DataAnnotations;
using ProtoBuf;

namespace Codify.GrpcCodeFirstDataAnnotations.TestServer.Models;

[ProtoContract]
public class HelloCodeFirstRequest
{
    [ProtoMember(1)]
    [Required]
    public required string Name { get; init; }

    [ProtoMember(2)]
    [MinLength(4)]
    public required string Action { get; init; }

    [ProtoMember(3)]
    [Range(0, 120)]
    public required int Age { get; init; }

    [ProtoMember(4)]
    [Range(typeof(TimeSpan), "00:30:00", "08:00:00")]
    public required TimeSpan Duration { get; init; }
}