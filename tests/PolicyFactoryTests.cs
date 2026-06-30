using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace PoliNorError.Extensions.DependencyInjection.Tests
{
	[TestFixture]
	public class PolicyFactoryTests
	{
		private ServiceCollection _services;
		private IServiceProvider _serviceProvider;

		[SetUp]
		public void SetUp()
		{
			_services = new ServiceCollection();
		}

		[Test]
		public void Should_RegisterPolicyFactoryAsSingleton()
		{
			// Arrange
			_services.AddPoliNorError(Assembly.GetExecutingAssembly());

			// Act
			var descriptor = _services.FirstOrDefault(s => s.ServiceType == typeof(IPolicyFactory));

			// Assert
			Assert.That(descriptor, Is.Not.Null);
			Assert.That(descriptor.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
			Assert.That(descriptor.ImplementationType, Is.EqualTo(typeof(PolicyFactory)));
		}

		[Test]
		public void Should_ResolvePolicyFactoryFromServiceProvider()
		{
			// Arrange
			_services.AddPoliNorError(Assembly.GetExecutingAssembly());
			_serviceProvider = _services.BuildServiceProvider();

			// Act
			var factory = _serviceProvider.GetService<IPolicyFactory>();

			// Assert
			Assert.That(factory, Is.Not.Null);
			Assert.That(factory, Is.InstanceOf<PolicyFactory>());
		}

		[Test]
		public void Should_ReturnSamePolicyFactoryInstanceOnMultipleResolves()
		{
			// Arrange
			_services.AddPoliNorError(Assembly.GetExecutingAssembly());
			_serviceProvider = _services.BuildServiceProvider();

			// Act
			var factory1 = _serviceProvider.GetService<IPolicyFactory>();
			var factory2 = _serviceProvider.GetService<IPolicyFactory>();

			// Assert
			Assert.That(factory1, Is.SameAs(factory2));
		}

		[Test]
		public void Should_CreatePolicyUsingFactory()
		{
			// Arrange
			_services.AddPoliNorError(Assembly.GetExecutingAssembly());
			_serviceProvider = _services.BuildServiceProvider();
			var factory = _serviceProvider.GetRequiredService<IPolicyFactory>();

			// Act
			var policy = factory.CreatePolicy<TestPolicyDerived>();

			// Assert
			Assert.That(policy, Is.Not.Null);
			Assert.That(policy, Is.InstanceOf<IPolicyBase>());
		}

		[Test]
		public void Should_CallCreateMethodOnPolicy()
		{
			// Arrange
			_services.AddPoliNorError(Assembly.GetExecutingAssembly());
			_serviceProvider = _services.BuildServiceProvider();
			var factory = _serviceProvider.GetRequiredService<IPolicyFactory>();

			// Act
			var policy = factory.CreatePolicy<TestPolicyDerived>() as TestPolicyDerived;

			// Assert
			Assert.That(policy, Is.Not.Null);
			Assert.That(policy.IsCreated, Is.True);
		}

		[Test]
		public void Should_CreatePolicyWithTransientLifetime()
		{
			// Arrange
			_services.AddPoliNorError(Assembly.GetExecutingAssembly(), ServiceLifetime.Transient);
			_serviceProvider = _services.BuildServiceProvider();
			var factory = _serviceProvider.GetRequiredService<IPolicyFactory>();

			// Act
			var policy1 = factory.CreatePolicy<TestPolicyDerived>();
			var policy2 = factory.CreatePolicy<TestPolicyDerived>();

			// Assert
			Assert.That(policy1, Is.Not.SameAs(policy2));
		}

		[Test]
		public void Should_CreatePolicyWithSingletonLifetime()
		{
			// Arrange
			_services.AddPoliNorError(Assembly.GetExecutingAssembly(), ServiceLifetime.Singleton);
			_serviceProvider = _services.BuildServiceProvider();
			var factory = _serviceProvider.GetRequiredService<IPolicyFactory>();

			// Act
			var policy1 = factory.CreatePolicy<TestPolicyDerived>();
			var policy2 = factory.CreatePolicy<TestPolicyDerived>();

			// Assert
			Assert.That(policy1, Is.SameAs(policy2));
		}

		[Test]
		public void Should_CreateDifferentPolicyTypes()
		{
			// Arrange
			_services.AddPoliNorError(Assembly.GetExecutingAssembly());
			_serviceProvider = _services.BuildServiceProvider();
			var factory = _serviceProvider.GetRequiredService<IPolicyFactory>();

			// Act
			var policy1 = factory.CreatePolicy<TestPolicyDerived>();
			var policy2 = factory.CreatePolicy<AnotherTestPolicyDerived>();

			// Assert
			Assert.That(policy1, Is.Not.Null);
			Assert.That(policy2, Is.Not.Null);
			Assert.That(policy1, Is.Not.SameAs(policy2));
			Assert.That(policy1.PolicyName, Is.EqualTo("TestPolicyDerived"));
			Assert.That(policy2.PolicyName, Is.EqualTo("AnotherTestPolicyDerived"));
		}

		[Test]
		public void Should_ThrowExceptionWhenPolicyNotRegistered()
		{
			// Arrange
			_services.AddSingleton<IPolicyFactory, PolicyFactory>();
			_serviceProvider = _services.BuildServiceProvider();
			var factory = _serviceProvider.GetRequiredService<IPolicyFactory>();

			// Act & Assert
			Assert.Throws<InvalidOperationException>(() => factory.CreatePolicy<TestPolicyDerived>());
		}

		[Test]
		public void Should_CreatePolicyWithMultipleAssemblies()
		{
			// Arrange
			_services.AddPoliNorError(ServiceLifetime.Transient, Assembly.GetExecutingAssembly());
			_serviceProvider = _services.BuildServiceProvider();
			var factory = _serviceProvider.GetRequiredService<IPolicyFactory>();

			// Act
			var policy = factory.CreatePolicy<TestPolicyDerived>();

			// Assert
			Assert.That(policy, Is.Not.Null);
			Assert.That(policy.PolicyName, Is.EqualTo("TestPolicyDerived"));
		}

		[Test]
		public void Should_RegisterPolicyFactoryOnlyOnceWithMultipleAssemblies()
		{
			// Arrange
			_services.AddPoliNorError(ServiceLifetime.Transient, Assembly.GetExecutingAssembly(), Assembly.GetExecutingAssembly());

			// Act
			var descriptors = _services.Where(s => s.ServiceType == typeof(IPolicyFactory)).ToList();

			// Assert
			Assert.That(descriptors.Count, Is.EqualTo(1));
			Assert.That(descriptors[0].Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
		}
	}

	[TestFixture]
	public class PolicyBaseTests
	{
		private ServiceCollection _services;
		private IServiceProvider _serviceProvider;

		[SetUp]
		public void SetUp()
		{
			_services = new ServiceCollection();
		}

		[Test]
		public void Should_CreateInnerPolicyOnCreate()
		{
			// Arrange
			var policy = new TestPolicyDerived();

			// Act
			policy.Create();

			// Assert
			Assert.That(policy.IsCreated, Is.True);
		}

		[Test]
		public void Should_DelegatePolicyNameToInnerPolicy()
		{
			// Arrange
			_services.AddPoliNorError(Assembly.GetExecutingAssembly());
			_serviceProvider = _services.BuildServiceProvider();
			var factory = _serviceProvider.GetRequiredService<IPolicyFactory>();

			// Act
			var policy = factory.CreatePolicy<TestPolicyDerived>();

			// Assert
			Assert.That(policy.PolicyName, Is.EqualTo("TestPolicyDerived"));
		}

		[Test]
		public void Should_DelegatePolicyProcessorToInnerPolicy()
		{
			// Arrange
			_services.AddPoliNorError(Assembly.GetExecutingAssembly());
			_serviceProvider = _services.BuildServiceProvider();
			var factory = _serviceProvider.GetRequiredService<IPolicyFactory>();

			// Act
			var policy = factory.CreatePolicy<TestPolicyDerived>();

			// Assert
			Assert.That(policy.PolicyProcessor, Is.Not.Null);
			Assert.That(policy.PolicyProcessor, Is.InstanceOf<IPolicyProcessor>());
		}

		[Test]
		public void Should_DelegateHandleActionToInnerPolicy()
		{
			// Arrange
			_services.AddPoliNorError(Assembly.GetExecutingAssembly());
			_serviceProvider = _services.BuildServiceProvider();
			var factory = _serviceProvider.GetRequiredService<IPolicyFactory>();
			var policy = factory.CreatePolicy<TestPolicyDerived>();
			var actionCalled = false;

			// Act
			var result = policy.Handle(() => actionCalled = true);

			// Assert
			Assert.That(actionCalled, Is.True);
			Assert.That(result, Is.Not.Null);
			Assert.That(result, Is.InstanceOf<PolicyResult>());
		}

		[Test]
		public void Should_DelegateHandleFuncToInnerPolicy()
		{
			// Arrange
			_services.AddPoliNorError(Assembly.GetExecutingAssembly());
			_serviceProvider = _services.BuildServiceProvider();
			var factory = _serviceProvider.GetRequiredService<IPolicyFactory>();
			var policy = factory.CreatePolicy<TestPolicyDerived>();

			// Act
			var result = policy.Handle(() => 42);

			// Assert
			Assert.That(result, Is.Not.Null);
			Assert.That(result, Is.InstanceOf<PolicyResult<int>>());
		}

		[Test]
		public async Task Should_DelegateHandleAsyncActionToInnerPolicy()
		{
			// Arrange
			_services.AddPoliNorError(Assembly.GetExecutingAssembly());
			_serviceProvider = _services.BuildServiceProvider();
			var factory = _serviceProvider.GetRequiredService<IPolicyFactory>();
			var policy = factory.CreatePolicy<TestPolicyDerived>();
			var actionCalled = false;

			// Act
			var result = await policy.HandleAsync(async (ct) => 
			{
				await Task.Delay(1, ct);
				actionCalled = true;
			});

			// Assert
			Assert.That(actionCalled, Is.True);
			Assert.That(result, Is.Not.Null);
			Assert.That(result, Is.InstanceOf<PolicyResult>());
		}

		[Test]
		public async Task Should_DelegateHandleAsyncFuncToInnerPolicy()
		{
			// Arrange
			_services.AddPoliNorError(Assembly.GetExecutingAssembly());
			_serviceProvider = _services.BuildServiceProvider();
			var factory = _serviceProvider.GetRequiredService<IPolicyFactory>();
			var policy = factory.CreatePolicy<TestPolicyDerived>();

			// Act
			var result = await policy.HandleAsync(async (ct) => 
			{
				await Task.Delay(1, ct);
				return "test";
			});

			// Assert
			Assert.That(result, Is.Not.Null);
			Assert.That(result, Is.InstanceOf<PolicyResult<string>>());
		}

		[Test]
		public void Should_PassCancellationTokenToHandle()
		{
			// Arrange
			_services.AddPoliNorError(Assembly.GetExecutingAssembly());
			_serviceProvider = _services.BuildServiceProvider();
			var factory = _serviceProvider.GetRequiredService<IPolicyFactory>();
			var policy = factory.CreatePolicy<TestPolicyDerived>();
			var cts = new CancellationTokenSource();

			// Act
			var result = policy.Handle(() => { }, cts.Token);

			// Assert
			Assert.That(result, Is.Not.Null);
		}

		[Test]
		public async Task Should_PassCancellationTokenToHandleAsync()
		{
			// Arrange
			_services.AddPoliNorError(Assembly.GetExecutingAssembly());
			_serviceProvider = _services.BuildServiceProvider();
			var factory = _serviceProvider.GetRequiredService<IPolicyFactory>();
			var policy = factory.CreatePolicy<TestPolicyDerived>();
			var cts = new CancellationTokenSource();

			// Act
			var result = await policy.HandleAsync(async (ct) => await Task.CompletedTask, false, cts.Token);

			// Assert
			Assert.That(result, Is.Not.Null);
		}

		[Test]
		public void Should_RegisterMultiplePolicyBaseInheritors()
		{
			// Arrange
			_services.AddPoliNorError(Assembly.GetExecutingAssembly());

			// Act
			var policyDescriptors = _services.Where(s => 
				s.ServiceType == typeof(TestPolicyDerived) || 
				s.ServiceType == typeof(AnotherTestPolicyDerived)).ToList();

			// Assert
			Assert.That(policyDescriptors.Count, Is.EqualTo(2));
		}

		[Test]
		public void Should_NotRegisterAbstractPolicyBase()
		{
			// Arrange
			_services.AddPoliNorError(Assembly.GetExecutingAssembly());

			// Act
			var abstractDescriptor = _services.FirstOrDefault(s => s.ServiceType == typeof(AbstractTestPolicy));

			// Assert
			Assert.That(abstractDescriptor, Is.Null);
		}

		[Test]
		public void Should_RegisterPoliciesWithSpecifiedLifetime()
		{
			// Arrange
			_services.AddPoliNorError(Assembly.GetExecutingAssembly(), ServiceLifetime.Scoped);

			// Act
			var descriptor = _services.FirstOrDefault(s => s.ServiceType == typeof(TestPolicyDerived));

			// Assert
			Assert.That(descriptor, Is.Not.Null);
			Assert.That(descriptor!.Lifetime, Is.EqualTo(ServiceLifetime.Scoped));
		}
	}

	[TestFixture]
	public class PolicyFactoryIntegrationTests
	{
		[Test]
		public void Should_CreateAndUsePolicyEndToEnd()
		{
			// Arrange
			var services = new ServiceCollection();
			services.AddPoliNorError(Assembly.GetExecutingAssembly());
			var serviceProvider = services.BuildServiceProvider();
			var factory = serviceProvider.GetRequiredService<IPolicyFactory>();

			// Act
			var policy = factory.CreatePolicy<TestPolicyDerived>();
			var executionCount = 0;
			var result = policy.Handle(() => executionCount++);

			// Assert
			Assert.That(policy, Is.Not.Null);
			Assert.That(executionCount, Is.EqualTo(1));
			Assert.That(result, Is.Not.Null);
		}

		[Test]
		public async Task Should_CreateAndUsePolicyAsyncEndToEnd()
		{
			// Arrange
			var services = new ServiceCollection();
			services.AddPoliNorError(Assembly.GetExecutingAssembly());
			var serviceProvider = services.BuildServiceProvider();
			var factory = serviceProvider.GetRequiredService<IPolicyFactory>();

			// Act
			var policy = factory.CreatePolicy<TestPolicyDerived>();
			var executionCount = 0;
			var result = await policy.HandleAsync(async (ct) =>
			{
				await Task.Delay(1, ct);
				executionCount++;
			});

			// Assert
			Assert.That(policy, Is.Not.Null);
			Assert.That(executionCount, Is.EqualTo(1));
			Assert.That(result, Is.Not.Null);
		}

		[Test]
		public void Should_CreateMultiplePoliciesWithDifferentBehaviors()
		{
			// Arrange
			var services = new ServiceCollection();
			services.AddPoliNorError(Assembly.GetExecutingAssembly());
			var serviceProvider = services.BuildServiceProvider();
			var factory = serviceProvider.GetRequiredService<IPolicyFactory>();

			// Act
			var policy1 = factory.CreatePolicy<TestPolicyDerived>();
			var policy2 = factory.CreatePolicy<AnotherTestPolicyDerived>();

			// Assert
			Assert.That(policy1.PolicyName, Is.EqualTo("TestPolicyDerived"));
			Assert.That(policy2.PolicyName, Is.EqualTo("AnotherTestPolicyDerived"));
		}
	}
}
