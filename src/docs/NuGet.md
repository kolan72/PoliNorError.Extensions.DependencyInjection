The PoliNorError.Extensions.DependencyInjection package extends  [PoliNorError](https://github.com/kolan72/PoliNorError) library to provide integration with Microsoft Dependency Injection.

## ⚡ Quick Start

Get up and running in **3 simple steps**:

### 1. Register policies in DI

```csharp
// Program.cs
services.AddPoliNorError(
	Assembly.GetExecutingAssembly());
```

This scans your assembly for all `IPolicyBuilder<>` implementations and wires up `IPolicy<T>` automatically.

---

### 2. Define your policy builders

```csharp
public class SomePolicyBuilder : IPolicyBuilder<SomePolicyBuilder>
{
	private readonly ILogger<SomePolicyBuilder> _logger;

	public SomePolicyBuilder(ILogger<SomePolicyBuilder> logger)
	{
		_logger = logger;
	}

	public IPolicyBase Build()
	{
		return new RetryPolicy(3)
				.WithErrorProcessor(new RetryLoggingErrorProcessor(_logger))
				.WithWait(new TimeSpan(0, 0, 3));
	}
}
```

Another example:

```csharp
public class AnotherPolicyBuilder : IPolicyBuilder<AnotherPolicyBuilder>
{
	private readonly ILogger<AnotherPolicyBuilder> _logger;

	public AnotherPolicyBuilder(ILogger<AnotherPolicyBuilder> logger)
	{
		_logger = logger;
	}

	public IPolicyBase Build()
	{
		return new RetryPolicy(2)
			.WithErrorProcessor(new RetryLoggingErrorProcessor(_logger))
			.WithWait(new TimeSpan(0, 0, 1));
	}
}
```
, where `RetryLoggingErrorProcessor`: 
```csharp
public class RetryLoggingErrorProcessor : ErrorProcessor
{
	private readonly ILogger _logger;

	public RetryLoggingErrorProcessor(ILogger logger)
	{
		_logger = logger;
	}

	public override void Execute(Exception error,
								ProcessingErrorInfo? catchBlockProcessErrorInfo = null,
								CancellationToken token = default)
	{
		_logger.LogError(error,
						"An error occurred while doing work on {Attempt} attempt.",
						catchBlockProcessErrorInfo.GetRetryCount() + 1);
	}
}
```
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
		await _somePolicy.HandleAsync(MightThrowAsync, token);
		await _anotherPolicy.HandleAsync(MightThrowAsync, token);
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

**IPolicyBuilder<TBuilder>**  
  - Implemented only in your builders.
  - A builder abstraction for creating policies.  
  - Encapsulates configuration (retry count, wait strategy, error processors, etc.).  
  - Registered automatically into DI via assembly scanning.  
---

**IPolicy<T>**  
  - Consumed only in your services.
  - A closed generic wrapper that represents a policy built by a specific builder.  
  - Resolved directly from DI, giving consumers a type-safe handle to the correct policy.  
  - Internally backed by `ProxyPolicy<T>` which delegates to the builder’s `Build()` result.  
---

**Automatic DI Registration**  
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
- **Extensible**: Add new [PoliNorError](https://github.com/kolan72/PoliNorError) policies by just adding new builders.

---

## 🏆 Samples

See samples folder for concrete example.

---
