using Consul;

namespace Feast.Extensions.ServiceDiscovery.Yarp.Consul;
 
internal delegate Task ConsulEventHandler(ConsulNotifyWorker worker, ServiceEntry[] entries, Exception? exception);