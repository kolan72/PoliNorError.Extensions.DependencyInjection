namespace PoliNorError.Extensions.DependencyInjection
{
	/// <summary>
	/// Internal base class that implements <see cref="IPolicy"/> by delegating every
	/// member to an inner <see cref="IPolicyBase"/> instance. Shared by the type-based
	/// <see cref="ProxyPolicy{TBuilder}"/> and the keyed <see cref="KeyedProxyPolicy"/>
	/// so that both keep identical delegation behavior.
	/// </summary>
	internal abstract class ProxyPolicyBase : IPolicy
	{
		/// <summary>
		/// The inner policy instance produced by a builder or factory.
		/// </summary>
		protected readonly IPolicyBase InnerPolicy;

		/// <summary>
		/// Initialises a new <see cref="ProxyPolicyBase"/> with the specified inner policy.
		/// </summary>
		/// <param name="innerPolicy">The <see cref="IPolicyBase"/> instance to delegate to.</param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="innerPolicy"/> is <see langword="null"/>.
		/// </exception>
		protected ProxyPolicyBase(IPolicyBase innerPolicy)
		{
			InnerPolicy = innerPolicy ?? throw new ArgumentNullException(nameof(innerPolicy));
		}

		public string PolicyName => InnerPolicy.PolicyName;

		public IPolicyProcessor PolicyProcessor => InnerPolicy.PolicyProcessor;

		public PolicyResult Handle(Action action, CancellationToken token = default) => InnerPolicy.Handle(action, token);

		public PolicyResult<T> Handle<T>(Func<T> func, CancellationToken token = default) => InnerPolicy.Handle(func, token);

		public Task<PolicyResult> HandleAsync(Func<CancellationToken, Task> func, bool configureAwait = false, CancellationToken token = default)
			=> InnerPolicy.HandleAsync(func, configureAwait, token);

		public Task<PolicyResult<T>> HandleAsync<T>(Func<CancellationToken, Task<T>> func, bool configureAwait = false, CancellationToken token = default)
			=> InnerPolicy.HandleAsync(func, configureAwait, token);
	}
}
