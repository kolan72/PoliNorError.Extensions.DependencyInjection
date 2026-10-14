using Microsoft.Extensions.DependencyInjection;
using System;

namespace PoliNorError.Extensions.DependencyInjection
{
	/// <summary>
	/// Provides read-only access to a registry of policies.
	/// </summary>
	/// <remarks>
	/// This interface is used to resolve policies that were registered via
	/// <see cref="ResilienceServiceCollectionExtensions.AddPolicyRegistry"/>.
	/// It provides a cleaner constructor signature when a consumer needs access to many policies.
	/// </remarks>
	public interface IPolicyRegistry
	{
		/// <summary>
		/// Retrieves a policy from the registry by its builder type.
		/// </summary>
		/// <typeparam name="TBuilder">The policy builder type used as the key.</typeparam>
		/// <returns>The resolved policy instance.</returns>
		/// <exception cref="InvalidOperationException">
		/// Thrown if no policy was registered for the specified builder type.
		/// </exception>
		IPolicyBase GetPolicy<TBuilder>() where TBuilder : IPolicyBuilder<TBuilder>;
	}

	/// <summary>
	/// Implementation of <see cref="IPolicyRegistry"/> that resolves policies
	/// from the DI container's keyed services.
	/// </summary>
	internal class PolicyRegistry : IPolicyRegistry
	{
		private readonly IServiceProvider _serviceProvider;

		/// <summary>
		/// Initializes a new instance of the <see cref="PolicyRegistry"/> class.
		/// </summary>
		/// <param name="serviceProvider">
		/// The service provider used to resolve keyed services.
		/// </param>
		public PolicyRegistry(IServiceProvider serviceProvider)
		{
			_serviceProvider = serviceProvider;
		}

		/// <inheritdoc />
		public IPolicyBase GetPolicy<TBuilder>() where TBuilder : IPolicyBuilder<TBuilder>
		{
			// Use the extension method instead of casting to IKeyedServiceProvider
			// This is more robust and works with any IServiceProvider implementation
			return _serviceProvider.GetRequiredKeyedService<IPolicyBase>(typeof(TBuilder));
		}
	}
}