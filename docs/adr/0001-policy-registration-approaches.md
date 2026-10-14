# ADR-0001: Policy Registration Approaches

**Date:** 2026-09-20  
**Status:** Accepted  
**Author:** Kiro (AI Assistant)  
**Context:** Architectural Refactoring Discussion  
**Related:** `src/ServiceCollectionExtensions.cs`, `samples/KeyedPolicyDemo/`, `samples/PolicyRegistryDemo/`

---

## Status

Accepted — All patterns will be supported with clear guidance on when to use each.

---

## Context

The library provides three mechanisms for registering and consuming resilience policies:

1. **Direct IServiceProvider Resolution** (original/primary approach)
   - Consumer receives `IPolicy<TBuilder>` via constructor injection
   - Policy resolution happens through standard DI container

2. **Keyed Services via `AddKeyedPolicy`** (string-keyed approach)
   - Policy is registered under a string key as a keyed `IPolicy` service
   - Consumer injects it with `[FromKeyedServices("key")]` or resolves it with `GetRequiredKeyedService<IPolicy>("key")`
   - Two overloads: builder-based (`AddKeyedPolicy<TBuilder>(key)`, reusing the `IPolicy<TBuilder>` pipeline) and lambda-based (`AddKeyedPolicy(key, factory)`)

3. **ResiliencePolicyBuilder + IPolicyRegistry** (optional/advanced approach - now PolicyRegistryBuilder + IPolicyRegistry)
   - Consumer receives `IPolicyRegistry` via constructor injection
   - Policy resolution happens through registry abstraction

The registry pattern was initially questioned for potentially introducing a service locator anti-pattern, but further analysis revealed it provides significant value for specific use cases.

**Note on naming:** The registry pattern types have been renamed for clarity:
- `AddResiliencePolicies` → `AddPolicyRegistry`
- `ResiliencePolicyBuilder` → `PolicyRegistryBuilder`
- `IPolicyRegistry.Get` → `GetPolicy`
- `KeyedServicePolicyRegistry` → `PolicyRegistry`

---

## Decision

**Keep all three patterns and document clear guidance on when to use each.**

### Primary Approach: Direct Resolution (1-4 policies)

**Use when:**
- Consumer uses 1-4 policies
- You want explicit constructor dependencies
- Code clarity and discoverability are priorities
- Compile-time safety is important

**Example:**
```csharp
public class MyService
{
    private readonly IPolicy<RetryPolicyBuilder> _retryPolicy;
    private readonly IPolicy<CircuitBreakerBuilder> _circuitBreaker;

    public MyService(
        IPolicy<RetryPolicyBuilder> retryPolicy,
        IPolicy<CircuitBreakerBuilder> circuitBreaker)
    {
        _retryPolicy = retryPolicy;
        _circuitBreaker = circuitBreaker;
    }
}
```

### Intermediate Approach: Keyed Policies (`AddKeyedPolicy`)

**Use when:**
- You need multiple named variants of the same builder (e.g., `"retry-primary"` vs `"retry-relaxed"`)
- Keys come from configuration (config-driven policy selection)
- You want `[FromKeyedServices("key")]` constructor injection
- You need an ad-hoc policy without defining a builder class (lambda overload)

**Example:**
```csharp
services
	.AddKeyedPolicy<SomePolicyBuilder>("retry-primary")
	.AddKeyedPolicy<SomePolicyBuilder>("retry-relaxed", ServiceLifetime.Singleton)
	.AddKeyedPolicy("orders-policy", sp => new RetryPolicy(2)...);

public class MyService
{
	public MyService([FromKeyedServices("retry-primary")] IPolicy retryPolicy) { }
}
```

### Advanced Approach: Registry Pattern (5+ policies)

**Use when:**
- Consumer uses 5+ policies (resilience orchestrator/coordinator)
- You need to avoid constructor bloat
- You need dynamic policy selection based on runtime data
- Building policy factories or strategy patterns

