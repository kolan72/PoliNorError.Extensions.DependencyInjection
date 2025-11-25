using Microsoft.Extensions.Logging;
using PoliNorError;

namespace Intro
{
	public class RetryLoggingErrorProcessor : ErrorProcessor
	{
		private readonly ILogger _logger;

		public RetryLoggingErrorProcessor(ILogger logger)
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
