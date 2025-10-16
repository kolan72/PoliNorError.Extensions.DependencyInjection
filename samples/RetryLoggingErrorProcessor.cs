using Microsoft.Extensions.Logging;
using PoliNorError;

namespace Samples
{
	public class RetryLoggingErrorProcessor<T> : ErrorProcessor
	{
		private readonly ILogger<T> _logger;

		public RetryLoggingErrorProcessor(ILogger<T> logger)
		{
			_logger = logger;
		}

		public override void Execute(Exception error,
									ProcessingErrorInfo? catchBlockProcessErrorInfo = null,
									CancellationToken token = default)
		{
			_logger.LogError(error,
							"An error occurred while doing work on {Attempt} attempt.",
							catchBlockProcessErrorInfo.GetRetryCount() + 1);
		}
	}
}
