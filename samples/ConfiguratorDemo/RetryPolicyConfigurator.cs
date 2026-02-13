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
		{
			var logger = _loggerFactory.CreateLogger(policy.PolicyName);

			policy.WithErrorProcessor(
							new RetryLoggingErrorProcessor(logger))
				.AddPolicyResultHandler(pr =>
				{
					Log.PolicyFailedToHandleException(
						logger,
						pr.UnprocessedError,
						pr.PolicyName);
				});
		}
	}
}
