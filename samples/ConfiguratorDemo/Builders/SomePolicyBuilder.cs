using PoliNorError.Extensions.DependencyInjection;
using PoliNorError;

namespace ConfiguratorDemo.Builders
{
	public class SomePolicyBuilder : PolicyBuilder<RetryPolicy, RetryPolicyConfigurator>, IPolicyBuilder<SomePolicyBuilder>
	{
		protected override RetryPolicy CreatePolicy() =>
			new RetryPolicy(3, retryDelay: ConstantRetryDelay.Create(new TimeSpan(0, 0, 3)))
			.WithPolicyName("SomeRetryPolicy");
	}
}
