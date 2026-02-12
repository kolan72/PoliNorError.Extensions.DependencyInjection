using Microsoft.Extensions.Logging;
using PoliNorError.Extensions.DependencyInjection;
using PoliNorError;
using Shared;

namespace Intro.Builders
{
	public class AnotherPolicyBuilder : IPolicyBuilder<AnotherPolicyBuilder>
	{
		private readonly ILogger<AnotherPolicyBuilder> _logger;

		public AnotherPolicyBuilder(ILogger<AnotherPolicyBuilder> logger)
		{
			_logger = logger;
		}

		public IPolicyBase Build()
		{
			return new RetryPolicy(2)
				.WithPolicyName("AnotherRetryPolicy")
				.WithErrorProcessor(new RetryLoggingErrorProcessor(_logger))
				.WithWait(new TimeSpan(0, 0, 1))
				.AddPolicyResultHandler(pr =>
				{
					Log.PolicyFailedToHandleException(
						_logger,
						pr.UnprocessedError,
						pr.PolicyName);
				});
		}
	}
}
