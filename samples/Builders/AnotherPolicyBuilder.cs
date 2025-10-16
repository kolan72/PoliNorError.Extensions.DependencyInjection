using Microsoft.Extensions.Logging;
using PoliNorError.Extensions.DependencyInjection;
using PoliNorError;

namespace Samples.Builders
{
	public class AnotherPolicyBuilder : IPolicyBuilder<AnotherPolicyBuilder>
	{
		private readonly ILoggerFactory _logger;

		public AnotherPolicyBuilder(ILoggerFactory logger)
		{
			_logger = logger;
		}

		public IPolicyBase Build()
		{
			return new RetryPolicy(2)
				.WithErrorProcessor(new RetryLoggingErrorProcessor<AnotherPolicyBuilder>(_logger.CreateLogger<AnotherPolicyBuilder>()))
				.WithWait(new TimeSpan(0, 0, 1));
		}
	}
}
