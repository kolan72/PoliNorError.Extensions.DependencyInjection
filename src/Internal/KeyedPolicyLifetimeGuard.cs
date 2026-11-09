using Microsoft.Extensions.DependencyInjection;

namespace PoliNorError.Extensions.DependencyInjection
{
	/// <summary>
	/// Guards keyed <see cref="IPolicy"/> registrations against captive-dependency mistakes:
	/// a keyed registration with <see cref="ServiceLifetime.Singleton"/> is executed once
	/// against the root provider, so a <see cref="ServiceLifetime.Scoped"/> link in its
	/// resolve chain either fails scope validation or silently becomes a captive dependency.
	/// </summary>
	internal static class KeyedPolicyLifetimeGuard
	{
		private const string GuardMessagePrefix = "Keyed policy '";

		/// <summary>
		/// Returns the service types consumed while a keyed <see cref="IPolicy{TBuilder}"/> is built:
		/// the builder itself, the proxy that wraps it, the configurator recovered from the builder's
		/// <see cref="PolicyBuilder{TPolicy, TConfigurator, TBuilder}"/> base and the configurator's
		/// one-level constructor parameter types. Types without a registration are skipped later
		/// by <see cref="ValidateOrThrow"/>.
		/// </summary>
		internal static Type[] GetDependencyTypes(Type tBuilder)
		{
			var dependencies = new List<Type>
			{
				typeof(IPolicyBuilder<>).MakeGenericType(tBuilder),
				typeof(IPolicy<>).MakeGenericType(tBuilder),
			};

			var configuratorType = GetConfiguratorType(tBuilder);
			if (configuratorType != null)
			{
				dependencies.Add(configuratorType);

				foreach (var constructor in configuratorType.GetConstructors())
				{
					foreach (var parameter in constructor.GetParameters())
					{
						dependencies.Add(parameter.ParameterType);
					}
				}
			}

			return dependencies.Distinct().ToArray();
		}

		/// <summary>
		/// Returns the lifetime of the last descriptor registered for <paramref name="serviceType"/>
		/// (last registration wins, mirroring MEDI), falling back to an open-generic descriptor.
		/// Returns <see langword="null"/> when nothing is registered — the caller must skip
		/// the dependency instead of throwing, because <c>AddPoliNorError</c> may not have been called yet.
		/// </summary>
		internal static ServiceLifetime? GetEffectiveLifetime(IServiceCollection services, Type serviceType)
		{
			ArgumentNullException.ThrowIfNull(services);
			ArgumentNullException.ThrowIfNull(serviceType);

			var exactMatch = services.LastOrDefault(d => d.ServiceType == serviceType);
			if (exactMatch != null)
			{
				return exactMatch.Lifetime;
			}

			if (!serviceType.IsGenericType)
			{
				return null;
			}

			var definition = serviceType.IsGenericTypeDefinition
				? serviceType
				: serviceType.GetGenericTypeDefinition();

			var openGenericMatch = services.LastOrDefault(
				d => d.ServiceType.IsGenericTypeDefinition && d.ServiceType == definition);
			return openGenericMatch?.Lifetime;
		}

		/// <summary>
		/// Throws when <paramref name="lifetime"/> is <see cref="ServiceLifetime.Singleton"/> and any
		/// dependency of the keyed policy resolves to a <see cref="ServiceLifetime.Scoped"/> registration.
		/// All other lifetimes (and unregistered dependencies) are accepted without checks.
		/// </summary>
		/// <exception cref="InvalidOperationException">
		/// A singleton key was combined with a scoped link in the policy chain.
		/// </exception>
		internal static void ValidateOrThrow(
			IServiceCollection services,
			string key,
			ServiceLifetime lifetime,
			IEnumerable<Type> dependencies)
		{
			if (lifetime != ServiceLifetime.Singleton)
			{
				return;
			}

			foreach (var dependency in dependencies)
			{
				if (GetEffectiveLifetime(services, dependency) == ServiceLifetime.Scoped)
				{
					throw new InvalidOperationException(
						$"Keyed policy '{key}' is registered as Singleton but '{FormatType(dependency)}' is registered as Scoped. " +
						"A singleton keyed policy resolves from the root provider. " +
						"Register the keyed policy as Transient, or register the whole chain as Singleton.");
				}
			}
		}

		/// <summary>
		/// Executes a keyed policy factory and rewrites root/scoped resolution failures into an
		/// error that names the key and the registration lifetime. Exceptions originating from
		/// <see cref="ValidateOrThrow"/> are rethrown untouched, so guard messages are never
		/// wrapped twice; everything else propagates as-is.
		/// </summary>
		internal static T Invoke<T>(string key, ServiceLifetime lifetime, Func<T> factory)
		{
			try
			{
				return factory();
			}
			catch (InvalidOperationException ex) when (IsRootScopedResolutionFailure(ex))
			{
				if (ex.Message.StartsWith(GuardMessagePrefix, StringComparison.Ordinal))
				{
					throw;
				}

				throw new InvalidOperationException(
					$"Resolving keyed policy '{key}' (lifetime: {lifetime}) failed: {ex.Message} " +
					"A singleton keyed policy is built against the root provider; scoped services cannot be resolved from it.",
					ex);
			}
		}

		private static Type? GetConfiguratorType(Type tBuilder)
		{
			var baseType = tBuilder.BaseType;
			if (baseType is null ||
				!baseType.IsGenericType ||
				baseType.GetGenericTypeDefinition() != typeof(PolicyBuilder<,,>))
			{
				return null;
			}

			return baseType.GetGenericArguments()[1];
		}

		private static bool IsRootScopedResolutionFailure(Exception ex)
			=> ex.Message.Contains("scoped service", StringComparison.OrdinalIgnoreCase)
			&& ex.Message.Contains("root provider", StringComparison.OrdinalIgnoreCase);

		private static string FormatType(Type type)
		{
			if (!type.IsGenericType)
			{
				return type.FullName ?? type.Name;
			}

			var name = type.Name;
			var arityIndex = name.IndexOf('`');
			if (arityIndex >= 0)
			{
				name = name.Substring(0, arityIndex);
			}

			var arguments = string.Join(", ", type.GetGenericArguments().Select(FormatType));
			return $"{name}<{arguments}>";
		}
	}
}