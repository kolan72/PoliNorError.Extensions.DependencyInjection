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
				.WithPolicyName("SomeRetryPolicy")
				.WithErrorProcessor(new RetryLoggingErrorProcessor(_logger))
				.WithWait(new TimeSpan(0, 0, 3))
				.AddPolicyResultHandler(pr =>
				{
					Log.PolicyFailedToHandleException(
						_logger,
						pr.UnprocessedError,
						pr.PolicyName);
				});
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
			.WithPolicyName("AnotherRetryPolicy")
			.WithErrorProcessor(new RetryLoggingErrorProcessor(_logger))
			.WithWait(new TimeSpan(0, 0, 1))
			.AddPolicyResultHandler(pr =>...);
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
						catchBlockProcessErrorInfo.GetAttemptCount());
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

## 🔥 Advanced Usage: Separation of Concerns with Configurators and Builders

For more complex scenarios, `PoliNorError.Extensions.DependencyInjectio`n supports an advanced pattern that separates policy **creation** from policy **configuration**. 

### Key Concepts:

- `PolicyConfigurator<TPolicy>` — an abstract base class for encapsulating cross‑cutting configuration logic (logging, enrichment, etc.).
- `PolicyBuilder<TPolicy, TConfigurator>` — an abstract base class that encapsulates policy creation and optional configurator wiring.

---

### ✅ Inheriting from PolicyConfigurator<TPolicy>

Create a subclass of `PolicyConfigurator<TPolicy>` and override the `Configure` method, where `TPolicy` is a policy from [PoliNorError](https://github.com/kolan72/PoliNorError) library.
Inheritors of `PolicyConfigurator` are automatically resolved from DI.

---

### ✅ Inheriting from PolicyBuilder<TPolicy, TConfigurator>

Create a subclass of `PolicyBuilder<TPolicy, TConfigurator>` and override the `CreatePolicy` method, where `TPolicy` is a policy from [PoliNorError](https://github.com/kolan72/PoliNorError) library, and `TConfigurator` inherits from `PolicyConfigurator<TPolicy>`.

---

### ✅ When to Use This Pattern

Use custom builders + configurators when:

- You want **policy creation** and **policy configuration** to be separate concerns.
- You want to **reuse the same configurator** across multiple builders.
- You want to keep your builder classes minimal and declarative.

---

### 🧱 Example: Retry Policy With a Custom Configurator

```csharp
public class RetryPolicyConfigurator : PolicyConfigurator<RetryPolicy>
{
	private readonly ILoggerFactory _loggerFactory;

	public RetryPolicyConfigurator(ILoggerFactory loggerFactory)
	{
		_loggerFactory = loggerFactory;
	}

		public override void Configure(RetryPolicy policy)
		{
			var logger = _loggerFactory.CreateLogger(policy.PolicyName);

			policy.WithErrorProcessor(new RetryLoggingErrorProcessor(logger))
				.AddPolicyResultHandler(pr =>
				{
					Log.PolicyFailedToHandleException(
						logger,
						pr.UnprocessedError,
						pr.PolicyName);
				});
		}
}
```
This configurator:

- Receives dependencies via DI (here: `ILoggerFactory`)
- Adds a logging error processor to the policy
- Uses the policy name to create a dedicated logger

```csharp
public class SomePolicyBuilder : PolicyBuilder<RetryPolicy, RetryPolicyConfigurator>, IPolicyBuilder<SomePolicyBuilder>
{
	protected override RetryPolicy CreatePolicy() =>
		new RetryPolicy(3, retryDelay: ConstantRetryDelay.Create(new TimeSpan(0, 0, 3)))
		.WithPolicyName("SomeRetryPolicy");
}
```
This builder:

- Creates a `RetryPolicy` with a fixed delay.
- Assigns a policy name (used later by the configurator).
- Delegates configuration to `RetryPolicyConfigurator`.

Once created, the configurator (a subclass of `PolicyConfigurator`) can be shared across multiple builders:

```csharp
public class AnotherPolicyBuilder : PolicyBuilder<RetryPolicy, RetryPolicyConfigurator>, IPolicyBuilder<AnotherPolicyBuilder>
{
	protected override RetryPolicy CreatePolicy() =>
		new RetryPolicy(2, retryDelay: ConstantRetryDelay.Create(new TimeSpan(0, 0, 1)))
		.WithPolicyName("AnotherRetryPolicy");
}
```
---

## ✅ Benefits of this approach

- **Single Responsibility Principle**: Each class has one clear responsibility
- **Reusability**: Configurators can be shared across multiple policy builders
- **Testability**: Configurators and builders can be tested independently
- **Maintainability**: Changes to configuration logic don't affect creation logic and vice versa

---

## 🏆 Samples

See samples folder for concrete example.

---
