using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Reflection;

namespace PoliNorError.Extensions.DependencyInjection.Tests
{
	/// <summary>
	/// Tests for keyed-services support: the <c>AddKeyedPolicy</c> registration
	/// overloads and the <c>GetKeyedPolicy</c>/<c>GetRequiredKeyedPolicy</c>
	/// resolver extensions.
	/// </summary>
	[TestFixture]
	public class KeyedPolicyTests
	{
		private const string PolicyKeyA = "policy-a";
		private const string PolicyKeyB = "policy-b";

		// -------------------------------------------------------------------------
		// Factory-based AddKeyedPolicy(key, factory, lifetime)
		// -------------------------------------------------------------------------

		[Test]
		public void Should_Resolve_Keyed_Policy_By_Const_String_Key()
		{
			// Arrange
			var services = new ServiceCollection();
			services.AddKeyedPolicy(PolicyKeyA, _ => new TestPolicy("PolicyA"));

			// Act
			var provider = services.BuildServiceProvider();
			var policy = provider.GetKeyedPolicy(PolicyKeyA);

			// Assert
			Assert.That(policy, Is.InstanceOf<IPolicy>());
			Assert.That(policy?.PolicyName, Is.EqualTo("PolicyA"));
		}

		[Test]
		public void Should_Resolve_Different_Policies_By_Distinct_Keys()
		{
			// Arrange
			var services = new ServiceCollection();
			services.AddKeyedPolicy(PolicyKeyA, _ => new TestPolicy("PolicyA"));
			services.AddKeyedPolicy(PolicyKeyB, _ => new TestPolicy("PolicyB"));
			var provider = services.BuildServiceProvider();

			// Act
			var policyA = provider.GetRequiredKeyedPolicy(PolicyKeyA);
			var policyB = provider.GetRequiredKeyedPolicy(PolicyKeyB);

			// Assert
			Assert.That(policyA.PolicyName, Is.EqualTo("PolicyA"));
			Assert.That(policyB.PolicyName, Is.EqualTo("PolicyB"));
			Assert.That(policyA, Is.Not.SameAs(policyB));
			Assert.That(provider.GetKeyedServices<IPolicy>(PolicyKeyA).Count(), Is.EqualTo(1));
		}

		[Test]
		public void Should_Return_Null_From_GetKeyedPolicy_For_Unknown_Key()
		{
			// Arrange
			var services = new ServiceCollection();
			services.AddKeyedPolicy(PolicyKeyA, _ => new TestPolicy("PolicyA"));
			var provider = services.BuildServiceProvider();

			// Act & Assert
			Assert.That(provider.GetKeyedPolicy("unknown-key"), Is.Null);
			Assert.Throws<InvalidOperationException>(
				() => provider.GetRequiredKeyedPolicy("unknown-key"));
		}

		[Test]
		public void Should_Create_Fresh_Policy_On_Each_Resolution_For_Transient_Lifetime()
		{
			// Arrange
			int factoryCalls = 0;
			var services = new ServiceCollection();
			services.AddKeyedPolicy(PolicyKeyA,
				_ => { factoryCalls++; return new TestPolicy("PolicyA"); });
			var provider = services.BuildServiceProvider();

			// Act
			var policy1 = provider.GetRequiredKeyedPolicy(PolicyKeyA);
			var policy2 = provider.GetRequiredKeyedPolicy(PolicyKeyA);

			// Assert
			Assert.That(policy1, Is.Not.SameAs(policy2));
			Assert.That(factoryCalls, Is.EqualTo(2));
		}

		[Test]
		public void Should_Scope_Policy_Per_Created_Scope_For_Scoped_Lifetime()
		{
			// Arrange
			var services = new ServiceCollection();
			services.AddKeyedPolicy(PolicyKeyA,
				_ => new TestPolicy("PolicyA"), ServiceLifetime.Scoped);
			using var provider = services.BuildServiceProvider();

			using var scope1 = provider.CreateScope();
			using var scope2 = provider.CreateScope();

			// Act
			var policy1 = scope1.ServiceProvider.GetRequiredKeyedPolicy(PolicyKeyA);
			var policyAgain = scope1.ServiceProvider.GetRequiredKeyedPolicy(PolicyKeyA);
			var policy2 = scope2.ServiceProvider.GetRequiredKeyedPolicy(PolicyKeyA);

			// Assert
			Assert.That(policy1, Is.SameAs(policyAgain));
			Assert.That(policy1, Is.Not.SameAs(policy2));
		}

