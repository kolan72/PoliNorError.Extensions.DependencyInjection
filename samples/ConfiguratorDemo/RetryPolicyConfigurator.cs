using Microsoft.Extensions.Logging;
using PoliNorError;
using PoliNorError.Extensions.DependencyInjection;
using Shared;

namespace ConfiguratorDemo
{
	public class RetryPolicyConfigurator : PolicyConfigurator<RetryPolicy>
	{
		private readonly ILoggerFactory _loggerFactory;

		public RetryPolicyConfigurator(ILoggerFactory loggerFactory)
        {
			_loggerFactory = loggerFactory;
		}

        public override void Configure(RetryPolicy policy)
			=> policy.WithErrorProcessor(
					new RetryLoggingErrorProcessor(_loggerFactory.CreateLogger(policy.PolicyName)));
	}
}
