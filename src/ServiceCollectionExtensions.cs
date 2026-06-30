using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
		/// <param name="assemblyToScan">The <see cref="Assembly"/> to scan for types that implement <see cref="IPolicyBuilder{TBuilder}"/>. If <see langword="null"/>, the assembly containing this extension method is used.</param>
		/// <param name="lifetime">The <see cref="ServiceLifetime"/> to use for registration of all PoliNorError services. Defaults to <see cref="ServiceLifetime.Transient"/>.</param>
		/// <returns>The <see cref="IServiceCollection"/> for method chaining.</returns>
		public static IServiceCollection AddPoliNorError(
			this IServiceCollection services,
			Assembly? assemblyToScan,
			ServiceLifetime lifetime = ServiceLifetime.Transient)
		{
			// Normalize null once at the public boundary; internal methods keep their own fallback.
			assemblyToScan ??= Assembly.GetCallingAssembly();

			services.AddAllPolicyBuilders(assemblyToScan, lifetime);
			services.AddAllPolicyConfigurators(assemblyToScan, lifetime);
			services.AddAllPolicies(assemblyToScan, lifetime);

			// TryAdd is a no-op when IPolicy<> is already registered, preventing duplicates.
			services.TryAdd(new ServiceDescriptor(typeof(IPolicy<>), typeof(ProxyPolicy<>), lifetime));
			services.TryAddSingleton<IPolicyFactory, PolicyFactory>();
			return services;
		}

		/// <summary>
		/// Adds PoliNorError core services to the specified <see cref="IServiceCollection"/>,
		/// scanning one or more assemblies for <see cref="IPolicyBuilder{TBuilder}"/> implementations
		/// and <see cref="PolicyConfigurator{TPolicy}"/> subclasses.
		/// </summary>
		/// <remarks>
		/// <para>
		/// When called with multiple assemblies, each assembly is scanned independently and all
		/// discovered types are registered. The open-generic <c>IPolicy&lt;&gt;</c> mapping is
		/// registered exactly once regardless of how many assemblies are provided.
		/// </para>
		/// <para>
		/// Note: <paramref name="lifetime"/> precedes <paramref name="assembliesToScan"/> to avoid
		/// ambiguity with the single-assembly overload <see cref="AddPoliNorError(IServiceCollection, Assembly, ServiceLifetime)"/>.
		/// </para>
		/// </remarks>
		/// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
		/// <param name="lifetime">The <see cref="ServiceLifetime"/> to use for all registrations. Defaults to <see cref="ServiceLifetime.Transient"/>.</param>
		/// <param name="assembliesToScan">
		/// One or more assemblies to scan. If empty, the calling assembly is used.
		/// </param>
		/// <returns>The <see cref="IServiceCollection"/> for method chaining.</returns>
		public static IServiceCollection AddPoliNorError(
			this IServiceCollection services,
			ServiceLifetime lifetime = ServiceLifetime.Transient,
			params Assembly[] assembliesToScan)
		{
			// Fall back to the calling assembly when no assemblies are provided.
			if (assembliesToScan.Length == 0)
				assembliesToScan = [Assembly.GetCallingAssembly()];

			foreach (var assembly in assembliesToScan)
			{
				services.AddAllPolicyBuilders(assembly, lifetime);
				services.AddAllPolicyConfigurators(assembly, lifetime);
				services.AddAllPolicies(assembly, lifetime);
			}

			// Register IPolicy<> exactly once regardless of how many assemblies were scanned.
			services.TryAdd(new ServiceDescriptor(typeof(IPolicy<>), typeof(ProxyPolicy<>), lifetime));
			services.TryAddSingleton<IPolicyFactory, PolicyFactory>();
			return services;
		}

		/// <summary>
		/// Scans the specified assembly (or the assembly containing the extension method if null)
		/// and registers all concrete classes that implement the <see cref="IPolicyBuilder{TBuilder}"/> interface.
		/// </summary>
		/// <param name="services">The IServiceCollection instance.</param>
		/// <param name="assemblyToScan">The assembly to scan for types. If null, the calling assembly is used.</param>
		/// <param name="lifetime">The ServiceLifetime to use for registration (Transient, Scoped, or Singleton).</param>
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
					InterfaceType = t.GetInterfaces()
						.FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGenericInterface)
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

		/// <summary>
		/// Scans the specified assembly (or the assembly containing the extension method if <see langword="null"/>)
		/// and registers all concrete classes that inherit from <see cref="PolicyConfigurator{TPolicy}"/>
		/// as self-mapped services.
		/// </summary>
		/// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
		/// <param name="assemblyToScan">The assembly to scan for types. If <see langword="null"/>, the assembly containing this extension method is used.</param>
		/// <param name="lifetime">The <see cref="ServiceLifetime"/> to use for registration. Defaults to <see cref="ServiceLifetime.Transient"/>.</param>
		/// <returns>The <see cref="IServiceCollection"/> for method chaining.</returns>
		internal static IServiceCollection AddAllPolicyConfigurators(this IServiceCollection services, Assembly assemblyToScan,
			ServiceLifetime lifetime = ServiceLifetime.Transient)
		{
			assemblyToScan ??= Assembly.GetExecutingAssembly();

			var configuratorTypes = assemblyToScan.GetTypes()
				.Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition && InheritsFromPolicyConfigurator(t));

			foreach (var type in configuratorTypes)
			{
				var descriptor = new ServiceDescriptor(type, type, lifetime);
				services.Add(descriptor);
			}
			return services;
		}

		/// <summary>
		/// Scans the specified assembly (or the assembly containing the extension method if <see langword="null"/>)
		/// and registers all concrete classes that inherit from <see cref="PolicyBase"/>
		/// as self-mapped services.
		/// </summary>
		/// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
		/// <param name="assemblyToScan">The assembly to scan for types. If <see langword="null"/>, the assembly containing this extension method is used.</param>
		/// <param name="lifetime">The <see cref="ServiceLifetime"/> to use for registration. Defaults to <see cref="ServiceLifetime.Transient"/>.</param>
		/// <returns>The <see cref="IServiceCollection"/> for method chaining.</returns>
		internal static IServiceCollection AddAllPolicies(this IServiceCollection services, Assembly assemblyToScan,
			ServiceLifetime lifetime = ServiceLifetime.Transient)
		{
			assemblyToScan ??= Assembly.GetExecutingAssembly();

			var policyTypes = assemblyToScan.GetTypes()
				.Where(t => t.IsClass && !t.IsAbstract && InheritsFromPolicyBase(t));

			foreach (var type in policyTypes)
			{
				var descriptor = new ServiceDescriptor(type, type, lifetime);
				services.Add(descriptor);
			}
			return services;
		}

		private static bool InheritsFromPolicyConfigurator(Type? type)
		{
			while (type != null && type != typeof(object))
			{
				if (type.IsGenericType &&
					type.GetGenericTypeDefinition() == typeof(PolicyConfigurator<>))
				{
					return true;
				}

				type = type.BaseType;
			}

			return false;
		}

		private static bool InheritsFromPolicyBase(Type? type)
		{
			while (type != null && type != typeof(object))
			{
				if (type == typeof(PolicyBase))
				{
					return true;
				}

				type = type.BaseType;
			}

			return false;
		}
	}
}
