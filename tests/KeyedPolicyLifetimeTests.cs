using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace PoliNorError.Extensions.DependencyInjection.Tests
{
	[TestFixture]
	public class KeyedPolicyLifetimeTests
	{
#pragma warning disable S2094 // Classes should not be empty
		private sealed class ScopedMarker
#pragma warning restore S2094 // Classes should not be empty
		{
		}

		private static ServiceProvider BuildValidatingProvider(IServiceCollection services)
		{
			if (services.All(d => d.ServiceType != typeof(IPolicyBase)))
			{
				services.AddSingleton<IPolicyBase, TestPolicy>();
			}

			return services.BuildServiceProvider(
				new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
		}

		// -------------------------------------------------------------------------
		// Registration-time guard: Singleton key + Scoped chain
		// -------------------------------------------------------------------------

		[Test]
		public void Should_ThrowAtRegistration_For_Singleton_BuilderKeyedPolicy_With_Scoped_Chain()
		{
			var services = new ServiceCollection();
			services.AddPoliNorError(Assembly.GetExecutingAssembly(), ServiceLifetime.Scoped);

			var ex = Assert.Throws<InvalidOperationException>(() =>
				services.AddKeyedPolicy<SomePolicyBuilder>("k", ServiceLifetime.Singleton));

			Assert.That(ex!.Message, Does.Contain("k"));
			Assert.That(ex.Message, Does.Contain("Singleton"));
			Assert.That(ex.Message, Does.Contain("Scoped"));
		}

		[Test]
		public void Should_ThrowAtFirstResolve_For_Singleton_BuilderKeyedPolicy_When_Scoped_Chain_Registered_Later()
		{
			var services = new ServiceCollection();
			services.AddKeyedPolicy<SomePolicyBuilder>("k", ServiceLifetime.Singleton);
			services.AddPoliNorError(Assembly.GetExecutingAssembly(), ServiceLifetime.Scoped);

			using var provider = BuildValidatingProvider(services);

			var ex = Assert.Throws<InvalidOperationException>(() =>
				provider.GetRequiredKeyedService<IPolicy>("k"));

			Assert.That(ex!.Message, Does.Contain("k"));
			Assert.That(ex.Message, Does.Contain("Singleton"));
			Assert.That(ex.Message, Does.Contain("Scoped"));
		}

		// -------------------------------------------------------------------------
		// Opaque factory overload: enrichment only, no chain inspection possible
		// -------------------------------------------------------------------------

		[Test]
		public void Should_EnrichRootScopedFailure_For_Singleton_OpaqueKeyedPolicy()
		{
			var services = new ServiceCollection();
			services.AddScoped<ScopedMarker>();
			services.AddKeyedPolicy("k", sp =>
			{
				sp.GetRequiredService<ScopedMarker>();
				return new TestPolicy("OpaquePolicy");
			}, ServiceLifetime.Singleton);

			using var provider = BuildValidatingProvider(services);

			var ex = Assert.Throws<InvalidOperationException>(() =>
				provider.GetRequiredKeyedService<IPolicy>("k"));

			Assert.That(ex!.Message, Does.Contain("k"));
		}

		[Test]
		public void Should_NotFalsePositive_For_Singleton_OpaqueKeyedPolicy_Without_Scoped_Dependencies()
		{
			var services = new ServiceCollection();
			services.AddKeyedPolicy("k", _ => new TestPolicy("HealthySingleton"), ServiceLifetime.Singleton);

			using var provider = BuildValidatingProvider(services);

			var policy1 = provider.GetRequiredKeyedService<IPolicy>("k");
			var policy2 = provider.GetRequiredKeyedService<IPolicy>("k");

			Assert.That(policy1.PolicyName, Is.EqualTo("HealthySingleton"));
			Assert.That(policy1, Is.SameAs(policy2));
		}

		// -------------------------------------------------------------------------
		// All-Singleton chain
		// -------------------------------------------------------------------------

		[Test]
		public void Should_Resolve_For_Singleton_BuilderKeyedPolicy_With_Singleton_Chain()
		{
			var services = new ServiceCollection();
			services.AddPoliNorError(Assembly.GetExecutingAssembly(), ServiceLifetime.Singleton);
			services.AddKeyedPolicy<SomePolicyBuilder>("k", ServiceLifetime.Singleton);

			using var provider = BuildValidatingProvider(services);

			var policy = provider.GetRequiredKeyedService<IPolicy>("k");

			Assert.That(policy.PolicyName, Is.EqualTo("SomePolicy"));
		}

		// -------------------------------------------------------------------------
		// Keyed Scoped lifetime
		// -------------------------------------------------------------------------

		[Test]
		public void Should_Work_In_Scope_And_Fail_OnRoot_For_Scoped_KeyedPolicy()
		{
			var services = new ServiceCollection();
			services.AddScoped<ScopedMarker>();
			services.AddKeyedPolicy("k", sp =>
			{
				sp.GetRequiredService<ScopedMarker>();
				return new TestPolicy("ScopedPolicy");
			}, ServiceLifetime.Scoped);

			using var provider = BuildValidatingProvider(services);

			using (var scope = provider.CreateScope())
			{
				var inScope = scope.ServiceProvider.GetRequiredKeyedService<IPolicy>("k");
				Assert.That(inScope.PolicyName, Is.EqualTo("ScopedPolicy"));
			}

			var ex = Assert.Throws<InvalidOperationException>(() =>
				provider.GetRequiredKeyedService<IPolicy>("k"));

			Assert.That(ex!.Message, Does.Contain("root provider"));
			Assert.That(ex.Message, Does.Contain("scoped service").IgnoreCase);
		}
	}
}