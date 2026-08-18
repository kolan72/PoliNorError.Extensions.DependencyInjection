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
		/// Registers a new keyed <see cref="IPolicy"/> service whose inner policy is produced
		/// by the specified <paramref name="factory"/> delegate. No builder class is required —
		/// the factory directly creates the <see cref="IPolicyBase"/> instance.
		/// </summary>
		/// <remarks>
		/// <para>
		/// The resulting policy can be resolved with
		/// <see cref="GetRequiredKeyedPolicy(IServiceProvider, object)"/>, e.g.
		/// <c>sp.GetRequiredKeyedPolicy("http-retry")</c>, or directly with
		/// <c>sp.GetRequiredKeyedService&lt;IPolicy&gt;("http-retry")</c>.
		/// </para>
		/// <para>
		/// Keys are compared using standard object equality at resolution time
		/// (value equality for strings, per the .NET keyed-services behavior).
		/// Use stable keys such as <c>const</c> or statically cached strings so
		/// that registration and resolution match reliably.
		/// </para>
		/// <para>
		/// With the default <see cref="ServiceLifetime.Transient"/> the factory runs on
		/// every resolution, producing a fresh policy each time. Register with
		/// <see cref="ServiceLifetime.Singleton"/> (and a factory that returns the
		/// same instance) to share one policy instance across the application.
		/// </para>
		/// </remarks>
		/// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
		/// <param name="key">The stable key used to resolve this policy. Must not be <see langword="null"/>.</param>
		/// <param name="factory">
		/// A delegate that creates the <see cref="IPolicyBase"/> instance from the
		/// <see cref="IServiceProvider"/>. Must not be <see langword="null"/> and must not
		/// return <see langword="null"/>.
		/// </param>
		/// <param name="lifetime">
		/// The <see cref="ServiceLifetime"/> for the keyed registration.
		/// Defaults to <see cref="ServiceLifetime.Transient"/>.
		/// </param>
		/// <returns>The <see cref="IServiceCollection"/> for method chaining.</returns>
		public static IServiceCollection AddKeyedPolicy(
			this IServiceCollection services,
			object key,
			Func<IServiceProvider, IPolicyBase> factory,
			ServiceLifetime lifetime = ServiceLifetime.Transient)
		{
			ArgumentNullException.ThrowIfNull(key);
			ArgumentNullException.ThrowIfNull(factory);

			services.Add(new ServiceDescriptor(
				typeof(IPolicy),
				key,
				(sp, _) => new KeyedProxyPolicy(factory(sp)
					?? throw new InvalidOperationException(
						$"The factory for keyed policy with key {key} returned null. " +
						"It must return a non-null IPolicyBase instance.")),
				lifetime));

			return services;
		}

		/// <summary>
		/// Registers a new keyed <see cref="IPolicy"/> service built by the
		/// <see cref="IPolicyBuilder{TBuilder}"/> implementation <typeparamref name="TBuilder"/>.
		/// </summary>
		/// <remarks>
		/// <para>
		/// When <typeparamref name="TBuilder"/> is not yet registered (for example because
		/// <see cref="AddPoliNorError(IServiceCollection, Assembly?, ServiceLifetime)"/> was not called, or was
		/// called for an assembly that
		/// does not contain the builder), it is registered automatically with
		/// <paramref name="lifetime"/>. An existing registration (e.g. from assembly
		/// scanning) always takes precedence.
		/// </para>
		/// <para>
		/// As with <see cref="IPolicy{TBuilder}"/> resolution, the builder's configurator is
		/// injected when the builder inherits from
		/// <see cref="PolicyBuilder{TPolicy,TConfigurator,TBuilder}"/>, so typed and keyed
		/// registrations share the same build pipeline.
		/// </para>
		/// <para>
		/// The resulting policy can be resolved with
		/// <see cref="GetRequiredKeyedPolicy(IServiceProvider, object)"/>, e.g.
		/// <c>sp.GetRequiredKeyedPolicy("db-retry")</c>. Keys are compared by value
		/// equality at resolution time, so use stable keys such as <c>const</c> strings.
		/// </para>
		/// </remarks>
		/// <typeparam name="TBuilder">
		/// The builder type whose <see cref="IPolicyBuilder{TBuilder}.Build"/> result this
		/// keyed policy wraps.
		/// </typeparam>
		/// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
		/// <param name="key">The stable key used to resolve this policy. Must not be <see langword="null"/>.</param>
		/// <param name="lifetime">
		/// The <see cref="ServiceLifetime"/> for the keyed registration.
		/// Defaults to <see cref="ServiceLifetime.Transient"/>.
		/// </param>
		/// <returns>The <see cref="IServiceCollection"/> for method chaining.</returns>
		public static IServiceCollection AddKeyedPolicy<TBuilder>(
			this IServiceCollection services,
			object key,
			ServiceLifetime lifetime = ServiceLifetime.Transient)
			where TBuilder : IPolicyBuilder<TBuilder>
		{
			ArgumentNullException.ThrowIfNull(key);

			// Make the call self-sufficient: register the builder only when nothing
			// else (e.g. assembly scanning) has registered it already.
			services.TryAdd(new ServiceDescriptor(typeof(IPolicyBuilder<TBuilder>), typeof(TBuilder), lifetime));

			services.Add(new ServiceDescriptor(
				typeof(IPolicy),
				key,
				(sp, _) => new KeyedProxyPolicy(
					PolicyBuilderInvoker.Build(sp.GetRequiredService<IPolicyBuilder<TBuilder>>(), sp)),
				lifetime));

			return services;
		}

		/// <summary>
		/// Resolves the keyed <see cref="IPolicy"/> registered via
		/// <see cref="AddKeyedPolicy"/> under the specified key.
		/// </summary>
		/// <remarks>
		/// Type-safe stand-in for <c>GetKeyedService&lt;IPolicy&gt;(key)</c> that keeps
		/// <see cref="IPolicy"/> out of consumer code that prefers explicit resolvers.
		/// Like the framework method it wraps, this method does not throw when the
		/// key is unknown; use <see cref="GetRequiredKeyedPolicy"/> for fail-fast behavior.
		/// </remarks>
		/// <param name="serviceProvider">
		/// The <see cref="IServiceProvider"/> to resolve the policy from.
		/// Must not be <see langword="null"/>.
		/// </param>
		/// <param name="key">The stable key the policy was registered with.</param>
		/// <returns>
		/// The resolved <see cref="IPolicy"/>, or <see langword="null"/> when no keyed
		/// policy with the specified key is registered, or the provider does not
		/// support keyed services.
		/// </returns>
		public static IPolicy? GetKeyedPolicy(this IServiceProvider serviceProvider, object? key)
		{
			ArgumentNullException.ThrowIfNull(serviceProvider);
			return (serviceProvider as IKeyedServiceProvider)?.GetKeyedService<IPolicy>(key);
		}

		/// <summary>
		/// Resolves the keyed <see cref="IPolicy"/> registered via
		/// <see cref="AddKeyedPolicy"/> under the specified key, failing fast when
		/// the policy does not exist.
		/// </summary>
		/// <remarks>
		/// Type-safe stand-in for <c>GetRequiredKeyedService&lt;IPolicy&gt;(key)</c>.
		/// </remarks>
		/// <param name="serviceProvider">
		/// The <see cref="IServiceProvider"/> to resolve the policy from.
		/// Must not be <see langword="null"/>.
		/// </param>
		/// <param name="key">The stable key the policy was registered with.</param>
		/// <returns>The resolved <see cref="IPolicy"/>.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="serviceProvider"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown when no keyed <see cref="IPolicy"/> with the specified key is registered,
		/// or when the provider does not implement <see cref="IKeyedServiceProvider"/>.
		/// </exception>
		public static IPolicy GetRequiredKeyedPolicy(this IServiceProvider serviceProvider, object? key)
		{
			ArgumentNullException.ThrowIfNull(serviceProvider);

			if (serviceProvider is IKeyedServiceProvider keyedProvider)
			{
				return keyedProvider.GetRequiredKeyedService<IPolicy>(key);
			}

			throw new InvalidOperationException(
				"The service provider does not implement IKeyedServiceProvider, " +
				"so keyed policies cannot be resolved from it. " +
				"Keyed services are supported by providers created from IServiceCollection.");
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
