using Microsoft.Extensions.Logging;
using PoliNorError;
using PoliNorError.Extensions.DependencyInjection;

namespace PolicyFactoryDemo.Policies
{
	/// <summary>
	/// A fallback policy that handles exceptions with a fallback value.
	/// Demonstrates creating a fallback policy using PolicyBase.
	/// </summary>
	public class SimpleFallbackPolicy : PolicyBase
	{
		private readonly ILogger<SimpleFallbackPolicy> _logger;

		public SimpleFallbackPolicy(ILogger<SimpleFallbackPolicy> logger)
		{
			_logger = logger;
		}

		protected override IPolicyBase CreateInner()
		{
			return new FallbackPolicy()
				.WithPolicyName("FallbackPolicy")
				.WithFallbackAction((token) =>
				{
					_logger.LogInformation("Executing fallback action...");
				})
				.AddPolicyResultHandler(pr =>
				{
					if (pr.IsFailed)
					{
						_logger.LogWarning("FallbackPolicy failed to handle exception: {Exception}", pr.UnprocessedError?.Message);
					}
					else
					{
						_logger.LogInformation("FallbackPolicy successfully handled exception with fallback.");
					}
				});
		}
	}
}
