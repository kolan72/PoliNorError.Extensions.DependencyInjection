using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace PoliNorError.Extensions.DependencyInjection.Tests
{
	/// <summary>
	/// NUnit tests that verify the correctness properties defined in the design document.
	/// Each test is tagged with the property it validates.
	/// </summary>
	[TestFixture]
	public class CorrectnessPropertyTests
	{
		// Fixed pool of assemblies used across all property tests.
		// The test assembly contains all the test builders and configurators.
		private static readonly Assembly TestAssembly = Assembly.GetExecutingAssembly();
		private static readonly Assembly LibraryAssembly = typeof(ServiceCollectionExtensions).Assembly;

		// -------------------------------------------------------------------------
		// Feature: architectural-refactoring
		// Property 1: Multi-assembly registration is the union of per-assembly registrations
		// Validates: Requirement 6.2
		// -------------------------------------------------------------------------

		[TestCase(Description = "Single assembly: combined call equals per-assembly scan")]
		public void Property1_MultiAssemblyUnion_SingleAssembly()
		{
			// Arrange — scan the test assembly individually
			var expected = new ServiceCollection();
			expected.AddAllPolicyBuilders(TestAssembly);
			expected.AddAllPolicyConfigurators(TestAssembly);
			var expectedTypes = BuilderServiceTypes(expected);

			// Act — call the params overload with the same single assembly
			var combined = new ServiceCollection();
			combined.AddPoliNorError(ServiceLifetime.Transient, TestAssembly);
			var actualTypes = BuilderServiceTypes(combined);

			// Assert
			Assert.That(actualTypes, Is.EquivalentTo(expectedTypes));
		}

		[TestCase(Description = "Two distinct assemblies: combined call equals union of per-assembly scans")]
		public void Property1_MultiAssemblyUnion_TwoDistinctAssemblies()
		{
			// Arrange — scan each assembly individually and union the results
			var fromTest = new ServiceCollection();
			fromTest.AddAllPolicyBuilders(TestAssembly);
			fromTest.AddAllPolicyConfigurators(TestAssembly);

			var fromLib = new ServiceCollection();
			fromLib.AddAllPolicyBuilders(LibraryAssembly);
			fromLib.AddAllPolicyConfigurators(LibraryAssembly);

			var expectedTypes = BuilderServiceTypes(fromTest)
				.Union(BuilderServiceTypes(fromLib))
				.ToHashSet();

			// Act — call the params overload with both assemblies at once
			var combined = new ServiceCollection();
			combined.AddPoliNorError(ServiceLifetime.Transient, TestAssembly, LibraryAssembly);
			var actualTypes = BuilderServiceTypes(combined).ToHashSet();

			// Assert
			Assert.That(actualTypes, Is.EquivalentTo(expectedTypes));
		}

		[TestCase(Description = "Duplicate assembly: combined call equals single-assembly scan (no duplicates)")]
		public void Property1_MultiAssemblyUnion_DuplicateAssembly()
		{
			// Arrange — scan the test assembly once
			var expected = new ServiceCollection();
			expected.AddAllPolicyBuilders(TestAssembly);
			expected.AddAllPolicyConfigurators(TestAssembly);
			var expectedTypes = BuilderServiceTypes(expected);

			// Act — pass the same assembly twice; union should equal single scan
			var combined = new ServiceCollection();
			combined.AddPoliNorError(ServiceLifetime.Transient, TestAssembly, TestAssembly);
			var actualTypes = BuilderServiceTypes(combined);

			// Assert: same set of service types (duplicates in the collection are allowed by DI,
			// but the set of distinct service types must match)
			Assert.That(actualTypes.ToHashSet(), Is.EquivalentTo(expectedTypes.ToHashSet()));
		}

		// -------------------------------------------------------------------------
		// Feature: architectural-refactoring
		// Property 2: IPolicy<> is registered exactly once per multi-assembly call
		// Validates: Requirement 6.3
		// -------------------------------------------------------------------------

		[TestCase(Description = "Single assembly: IPolicy<> registered exactly once")]
		public void Property2_IPolicyRegisteredOnce_SingleAssembly()
		{
			var services = new ServiceCollection();
			services.AddPoliNorError(ServiceLifetime.Transient, TestAssembly);

			Assert.That(IPolicyDescriptorCount(services), Is.EqualTo(1));
		}

		[TestCase(Description = "Two distinct assemblies: IPolicy<> registered exactly once")]
		public void Property2_IPolicyRegisteredOnce_TwoDistinctAssemblies()
		{
			var services = new ServiceCollection();
			services.AddPoliNorError(ServiceLifetime.Transient, TestAssembly, LibraryAssembly);

			Assert.That(IPolicyDescriptorCount(services), Is.EqualTo(1));
		}

		[TestCase(Description = "Duplicate assembly: IPolicy<> registered exactly once")]
		public void Property2_IPolicyRegisteredOnce_DuplicateAssembly()
		{
			var services = new ServiceCollection();
			services.AddPoliNorError(ServiceLifetime.Transient, TestAssembly, TestAssembly);

			Assert.That(IPolicyDescriptorCount(services), Is.EqualTo(1));
		}

		// -------------------------------------------------------------------------
		// Feature: architectural-refactoring
		// Property 3: Repeated AddPoliNorError calls do not duplicate IPolicy<> registration
		// Validates: Requirement 7.1
		// -------------------------------------------------------------------------

		[TestCase(1, Description = "1 call: IPolicy<> registered exactly once")]
		[TestCase(3, Description = "3 calls: IPolicy<> registered exactly once")]
		[TestCase(10, Description = "10 calls: IPolicy<> registered exactly once")]
		public void Property3_RepeatedCalls_DoNotDuplicateIPolicy(int callCount)
		{
			// Feature: architectural-refactoring, Property 3: Repeated AddPoliNorError calls do not duplicate IPolicy<> registration
			var services = new ServiceCollection();

			for (int i = 0; i < callCount; i++)
				services.AddPoliNorError(TestAssembly);

			Assert.That(IPolicyDescriptorCount(services), Is.EqualTo(1));
		}

		// -------------------------------------------------------------------------
		// Feature: architectural-refactoring
		// Property 4: Unified LINQ scanning produces identical registrations to the original
		// Validates: Requirement 10.2
		// -------------------------------------------------------------------------

		[Test]
		public void Property4_UnifiedLinq_ProducesIdenticalRegistrations_ToArrayFind()
		{
			// Feature: architectural-refactoring, Property 4: Unified LINQ scanning produces identical registrations to the original
			var openGenericInterface = typeof(IPolicyBuilder<>);

			// Reference implementation using Array.Find (the original approach)
			var referenceRegistrations = TestAssembly.GetTypes()
				.Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition)
				.Select(t => new
				{
					ImplementationType = t,
					InterfaceType = Array.Find(
						t.GetInterfaces(),
						i => i.IsGenericType && i.GetGenericTypeDefinition() == openGenericInterface)
				})
				.Where(x => x.InterfaceType != null)
				.Select(x => (ServiceType: x.InterfaceType!, ImplementationType: x.ImplementationType))
				.ToHashSet();

			// Refactored implementation using FirstOrDefault (the new approach)
			var refactoredServices = new ServiceCollection();
			refactoredServices.AddAllPolicyBuilders(TestAssembly);
			var refactoredRegistrations = refactoredServices
				.Where(d => d.ServiceType.IsGenericType
						 && d.ServiceType.GetGenericTypeDefinition() == openGenericInterface)
				.Select(d => (ServiceType: d.ServiceType, ImplementationType: d.ImplementationType!))
				.ToHashSet();

			// Assert: both approaches produce the same (ServiceType, ImplementationType) pairs
			Assert.That(refactoredRegistrations, Is.EquivalentTo(referenceRegistrations));
		}

		// -------------------------------------------------------------------------
		// Helpers
		// -------------------------------------------------------------------------

		private static IEnumerable<Type> BuilderServiceTypes(IServiceCollection services) =>
			services
				.Where(d => d.ServiceType.IsGenericType
						 && d.ServiceType.GetGenericTypeDefinition() == typeof(IPolicyBuilder<>))
				.Select(d => d.ServiceType);

		private static int IPolicyDescriptorCount(IServiceCollection services) =>
			services.Count(d => d.ServiceType == typeof(IPolicy<>));
	}
}
