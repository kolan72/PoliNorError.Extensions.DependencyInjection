using PoliNorError.Extensions.DependencyInjection;
using PoliNorError;

namespace ConfiguratorDemo.Builders
{
	public class AnotherPolicyBuilder : PolicyBuilder<RetryPolicy, RetryPolicyConfigurator>, IPolicyBuilder<AnotherPolicyBuilder>
	{
		protected override RetryPolicy CreatePolicy() =>
			new RetryPolicy(2, retryDelay: ConstantRetryDelay.Create(new TimeSpan(0, 0, 1)))
			.WithPolicyName("AnotherRetryPolicy");
	}
}
