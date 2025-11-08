using Microsoft.Extensions.DependencyInjection;

namespace PoliNorError.Extensions.DependencyInjection
{
	public abstract class PolicyBuilder<TPolicy, TConfigurator> : ISetConfigurator where TPolicy : Policy, IPolicyBase where TConfigurator : PolicyConfigurator<TPolicy>
	{
		private TConfigurator? _configurator;

		public IPolicyBase Build()
		{
			var result = CreatePolicy();
			_configurator!.Configure(result);
			return result;
		}

		protected abstract TPolicy CreatePolicy();

		void ISetConfigurator.SetConfigurator(IServiceProvider serviceProvider)
		{
			_configurator = serviceProvider.GetRequiredService<TConfigurator>();
		}
	}
}
