namespace PoliNorError.Extensions.DependencyInjection
{
	/// <summary>
	/// Internal implementation of <see cref="IPolicy{TBuilder}"/> that delegates all policy
	/// operations to the <see cref="IPolicyBase"/> instance produced by the associated
	/// <see cref="IPolicyBuilder{TBuilder}"/>. Registered by
	/// <see cref="ServiceCollectionExtensions"/> as the open-generic mapping
	/// <c>IPolicy&lt;&gt; →ProxyPolicy&lt;&gt;</c>.
	/// </summary>
	/// <typeparam name="TBuilder">
	/// The builder type whose <see cref="IPolicyBuilder{TBuilder}.Build"/> result this proxy wraps.
	/// </typeparam>
	internal class ProxyPolicy<TBuilder> : ProxyPolicyBase, IPolicy<TBuilder> where TBuilder : IPolicyBuilder<TBuilder>
	{
		/// <summary>
		/// Initialises a new <see cref="ProxyPolicy{TBuilder}"/> by optionally injecting a
		/// configurator into the factory (when it implements <see cref="ISetConfigurator"/>),
		/// then calling <see cref="IPolicyBuilder{TBuilder}.Build"/> to obtain the inner policy.
		/// </summary>
		/// <param name="factory">The builder that produces the inner policy.</param>
		/// <param name="serviceProvider">
		/// The DI service provider, used to resolve the configurator when the factory implements
		/// <see cref="ISetConfigurator"/>.
		/// </param>
		/// <exception cref="InvalidOperationException">
		/// Thrown when <paramref name="factory"/>.<see cref="IPolicyBuilder{TBuilder}.Build"/>
		/// returns <see langword="null"/>.
		/// </exception>
		public ProxyPolicy(IPolicyBuilder<TBuilder> factory, IServiceProvider serviceProvider)
			: base(PolicyBuilderInvoker.Build(factory, serviceProvider))
		{
		}
	}
}
