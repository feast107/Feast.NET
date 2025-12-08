# Feast.Extensions.ServiceDiscovery.Yarp.Consul

> Use `Consul` as the destination resolving backend of `Yarp`

## 📦️ Dependencies

+ [Yarp.ReverseProxy](https://www.nuget.org/packages/Yarp.ReverseProxy) >2.1.0
+ [Consul](https://www.nuget.org/packages/Consul) >1.6.1.1

## 📕 Usage

in `Program.cs`

```csharp
// any chance we can resolve IConsulClient from service provider
builder.Services.Add<IConsulClient>();

// configure Yarp
builder.Services.AddReverseProxy()
    .LoadFrom(whatever_it_is)
    .AddConsulDestinationResolver(); // add consul as destination resolver
```

in `Yarp` configuration `whatever_it_is.json` like

```json
{
  "Routes": {
    "My-Service": {
      "ClusterId": "NotImportant",
      "Match": {
        "Path": "{**all}"
      }
    }
  },
  "Clusters": {
    "NotImportant": {
      "Destinations": {
        "consul/dc1": {
          "Address": "https+http://my-service"
        },
        "others will still working" : {
          "Address" : "..."
        }
      }
    }
  }
}
```

In `Destinations` field, the name should be formatted like `consul/dc{number}`
indicates that this destination points to consul

the `Host` of `Address` will be filled into the query as `service` field,
and send request like

```
http://your-consul/dc1/health/service/my-service
```

## 🛠 Customize

If you want to self-resolve naming policy instead of `consul/dc` like, you can

```csharp
builder.Services.AddReverseProxy()
    .AddConsulDestinationResolver(o => 
    {
        o.TransformContext = (name, config) => 
        {
            // your custom strategy, just tell me datacenter and service as well
        }
    });
```

Also including query options configuring

```csharp
builder.Services.AddReverseProxy()
    .AddConsulDestinationResolver(o => 
    {
        o.OptionsFactory = context => 
        {
            return your options;
        }
    });
```