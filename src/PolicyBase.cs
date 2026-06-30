namespace PoliNorError.Extensions.DependencyInjection
{
	/// <summary>
	/// Abstract base class for policy implementations that delegates to an inner policy.
	/// </summary>
	public abstract class PolicyBase : IPolicyBase
	{
		private IPolicyBase _innerPolicy = null!;

		/// <summary>
		/// Initializes the policy by creating the inner policy implementation.
		/// This method must be called before using the policy.
		/// </summary>
		public void Create()
		{
			_innerPolicy = CreateInner();
		}

		/// <summary>
		/// Creates the inner policy implementation.
		/// Override this method to define the actual policy logic.
		/// </summary>
		/// <returns>The inner policy implementation.</returns>
		protected abstract IPolicyBase CreateInner();

		/// <inheritdoc />
		public IPolicyProcessor PolicyProcessor => _innerPolicy.PolicyProcessor;

		/// <inheritdoc />
		public string PolicyName => _innerPolicy.PolicyName;

		/// <inheritdoc />
		public PolicyResult Handle(Action action, CancellationToken token = default) 
			=> _innerPolicy.Handle(action, token);

		/// <inheritdoc />
		public PolicyResult<T> Handle<T>(Func<T> func, CancellationToken token = default) 
			=> _innerPolicy.Handle(func, token);

		/// <inheritdoc />
		public Task<PolicyResult> HandleAsync(Func<CancellationToken, Task> func, bool configureAwait = false, CancellationToken token = default)
			=> _innerPolicy.HandleAsync(func, configureAwait, token);

		/// <inheritdoc />
		public Task<PolicyResult<T>> HandleAsync<T>(Func<CancellationToken, Task<T>> func, bool configureAwait = false, CancellationToken token = default)
			=> _innerPolicy.HandleAsync(func, configureAwait, token);
	}
}