**Example:**
```csharp
public class ResilienceCoordinator
{
    private readonly IPolicyBase _retryPolicy;
    private readonly IPolicyBase _circuitBreaker;
    private readonly IPolicyBase _timeoutPolicy;

    public ResilienceCoordinator(IPolicyRegistry registry)
    {
        _retryPolicy = registry.GetPolicy<RetryPolicyBuilder>(); // Renamed from Get
        _circuitBreaker = registry.GetPolicy<CircuitBreakerBuilder>(); // Renamed from Get
        _timeoutPolicy = registry.GetPolicy<TimeoutPolicyBuilder>(); // Renamed from Get
        // ... additional policies
    }
}
```

---

## Consequences

### Direct Resolution Pros
- **Explicit Dependencies**: Constructor clearly shows what policies are needed
- **DI Best Practices**: Follows standard constructor injection pattern
- **Better Testability**: One-to-one mocking, no intermediate abstraction
- **Better Discoverability**: "Find References" works, IntelliSense shows types
- **Compile-Time Safety**: Wrong type = compile error
- **No Service Locator**: True dependency injection pattern

### Direct Resolution Cons
- **Constructor Bloat**: 10+ policies = unwieldy constructor
- **Refactoring Pain**: Adding a policy requires constructor change
- **Reduced Clarity at Scale**: Long parameter lists hard to read

### Registry Pattern Pros
- **Constructor Cleanliness**: Single parameter regardless of policy count
- **Scalability**: Scales linearly to any number of policies
- **Flexibility**: Can add policies without breaking consumer signatures
- **Runtime Selection**: Can choose policies dynamically based on runtime data
- **Pragmatic Trade-off**: Acceptable when explicit dependencies become unwieldy

### Registry Pattern Cons
- **Service Locator Pattern**: Registry hides actual dependencies
- **Reduced Discoverability**: Can't tell which policies are used from constructor
- **Two-Step Resolution**: Inject registry, then call `.GetPolicy<T>()` // Renamed from Get
- **Runtime Resolution**: Failures happen at construction rather than container validation
- **Additional Abstraction**: Requires understanding registry pattern

### Keyed Policy Pattern Pros
- **Named Variants**: Same builder registered under multiple keys with independent lifetimes
- **Config-Driven**: Keys can come from configuration without code changes
- **Standard .NET API**: Uses the built-in keyed services (`[FromKeyedServices]`, `GetRequiredKeyedService`)
- **Ad-hoc Policies**: Lambda overload creates policies without a builder class

### Keyed Policy Pattern Cons
- **String Keys**: Typo-prone; no compile-time check on key names
- **Reduced Discoverability**: Same trade-off as the registry — dependencies hidden behind keys
- **.NET 8+ Only**: Keyed services API requires modern target frameworks

### Performance Impact
- Direct resolution: One service provider lookup
- Registry pattern: Two lookups (registry + keyed service)
- **Negligible in practice** - policies are cached after first resolution

---

## Alternatives Considered

### Alternative 1: Only Direct Resolution

**Decision:** Rejected because constructor bloat is a real pain for resilience coordinators that legitimately need many policies (e.g., 10+ for API gateway, message processors).

### Alternative 2: Only Registry Pattern

**Decision:** Rejected because:
- Service locator anti-pattern obscures dependencies
- Reduced testability and discoverability
- Adds complexity without benefit for simple cases
- Violates principle of least surprise for .NET developers

### Alternative 3: Automatically Register All Policies in Registry

**Considered:** Auto-register all discovered builders in registry via `AddPolicyRegistry()` extension method.

**Decision:** Deferred for now. Could be added as a convenience method if there's demand, but explicit opt-in (`AddPolicyRegistry`) is clearer for learning the pattern. // Renamed from AddResiliencePolicies

### Alternative 4: Remove Registry Pattern Entirely

**Decision:** Rejected because:
- Legitimate use cases exist (orchestrators, coordinators)
- Constructor over-injection is sometimes necessary, not a design smell
- Similar patterns exist in industry (IHttpClientFactory, IMediator, ILoggerFactory)

---

## Implementation Plan

### Documentation

1. **Update README**: Add section on policy registration approaches
2. **Add API Docs**: XML documentation on `IPolicyRegistry` explaining when to use
3. **Create Migration Guide**: Document when to migrate from direct to registry
4. **Update Samples**: Show both patterns in different sample contexts

### Code Quality

