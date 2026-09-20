using Microsoft.Extensions.Logging;
using PoliNorError;
using PoliNorError.Extensions.DependencyInjection;
using Shared;

namespace KeyedPolicyDemo.Builders
{
	public class ThirdPolicyBuilder : IPolicyBuilder<ThirdPolicyBuilder>
	{
		private readonly ILogger<ThirdPolicyBuilder> _logger;

		public ThirdPolicyBuilder(ILogger<ThirdPolicyBuilder> logger)
		{
			_logger = logger;
		}

		public IPolicyBase Build()
		{
			return new RetryPolicy(4)
				.WithPolicyName("ThirdPolicyBuilder")
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
