namespace PoliNorError.Extensions.DependencyInjection
{
	/// <summary>
	/// Internal helper that runs the common "apply the configurator (when applicable),
	/// then build" sequence for both the type-based <see cref="ProxyPolicy{TBuilder}"/>
	/// and the keyed <see cref="KeyedProxyPolicy"/> registrations.
	/// </summary>
	internal static class PolicyBuilderInvoker
	{
		/// <summary>
		/// Injects a <see cref="PolicyConfigurator{TPolicy}"/> into the builder when it
		/// implements <see cref="ISetConfigurator"/>, calls <see cref="IPolicyBuilder{TBuilder}.Build"/>,
		/// and returns the built policy.
		/// </summary>
		/// <typeparam name="TBuilder">The builder type.</typeparam>
		/// <param name="builder">The builder that produces the policy.</param>
		/// <param name="serviceProvider">
		/// The DI service provider, used to resolve the configurator when the builder
		/// implements <see cref="ISetConfigurator"/>.
		/// </param>
		/// <returns>The built <see cref="IPolicyBase"/> instance.</returns>
		/// <exception cref="InvalidOperationException">
		/// Thrown when <paramref name="builder"/><see cref="IPolicyBuilder{TBuilder}.Build"/>
		/// returns <see langword="null"/>.
		/// </exception>
		public static IPolicyBase Build<TBuilder>(
			IPolicyBuilder<TBuilder> builder,
			IServiceProvider serviceProvider)
			where TBuilder : IPolicyBuilder<TBuilder>
		{
			if (builder is ISetConfigurator configurable)
			{
				configurable.SetConfigurator(serviceProvider);
			}

			return builder.Build()
				?? throw new InvalidOperationException(
					$"{builder.GetType().Name}.Build() returned null. " +
					"Build() must return a non-null IPolicyBase instance.");
		}
	}
}