1. **No breaking changes needed** - Both patterns already implemented
2. **Update naming** to improve clarity:
   - `AddResiliencePolicies()` → `AddPolicyRegistry()`
   - `ResiliencePolicyBuilder` → `PolicyRegistryBuilder`
   - `IPolicyRegistry.Get` → `GetPolicy`
   - `KeyedServicePolicyRegistry` → `PolicyRegistry`
3. **Consider auto-registration**: `AddPolicyRegistry()` that auto-registers all builders

### Testing

1. **Add integration tests** for registry pattern samples
2. **Add performance benchmarks** comparing both approaches
3. **Add documentation tests** showing when to use each pattern

---

## Migration Guide

### Migrating from Direct Resolution to Registry Pattern

**When to migrate:** When you have 5+ policies in a consumer and constructor bloat is painful.

**Steps:**

1. **Update constructor:**
   ```csharp
   // Before
   public Worker(IPolicy<Policy1> p1, ..., IPolicy<Policy10> p10) { }

   // After
   public Worker(IPolicyRegistry registry)
   {
       _p1 = registry.GetPolicy<Policy1>(); // Renamed from Get
       // ... _p10 = registry.GetPolicy<Policy10>(); // Renamed from Get
   }
   ```

2. **Update field declarations:**
   ```csharp
   // Before
   private readonly IPolicy<Policy1> _p1;

   // After
   private readonly IPolicyBase _p1;
   ```

3. **Verify no breaking changes** - Registry is opt-in, consumers can mix approaches

### Migrating from Registry Pattern to Direct Resolution

**When to migrate:** When reducing policy count or refactoring to smaller, focused services.

**Steps:**

1. **Update constructor:**
   ```csharp
   // Before
   public Worker(IPolicyRegistry registry) { }

   // After
   public Worker(IPolicy<Policy1> p1, IPolicy<Policy2> p2) { }
   ```

2. **Update field declarations:**
   ```csharp
   // Before
   private readonly IPolicyBase _p1;

   // After
   private readonly IPolicy<Policy1> _p1;
   ```

3. **Remove registry registration** - Only needed if other consumers still use it

---

## Related Documentation

- [Detailed Comparison](../docs/policy-registration-comparison.md) - Full analysis of trade-offs
- [ServiceCollectionExtensions.cs](../src/ServiceCollectionExtensions.cs) - Implementation reference
- [PolicyRegistryDemo sample](../samples/PolicyRegistryDemo/) - Registry pattern example
- [KeyedPolicyDemo sample](../samples/KeyedPolicyDemo/) - Keyed policy example
- [Intro sample](../samples/Intro/) - Direct resolution example

---

## Decision Log

| Date | Decision | Reason |
|------|----------|--------|
| 2024-09-20 | Keep both patterns | Direct for 1-4 policies, registry for 5+ |
| 2024-09-20 | Document scale threshold | Clear guidance on when to switch |
| 2024-09-20 | Define migration paths | Support for evolving architecture |

---

## Notes

### Why Both Patterns Make Sense

The registry pattern here is closer to a **bounded service locator** than the anti-pattern. It:
- Only resolves policies, not arbitrary services
- Maintains type safety via `Get<TBuilder>()`
- Serves a specific purpose (resilience orchestration)
- Has industry precedent (IHttpClientFactory, IMediator)

### Legitimate Multi-Policy Consumers

Having 5+ policies doesn't always violate SRP. Examples of legitimate orchestrators:
- **API Gateway** applying multiple policies per route
- **Message Processor** with retry, circuit breaker, timeout, fallback, bulkhead
- **Resilience Coordinator** selecting policies based on operation type
- **Cross-Cutting Concern** managing multiple aspects of resilience

These are not "god objects" - they're the **policy orchestrator pattern**, which is a valid architectural style for resilience.

### Future Considerations

If you later add:
- **Policy Combinators**: Composite policies that combine others
- **Policy Collections**: Grouping policies by concern
- **Auto-Registry**: `AddPolicyRegistry()` that auto-registers all builders

These would complement, not replace, the existing patterns.

---

## References

- [Explicit Dependencies Principle](https://freecontent.manning.com/understanding-the-dependency-inversion-principle/)
- [Service Locator Anti-Pattern](https://freecontent.manning.com/hiding-the-service-locator-anti-pattern/)
- [IHttpClientFactory Documentation](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests)
- [MediatR Documentation](https://github.com/jbogard/MediatR)