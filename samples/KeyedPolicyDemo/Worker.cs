using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PoliNorError;
using PoliNorError.Extensions.DependencyInjection;
using Shared;

namespace KeyedPolicyDemo
{
	public class Worker
	{
		private readonly IPolicy _primaryPolicy;
		private readonly ILogger<Worker> _logger;
		private readonly IServiceProvider _serviceProvider;

		// Constructor attribute injection: [FromKeyedServices("key")] resolves the keyed IPolicy.
		public Worker([FromKeyedServices("retry-primary")] IPolicy primaryPolicy,
					  ILogger<Worker> logger,
					  IServiceProvider serviceProvider)
		{
			_primaryPolicy = primaryPolicy;
			_logger = logger;
			_serviceProvider = serviceProvider;
		}

		public async Task DoWorkAsync(CancellationToken token)
		{
			// 1. Policy injected via [FromKeyedServices].
			_logger.LogInformation("Handling with the 'retry-primary' keyed policy...");
			var primaryResult = await _primaryPolicy.HandleAsync(MightThrowAsync, token);
			_logger.LogInformation("'retry-primary' result: {Outcome}", primaryResult.IsSuccess ? "Success" : "Failed");

			// 2. Runtime resolution of another keyed policy.
			var ordersPolicy = _serviceProvider.GetRequiredKeyedService<IPolicy>("orders-policy");
			_logger.LogInformation("Handling with the runtime-resolved 'orders-policy'...");
			var ordersResult = await ordersPolicy.HandleAsync(MightThrowAsync, token);
			_logger.LogInformation("'orders-policy' result: {Outcome}", ordersResult.IsSuccess ? "Success" : "Failed");

			// 3. Runtime resolution of the same builder under a different (singleton) key.
			var relaxedPolicy = _serviceProvider.GetRequiredKeyedService<IPolicy>("retry-relaxed");
			_logger.LogInformation("Handling with the 'retry-relaxed' keyed policy...");
			var relaxedResult = await relaxedPolicy.HandleAsync(MightThrowAsync, token);
			_logger.LogInformation("'retry-relaxed' result: {Outcome}", relaxedResult.IsSuccess ? "Success" : "Failed");
		}

		private async Task MightThrowAsync(CancellationToken token)
		{
			await Task.Delay(100, token); // Simulate async work
			throw new SomeException("Something went wrong in MightThrowAsync.");
		}
	}
}
