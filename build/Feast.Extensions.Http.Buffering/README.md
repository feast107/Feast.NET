# Feast.Extensions.Http.Buffering

> Don't `EnableBuffering()` in reverse proxy!

## 📕 Usage

`EnableSwitchableBuffering()` returns `IDisposable`

call or auto dispose to stop buffering

```csharp
async (HttpContext context, RequestDelegate next) => 
{
    using(context.Request.EnableSwitchableBuffering())
    {
        //process your request
    }//auto stop buffering before pass your request
    await next(context);
}
```

## ❓Why

`EnableBuffering()` performed well in end-point api, 
however in proxy layer we don't always need to check the whole request body.

When doing proxy, the request body will be completely read and cached,
even if we don't need it.

So we have to find a way to cache the part we need and stop buffering before
starting the proxy.

Thus the method, `EnableSwitchableBuffering()`