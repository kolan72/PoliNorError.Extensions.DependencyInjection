namespace PoliNorError.Extensions.DependencyInjection
{
#pragma warning disable S1694 // An abstract class should have both abstract and concrete methods
	public abstract class PolicyConfigurator<TPolicy> where TPolicy : Policy, IPolicyBase
#pragma warning restore S1694 // An abstract class should have both abstract and concrete methods
	{
		public abstract void Configure(TPolicy policy);
	}

	internal interface ISetConfigurator
	{
		void SetConfigurator(IServiceProvider serviceProvider);
	}
}
