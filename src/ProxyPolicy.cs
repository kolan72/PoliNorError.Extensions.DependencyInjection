namespace PoliNorError.Extensions.DependencyInjection
{
	internal class ProxyPolicy<TBuilder> : IPolicy<TBuilder> where TBuilder : IPolicyBuilder<TBuilder>
	{
		private readonly IPolicyBase _innerPolicy;

		public ProxyPolicy(IPolicyBuilder<TBuilder> factory)
		{
			_innerPolicy = factory.Build();
		}

		public string PolicyName => _innerPolicy.PolicyName;

		public IPolicyProcessor PolicyProcessor => _innerPolicy.PolicyProcessor;

		public PolicyResult Handle(Action action, CancellationToken token = default) => _innerPolicy.Handle(action, token);

		public PolicyResult<T1> Handle<T1>(Func<T1> func, CancellationToken token = default) => _innerPolicy.Handle(func, token);

		public Task<PolicyResult> HandleAsync(Func<CancellationToken, Task> func, bool configureAwait = false, CancellationToken token = default)
			=> _innerPolicy.HandleAsync(func, configureAwait, token);

		public Task<PolicyResult<T1>> HandleAsync<T1>(Func<CancellationToken, Task<T1>> func, bool configureAwait = false, CancellationToken token = default)
			=> _innerPolicy.HandleAsync(func, configureAwait, token);
	}
}