		[Test]
		public void Should_Return_Same_Policy_Without_Rerunning_Factory_For_Singleton_Lifetime()
		{
			// Arrange
			int factoryCalls = 0;
			var services = new ServiceCollection();
			services.AddKeyedPolicy(PolicyKeyA,
				_ => { factoryCalls++; return new TestPolicy("PolicyA"); },
				ServiceLifetime.Singleton);
			var provider = services.BuildServiceProvider();

			// Act
			var policy1 = provider.GetRequiredKeyedPolicy(PolicyKeyA);
			var policy2 = provider.GetRequiredKeyedPolicy(PolicyKeyA);

			// Assert
			Assert.That(policy1, Is.SameAs(policy2));
			Assert.That(factoryCalls, Is.EqualTo(1));
		}

		[Test]
		public void Should_Throw_ArgumentNullException_When_Key_Is_Null()
		{
			// Arrange
			var services = new ServiceCollection();

			// Act & Assert
			Assert.Throws<ArgumentNullException>(
				() => services.AddKeyedPolicy(null!, _ => new TestPolicy("PolicyA")));
			Assert.Throws<ArgumentNullException>(
				() => services.AddKeyedPolicy<TestPolicyBuilderA>(null!));
		}

		[Test]
		public void Should_Throw_ArgumentNullException_When_Factory_Is_Null()
		{
			// Arrange
			var services = new ServiceCollection();

			// Act & Assert
			Assert.Throws<ArgumentNullException>(
				() => services.AddKeyedPolicy(PolicyKeyA, null!));
		}

		[Test]
		public void Should_Throw_InvalidOperationException_When_Factory_Returns_Null()
		{
			// Arrange
			var services = new ServiceCollection();
			services.AddKeyedPolicy(PolicyKeyA, _ => null!);
			var provider = services.BuildServiceProvider();

			// Act & Assert
			var ex = Assert.Throws<InvalidOperationException>(() => provider.GetKeyedPolicy(PolicyKeyA));
			Assert.That(ex!.Message, Does.Contain(PolicyKeyA),
				"Exception message should identify the key of the failing factory.");
		}

		[Test]
		public void Should_Pass_ServiceProvider_To_Factory()
		{
			// Arrange
			var services = new ServiceCollection();
			services.AddSingleton<IPolicyBase>(_ => new TestPolicy("FromProvider"));
			services.AddKeyedPolicy(PolicyKeyA, sp => sp.GetRequiredService<IPolicyBase>());
			var provider = services.BuildServiceProvider();

			// Act
			var policy = provider.GetRequiredKeyedPolicy(PolicyKeyA);

			// Assert
			Assert.That(policy.PolicyName, Is.EqualTo("FromProvider"));
		}

		[Test]
		public async Task Should_Delegate_Handle_And_HandleAsync_To_Inner_Policy()
		{
			// Arrange
			var innerPolicy = new TestPolicy("DelegationPolicy");
			var services = new ServiceCollection();
			services.AddKeyedPolicy(PolicyKeyA, _ => innerPolicy);
			var policy = services.BuildServiceProvider().GetRequiredKeyedPolicy(PolicyKeyA);

			// Act
			policy.Handle(() => 42);
			var result = policy.Handle(() => "sync");
			var asyncResult = await policy.HandleAsync(async (token) =>
			{
				await Task.Delay(1, token);
				return "async-keyed";
			});

			// Assert
			Assert.That(innerPolicy.HandleFuncCalled, Is.True);
			Assert.That(result.IsSuccess, Is.True);
			Assert.That(asyncResult.IsSuccess, Is.True);
		}

