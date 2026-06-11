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
	internal class ProxyPolicy<TBuilder> : IPolicy<TBuilder> where TBuilder : IPolicyBuilder<TBuilder>
	{
		private readonly IPolicyBase _innerPolicy;

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
		{
			if (factory is ISetConfigurator configurable)
			{
				configurable.SetConfigurator(serviceProvider);
			}

			_innerPolicy = factory.Build()
				?? throw new InvalidOperationException(
					$"{factory.GetType().Name}.Build() returned null. " +
					"Build() must return a non-null IPolicyBase instance.");
		}

		public string PolicyName => _innerPolicy.PolicyName;

		public IPolicyProcessor PolicyProcessor => _innerPolicy.PolicyProcessor;

		public PolicyResult Handle(Action action, CancellationToken token = default) => _innerPolicy.Handle(action, token);

		public PolicyResult<T> Handle<T>(Func<T> func, CancellationToken token = default) => _innerPolicy.Handle(func, token);

		public Task<PolicyResult> HandleAsync(Func<CancellationToken, Task> func, bool configureAwait = false, CancellationToken token = default)
			=> _innerPolicy.HandleAsync(func, configureAwait, token);

		public Task<PolicyResult<T>> HandleAsync<T>(Func<CancellationToken, Task<T>> func, bool configureAwait = false, CancellationToken token = default)
			=> _innerPolicy.HandleAsync(func, configureAwait, token);
	}
}
