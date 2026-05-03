using Microsoft.Extensions.DependencyInjection;

namespace PoliNorError.Extensions.DependencyInjection
{
	/// <summary>
	/// Abstract base class that separates policy <em>creation</em> from policy <em>configuration</em>
	/// and satisfies the <see cref="IPolicyBuilder{TBuilder}"/> contract at the base-class level,
	/// so concrete subclasses do not need to re-declare it.
	/// </summary>
	/// <typeparam name="TPolicy">The PoliNorError policy type to create, e.g. <c>RetryPolicy</c>.</typeparam>
	/// <typeparam name="TConfigurator">
	/// The <see cref="PolicyConfigurator{TPolicy}"/> subclass responsible for configuring the policy.
	/// </typeparam>
	/// <typeparam name="TBuilder">
	/// The concrete builder type (self-referential CRTP parameter) that implements this base class.
	/// </typeparam>
	public abstract class PolicyBuilder<TPolicy, TConfigurator, TBuilder>
		: ISetConfigurator, IPolicyBuilder<TBuilder>
		where TPolicy      : Policy, IPolicyBase
		where TConfigurator: PolicyConfigurator<TPolicy>
		where TBuilder     : PolicyBuilder<TPolicy, TConfigurator, TBuilder>
	{
		private TConfigurator? _configurator;

		/// <summary>
		/// Creates the policy via <see cref="CreatePolicy"/>, applies the configurator, and returns
		/// the fully configured <see cref="IPolicyBase"/> instance.
		/// </summary>
		/// <returns>The configured policy.</returns>
		/// <exception cref="InvalidOperationException">
		/// Thrown when <c>SetConfigurator</c> has not been called before <c>Build()</c>, which
		/// typically means the builder was instantiated outside of the DI container.
		/// </exception>
		public IPolicyBase Build()
		{
			if (_configurator is null)
				throw new InvalidOperationException(
					$"{GetType().Name} requires a configurator. " +
					$"Ensure {GetType().Name} is registered via AddPoliNorError so that " +
					$"SetConfigurator is called before Build().");

			var result = CreatePolicy();
			_configurator.Configure(result);
			return result;
		}

		/// <summary>
		/// Creates and returns a new, unconfigured <typeparamref name="TPolicy"/> instance.
		/// Override this method to supply the policy's creation parameters (retry count, delay, etc.).
		/// </summary>
		/// <returns>A new, unconfigured <typeparamref name="TPolicy"/> instance.</returns>
		protected abstract TPolicy CreatePolicy();

		void ISetConfigurator.SetConfigurator(IServiceProvider serviceProvider)
		{
			_configurator = serviceProvider.GetRequiredService<TConfigurator>();
		}
	}
}
