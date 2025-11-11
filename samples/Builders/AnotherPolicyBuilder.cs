using Microsoft.Extensions.Logging;
using PoliNorError.Extensions.DependencyInjection;
using PoliNorError;

namespace Samples.Builders
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
				.WithErrorProcessor(new RetryLoggingErrorProcessor(_logger))
				.WithWait(new TimeSpan(0, 0, 1));
		}
	}
}
