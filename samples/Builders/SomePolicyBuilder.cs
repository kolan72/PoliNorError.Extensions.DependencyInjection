using PoliNorError.Extensions.DependencyInjection;
using PoliNorError;
using Microsoft.Extensions.Logging;

namespace Samples.Builders
{
	public class SomePolicyBuilder : IPolicyBuilder<SomePolicyBuilder>
	{
		private readonly ILoggerFactory _logger;

		public SomePolicyBuilder(ILoggerFactory logger)
		{
			_logger = logger;
		}

		public IPolicyBase Build()
		{
			return new RetryPolicy(3)
					.WithErrorProcessor(new RetryLoggingErrorProcessor<SomePolicyBuilder>(_logger.CreateLogger<SomePolicyBuilder>()))
					.WithWait(new TimeSpan(0, 0, 3));
		}
	}
}
