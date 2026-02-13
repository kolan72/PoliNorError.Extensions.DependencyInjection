using Microsoft.Extensions.Logging;

namespace Shared
{
	public static partial class Log
	{
		[LoggerMessage(
		EventId = 1,
		Level = LogLevel.Error,
		Message = "{PolicyName} failed to handle the exception.")]
		public static partial void PolicyFailedToHandleException(ILogger logger, Exception exception, string policyName);
	}
}
