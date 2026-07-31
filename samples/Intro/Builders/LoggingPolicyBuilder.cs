using PoliNorError.Extensions.DependencyInjection;
using PoliNorError;
using Shared;

namespace Intro.Builders
{
	public class LoggingPolicyBuilder : IPolicyBuilder<LoggingPolicyBuilder>
	{
		private readonly RetryLoggingErrorProcessor<LoggingPolicyBuilder> _errorProcessor;

		public LoggingPolicyBuilder(RetryLoggingErrorProcessor<LoggingPolicyBuilder> errorProcessor)
		{
			_errorProcessor = errorProcessor;
		}

		public IPolicyBase Build()
		{
			return new RetryPolicy(4)
					.WithPolicyName("LoggingRetryPolicy")
					.WithErrorProcessor(_errorProcessor)
					.WithWait(new TimeSpan(0, 0, 2));
		}
	}
}
