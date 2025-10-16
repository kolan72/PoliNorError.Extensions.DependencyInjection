using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework.Internal;
using System.Reflection;

namespace PoliNorError.Extensions.DependencyInjection.Tests
{
	[TestFixture]
	public class ServiceCollectionExtensionsTests
	{
		private IServiceCollection? _services;
		private Assembly? _testAssembly;

		[SetUp]
		public void SetUp()
		{
			_services = new ServiceCollection();
			_testAssembly = Assembly.GetExecutingAssembly();
		}

		[Test]
		public void Should_AddPoliNorErrorWithDefaultLifetime()
		{
			// Act
			_services!.AddPoliNorError(Assembly.GetExecutingAssembly());

			// Assert
			var descriptor = _services!.FirstOrDefault(s => s.ServiceType == typeof(IPolicy<>));
			Assert.That(descriptor, Is.Not.Null);
			Assert.That(descriptor?.ImplementationType, Is.EqualTo(typeof(ProxyPolicy<>)));
			Assert.That(descriptor?.Lifetime, Is.EqualTo(ServiceLifetime.Transient));
		}

		[Test]
		public void Should_AddPoliNorErrorWithSpecifiedLifetime()
		{
			// Act
			_services!.AddPoliNorError(Assembly.GetExecutingAssembly(), ServiceLifetime.Singleton);

			// Assert
			var descriptor = _services!.FirstOrDefault(s => s.ServiceType == typeof(IPolicy<>));
			Assert.That(descriptor, Is.Not.Null);
			Assert.That(descriptor?.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
		}

		[Test]
		public void Should_AddPoliNorErrorWithCustomAssembly()
		{
			// Arrange
			var customAssembly = Assembly.GetExecutingAssembly();

			// Act
			_services!.AddPoliNorError(assemblyToScan: customAssembly);

			// Assert
			var descriptor = _services!.FirstOrDefault(s => s.ServiceType == typeof(IPolicy<>));
			Assert.That(descriptor, Is.Not.Null);
		}

		[Test]
		public void Should_Register_IPolicy_With_ProxyPolicy()
		{
			var services = new ServiceCollection();

			services.AddPoliNorError(Assembly.GetExecutingAssembly(), ServiceLifetime.Singleton);
			services.AddSingleton<IPolicyBase, TestPolicy>();

			var serviceProvider = services.BuildServiceProvider();
			var policy = serviceProvider.GetService<IPolicy<TestPolicyBuilder>>();

			Assert.That(policy, Is.InstanceOf<ProxyPolicy<TestPolicyBuilder>>());
		}

		[Test]
		public void Should_Register_PolicyFactories_From_Assembly()
		{
			var services = new ServiceCollection();

			services.AddAllPolicyBuilders(Assembly.GetExecutingAssembly(), ServiceLifetime.Singleton);
			services.AddSingleton<IPolicyBase, TestPolicy>();

			var serviceProvider = services.BuildServiceProvider();
			var factory = serviceProvider.GetService<IPolicyBuilder<TestPolicyBuilder>>();

			Assert.That(factory, Is.InstanceOf<TestPolicyBuilder>());
		}

		[Test]
		public void Should_AddAllPolicyBuildersWithDefaultLifetime()
		{
			// Act
			_services!.AddAllPolicyBuilders(assemblyToScan: _testAssembly!);

			// Assert
			var descriptors = _services!.Where(s => s.ServiceType.IsGenericType &&
												 s.ServiceType.GetGenericTypeDefinition() == typeof(IPolicyBuilder<>)).ToList();

			Assert.That(descriptors, Is.Not.Empty);
			Assert.That(descriptors.TrueForAll(d => d.Lifetime == ServiceLifetime.Transient), Is.True);
		}

		[Test]
		public void Should_AddAllPolicyBuildersWithSpecifiedLifetime()
		{
			// Act
			_services!.AddAllPolicyBuilders(_testAssembly!, ServiceLifetime.Scoped);

			// Assert
			var descriptors = _services!.Where(s => s.ServiceType.IsGenericType &&
												 s.ServiceType.GetGenericTypeDefinition() == typeof(IPolicyBuilder<>)).ToList();

			Assert.That(descriptors, Is.Not.Empty);
			Assert.That(descriptors.TrueForAll(d => d.Lifetime == ServiceLifetime.Scoped), Is.True);
		}

		[Test]
		public void Should_RegisterConcretePolicyBuilders()
		{
			// Act
			_services!.AddAllPolicyBuilders(assemblyToScan: _testAssembly!);

			// Assert
			var testBuilderADescriptor = _services?.FirstOrDefault(s => s.ServiceType == typeof(IPolicyBuilder<TestPolicyBuilderA>));
			var testBuilderBDescriptor = _services?.FirstOrDefault(s => s.ServiceType == typeof(IPolicyBuilder<TestPolicyBuilderB>));

			Assert.That(testBuilderADescriptor, Is.Not.Null);
			Assert.That(testBuilderADescriptor!.ImplementationType, Is.EqualTo(typeof(TestPolicyBuilderA)));

			Assert.That(testBuilderBDescriptor, Is.Not.Null);
			Assert.That(testBuilderBDescriptor!.ImplementationType, Is.EqualTo(typeof(TestPolicyBuilderB)));
		}

		[Test]
		public void Should_NotRegisterAbstractClasses()
		{
			// Act
			_services!.AddAllPolicyBuilders(assemblyToScan: _testAssembly!);

			// Assert
			var abstractDescriptor = _services!.FirstOrDefault(s => s.ImplementationType?.IsAbstract == true);
			Assert.That(abstractDescriptor, Is.Null);
		}

		[Test]
		public void Should_ReturnSameServiceCollectionInstance()
		{
			// Act
			var result = _services?.AddAllPolicyBuilders(assemblyToScan: _testAssembly!);

			// Assert
			Assert.That(result, Is.SameAs(_services));
		}

	}

