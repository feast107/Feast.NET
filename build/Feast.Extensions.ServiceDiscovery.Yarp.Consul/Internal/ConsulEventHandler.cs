using Consul;

namespace Feast.Extensions.ServiceDiscovery.Internal;
 
internal delegate Task ConsulEventHandler(ConsulNotifyWorker worker, ServiceEntry[] entries, Exception? exception);