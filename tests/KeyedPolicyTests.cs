using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace PoliNorError.Extensions.DependencyInjection.Tests
{
	[TestFixture]
	public class KeyedPolicyTests
	{
		// -------------------------------------------------------------------------
		// Lambda-based keyed policy registration
		// -------------------------------------------------------------------------

		[Test]
		public void Should_AddKeyedPolicy_WithLambdaFactory()
		{
			var services = new ServiceCollection();
			services.AddKeyedPolicy("my-key", _ => new TestPolicy("LambdaPolicy"));

			var provider = services.BuildServiceProvider();
			var policy = provider.GetRequiredKeyedService<IPolicy>("my-key");

			Assert.That(policy, Is.InstanceOf<IPolicy>());
			Assert.That(policy.PolicyName, Is.EqualTo("LambdaPolicy"));
		}

		[Test]
		public void Should_AddKeyedPolicy_WithLambdaFactory_NullKey_ThrowsArgumentNullException()
		{
			var services = new ServiceCollection();
			Assert.Throws<ArgumentNullException>(() =>
				services.AddKeyedPolicy(null!, _ => new TestPolicy("test")));
		}

		[Test]
		public void Should_AddKeyedPolicy_WithLambdaFactory_NullFactory_ThrowsArgumentNullException()
		{
			var services = new ServiceCollection();
			Assert.Throws<ArgumentNullException>(() =>
				services.AddKeyedPolicy("key", null!));
		}

		[Test]
		public void Should_Resolve_Different_Policies_By_Key()
		{
			var services = new ServiceCollection();
			services.AddKeyedPolicy("policy-a", _ => new TestPolicy("PolicyA"));
			services.AddKeyedPolicy("policy-b", _ => new TestPolicy("PolicyB"));

			var provider = services.BuildServiceProvider();
			var policyA = provider.GetRequiredKeyedService<IPolicy>("policy-a");
			var policyB = provider.GetRequiredKeyedService<IPolicy>("policy-b");

			Assert.That(policyA.PolicyName, Is.EqualTo("PolicyA"));
			Assert.That(policyB.PolicyName, Is.EqualTo("PolicyB"));
			Assert.That(policyA, Is.Not.SameAs(policyB));
		}

		[Test]
		public void Should_CreateSeparateInstances_For_Transient_KeyedPolicy()
		{
			var services = new ServiceCollection();
			services.AddKeyedPolicy("my-key", _ => new TestPolicy("TransientPolicy"));

			var provider = services.BuildServiceProvider();
			var policy1 = provider.GetRequiredKeyedService<IPolicy>("my-key");
			var policy2 = provider.GetRequiredKeyedService<IPolicy>("my-key");

			Assert.That(policy1, Is.Not.SameAs(policy2));
		}

		[Test]
		public void Should_CreateSameInstance_For_Singleton_KeyedPolicy()
		{
			var services = new ServiceCollection();
			services.AddKeyedPolicy("my-key", _ => new TestPolicy("SingletonPolicy"), ServiceLifetime.Singleton);

			var provider = services.BuildServiceProvider();
			var policy1 = provider.GetRequiredKeyedService<IPolicy>("my-key");
			var policy2 = provider.GetRequiredKeyedService<IPolicy>("my-key");

			Assert.That(policy1, Is.SameAs(policy2));
		}

		[Test]
		public void Should_Work_With_Handle()
		{
			var services = new ServiceCollection();
			services.AddKeyedPolicy("my-key", _ => new TestPolicy("TestPolicy"));

			var provider = services.BuildServiceProvider();
			var policy = provider.GetRequiredKeyedService<IPolicy>("my-key");

			var testValue = "test";
			var result = policy.Handle(() => testValue);

			Assert.That(result.IsSuccess, Is.True);
		}

		[Test]
		public async Task Should_Work_With_HandleAsync()
		{
			var services = new ServiceCollection();
			services.AddKeyedPolicy("my-key", _ => new TestPolicy("TestPolicy"));

			var provider = services.BuildServiceProvider();
			var policy = provider.GetRequiredKeyedService<IPolicy>("my-key");

			var result = await policy.HandleAsync(async (token) =>
			{
				await Task.Delay(1, token);
				return "async-test";
			});

			Assert.That(result.IsSuccess, Is.True);
		}

		[Test]
		public void Should_Return_Same_ServiceCollection_For_Chaining()
		{
			var services = new ServiceCollection();
			var result = services.AddKeyedPolicy("key", _ => new TestPolicy("Policy"));

			Assert.That(result, Is.SameAs(services));
		}

		// -------------------------------------------------------------------------
		// Builder-based keyed policy registration
		// -------------------------------------------------------------------------

		[Test]
		public void Should_AddKeyedPolicy_BuilderBased()
		{
			var services = new ServiceCollection();
			services.AddPoliNorError(Assembly.GetExecutingAssembly());
			services.AddKeyedPolicy<TestPolicyBuilder1>("builder-key");

			var provider = services.BuildServiceProvider();
			var policy = provider.GetRequiredKeyedService<IPolicy>("builder-key");

			Assert.That(policy, Is.InstanceOf<IPolicy>());
			Assert.That(policy.PolicyName, Is.EqualTo("TestPolicy1"));
		}

		[Test]
		public void Should_AddKeyedPolicy_BuilderBased_NullKey_ThrowsArgumentNullException()
		{
			var services = new ServiceCollection();
			Assert.Throws<ArgumentNullException>(() =>
				services.AddKeyedPolicy<TestPolicyBuilder1>(null!));
		}

		[Test]
		public void Should_Resolve_Different_Policies_By_Builder_Key()
		{
			var services = new ServiceCollection();
			services.AddPoliNorError(Assembly.GetExecutingAssembly());
			services.AddKeyedPolicy<TestPolicyBuilder1>("key-1");
			services.AddKeyedPolicy<TestPolicyBuilder2>("key-2");
			services.AddKeyedPolicy<TestPolicyBuilder3>("key-3");

			var provider = services.BuildServiceProvider();
			var policy1 = provider.GetRequiredKeyedService<IPolicy>("key-1");
			var policy2 = provider.GetRequiredKeyedService<IPolicy>("key-2");
			var policy3 = provider.GetRequiredKeyedService<IPolicy>("key-3");

			Assert.That(policy1.PolicyName, Is.EqualTo("TestPolicy1"));
			Assert.That(policy2.PolicyName, Is.EqualTo("TestPolicy2"));
			Assert.That(policy3.PolicyName, Is.EqualTo("TestPolicy3"));
		}

		[Test]
		public void Should_CreateSameInstance_For_Singleton_BuilderKeyedPolicy()
		{
			var services = new ServiceCollection();
			services.AddPoliNorError(Assembly.GetExecutingAssembly());
			services.AddKeyedPolicy<TestPolicyBuilder1>("singleton-key", ServiceLifetime.Singleton);

			var provider = services.BuildServiceProvider();
			var policy1 = provider.GetRequiredKeyedService<IPolicy>("singleton-key");
			var policy2 = provider.GetRequiredKeyedService<IPolicy>("singleton-key");

			Assert.That(policy1, Is.SameAs(policy2));
		}

		// -------------------------------------------------------------------------
		// Coexistence with existing IPolicy<TBuilder>
		// -------------------------------------------------------------------------

		[Test]
		public void Should_Coexist_With_Generic_IPolicy()
		{
			var services = new ServiceCollection();
			services.AddPoliNorError(Assembly.GetExecutingAssembly());
			services.AddKeyedPolicy<TestPolicyBuilder1>("keyed-policy");

			var provider = services.BuildServiceProvider();

			// Generic IPolicy<T> still works
			var genericPolicy = provider.GetRequiredService<IPolicy<TestPolicyBuilder1>>();
			Assert.That(genericPolicy.PolicyName, Is.EqualTo("TestPolicy1"));

			// Keyed IPolicy also works
			var keyedPolicy = provider.GetRequiredKeyedService<IPolicy>("keyed-policy");
			Assert.That(keyedPolicy.PolicyName, Is.EqualTo("TestPolicy1"));
		}

		[Test]
		public void Should_Allow_Same_Builder_Different_Keys()
		{
			var services = new ServiceCollection();
			services.AddPoliNorError(Assembly.GetExecutingAssembly());
			services.AddKeyedPolicy<TestPolicyBuilder1>("retry-primary");
			services.AddKeyedPolicy<TestPolicyBuilder1>("retry-secondary");

			var provider = services.BuildServiceProvider();
			var primary = provider.GetRequiredKeyedService<IPolicy>("retry-primary");
			var secondary = provider.GetRequiredKeyedService<IPolicy>("retry-secondary");

			Assert.That(primary.PolicyName, Is.EqualTo("TestPolicy1"));
			Assert.That(secondary.PolicyName, Is.EqualTo("TestPolicy1"));
		}

		// -------------------------------------------------------------------------
		// PolicyConfigurator + Keyed PolicyBuilder
		// -------------------------------------------------------------------------

		[Test]
		public void Should_Resolve_BuilderKeyedPolicy_WithConfigurator()
		{
			var services = new ServiceCollection();
			services.AddPoliNorError(Assembly.GetExecutingAssembly());
			services.AddKeyedPolicy<SomePolicyBuilder>("configurable-key");

			var provider = services.BuildServiceProvider();
			var policy = provider.GetRequiredKeyedService<IPolicy>("configurable-key");

			Assert.That(policy.PolicyName, Is.EqualTo("SomePolicy"));
			Assert.That(policy.PolicyProcessor.Count(), Is.EqualTo(1));
		}
	}
}
