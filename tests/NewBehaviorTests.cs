using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace PoliNorError.Extensions.DependencyInjection.Tests
{
	/// <summary>
	/// Unit tests for all new behaviors introduced by the architectural refactoring.
	/// </summary>
	[TestFixture]
	public class NewBehaviorTests
	{
		// -------------------------------------------------------------------------
		// Req 3: Safe Build() in PolicyBuilder — InvalidOperationException guard
		// -------------------------------------------------------------------------

		[Test]
		public void PolicyBuilder_Build_WithoutSetConfigurator_ThrowsInvalidOperationException()
		{
			// Arrange: instantiate the builder directly, bypassing DI (SetConfigurator never called)
			var builder = new SomePolicyBuilder();

			// Act & Assert
			var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
			Assert.That(ex!.Message, Does.Contain(nameof(SomePolicyBuilder)),
				"Exception message should identify the concrete builder type.");
		}

		[Test]
		public void PolicyBuilder_Build_AfterSetConfigurator_ReturnsNonNullPolicy()
		{
			// Arrange: wire up DI so SetConfigurator is called before Build()
			var services = new ServiceCollection();
			services.AddPoliNorError(Assembly.GetExecutingAssembly());
			var provider = services.BuildServiceProvider();

			// Act: resolve IPolicy<SomePolicyBuilder> — this triggers SetConfigurator then Build()
			var policy = provider.GetRequiredService<IPolicy<SomePolicyBuilder>>();

			// Assert
			Assert.That(policy, Is.Not.Null);
			Assert.That(policy.PolicyName, Is.EqualTo("SomePolicy"));
		}

		// -------------------------------------------------------------------------
		// Req 5: ProxyPolicy null-return guard
		// -------------------------------------------------------------------------

		[Test]
		public void ProxyPolicy_Constructor_WhenBuildReturnsNull_ThrowsInvalidOperationException()
		{
			// Arrange: a builder whose Build() returns null
			var nullBuilder = new NullReturningPolicyBuilder();

			// Act & Assert
			var ex = Assert.Throws<InvalidOperationException>(
				() => new ProxyPolicy<NullReturningPolicyBuilder>(nullBuilder, null!));
			Assert.That(ex!.Message, Does.Contain(nameof(NullReturningPolicyBuilder)));
		}

		// -------------------------------------------------------------------------
		// Req 2: AddAllPolicyConfigurators is internal (not public)
		// -------------------------------------------------------------------------

		[Test]
		public void AddAllPolicyConfigurators_IsNotPublic()
		{
			var method = typeof(ServiceCollectionExtensions)
				.GetMethod("AddAllPolicyConfigurators",
					BindingFlags.Static | BindingFlags.Public);

			Assert.That(method, Is.Null,
				"AddAllPolicyConfigurators should not be accessible as a public method.");
		}

		// -------------------------------------------------------------------------
		// Req 1: AddPoliNorError null assembly normalization
		// -------------------------------------------------------------------------

		[Test]
		public void AddPoliNorError_NullAssembly_RegistersTypesFromCallingAssembly()
		{
			// Arrange: pass null — should fall back to the calling assembly (this test assembly),
			// which contains TestPolicyBuilderA and TestPolicyBuilderB.
			var services = new ServiceCollection();
			services.AddPoliNorError(assemblyToScan: null);

			// Assert: builders from the test assembly are registered
			var builderA = services.FirstOrDefault(d => d.ImplementationType == typeof(TestPolicyBuilderA));
			Assert.That(builderA, Is.Not.Null,
				"TestPolicyBuilderA from the calling assembly should be registered when null is passed.");
		}

		// -------------------------------------------------------------------------
		// Req 4: IPolicyBuilder<> discovery without explicit re-declaration
		// -------------------------------------------------------------------------

		[Test]
		public void AddAllPolicyBuilders_DiscoversBuildersWithoutExplicitIPolicyBuilderDeclaration()
		{
			// Arrange: SomePolicyBuilder inherits PolicyBuilder<,,TBuilder> without re-declaring
			// IPolicyBuilder<SomePolicyBuilder> — the base class satisfies the contract.
			var services = new ServiceCollection();
			services.AddAllPolicyBuilders(Assembly.GetExecutingAssembly());

			// Assert: SomePolicyBuilder is discovered and registered
			var descriptor = services.FirstOrDefault(
				d => d.ImplementationType == typeof(SomePolicyBuilder));
			Assert.That(descriptor, Is.Not.Null,
				"SomePolicyBuilder should be discovered even without an explicit IPolicyBuilder<T> re-declaration.");
		}

		// -------------------------------------------------------------------------
		// Req 7: AddPoliNorError idempotence — no duplicate IPolicy<> registration
		// -------------------------------------------------------------------------

		[Test]
		public void AddPoliNorError_WhenIPolicyAlreadyRegistered_DoesNotAddDuplicate()
		{
			// Arrange: pre-register IPolicy<> manually
			IServiceCollection services = new ServiceCollection();
			services.Add(new ServiceDescriptor(typeof(IPolicy<>), typeof(ProxyPolicy<>), ServiceLifetime.Singleton));

			// Act: call AddPoliNorError — TryAdd should be a no-op for IPolicy<>
			services.AddPoliNorError(Assembly.GetExecutingAssembly());

			// Assert: still exactly one IPolicy<> descriptor
			var count = services.Count(d => d.ServiceType == typeof(IPolicy<>));
			Assert.That(count, Is.EqualTo(1));
		}

		// -------------------------------------------------------------------------
		// Req 6: params Assembly[] overload
		// -------------------------------------------------------------------------

		[Test]
		public void AddPoliNorError_ParamsOverload_NoArguments_RegistersTypesFromCallingAssembly()
		{
			// Arrange: call with no assembly arguments — should fall back to calling assembly
			var services = new ServiceCollection();
			services.AddPoliNorError();

			// Assert: builders from the test assembly are registered
			var builderA = services.FirstOrDefault(d => d.ImplementationType == typeof(TestPolicyBuilderA));
			Assert.That(builderA, Is.Not.Null,
				"TestPolicyBuilderA should be registered when AddPoliNorError() is called with no assemblies.");
		}

		[Test]
		public void AddPoliNorError_ParamsOverload_TwoAssemblies_RegistersBuildersFromBoth()
		{
			// Arrange: use the test assembly (has builders) and the library assembly (has none)
			var testAssembly = Assembly.GetExecutingAssembly();
			var libAssembly = typeof(ServiceCollectionExtensions).Assembly;

			var services = new ServiceCollection();
			services.AddPoliNorError(ServiceLifetime.Transient, testAssembly, libAssembly);

			// Assert: builders from the test assembly are present
			var builderA = services.FirstOrDefault(d => d.ImplementationType == typeof(TestPolicyBuilderA));
			Assert.That(builderA, Is.Not.Null,
				"TestPolicyBuilderA from the test assembly should be registered.");

			// And IPolicy<> is registered exactly once
			Assert.That(services.Count(d => d.ServiceType == typeof(IPolicy<>)), Is.EqualTo(1));
		}

		// -------------------------------------------------------------------------
		// AddPolicyRegistry Tests
		// -------------------------------------------------------------------------

		[Test]
		public void AddPolicyRegistry_WithTransientLifetime_RegistersPoliciesAsTransient()
		{
			// Arrange
			var services = new ServiceCollection();
			services.AddPoliNorError(Assembly.GetExecutingAssembly(), ServiceLifetime.Transient);

			// Act
			services.AddPolicyRegistry(
				builder => builder.AddPolicy<TestPolicyBuilder>(),
				ServiceLifetime.Transient);

			// Assert
			var descriptor = services.FirstOrDefault(d => 
				d.ServiceType == typeof(IPolicyBase) && 
				d.ServiceKey?.Equals(typeof(TestPolicyBuilder)) == true);
			
			Assert.That(descriptor, Is.Not.Null);
			Assert.That(descriptor!.Lifetime, Is.EqualTo(ServiceLifetime.Transient));
		}

		[Test]
		public void AddPolicyRegistry_WithScopedLifetime_RegistersPoliciesAsScoped()
		{
			// Arrange
			var services = new ServiceCollection();
			services.AddPoliNorError(Assembly.GetExecutingAssembly(), ServiceLifetime.Scoped);

			// Act
			services.AddPolicyRegistry(
				builder => builder.AddPolicy<TestPolicyBuilder>(),
				ServiceLifetime.Scoped);

			// Assert
			var descriptor = services.FirstOrDefault(d => 
				d.ServiceType == typeof(IPolicyBase) && 
				d.ServiceKey?.Equals(typeof(TestPolicyBuilder)) == true);
			
			Assert.That(descriptor, Is.Not.Null);
			Assert.That(descriptor!.Lifetime, Is.EqualTo(ServiceLifetime.Scoped));
		}

		[Test]
		public void AddPolicyRegistry_WithSingletonLifetime_RegistersPoliciesAsSingleton()
		{
			// Arrange
			var services = new ServiceCollection();
			services.AddPoliNorError(Assembly.GetExecutingAssembly(), ServiceLifetime.Singleton);

			// Act
			services.AddPolicyRegistry(
				builder => builder.AddPolicy<TestPolicyBuilder>(),
				ServiceLifetime.Singleton);

			// Assert
			var descriptor = services.FirstOrDefault(d => 
				d.ServiceType == typeof(IPolicyBase) && 
				d.ServiceKey?.Equals(typeof(TestPolicyBuilder)) == true);
			
			Assert.That(descriptor, Is.Not.Null);
			Assert.That(descriptor!.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
		}

		[Test]
		public void AddPolicyRegistry_DefaultLifetime_IsTransient()
		{
			// Arrange
			var services = new ServiceCollection();
			services.AddPoliNorError(Assembly.GetExecutingAssembly());

			// Act - no lifetime specified, should default to Transient
			services.AddPolicyRegistry(builder => builder.AddPolicy<TestPolicyBuilder>());

			// Assert
			var descriptor = services.FirstOrDefault(d => 
				d.ServiceType == typeof(IPolicyBase) && 
				d.ServiceKey?.Equals(typeof(TestPolicyBuilder)) == true);
			
			Assert.That(descriptor, Is.Not.Null);
			Assert.That(descriptor!.Lifetime, Is.EqualTo(ServiceLifetime.Transient));
		}

		[Test]
		public void AddPolicyRegistry_RegistryIsAlwaysSingleton()
		{
			// Arrange
			var services = new ServiceCollection();
			services.AddPoliNorError(Assembly.GetExecutingAssembly());

			// Act - call with different lifetimes
			services.AddPolicyRegistry(builder => builder.AddPolicy<TestPolicyBuilder>(), ServiceLifetime.Transient);
			services.AddPolicyRegistry(builder => builder.AddPolicy<AnotherPolicyBuilder>(), ServiceLifetime.Scoped);

			// Assert - registry should be singleton and registered only once
			var registryDescriptors = services.Where(d => d.ServiceType == typeof(IPolicyRegistry)).ToList();
			Assert.That(registryDescriptors, Has.Count.EqualTo(1));
			Assert.That(registryDescriptors[0].Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
		}

		[Test]
		public void AddPolicyRegistry_CalledMultipleTimes_DoesNotDuplicateRegistry()
		{
			// Arrange
			var services = new ServiceCollection();
			services.AddPoliNorError(Assembly.GetExecutingAssembly());

			// Act - call multiple times
			services.AddPolicyRegistry(builder => builder.AddPolicy<TestPolicyBuilder>());
			services.AddPolicyRegistry(builder => builder.AddPolicy<AnotherPolicyBuilder>());
			services.AddPolicyRegistry(builder => builder.AddPolicy<ThirdPolicyBuilder>());

			// Assert - registry should be registered only once
			var registryDescriptors = services.Where(d => d.ServiceType == typeof(IPolicyRegistry)).ToList();
			Assert.That(registryDescriptors, Has.Count.EqualTo(1), 
				"Registry should be registered exactly once regardless of how many times AddPolicyRegistry is called");
		}

		[Test]
		public void TransientPolicies_CreateNewInstanceEachTime()
		{
			// Arrange
			var services = new ServiceCollection();
			services.AddLogging();
			services.AddPoliNorError(Assembly.GetExecutingAssembly(), ServiceLifetime.Transient);
			services.AddPolicyRegistry(
				builder => builder.AddPolicy<StatefulPolicyBuilder>(),
				ServiceLifetime.Transient);

			var provider = services.BuildServiceProvider();
			var registry = provider.GetRequiredService<IPolicyRegistry>();

			// Act
			var policy1 = registry.GetPolicy<StatefulPolicyBuilder>();
			var policy2 = registry.GetPolicy<StatefulPolicyBuilder>();

			// Assert - different instances
			Assert.That(policy1, Is.Not.SameAs(policy2));
		}

		[Test]
		public void SingletonPolicies_ReuseSameInstance()
		{
			// Arrange
			var services = new ServiceCollection();
			services.AddLogging();
			services.AddPoliNorError(Assembly.GetExecutingAssembly(), ServiceLifetime.Singleton);
			services.AddPolicyRegistry(
				builder => builder.AddPolicy<StatefulPolicyBuilder>(),
				ServiceLifetime.Singleton);
			
			var provider = services.BuildServiceProvider();
			var registry = provider.GetRequiredService<IPolicyRegistry>();

			// Act
			var policy1 = registry.GetPolicy<StatefulPolicyBuilder>();
			var policy2 = registry.GetPolicy<StatefulPolicyBuilder>();

			// Assert - same instance
			Assert.That(policy1, Is.SameAs(policy2));
		}

		[Test]
		public void AddPolicyRegistry_UsesIServiceProvider_NotCast()
		{
			// Arrange
			var services = new ServiceCollection();
			services.AddPoliNorError(Assembly.GetExecutingAssembly());
			services.AddPolicyRegistry(builder => builder.AddPolicy<TestPolicyBuilderA>());
			var provider = services.BuildServiceProvider();

			// Act - registry should resolve without needing IKeyedServiceProvider cast
			var registry = provider.GetRequiredService<IPolicyRegistry>();
			var policy = registry.GetPolicy<TestPolicyBuilderA>();

			// Assert
			Assert.That(policy, Is.Not.Null);
		}
	}

	// -------------------------------------------------------------------------
	// Test doubles used only in this file
	// -------------------------------------------------------------------------

	/// <summary>A builder whose Build() deliberately returns null to test the ProxyPolicy guard.</summary>
	public class NullReturningPolicyBuilder : IPolicyBuilder<NullReturningPolicyBuilder>
	{
#pragma warning disable CS8603 // Possible null reference return — intentional for testing
		public IPolicyBase Build() => null!;
#pragma warning restore CS8603
	}
}