		[Test]
		public void Should_Register_Keyed_Ipolicy_Descriptor_With_Specified_Lifetime()
		{
			// Arrange
			var services = new ServiceCollection();

			// Act
			var returnedCollection = services.AddKeyedPolicy(
				PolicyKeyA, _ => new TestPolicy("PolicyA"), ServiceLifetime.Singleton);

			// Assert
			Assert.That(returnedCollection, Is.SameAs(services));
			var descriptor = services.Single(
				d => d.ServiceType == typeof(IPolicy) && d.IsKeyedService);
			Assert.That(descriptor.ServiceKey, Is.EqualTo(PolicyKeyA));
			Assert.That(descriptor.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
		}

		[Test]
		public void Should_Resolve_Policy_When_Keys_Are_Value_Equal_Strings()
		{
			// Arrange: string keys are matched by value equality during keyed resolution,
			// so distinct but equal-valued instances refer to the same registration.
			var registrationKey = new string("distinct-key");
			var services = new ServiceCollection();
			services.AddKeyedPolicy(registrationKey, _ => new TestPolicy("Distinct"));
			var provider = services.BuildServiceProvider();

			// Act
			var bySameReference = provider.GetKeyedPolicy(registrationKey);
			var byEqualInstance = provider.GetKeyedPolicy("distinct-key");

			// Assert
			Assert.That(bySameReference, Is.Not.Null);
			Assert.That(byEqualInstance, Is.Not.Null,
				"Value-equal string keys must resolve the same keyed registration.");
		}

		// -------------------------------------------------------------------------
		// Builder-based AddKeyedPolicy<TBuilder>(key, lifetime)
		// -------------------------------------------------------------------------

		[Test]
		public void Should_Resolve_Keyed_Policy_Built_By_Builder_Without_AddPoliNorError()
		{
			// Arrange: no AddPoliNorError call — the builder must be registered automatically
			var services = new ServiceCollection();
			services.AddKeyedPolicy<TestPolicyBuilderA>(PolicyKeyA);
			var provider = services.BuildServiceProvider();

			// Act
			var policy = provider.GetRequiredKeyedPolicy(PolicyKeyA);

			// Assert
			Assert.That(policy.PolicyName, Is.EqualTo("TestPolicyA"));
		}

		[Test]
		public void Should_Not_Duplicate_Builder_Registration_When_Already_Scanned()
		{
			// Arrange
			var services = new ServiceCollection();
			services.AddPoliNorError(assemblyToScan: Assembly.GetExecutingAssembly(), ServiceLifetime.Singleton);
			services.AddKeyedPolicy<TestPolicyBuilderA>(PolicyKeyA, ServiceLifetime.Transient);

			// Assert: TryAdd must not add a second IPolicyBuilder<TestPolicyBuilderA> descriptor
			var builderDescriptors = services.Count(
				d => d.ServiceType == typeof(IPolicyBuilder<TestPolicyBuilderA>));
			Assert.That(builderDescriptors, Is.EqualTo(1));
			// the pre-existing scanned registration keeps its lifetime
			Assert.That(services.First(
				d => d.ServiceType == typeof(IPolicyBuilder<TestPolicyBuilderA>)).Lifetime,
				Is.EqualTo(ServiceLifetime.Singleton));
		}

		[Test]
		public void Should_Wire_Configurator_Into_Keyed_Builder_Policy()
		{
			// Arrange: SomePolicyBuilder uses FakeConfigurator, which adds one error processor
			var services = new ServiceCollection();
			services.AddPoliNorError(assemblyToScan: Assembly.GetExecutingAssembly());
			services.AddKeyedPolicy<SomePolicyBuilder>(PolicyKeyA);
			var provider = services.BuildServiceProvider();

			// Act
			var policy = provider.GetRequiredKeyedPolicy(PolicyKeyA);

			// Assert
			Assert.That(policy.PolicyName, Is.EqualTo("SomePolicy"));
			Assert.That(policy.PolicyProcessor.Count(), Is.EqualTo(1));
		}

		[Test]
		public void Should_Allow_Same_Builder_Under_Multiple_Keys()
		{
			// Arrange
			var services = new ServiceCollection();
			services.AddKeyedPolicy<TestPolicyBuilderA>(PolicyKeyA);
			services.AddKeyedPolicy<TestPolicyBuilderA>(PolicyKeyB);
			var provider = services.BuildServiceProvider();

			// Act
			var policyA = provider.GetRequiredKeyedPolicy(PolicyKeyA);
			var policyB = provider.GetRequiredKeyedPolicy(PolicyKeyB);

			// Assert
			Assert.That(policyA.PolicyName, Is.EqualTo("TestPolicyA"));
			Assert.That(policyB.PolicyName, Is.EqualTo("TestPolicyA"));
			Assert.That(policyA, Is.Not.SameAs(policyB));
		}

		[Test]
		public void Should_Return_Same_Instance_For_Singleton_Keyed_Builder_Policy()
		{
			// Arrange
			var services = new ServiceCollection();
			services.AddKeyedPolicy<TestPolicyBuilderA>(PolicyKeyA, ServiceLifetime.Singleton);
			var provider = services.BuildServiceProvider();

			// Act
			var policy1 = provider.GetRequiredKeyedPolicy(PolicyKeyA);
			var policy2 = provider.GetRequiredKeyedPolicy(PolicyKeyA);

			// Assert
			Assert.That(policy1, Is.SameAs(policy2));
		}

		[Test]
		public void Should_Resolve_Keyed_Builder_Policy_With_Constructor_Dependencies()
		{
			// Arrange: TestPolicyBuilder requires IPolicyBase via constructor
			var services = new ServiceCollection();
			services.AddSingleton<IPolicyBase, TestPolicy>();
			services.AddKeyedPolicy<TestPolicyBuilder>(PolicyKeyA);
			var provider = services.BuildServiceProvider();

			// Act
			var policy = provider.GetRequiredKeyedPolicy(PolicyKeyA);

			// Assert: the injected TestPolicy (default name) is wrapped by the keyed proxy
			Assert.That(policy.PolicyName, Is.EqualTo(nameof(TestPolicy)));
		}

		// -------------------------------------------------------------------------
		// GetKeyedPolicy / GetRequiredKeyedPolicy resolver extensions
		// -------------------------------------------------------------------------

		[Test]
		public void Should_Throw_ArgumentNullException_When_ServiceProvider_Is_Null()
		{
			IServiceProvider nullProvider = null!;

			Assert.Throws<ArgumentNullException>(() => nullProvider.GetKeyedPolicy(PolicyKeyA));
			Assert.Throws<ArgumentNullException>(() => nullProvider.GetRequiredKeyedPolicy(PolicyKeyA));
		}

		[Test]
		public void Should_Return_Null_From_GetKeyedPolicy_When_Provider_Does_Not_Support_Keyed_Services()
		{
			var provider = new NonKeyedServiceProvider();

			Assert.That(provider.GetKeyedPolicy(PolicyKeyA), Is.Null);
		}

		[Test]
		public void Should_Throw_InvalidOperationException_From_GetRequiredKeyedPolicy_When_Provider_Does_Not_Support_Keyed_Services()
		{
			var provider = new NonKeyedServiceProvider();

			var ex = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredKeyedPolicy(PolicyKeyA));
			Assert.That(ex!.Message, Does.Contain("IKeyedServiceProvider"));
		}

