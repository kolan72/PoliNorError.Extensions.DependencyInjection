using Microsoft.Extensions.Logging;
using PolicyFactoryDemo.Policies;
using PoliNorError;
using PoliNorError.Extensions.DependencyInjection;
using Shared;

namespace PolicyFactoryDemo
{
	public class Worker
	{
		private readonly IPolicyFactory _policyFactory;
		private readonly ILogger<Worker> _logger;

		public Worker(IPolicyFactory policyFactory, ILogger<Worker> logger)
		{
			_policyFactory = policyFactory;
			_logger = logger;
		}

		public async Task DoWorkAsync(CancellationToken token)
		{
			_logger.LogInformation("Demonstrating IPolicyFactory usage...");

			// Create policies dynamically using IPolicyFactory
			var quickRetryPolicy = _policyFactory.CreatePolicy<QuickRetryPolicy>();
			var slowRetryPolicy = _policyFactory.CreatePolicy<SlowRetryPolicy>();
			var fallbackPolicy = _policyFactory.CreatePolicy<SimpleFallbackPolicy>();

			_logger.LogInformation("Created policies: {QuickRetry}, {SlowRetry}, {Fallback}", 
				quickRetryPolicy.PolicyName, 
				slowRetryPolicy.PolicyName, 
				fallbackPolicy.PolicyName);

			// Use the quick retry policy
			_logger.LogInformation("Testing with QuickRetryPolicy...");
			var result1 = await quickRetryPolicy.HandleAsync(SimulateQuickFailureAsync, token).ConfigureAwait(false);
			_logger.LogInformation("QuickRetryPolicy result: {IsFailed}", result1.IsFailed);

			// Use the slow retry policy
			_logger.LogInformation("Testing with SlowRetryPolicy...");
			var result2 = await slowRetryPolicy.HandleAsync(SimulateSlowFailureAsync, token).ConfigureAwait(false);
			_logger.LogInformation("SlowRetryPolicy result: {IsFailed}", result2.IsFailed);

			// Use the fallback policy
			_logger.LogInformation("Testing with FallbackPolicy...");
			var result3 = await fallbackPolicy.HandleAsync(SimulatePersistentFailureAsync, token).ConfigureAwait(false);
			_logger.LogInformation("FallbackPolicy result: {IsFailed}", result3.IsFailed);

			_logger.LogInformation("All policy factory demonstrations completed.");
		}

		private async Task SimulateQuickFailureAsync(CancellationToken token)
		{
			await Task.Delay(50, token);
			_logger.LogWarning("SimulateQuickFailureAsync throwing exception...");
			throw new SomeException("Quick failure occurred.");
		}

		private async Task SimulateSlowFailureAsync(CancellationToken token)
		{
			await Task.Delay(200, token);
			_logger.LogWarning("SimulateSlowFailureAsync throwing exception...");
			throw new SomeException("Slow failure occurred.");
		}

		private async Task SimulatePersistentFailureAsync(CancellationToken token)
		{
			await Task.Delay(100, token);
			_logger.LogWarning("SimulatePersistentFailureAsync throwing exception...");
			throw new SomeException("Persistent failure that will be handled by fallback.");
		}
	}
}
