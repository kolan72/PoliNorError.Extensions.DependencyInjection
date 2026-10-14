using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace PoliNorError.Extensions.DependencyInjection
{
	/// <summary>
	/// Extension methods for adding policy registry functionality to <see cref="IServiceCollection"/>.
	/// </summary>
	public static class ResilienceServiceCollectionExtensions
	{
		/// <summary>
		/// Registers policies for resolution via <see cref="IPolicyRegistry"/>.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This method registers the <see cref="IPolicyRegistry"/> as a singleton and
		/// configures policies to be registered as keyed services with the specified lifetime.
		/// </para>
		/// <para>
		/// The <paramref name="lifetime"/> parameter should typically match the lifetime used
		/// in AddPoliNorError to maintain consistency.
		/// </para>
		/// </remarks>
		/// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
		/// <param name="configure">Action to configure which policies should be available in the registry.</param>
		/// <param name="lifetime">
		/// The <see cref="ServiceLifetime"/> for registered policies. Defaults to <see cref="ServiceLifetime.Transient"/>.
		/// The registry itself is always registered as a singleton for efficiency.
		/// </param>
		/// <returns>The <see cref="IServiceCollection"/> for method chaining.</returns>
		/// <example>
		/// <code>
		/// services
		///     .AddPoliNorError(Assembly.GetExecutingAssembly(), ServiceLifetime.Scoped)
		///     .AddPolicyRegistry(
		///         builder => builder.AddPolicy&lt;RetryPolicyBuilder&gt;(),
		///         ServiceLifetime.Scoped); // Match the lifetime from AddPoliNorError
		/// </code>
		/// </example>
		public static IServiceCollection AddPolicyRegistry(
			this IServiceCollection services,
			Action<PolicyRegistryBuilder> configure,
			ServiceLifetime lifetime = ServiceLifetime.Transient)
		{
			ArgumentNullException.ThrowIfNull(services);
			ArgumentNullException.ThrowIfNull(configure);

			// Register the registry as singleton (idempotent - won't duplicate if called multiple times)
			// Registry is a resolver, not state, so singleton is most efficient
			services.TryAddSingleton<IPolicyRegistry, PolicyRegistry>();

			// Create the builder with the specified lifetime and configure policies
			var builder = new PolicyRegistryBuilder(services, lifetime);
			configure(builder);

			return services;
		}
	}
}