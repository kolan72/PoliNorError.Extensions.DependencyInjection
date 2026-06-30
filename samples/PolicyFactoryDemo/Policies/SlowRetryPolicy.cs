using Microsoft.Extensions.Logging;
using PoliNorError;
using PoliNorError.Extensions.DependencyInjection;
using Shared;

namespace PolicyFactoryDemo.Policies
{
	/// <summary>
	/// A slow retry policy with 3 retry attempts and a 3-second delay.
	/// Demonstrates creating a policy that inherits from PolicyBase with longer wait times.
	/// </summary>
	public class SlowRetryPolicy : PolicyBase
	{
		private readonly ILogger<SlowRetryPolicy> _logger;

		public SlowRetryPolicy(ILogger<SlowRetryPolicy> logger)
		{
			_logger = logger;
		}

		protected override IPolicyBase CreateInner()
		{
			return new RetryPolicy(3)
				.WithPolicyName("SlowRetryPolicy")
				.WithErrorProcessor(new RetryLoggingErrorProcessor(_logger))
				.WithWait(TimeSpan.FromSeconds(3))
				.AddPolicyResultHandler(pr =>
				{
					if (pr.IsFailed)
					{
						Log.PolicyFailedToHandleException(
							_logger,
							pr.UnprocessedError,
							pr.PolicyName);
					}
					else
					{
						_logger.LogInformation("SlowRetryPolicy succeeded after retries.");
					}
				});
		}
	}
}
