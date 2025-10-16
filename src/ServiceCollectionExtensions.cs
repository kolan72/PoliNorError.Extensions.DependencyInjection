using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace PoliNorError.Extensions.DependencyInjection
{
	public static class ServiceCollectionExtensions
	{
		/// <summary>
		/// Adds PoliNorError core services to the specified <see cref="IServiceCollection"/>.
		/// </summary>
		/// <remarks>
		/// This method registers all concrete implementations of <see cref="IPolicyBuilder{TBuilder}"/>
		/// found in the specified assembly (or the calling assembly) and registers the
		/// core <see cref="IPolicy{T}"/> service using the <see cref="ProxyPolicy{T}"/> implementation.
		/// </remarks>
		/// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
		/// <param name="lifetime">The <see cref="ServiceLifetime"/> to use for registration of all PoliNorError services. Defaults to <see cref="ServiceLifetime.Transient"/>.</param>
		/// <param name="assemblyToScan">The <see cref="Assembly"/> to scan for types that implement <see cref="IPolicyBuilder{TBuilder}"/>. If <see langword="null"/>, the assembly containing this extension method is used.</param>
		/// <returns>The <see cref="IServiceCollection"/> for method chaining.</returns>
		public static IServiceCollection AddPoliNorError(
		this IServiceCollection services,
			Assembly assemblyToScan,
			ServiceLifetime lifetime = ServiceLifetime.Transient)
		{
			services.AddAllPolicyBuilders(assemblyToScan, lifetime);

			services.Add(new ServiceDescriptor(typeof(IPolicy<>), typeof(ProxyPolicy<>), lifetime));
			return services;
		}

		/// <summary>
		/// Scans the specified assembly (or the assembly containing the extension method if null)
		/// and registers all concrete classes that implement the IPolicyBuilder<TBuilder> interface.
		/// </summary>
		/// <param name="services">The IServiceCollection instance.</param>
		/// <param name="lifetime">The ServiceLifetime to use for registration (Transient, Scoped, or Singleton).</param>
		/// <param name="assemblyToScan">The assembly to scan for types. If null, the calling assembly is used.</param>
		/// <returns>The IServiceCollection for chaining.</returns>
		internal static IServiceCollection AddAllPolicyBuilders(
			this IServiceCollection services,
			Assembly assemblyToScan,
			ServiceLifetime lifetime = ServiceLifetime.Transient)
		{
			// Use the assembly where this extension method is defined if none is specified.
			assemblyToScan ??= Assembly.GetExecutingAssembly();

			// 1. Define the open generic interface type to search for.
			var openGenericInterface = typeof(IPolicyBuilder<>);

			// 2. Scan the assembly for all types that are concrete classes and implement IPolicyBuilder<TBuilder>.
			var builderTypes = assemblyToScan.GetTypes()
				.Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition)
				.Select(t => new
				{
					ImplementationType = t,
					InterfaceType = Array.Find(t.GetInterfaces(),
											i => i.IsGenericType && i.GetGenericTypeDefinition() == openGenericInterface)
				})
				.Where(x => x.InterfaceType != null);

			// 3. Register each implementation.
			foreach (var builderRegistration in builderTypes)
			{
				var serviceType = builderRegistration.InterfaceType;
				var implementationType = builderRegistration.ImplementationType;

				var descriptor = new ServiceDescriptor(serviceType!, implementationType, lifetime);
				services.Add(descriptor);
			}

			return services;
		}
	}
}