	[TestFixture]
	public class MultiplePolicyBuilderTests
	{
		[Test]
		public void Should_Register_Multiple_PolicyBuilders_From_Assembly()
		{
			var services = new ServiceCollection();

			services.AddAllPolicyBuilders(Assembly.GetExecutingAssembly(), ServiceLifetime.Singleton);

			var serviceProvider = services.BuildServiceProvider();
			var factory1 = serviceProvider.GetService<IPolicyBuilder<TestPolicyBuilder1>>();
			var factory2 = serviceProvider.GetService<IPolicyBuilder<TestPolicyBuilder2>>();
			var factory3 = serviceProvider.GetService<IPolicyBuilder<TestPolicyBuilder3>>();

			Assert.That(factory1, Is.InstanceOf<TestPolicyBuilder1>());
			Assert.That(factory2, Is.InstanceOf<TestPolicyBuilder2>());
			Assert.That(factory3, Is.InstanceOf<TestPolicyBuilder3>());
		}

		[Test]
		public void Should_Resolve_Correct_Policy_For_Each_Builder_Type()
		{
			var services = new ServiceCollection();

			services.AddPoliNorError(Assembly.GetExecutingAssembly(), ServiceLifetime.Singleton);

			var serviceProvider = services.BuildServiceProvider();
			var policy1 = serviceProvider.GetService<IPolicy<TestPolicyBuilder1>>();
			var policy2 = serviceProvider.GetService<IPolicy<TestPolicyBuilder2>>();
			var policy3 = serviceProvider.GetService<IPolicy<TestPolicyBuilder3>>();

			Assert.That(policy1, Is.InstanceOf<ProxyPolicy<TestPolicyBuilder1>>());
			Assert.That(policy2, Is.InstanceOf<ProxyPolicy<TestPolicyBuilder2>>());
			Assert.That(policy3, Is.InstanceOf<ProxyPolicy<TestPolicyBuilder3>>());
		}

		[Test]
		public void Should_Not_Register_Abstract_Classes_Or_GenericTypes()
		{
			var services = new ServiceCollection();

			services.AddAllPolicyBuilders(Assembly.GetExecutingAssembly(), ServiceLifetime.Singleton);

			var serviceProvider = services.BuildServiceProvider();

			// Should not register abstract class
			var abstractFactory = serviceProvider.GetService<IPolicyBuilder<AbstractPolicyBuilder>>();
			Assert.That(abstractFactory, Is.Null);

			// Should not register generic type definition
			var genericFactory = serviceProvider.GetService<IPolicyBuilder<GenericPolicyBuilder<int>>>();
			Assert.That(genericFactory, Is.Null);
		}

		[Test]
		public void Should_Register_Only_Concrete_NonGeneric_Types()
		{
			var services = new ServiceCollection();

			services.AddAllPolicyBuilders(Assembly.GetExecutingAssembly(), ServiceLifetime.Singleton);

			services.BuildServiceProvider();
			var allServices = services.Where(s =>
				s.ServiceType.IsGenericType &&
				s.ServiceType.GetGenericTypeDefinition() == typeof(IPolicyBuilder<>));

			Assert.That(allServices.Count(), Is.EqualTo(6));
		}

		[Test]
		public void Should_Use_Specified_ServiceLifetime_For_All_Registrations()
		{
			var services = new ServiceCollection();

			services.AddPoliNorError(Assembly.GetExecutingAssembly(), ServiceLifetime.Scoped);

			var policyDescriptor = services.First(s => s.ServiceType == typeof(IPolicy<>));
			var factoryDescriptors = services.Where(s =>
				s.ServiceType.IsGenericType &&
				s.ServiceType.GetGenericTypeDefinition() == typeof(IPolicyBuilder<>));

			Assert.That(policyDescriptor.Lifetime, Is.EqualTo(ServiceLifetime.Scoped));
			Assert.That(factoryDescriptors.All(d => d.Lifetime == ServiceLifetime.Scoped), Is.True);
		}

		[Test]
		public void Should_Work_With_Different_ServiceLifetimes()
		{
			// Test Transient
			var transientServices = new ServiceCollection();
			transientServices.AddPoliNorError(Assembly.GetExecutingAssembly(), ServiceLifetime.Transient);
			var transientProvider = transientServices.BuildServiceProvider();

			var transient1 = transientProvider.GetService<IPolicy<TestPolicyBuilder1>>();
			var transient2 = transientProvider.GetService<IPolicy<TestPolicyBuilder1>>();
			Assert.That(transient1, Is.Not.SameAs(transient2));

			// Test Singleton
			var singletonServices = new ServiceCollection();
			singletonServices.AddPoliNorError(Assembly.GetExecutingAssembly(), ServiceLifetime.Singleton);
			var singletonProvider = singletonServices.BuildServiceProvider();

			var singleton1 = singletonProvider.GetService<IPolicy<TestPolicyBuilder1>>();
			var singleton2 = singletonProvider.GetService<IPolicy<TestPolicyBuilder1>>();
			Assert.That(singleton1, Is.SameAs(singleton2));
		}
	}
}
