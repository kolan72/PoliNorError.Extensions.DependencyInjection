using Microsoft.Extensions.DependencyInjection;
using System;

namespace PoliNorError.Extensions.DependencyInjection
{
	/// <summary>
	/// Builder for configuring policies that will be available through <see cref="IPolicyRegistry"/>.
	/// </summary>
	public class PolicyRegistryBuilder
	{
		private readonly IServiceCollection _services;
		private readonly ServiceLifetime _lifetime;

		/// <summary>
		/// Initializes a new instance of the <see cref="PolicyRegistryBuilder"/> class.
		/// </summary>
		/// <param name="services">The service collection to register policies into.</param>
		/// <param name="lifetime">The lifetime to use for registered policies.</param>
		internal PolicyRegistryBuilder(IServiceCollection services, ServiceLifetime lifetime)
		{
			_services = services;
			_lifetime = lifetime;
		}

		/// <summary>
		/// Registers a policy builder type to be available through the registry.
		/// </summary>
		/// <typeparam name="TPolicyBuilder">
		/// The policy builder type. Must implement <see cref="IPolicyBuilder{TBuilder}"/>.
		/// </typeparam>
		/// <returns>The <see cref="PolicyRegistryBuilder"/> for method chaining.</returns>
		/// <remarks>
		/// The policy is registered as a keyed service using the builder type as the key.
		/// The lifetime is determined by the value passed to <see cref="ResilienceServiceCollectionExtensions.AddPolicyRegistry"/>.
		/// </remarks>
		public PolicyRegistryBuilder AddPolicy<TPolicyBuilder>() 
			where TPolicyBuilder : IPolicyBuilder<TPolicyBuilder>
		{
			// Register as keyed service with the configured lifetime
			// Key is the builder type, service is resolved via IPolicy<TPolicyBuilder>
			var descriptor = new ServiceDescriptor(
				serviceType: typeof(IPolicyBase),
				serviceKey: typeof(TPolicyBuilder),
				factory: (sp, key) => sp.GetRequiredService<IPolicy<TPolicyBuilder>>(),
				lifetime: _lifetime);

			_services.Add(descriptor);

			return this; // Return 'this' for fluent chaining
		}
	}
}