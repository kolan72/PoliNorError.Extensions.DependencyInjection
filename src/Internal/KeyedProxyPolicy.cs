namespace PoliNorError.Extensions.DependencyInjection
{
	/// <summary>
	/// Internal implementation of the non-generic <see cref="IPolicy"/> interface that
	/// delegates all policy operations to an inner <see cref="IPolicyBase"/> instance.
	/// Used when policies are registered as keyed services.
	/// </summary>
	internal class KeyedProxyPolicy : IPolicy
	{
		private readonly IPolicyBase _innerPolicy;

		public KeyedProxyPolicy(IPolicyBase innerPolicy)
		{
			_innerPolicy = innerPolicy ?? throw new ArgumentNullException(nameof(innerPolicy));
		}

		public string PolicyName => _innerPolicy.PolicyName;

		public IPolicyProcessor PolicyProcessor => _innerPolicy.PolicyProcessor;

		public PolicyResult Handle(Action action, CancellationToken token = default)
			=> _innerPolicy.Handle(action, token);

		public PolicyResult<T> Handle<T>(Func<T> func, CancellationToken token = default)
			=> _innerPolicy.Handle(func, token);

		public Task<PolicyResult> HandleAsync(Func<CancellationToken, Task> func, bool configureAwait = false, CancellationToken token = default)
			=> _innerPolicy.HandleAsync(func, configureAwait, token);

		public Task<PolicyResult<T>> HandleAsync<T>(Func<CancellationToken, Task<T>> func, bool configureAwait = false, CancellationToken token = default)
			=> _innerPolicy.HandleAsync(func, configureAwait, token);
	}
}
