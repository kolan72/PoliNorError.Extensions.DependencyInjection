using Microsoft.Extensions.Logging;
using PoliNorError;
using PoliNorError.Extensions.DependencyInjection;

namespace Shared
{
	public class RetryLoggingErrorProcessor<TBuilder> : ErrorProcessor where TBuilder : IPolicyBuilder<TBuilder>
	{
		private readonly ILogger<RetryLoggingErrorProcessor<TBuilder>> _logger;

		public RetryLoggingErrorProcessor(ILogger<RetryLoggingErrorProcessor<TBuilder>> logger)
		{
			_logger = logger;
		}

		public override void Execute(Exception error,
									ProcessingErrorInfo? catchBlockProcessErrorInfo = null,
									CancellationToken token = default)
		{
			_logger.LogError(error,
							"An error occurred while doing work on {Attempt} attempt.",
							catchBlockProcessErrorInfo.GetAttemptCount());
		}
	}
}
