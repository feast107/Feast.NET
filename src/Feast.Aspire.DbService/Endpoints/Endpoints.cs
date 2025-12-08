namespace Feast.Aspire.DbService.Endpoints;

public static partial class Endpoints
{
    private enum State
    {
        Reject,
        Timeout,
    }

    private static readonly Dictionary<State, bool> States = new()
    {
        { State.Reject, false },
        { State.Timeout, false },
    };

    private static CancellationTokenSource timeoutChanges = new();

    private static       IResult       AsJsonResult<T>(this T data)       => Results.Json(data);
    private static async Task<IResult> AsJsonResult<T>(this Task<T> data) => Results.Json(await data);
}