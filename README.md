# PoliNorError.Extensions.DependencyInjection

![PoliNorError.Extensions.DependencyInjection](PoliNorError.png)

The PoliNorError.Extensions.DependencyInjection package extends  [PoliNorError](https://github.com/kolan72/PoliNorError) library to provide integration with Microsoft Dependency Injection.

## ⚡ Quick Start

Get up and running in **3 simple steps**:

### 1. Register policies in DI

```csharp
// Program.cs / Startup.cs
services.AddPoliNorError(
	Assembly.GetExecutingAssembly());
```

This scans your assembly for all `IPolicyBuilder<>` implementations and wires up `IPolicy<T>` automatically.

---

### 2. Define your policy builders

```csharp
public class SomePolicyBuilder : IPolicyBuilder<SomePolicyBuilder>
{
    private readonly ILoggerFactory _logger;
    public SomePolicyBuilder(ILoggerFactory logger) => _logger = logger;

    public IPolicyBase Build() =>
        new RetryPolicy(3)
            .WithErrorProcessor(new RetryLoggingErrorProcessor<SomePolicyBuilder>(
                _logger.CreateLogger<SomePolicyBuilder>()))
            .WithWait(TimeSpan.FromSeconds(3));
}
```

Another example:

```csharp
public class AnotherPolicyBuilder : IPolicyBuilder<AnotherPolicyBuilder>
{
    private readonly ILoggerFactory _logger;
    public AnotherPolicyBuilder(ILoggerFactory logger) => _logger = logger;

    public IPolicyBase Build() =>
        new RetryPolicy(2)
            .WithErrorProcessor(new RetryLoggingErrorProcessor<AnotherPolicyBuilder>(
                _logger.CreateLogger<AnotherPolicyBuilder>()))
            .WithWait(TimeSpan.FromSeconds(1));
}
```
, where `RetryLoggingErrorProcessor<T>`: 
```csharp
class RetryLoggingErrorProcessor<T> : ErrorProcessor
{
    private readonly ILogger<T> _logger;

    public RetryLoggingErrorProcessor(ILogger<T> logger) => _logger = logger;

    public override void Execute(Exception error,
        ProcessingErrorInfo? info = null,
        CancellationToken token = default)
    {
        _logger.LogError(error,
            "An error occurred while doing work on {Attempt} attempt.",
            info.GetRetryCount() + 1);
    }
}

---

### 3. Consume policies in your services

```csharp
public class Worker
{
    private readonly IPolicy<SomePolicyBuilder> _somePolicy;
    private readonly IPolicy<AnotherPolicyBuilder> _anotherPolicy;

    public Worker(IPolicy<SomePolicyBuilder> somePolicy,
                  IPolicy<AnotherPolicyBuilder> anotherPolicy)
    {
        _somePolicy = somePolicy;
        _anotherPolicy = anotherPolicy;
    }

    public async Task DoWorkAsync(CancellationToken token)
    {
        var result1 = await _somePolicy.HandleAsync(MightThrowAsync, false, token).ConfigureAwait(false);
        var result2 = await _anotherPolicy.HandleAsync(MightThrowAsync, false, token).ConfigureAwait(false);
    }

    private async Task MightThrowAsync(CancellationToken token)
    {
        await Task.Delay(100, token);
        throw new SomeException("Something went wrong.");
    }
}
```

✅ That’s it!  
- **Builders** encapsulate configuration.  
- **Consumers** inject `IPolicy<T>` and just use it.  
- **DI** takes care of wiring everything together.

---

## ✨ Key Features

- **`IPolicyBuilder<TBuilder>`**  
  - A builder abstraction for creating policies.  
  - Encapsulates configuration (retry count, wait strategy, error processors, etc.).  
  - Registered automatically into DI via assembly scanning.  

- **`IPolicy<T>`**  
  - A closed generic wrapper that represents a policy built by a specific builder.  
  - Resolved directly from DI, giving consumers a type-safe handle to the correct policy.  
  - Internally backed by `ProxyPolicy<T>` which delegates to the builder’s `Build()` result.  

- **Automatic DI Registration**  
  - `AddPoliNorError()` scans assemblies for all `IPolicyBuilder<>` implementations.  
  - Registers them and wires up `IPolicy<T>` → `ProxyPolicy<T>` automatically.  

---

## 🧩 How It Works

1. You create builder classes that implement `IPolicyBuilder<TBuilder>`.  
2. `AddPoliNorError` registers the open generic mapping `IPolicy<> -> ProxyPolicy<>`.  
3. When a consumer requests `IPolicy<TBuilder>`, DI resolves `ProxyPolicy<TBuilder>`.  
4. The proxy calls the builder’s `Build()` method to produce the actual policy.  
5. All calls (`Handle`, `HandleAsync`, etc.) are delegated to the built policy.  

---

## ✅ Benefits

- **Type-safe DI**: No string keys or manual lookups.  
- **Separation of concerns**: Builders configure, consumers execute.  
- **Discoverable**: Constructor injection makes dependencies explicit.  
- **Testable**: Swap out builders or inject fake policies in tests.  
- **Extensible**:  Add new [PoliNorError](https://github.com/kolan72/PoliNorError) policies by just adding new builders.

---

## 🏆 Samples

See samples folder for concrete example. [![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](samples)

---
