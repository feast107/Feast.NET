using Consul;

namespace Feast.Extensions.ServiceDiscovery.Yarp.Consul;

internal class ConsulNotifyWorker(
    IConsulClient client,
    ConsulClusterContext context,
    ConsulEventHandler onChanged,
    Func<ConsulClusterContext, ConsulQueryOptions> optionsFactory) : IAsyncDisposable
{
    private ulong lastIndex;
    private Task? query;
    
    public void Start(TaskCompletionSource initialize, CancellationToken stop)
    {
        if (query != null) return;
        query = StartInternal(initialize, stop);
    }

    private async Task StartInternal(TaskCompletionSource initialize, CancellationToken stop)
    {
        await QueryAsync(stop);
        initialize.TrySetResult();
        while (!stop.IsCancellationRequested) await QueryAsync(stop);
    }

    private async Task QueryAsync(CancellationToken cancel)
    {
        var option = optionsFactory(context);
        option.WaitIndex  = lastIndex;
        option.Datacenter = context.DataCenter;
        try
        {
            var result = await client.Health.Service(context.Service, option.Tag, option.PassingOnly, option, cancel);
            lastIndex = result.LastIndex;
            await onChanged(this, result.Response, null);
        }
        catch (Exception ex)
        {
            await onChanged(this, [], ex);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (query is not null)
        {
            await query;
            query.Dispose();
        }
    }

    public override string ToString() => $"{nameof(ConsulNotifyWorker)}:[{context.DataCenter}/{context.Service}]";
}