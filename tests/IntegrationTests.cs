using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PoliNorError.Extensions.DependencyInjection.Tests
{
	[TestFixture]
	public class IntegrationTests
	{
		private ServiceProvider? _serviceProvider;
		private IServiceCollection? _services;

		[SetUp]
		public void SetUp()
		{
			_services = new ServiceCollection();
			_services.AddPoliNorError(assemblyToScan: Assembly.GetExecutingAssembly());
			_serviceProvider = _services.BuildServiceProvider();
		}

		[TearDown]
		public void TearDown()
		{
			_serviceProvider?.Dispose();
		}

		[Test]
		public void Should_ResolvePolicyBuilderA()
		{
			// Act
			var policyBuilder = _serviceProvider!.GetService<IPolicyBuilder<TestPolicyBuilderA>>();

			// Assert
			Assert.That(policyBuilder, Is.Not.Null);
			Assert.That(policyBuilder, Is.TypeOf<TestPolicyBuilderA>());
		}

		[Test]
		public void Should_ResolvePolicyBuilderB()
		{
			// Act
			var policyBuilder = _serviceProvider!.GetService<IPolicyBuilder<TestPolicyBuilderB>>();

			// Assert
			Assert.That(policyBuilder, Is.Not.Null);
			Assert.That(policyBuilder, Is.TypeOf<TestPolicyBuilderB>());
		}

		[Test]
		public void Should_ResolvePolicyA()
		{
			// Act
			var policy = _serviceProvider!.GetService<IPolicy<TestPolicyBuilderA>>();

			// Assert
			Assert.That(policy, Is.Not.Null);
			Assert.That(policy, Is.TypeOf<ProxyPolicy<TestPolicyBuilderA>>());
			Assert.That(policy?.PolicyName, Is.EqualTo("TestPolicyA"));
		}

		[Test]
		public void Should_ResolvePolicyB()
		{
			// Act
			var policy = _serviceProvider!.GetService<IPolicy<TestPolicyBuilderB>>();

			// Assert
			Assert.That(policy, Is.Not.Null);
			Assert.That(policy, Is.TypeOf<ProxyPolicy<TestPolicyBuilderB>>());
			Assert.That(policy?.PolicyName, Is.EqualTo("TestPolicyB"));
		}

		[Test]
		public void Should_Resolve_Policy_Inherited_From_PolicyBuilder()
		{
			var policy = _serviceProvider!.GetService<IPolicy<SomePolicyBuilder>>();
			Assert.That(policy, Is.Not.Null);
			Assert.That(policy, Is.TypeOf<ProxyPolicy<SomePolicyBuilder>>());
			Assert.That(policy?.PolicyName, Is.EqualTo("SomePolicy"));
		}

		[Test]
		public void Should_Configurator_Configure_In_Policy_Created_By_PolicyBuilder_Inheritor()
		{
			var policy = _serviceProvider!.GetService<IPolicy<SomePolicyBuilder>>();
			Assert.That(policy!.PolicyProcessor.Count(), Is.EqualTo(1));
		}

		[Test]
		public void Should_CreateSeparateInstancesForTransientLifetime()
		{
			// Act
			var policy1 = _serviceProvider!.GetService<IPolicy<TestPolicyBuilderA>>();
			var policy2 = _serviceProvider!.GetService<IPolicy<TestPolicyBuilderA>>();

			// Assert
			Assert.That(policy1, Is.Not.Null);
			Assert.That(policy2, Is.Not.Null);
			Assert.That(policy1, Is.Not.SameAs(policy2));
		}

		[Test]
		public void Should_WorkEndToEndWithPolicyExecution()
		{
			// Arrange
			var policy = _serviceProvider!.GetRequiredService<IPolicy<TestPolicyBuilderA>>();
			var testValue = "integration test";

			// Act
			var result = policy.Handle(() => testValue);

			// Assert
			Assert.That(result.IsSuccess, Is.True);
		}

		[Test]
		public async Task Should_WorkEndToEndWithAsyncPolicyExecution()
		{
			// Arrange
			var policy = _serviceProvider!.GetRequiredService<IPolicy<TestPolicyBuilderA>>();
			var testValue = "async integration test";

			// Act
			var result = await policy.HandleAsync(async (token) =>
			{
				await Task.Delay(10, token);
				return testValue;
			});

			// Assert
			Assert.That(result.IsSuccess, Is.True);
		}

		[Test]
		public void Should_RegisterMultiplePolicyBuildersIndependently()
		{
			// Act
			var policyA = _serviceProvider!.GetRequiredService<IPolicy<TestPolicyBuilderA>>();
			var policyB = _serviceProvider!.GetRequiredService<IPolicy<TestPolicyBuilderB>>();

			// Assert
			Assert.That(policyA.PolicyName, Is.EqualTo("TestPolicyA"));
			Assert.That(policyB.PolicyName, Is.EqualTo("TestPolicyB"));
			Assert.That(policyA, Is.Not.SameAs(policyB));
		}
	}
}
