namespace PoliNorError.Extensions.DependencyInjection
{
	internal class ProxyPolicy<TBuilder> : IPolicy<TBuilder> where TBuilder : IPolicyBuilder<TBuilder>
	{
		private readonly IPolicyBase _innerPolicy;

		public ProxyPolicy(IPolicyBuilder<TBuilder> factory, IServiceProvider serviceProvider)
		{
			if (IsSubclassOfGenericDefinition(factory.GetType(), typeof(PolicyBuilder<,>)))
			{
				((ISetConfigurator)factory).SetConfigurator(serviceProvider);
				_innerPolicy = factory.Build();
			}
			else
			{
				_innerPolicy = factory.Build();
			}
		}

		public string PolicyName => _innerPolicy.PolicyName;

		public IPolicyProcessor PolicyProcessor => _innerPolicy.PolicyProcessor;

		public PolicyResult Handle(Action action, CancellationToken token = default) => _innerPolicy.Handle(action, token);

		public PolicyResult<T> Handle<T>(Func<T> func, CancellationToken token = default) => _innerPolicy.Handle(func, token);

		public Task<PolicyResult> HandleAsync(Func<CancellationToken, Task> func, bool configureAwait = false, CancellationToken token = default)
			=> _innerPolicy.HandleAsync(func, configureAwait, token);

		public Task<PolicyResult<T>> HandleAsync<T>(Func<CancellationToken, Task<T>> func, bool configureAwait = false, CancellationToken token = default)
			=> _innerPolicy.HandleAsync(func, configureAwait, token);

		private static bool IsSubclassOfGenericDefinition(Type? candidate, Type genericBase)
		{
			while (candidate != null && candidate != typeof(object))
			{
				var current = candidate.IsGenericType ? candidate.GetGenericTypeDefinition() : candidate;
				if (current == genericBase)
					return true;

				candidate = candidate.BaseType;
			}

			return false;
		}
	}
}
