using Grpc.Core;

namespace Codify.GrpcCodeFirstDataAnnotations.Internal;

internal class ValidationRpcException(Status status, Metadata trailers) : RpcException(status, trailers);
