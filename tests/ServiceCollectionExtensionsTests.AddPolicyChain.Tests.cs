using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PoliNorError.Extensions.DependencyInjection.Tests
{
	// Tests for AddPolicyChain — DI registration of ordered policy pipelines (PolicyCollection-backed).
	public partial class ServiceCollectionExtensionsTests
	{
		[Test]
		public void Should_AddPolicyChain_WithDefaultTransientLifetime()
		{
			// Act
			_services!.AddPolicyChain<TestPolicyBuilder1, TestPolicyBuilder2>();

			// Assert
			var descriptor = _services!.FirstOrDefault(s => s.ServiceType == typeof(IPolicyBase));
			Assert.That(descriptor, Is.Not.Null);
			Assert.That(descriptor?.Lifetime, Is.EqualTo(ServiceLifetime.Transient));
		}

		[Test]
		public void Should_AddPolicyChain_WithSpecifiedLifetime()
		{
			// Act
			_services!.AddPolicyChain<TestPolicyBuilder1, TestPolicyBuilder2>(lifetime: ServiceLifetime.Singleton);

			// Assert
			var descriptor = _services!.FirstOrDefault(s => s.ServiceType == typeof(IPolicyBase));
			Assert.That(descriptor, Is.Not.Null);
			Assert.That(descriptor?.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
		}

		[Test]
		public void Should_AddPolicyChain_ReturnSameServiceCollectionInstance()
		{
			// Act
			var result = _services!.AddPolicyChain<TestPolicyBuilder1, TestPolicyBuilder2>();

			// Assert
			Assert.That(result, Is.SameAs(_services));
		}

		[Test]
		public void Should_AddPolicyChain_ResolveElements_InOrder()
		{
			// Arrange
			_services!.AddPoliNorError(_testAssembly!);
			_services!.AddPolicyChain<TestPolicyBuilder1, TestPolicyBuilder2>();

			var provider = _services!.BuildServiceProvider();

			// Act
			var chain = provider.GetService<IPolicyBase>();
			var elements = ((IEnumerable<IPolicyBase>)chain!).ToList();

			// Assert
			Assert.That(chain, Is.InstanceOf<PolicyCollection>());
			Assert.That(elements, Has.Count.EqualTo(2));
			Assert.That(elements[0].PolicyName, Is.EqualTo("TestPolicy1"));
			Assert.That(elements[1].PolicyName, Is.EqualTo("TestPolicy2"));
		}

		[Test]
		public void Should_AddPolicyChain_UseDefaultName_WhenPolicyNameOmitted()
		{
			// Arrange
			_services!.AddPoliNorError(_testAssembly!);
			_services!.AddPolicyChain<TestPolicyBuilder1, TestPolicyBuilder2>();

			var provider = _services!.BuildServiceProvider();

			// Act
			var chain = provider.GetService<IPolicyBase>();

			// Assert — without WithPolicyName, PolicyCollection falls back to its type-name default.
			Assert.That(chain, Is.Not.Null);
			Assert.That(chain!.PolicyName, Is.EqualTo("PolicyCollection"));
		}

		[Test]
		public void Should_AddPolicyChain_WithPolicyName_StampsName()
		{
			// Arrange
			_services!.AddPoliNorError(_testAssembly!);
			_services!.AddPolicyChain<TestPolicyBuilder1, TestPolicyBuilder2>(policyName: "orders-pipeline");

			var provider = _services!.BuildServiceProvider();

			// Act
			var chain = provider.GetService<IPolicyBase>();
			var result = chain!.Handle(() => { });

			// Assert
			Assert.That(chain.PolicyName, Is.EqualTo("orders-pipeline"));
			Assert.That(result.IsSuccess, Is.True);
			Assert.That(result.PolicyName, Is.EqualTo("orders-pipeline"));
		}

		[Test]
		public void Should_AddPolicyChain_BuildElements_From_DI()
		{
			// Arrange — TestPolicyBuilder requires IPolicyBase via its constructor; the chain element
			// is therefore a DI-constructed ProxyPolicy whose builder received the injected dependency.
			_services!.AddPoliNorError(_testAssembly!);
			_services!.AddPolicyChain<TestPolicyBuilder, TestPolicyBuilderA>();
			var testPolicy = new TestPolicy("InjectedTestPolicy");
			_services!.AddSingleton<IPolicyBase>(testPolicy);

			var provider = _services!.BuildServiceProvider();

			// Act — the chain is the first IPolicyBase registration; the TestPolicy singleton is the last.
			var chain = provider.GetServices<IPolicyBase>().First();
			var elements = ((IEnumerable<IPolicyBase>)chain).ToList();

			// Assert — invoking the first element routes through the DI-built builder to the injected policy.
			Assert.That(elements, Has.Count.EqualTo(2));
			Assert.That(elements[0], Is.InstanceOf<ProxyPolicy<TestPolicyBuilder>>());
			elements[0].Handle(() => { });
			Assert.That(testPolicy.HandleActionCalled, Is.True);
			Assert.That(elements[1].PolicyName, Is.EqualTo("TestPolicyA"));
		}

		[Test]
		public void Should_AddPolicyChain_Throw_WhenBuilderNotRegistered()
		{
			// Arrange — builders are intentionally not registered.
			_services!.AddPolicyChain<TestPolicyBuilder2, TestPolicyBuilder3>();

			var provider = _services!.BuildServiceProvider();

			// Act & Assert
			Assert.Throws<InvalidOperationException>(() => provider.GetService<IPolicyBase>());
		}

		[Test]
		public void Should_AddPolicyChain_RegisterMultipleChains_And_ResolveLast()
		{
			// Arrange
			_services!.AddPoliNorError(_testAssembly!);
			_services!.AddPolicyChain<TestPolicyBuilder1, TestPolicyBuilder2>(policyName: "chain-one");
			_services!.AddPolicyChain<AnotherPolicyBuilder, ThirdPolicyBuilder>(policyName: "chain-two");

			var provider = _services!.BuildServiceProvider();

			// Act
			var resolved = provider.GetService<IPolicyBase>();
			var all = provider.GetServices<IPolicyBase>().ToList();

			// Assert
			Assert.That(_services!.Count(s => s.ServiceType == typeof(IPolicyBase)), Is.EqualTo(2));
			Assert.That(resolved!.PolicyName, Is.EqualTo("chain-two"));
			Assert.That(all, Has.Count.EqualTo(2));
		}

		[Test]
		public void Should_AddPolicyChain_UseAdd_NotTryAdd_Semantics()
		{
			// Act
			_services!.AddPolicyChain<TestPolicyBuilder1, TestPolicyBuilder2>();
			_services!.AddPolicyChain<TestPolicyBuilder1, TestPolicyBuilder2>();

			// Assert
			Assert.That(_services!.Count(s => s.ServiceType == typeof(IPolicyBase)), Is.EqualTo(2));
		}

		[Test]
		public void Should_AddPolicyChain_WithThreeBuilders()
		{
			// Arrange
			_services!.AddPoliNorError(_testAssembly!);
			_services!.AddPolicyChain<TestPolicyBuilder1, TestPolicyBuilder2, TestPolicyBuilder3>();

			var provider = _services!.BuildServiceProvider();

			// Act
			var chain = provider.GetService<IPolicyBase>();
			var elements = ((IEnumerable<IPolicyBase>)chain!).ToList();

			// Assert
			Assert.That(elements, Has.Count.EqualTo(3));
			Assert.That(elements.Select(p => p.PolicyName).ToList(),
				Is.EqualTo(new[] { "TestPolicy1", "TestPolicy2", "TestPolicy3" }));
		}

		[Test]
		public void Should_AddPolicyChain_WithFourBuilders()
		{
			// Arrange
			_services!.AddPoliNorError(_testAssembly!);
			_services!.AddPolicyChain<TestPolicyBuilder1, TestPolicyBuilder2, AnotherPolicyBuilder, ThirdPolicyBuilder>();

			var provider = _services!.BuildServiceProvider();

			// Act
			var chain = provider.GetService<IPolicyBase>();
			var elements = ((IEnumerable<IPolicyBase>)chain!).ToList();

			// Assert
			Assert.That(elements, Has.Count.EqualTo(4));
			Assert.That(elements.Select(p => p.PolicyName).ToList(),
				Is.EqualTo(new[] { "TestPolicy1", "TestPolicy2", "AnotherPolicy", "ThirdPolicy" }));
		}


		[Test]
		public void Should_AddPolicyChain_ResolveNewInstance_ForTransientChain()
		{
			// Arrange
			_services!.AddPoliNorError(_testAssembly!);
			_services!.AddPolicyChain<TestPolicyBuilder1, TestPolicyBuilder2>();

			var provider = _services!.BuildServiceProvider();

			// Act
			var chain1 = provider.GetService<IPolicyBase>();
			var chain2 = provider.GetService<IPolicyBase>();

			// Assert
			Assert.That(chain1, Is.Not.SameAs(chain2));
		}

		[Test]
		public void Should_AddPolicyChain_ResolveSameInstance_ForSingletonChain()
		{
			// Arrange
			_services!.AddPoliNorError(_testAssembly!);
			_services!.AddPolicyChain<TestPolicyBuilder1, TestPolicyBuilder2>(lifetime: ServiceLifetime.Singleton);

			var provider = _services!.BuildServiceProvider();

			// Act
			var chain1 = provider.GetService<IPolicyBase>();
			var chain2 = provider.GetService<IPolicyBase>();

			// Assert
			Assert.That(chain1, Is.SameAs(chain2));
		}

		[Test]
		public void Should_AddPolicyChain_Order_MatchesTypeArgumentOrder()
		{
			// Arrange
			_services!.AddPoliNorError(_testAssembly!);
			_services!.AddPolicyChain<TestPolicyBuilder1, TestPolicyBuilder2, AnotherPolicyBuilder, ThirdPolicyBuilder>();

			var provider = _services!.BuildServiceProvider();

			// Act
			var elements = ((IEnumerable<IPolicyBase>)provider!.GetService<IPolicyBase>()!).ToList();

			// Assert
			Assert.That(elements.Select(p => p.PolicyName).ToList(),
				Is.EqualTo(new[] { "TestPolicy1", "TestPolicy2", "AnotherPolicy", "ThirdPolicy" }));
		}

		[Test]
		public void Should_AddPolicyChain_ReverseOrder_ResolveReversedOrder()
		{
			// Arrange
			_services!.AddPoliNorError(_testAssembly!);
			_services!.AddPolicyChain<ThirdPolicyBuilder, AnotherPolicyBuilder, TestPolicyBuilder2, TestPolicyBuilder1>();

			var provider = _services!.BuildServiceProvider();

			// Act
			var elements = ((IEnumerable<IPolicyBase>)provider!.GetService<IPolicyBase>()!).ToList();

			// Assert — reversing the type arguments reverses the chain order.
			Assert.That(elements.Select(p => p.PolicyName).ToList(),
				Is.EqualTo(new[] { "ThirdPolicy", "AnotherPolicy", "TestPolicy2", "TestPolicy1" }));
		}

		[Test]
		public void Should_AddPolicyChain_ResolveSecond_ChainWhenMultipleRegistered()
		{
			// Arrange
			_services!.AddPoliNorError(_testAssembly!);
			_services!.AddPolicyChain<TestPolicyBuilder1, TestPolicyBuilder2>(policyName: "chain-first");
			_services!.AddPolicyChain<AnotherPolicyBuilder, ThirdPolicyBuilder>(policyName: "chain-second");

			var provider = _services!.BuildServiceProvider();

			// Act
			var single = provider.GetService<IPolicyBase>();
			var all = provider.GetServices<IPolicyBase>().ToList();

			// Assert
			Assert.That(all, Has.Count.EqualTo(2));
			Assert.That(single!.PolicyName, Is.EqualTo("chain-second"));
		}

		[Test]
		public void Should_AddPolicyChain_MultipleRegistrations_KeepMultipleDescriptors()
		{
			// Arrange
			_services!.AddPolicyChain<TestPolicyBuilder1, TestPolicyBuilder2>();
			_services!.AddPolicyChain<AnotherPolicyBuilder, ThirdPolicyBuilder>();

			// Assert
			Assert.That(_services!.Count(s => s.ServiceType == typeof(IPolicyBase)), Is.EqualTo(2));
		}
	}
}
