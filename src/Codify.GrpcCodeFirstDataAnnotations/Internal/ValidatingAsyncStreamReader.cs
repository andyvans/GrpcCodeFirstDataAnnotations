using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace Codify.GrpcCodeFirstDataAnnotations.Internal;

internal class ValidatingAsyncStreamReader<TRequest>(
    IAsyncStreamReader<TRequest> innerReader,
    Func<TRequest, Task> validator) : IAsyncStreamReader<TRequest>
{
    public async Task<bool> MoveNext(CancellationToken cancellationToken)
    {
        var success = await innerReader.MoveNext(cancellationToken).ConfigureAwait(false);
        if (success)
        {
            await validator.Invoke(Current).ConfigureAwait(false);
        }

        return success;
    }

    public TRequest Current => innerReader.Current;
}
