using Microsoft.Extensions.Logging;
using PoliNorError;
using PoliNorError.Extensions.DependencyInjection;
using Shared;

namespace PolicyFactoryDemo.Policies
{
	/// <summary>
	/// A quick retry policy with 2 retry attempts and a 1-second delay.
	/// Demonstrates creating a policy that inherits from PolicyBase.
	/// </summary>
	public class QuickRetryPolicy : PolicyBase
	{
		private readonly ILogger<QuickRetryPolicy> _logger;

		public QuickRetryPolicy(ILogger<QuickRetryPolicy> logger)
		{
			_logger = logger;
		}

		protected override IPolicyBase CreateInner()
		{
			return new RetryPolicy(2)
				.WithPolicyName("QuickRetryPolicy")
				.WithErrorProcessor(new RetryLoggingErrorProcessor(_logger))
				.WithWait(TimeSpan.FromSeconds(1))
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
						_logger.LogInformation("QuickRetryPolicy succeeded after retries.");
					}
				});
		}
	}
}
