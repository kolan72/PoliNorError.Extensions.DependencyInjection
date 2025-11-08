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
	public partial class ServiceCollectionExtensionsTests
    {
		[Test]
		public void Should_Register_Concrete_Configurator_Types()
		{
			// Arrange
			var assembly = Assembly.GetExecutingAssembly();

			// Act
			_services!.AddAllPolicyConfigurators(assembly);

			// Assert
			Assert.That(_services!, Has.Count.EqualTo(4),
				"Should register 3 configurators: TestConfigurator, AnotherConfigurator, MultipleInterfaceConfigurator");

			Assert.That(_services!.Any(s => s.ImplementationType == typeof(TestConfigurator)), Is.True);
			Assert.That(_services!.Any(s => s.ImplementationType == typeof(AnotherConfigurator)), Is.True);
			Assert.That(_services!.Any(s => s.ImplementationType == typeof(MultipleInterfaceConfigurator)), Is.True);
		}

		[Test]
		public void Should_Not_Register_Abstract_Classes()
		{
			// Arrange
			var assembly = Assembly.GetExecutingAssembly();

			// Act
			_services!.AddAllPolicyConfigurators(assembly);

			// Assert
			Assert.That(_services!.Any(s => s.ImplementationType == typeof(AbstractConfigurator)), Is.False);
		}

		[Test]
		public void Should_Not_Register_Generic_Type_Definitions()
		{
			// Arrange
			var assembly = Assembly.GetExecutingAssembly();

			// Act
			_services!.AddAllPolicyConfigurators(assembly);

			// Assert
			Assert.That(_services!.Any(s => s.ImplementationType!.IsGenericTypeDefinition), Is.False);
		}

		[Test]
		public void Should_Not_Register_Non_Configurator_Classes()
		{
			// Arrange
			var assembly = Assembly.GetExecutingAssembly();

			// Act
			_services!.AddAllPolicyConfigurators(assembly);

			// Assert
			Assert.That(_services!.Any(s => s.ImplementationType == typeof(NotAConfigurator)), Is.False);
		}

		[Test]
		public void Should_Register_With_Default_Transient_Lifetime()
		{
			// Arrange
			var assembly = Assembly.GetExecutingAssembly();

			// Act
			_services!.AddAllPolicyConfigurators(assembly);

			// Assert
			var registration = _services!.First(s => s.ImplementationType == typeof(TestConfigurator));
			Assert.That(registration.Lifetime, Is.EqualTo(ServiceLifetime.Transient));
		}

		[Test]
		public void Should_Register_With_Specified_Singleton_Lifetime()
		{
			// Arrange
			var assembly = Assembly.GetExecutingAssembly();

			// Act
			_services!.AddAllPolicyConfigurators(assembly, ServiceLifetime.Singleton);

			// Assert
			var registration = _services!.First(s => s.ImplementationType == typeof(TestConfigurator));
			Assert.That(registration.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
		}

		[Test]
		public void Should_Register_With_Specified_Scoped_Lifetime()
		{
			// Arrange
			var assembly = Assembly.GetExecutingAssembly();

			// Act
			_services!.AddAllPolicyConfigurators(assembly, ServiceLifetime.Scoped);

			// Assert
			var registration = _services!.First(s => s.ImplementationType == typeof(TestConfigurator));
			Assert.That(registration.Lifetime, Is.EqualTo(ServiceLifetime.Scoped));
		}

		[Test]
		public void Should_Register_Service_As_Self()
		{
			// Arrange
			var assembly = Assembly.GetExecutingAssembly();

			// Act
			_services!.AddAllPolicyConfigurators(assembly);

			// Assert
			var registration = _services!.First(s => s.ImplementationType == typeof(TestConfigurator));
			Assert.That(registration.ServiceType, Is.EqualTo(typeof(TestConfigurator)));
			Assert.That(registration.ImplementationType, Is.EqualTo(typeof(TestConfigurator)));
		}

		[Test]
		public void Should_Return_Same_ServiceCollection_Instance()
		{
			// Arrange
			var assembly = Assembly.GetExecutingAssembly();

			// Act
			var result = _services!.AddAllPolicyConfigurators(assembly);

			// Assert
			Assert.That(result, Is.SameAs(_services));
		}

		[Test]
		public void Should_Register_Types_With_Multiple_Interfaces_Once()
		{
			// Arrange
			var assembly = Assembly.GetExecutingAssembly();

			// Act
			_services!.AddAllPolicyConfigurators(assembly);

			// Assert
			Assert.That(_services!.Count(s => s.ImplementationType == typeof(MultipleInterfaceConfigurator)),
				Is.EqualTo(1), "MultipleInterfaceConfigurator should be registered only once");
		}

		[Test]
		public void Should_Respect_Specified_Lifetime()
		{
			// Act
			_services!.AddAllPolicyConfigurators(typeof(FakeConfigurator).Assembly, ServiceLifetime.Singleton);
			var provider = _services!.BuildServiceProvider();

			var instance1 = provider.GetService<FakeConfigurator>();
			var instance2 = provider.GetService<FakeConfigurator>();

			Assert.That(instance1, Is.EqualTo(instance2));
		}

		[Test]
		public void Should_Fallback_To_Executing_Assembly_When_Assembly_Is_Null()
		{
			// Act
			_services!.AddAllPolicyConfigurators(null!);

			var provider = _services!.BuildServiceProvider();
			var result = provider.GetServices<FakeConfigurator>();

			Assert.That(result, Is.Not.Null);
		}
	}
}