		[Test]
		public void Should_Return_Null_From_GetKeyedPolicy_For_Null_Key()
		{
			// Arrange: no keyless (null-key) IPolicy registrations exist
			var services = new ServiceCollection();
			services.AddKeyedPolicy(PolicyKeyA, _ => new TestPolicy("PolicyA"));
			var provider = services.BuildServiceProvider();

			// Act
			var policy = provider.GetKeyedPolicy(null);

			// Assert
			Assert.That(policy, Is.Null);
		}

		// -------------------------------------------------------------------------
		// Coexistence with the typed IPolicy<TBuilder> pipeline
		// -------------------------------------------------------------------------

		[Test]
		public void Should_Coexist_With_Typed_Ipolicy_Registration()
		{
			// Arrange
			var services = new ServiceCollection();
			services.AddPoliNorError(assemblyToScan: Assembly.GetExecutingAssembly());
			services.AddKeyedPolicy<TestPolicyBuilderA>(PolicyKeyA);
			var provider = services.BuildServiceProvider();

			// Act
			var typedPolicy = provider.GetRequiredService<IPolicy<TestPolicyBuilderA>>();
			var keyedPolicy = provider.GetRequiredKeyedPolicy(PolicyKeyA);

			// Assert
			Assert.That(typedPolicy.PolicyName, Is.EqualTo("TestPolicyA"));
			Assert.That(keyedPolicy.PolicyName, Is.EqualTo("TestPolicyA"));
		}

		[Test]
		public void Should_Not_Register_Open_Generic_Ipolicy_When_Only_Keyed_Policies_Added()
		{
			// Arrange & Act
			var services = new ServiceCollection();
			services.AddKeyedPolicy(PolicyKeyA, _ => new TestPolicy("PolicyA"));
			services.AddKeyedPolicy<TestPolicyBuilderA>(PolicyKeyB);

			// Assert: keyed support must not leak into the typed pipeline
			Assert.That(services.Count(d => d.ServiceType == typeof(IPolicy<>)), Is.EqualTo(0));
		}

		[Test]
		public void Should_Expose_Typed_Proxy_Policy_As_Ipolicy()
		{
			// Arrange
			var services = new ServiceCollection();
			services.AddPoliNorError(assemblyToScan: Assembly.GetExecutingAssembly());
			var provider = services.BuildServiceProvider();

			// Act
			var typedPolicy = provider.GetRequiredService<IPolicy<TestPolicyBuilderA>>();

			// Assert: the refactored ProxyPolicy<TBuilder> still satisfies IPolicy<TBuilder>
			// and the new shared IPolicy contract
			Assert.That(typedPolicy, Is.InstanceOf<ProxyPolicy<TestPolicyBuilderA>>());
			Assert.That(typedPolicy, Is.InstanceOf<IPolicy>());
		}
	}

	/// A minimal <see cref="IServiceProvider"/> that does not implement
	/// <c>IKeyedServiceProvider</c>, used to verify the resolvers' degradation path.
	sealed class NonKeyedServiceProvider : IServiceProvider
	{
		public object? GetService(Type serviceType) => null;
	}
}
