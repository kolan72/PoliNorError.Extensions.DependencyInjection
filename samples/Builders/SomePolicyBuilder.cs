using PoliNorError.Extensions.DependencyInjection;
using PoliNorError;
using Microsoft.Extensions.Logging;

namespace Samples.Builders
{
	public class SomePolicyBuilder : IPolicyBuilder<SomePolicyBuilder>
	{
		private readonly ILogger<SomePolicyBuilder> _logger;

		public SomePolicyBuilder(ILogger<SomePolicyBuilder> logger)
		{
			_logger = logger;
		}

		public IPolicyBase Build()
		{
			return new RetryPolicy(3)
					.WithErrorProcessor(new RetryLoggingErrorProcessor(_logger))
					.WithWait(new TimeSpan(0, 0, 3));
		}
	}
}
