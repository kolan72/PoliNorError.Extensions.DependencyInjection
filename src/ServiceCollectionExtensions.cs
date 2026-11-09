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
		/// <para>
		/// This method registers all concrete implementations of <see cref="IPolicyBuilder{TBuilder}"/>
		/// found in the specified assembly (or the calling assembly) and registers the
		/// core <see cref="IPolicy{T}"/> service using the <see cref="ProxyPolicy{T}"/> implementation.
		/// </para>
		/// <para>
		/// The <paramref name="lifetime"/> also colors the policy chain consumed by
		/// <see cref="AddKeyedPolicy{TBuilder}(IServiceCollection, string, ServiceLifetime)"/>:
		/// when this value is <see cref="ServiceLifetime.Scoped"/> and that method registers its
		/// key as <see cref="ServiceLifetime.Singleton"/>, the registration throws a lifetime-compatibility error.
		/// </para>
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

			// TryAdd is a no-op when IPolicy<> is already registered, preventing duplicates.
			services.TryAdd(new ServiceDescriptor(typeof(IPolicy<>), typeof(ProxyPolicy<>), lifetime));
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
		/// <para>
		/// The <paramref name="lifetime"/> also colors the policy chain consumed by
		/// <see cref="AddKeyedPolicy{TBuilder}(IServiceCollection, string, ServiceLifetime)"/>:
		/// when this value is <see cref="ServiceLifetime.Scoped"/> and that method registers its
		/// key as <see cref="ServiceLifetime.Singleton"/>, the registration throws a lifetime-compatibility error.
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
			}

			// Register IPolicy<> exactly once regardless of how many assemblies were scanned.
			services.TryAdd(new ServiceDescriptor(typeof(IPolicy<>), typeof(ProxyPolicy<>), lifetime));
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
		/// Registers a policy as a keyed <see cref="IPolicy"/> service using a factory delegate.
		/// </summary>
		/// <remarks>
		/// <para>
		/// The resulting <see cref="IPolicy"/> can be resolved via
		/// <c>sp.GetRequiredKeyedService&lt;IPolicy&gt;("key")</c> or injected with
		/// <c>[FromKeyedServices("key")] IPolicy policy</c>.
		/// </para>
		/// <para>
		/// This method does not require a builder class — the factory delegate directly produces
		/// the <see cref="IPolicyBase"/> instance.
		/// </para>
		/// <para>
		/// Lifetime guidance for the registration:
		/// </para>
		/// <list type="bullet">
		/// <item><description>
		/// <c>Transient</c> (default) — the factory runs against the provider of the current scope
		/// on every resolution, so scoped services resolved inside the factory are always safe.
		/// </description></item>
		/// <item><description>
		/// <c>Scoped</c> — the policy must be resolved from within an <see cref="IServiceScope"/>;
		/// resolving it from the root provider fails MEDI's scope validation
		/// ("cannot resolve scoped service ... from root provider").
		/// </description></item>
		/// <item><description>
		/// <c>Singleton</c> — the factory runs exactly once against the root provider, so every
		/// service resolved inside it must be singleton-compatible (a trapped <c>Scoped</c>
		/// resolution throws an <see cref="InvalidOperationException"/> at the first resolve that
		/// names this key and the registration lifetime).
		/// </description></item>
		/// </list>
		/// <para>
		/// When in doubt, keep the default: <c>Transient</c>.
		/// </para>
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		/// Thrown at the first resolve when the factory cannot resolve a scoped service from the
		/// root provider. The message starts with
		/// <c>Resolving keyed policy '{key}' (lifetime: {lifetime}) failed:</c> and appends
		/// remediation guidance after the original scope-validation error.
		/// </exception>
		/// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
		/// <param name="key">The service key used to resolve this policy.</param>
		/// <param name="factory">
		/// A delegate that creates the <see cref="IPolicyBase"/> instance from the
		/// <see cref="IServiceProvider"/>.
		/// </param>
		/// <param name="lifetime">The <see cref="ServiceLifetime"/> for the registration. Defaults to <see cref="ServiceLifetime.Transient"/>.</param>
		/// <returns>The <see cref="IServiceCollection"/> for method chaining.</returns>
		public static IServiceCollection AddKeyedPolicy(
			this IServiceCollection services,
			string key,
			Func<IServiceProvider, IPolicyBase> factory,
			ServiceLifetime lifetime = ServiceLifetime.Transient)
		{
			ArgumentNullException.ThrowIfNull(key);
			if (string.IsNullOrWhiteSpace(key))
				throw new ArgumentException("Key must be a non-empty, non-whitespace string.", nameof(key));
			ArgumentNullException.ThrowIfNull(factory);

			services.Add(new ServiceDescriptor(
				typeof(IPolicy),
				key,
				(sp, _) => KeyedPolicyLifetimeGuard.Invoke(key, lifetime, () => new KeyedProxyPolicy(factory(sp))),
				lifetime));

			return services;
		}

		/// <summary>
		/// Registers a policy as a keyed <see cref="IPolicy"/> service, using the existing
		/// <see cref="IPolicyBuilder{TBuilder}"/> infrastructure to produce the policy.
		/// </summary>
		/// <remarks>
		/// <para>
		/// The builder type <typeparamref name="TBuilder"/> must already be registered in the
		/// <see cref="IServiceCollection"/> (e.g., via <see cref="AddPoliNorError(IServiceCollection, Assembly, ServiceLifetime)"/>).
		/// </para>
		/// <para>
		/// The resulting <see cref="IPolicy"/> can be resolved via
		/// <c>sp.GetRequiredKeyedService&lt;IPolicy&gt;("key")</c> or injected with
		/// <c>[FromKeyedServices("key")] IPolicy policy</c>.
		/// </para>
		/// <para>
		/// Lifetime guidance for the registration:
		/// </para>
		/// <list type="bullet">
		/// <item><description>
		/// <c>Transient</c> (default) — resolves from the current scope on every call; always
		/// safe, even when the builder chain contains scoped services.
		/// </description></item>
		/// <item><description>
		/// <c>Scoped</c> — must be resolved within an <see cref="IServiceScope"/>; resolving from
		/// the root provider fails MEDI's scope validation.
		/// </description></item>
		/// <item><description>
		/// <c>Singleton</c> — built once against the root provider, so every service in the chain
		/// must be singleton-compatible. Build-time <c>Transient</c> pieces are accepted but become
		/// rooted — fine for stateless builders/configurators, wrong for stateful services. A
		/// <c>Scoped</c> link in the chain throws at registration and again at the first resolve.
		/// </description></item>
		/// </list>
		/// <para>
		/// When in doubt, keep the default: <c>Transient</c>.
		/// </para>
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		/// Thrown at registration (and at the first resolve as a safety net when registrations are
		/// added afterwards) when the key is registered as <c>Singleton</c> but the policy chain
		/// contains a <c>Scoped</c> registration. The message is:
		/// <c>Keyed policy '{key}' is registered as Singleton but '{chain type}' is registered as
		/// Scoped. A singleton keyed policy resolves from the root provider. Register the keyed
		/// policy as Transient, or register the whole chain as Singleton.</c>
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown at the first resolve when a scoped service cannot be resolved from the root
		/// provider while building the policy. The message starts with
		/// <c>Resolving keyed policy '{key}' (lifetime: {lifetime}) failed:</c>.
		/// </exception>
		/// <typeparam name="TBuilder">
		/// The builder type whose <see cref="IPolicyBuilder{TBuilder}.Build"/> result this policy wraps.
		/// Must implement <see cref="IPolicyBuilder{TBuilder}"/>.
		/// </typeparam>
		/// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
		/// <param name="key">The service key used to resolve this policy.</param>
		/// <param name="lifetime">The <see cref="ServiceLifetime"/> for the registration. Defaults to <see cref="ServiceLifetime.Transient"/>.</param>
		/// <returns>The <see cref="IServiceCollection"/> for method chaining.</returns>
		public static IServiceCollection AddKeyedPolicy<TBuilder>(
			this IServiceCollection services,
			string key,
			ServiceLifetime lifetime = ServiceLifetime.Transient)
			where TBuilder : IPolicyBuilder<TBuilder>
		{
			ArgumentNullException.ThrowIfNull(key);
			if (string.IsNullOrWhiteSpace(key))
				throw new ArgumentException("Key must be a non-empty, non-whitespace string.", nameof(key));

			Type[]? chainDependencies = lifetime == ServiceLifetime.Singleton
				? KeyedPolicyLifetimeGuard.GetDependencyTypes(typeof(TBuilder))
				: null;

			if (chainDependencies is not null)
			{
				KeyedPolicyLifetimeGuard.ValidateOrThrow(services, key, lifetime, chainDependencies);
			}

			services.Add(new ServiceDescriptor(
				typeof(IPolicy),
				key,
				(sp, _) => KeyedPolicyLifetimeGuard.Invoke(key, lifetime, () =>
				{
					if (chainDependencies is not null)
					{
						KeyedPolicyLifetimeGuard.ValidateOrThrow(services, key, lifetime, chainDependencies);
					}

					return new KeyedProxyPolicy(
						sp.GetRequiredService<IPolicy<TBuilder>>());
				}),
				lifetime));

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
	}
}
