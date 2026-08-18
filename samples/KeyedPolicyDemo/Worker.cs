using Microsoft.Extensions.DependencyInjection;
using PoliNorError;
using PoliNorError.Extensions.DependencyInjection;
using Shared;

namespace KeyedPolicyDemo
{
	public class Worker
	{
		private readonly IServiceProvider _serviceProvider;

		public Worker(IServiceProvider serviceProvider)
		{
			_serviceProvider = serviceProvider;
		}

		public async Task DoWorkAsync(CancellationToken token)
		{
			// Resolve a keyed policy registered via AddKeyedPolicy (lambda factory)
			var lambdaPolicy = _serviceProvider.GetRequiredKeyedService<IPolicy>("lambda-retry");
			await lambdaPolicy.HandleAsync(MightThrowAsync, token);

			// Resolve a keyed policy registered via AddKeyedPolicy<TBuilder> (builder-based)
			var builderPolicy = _serviceProvider.GetRequiredKeyedService<IPolicy>("builder-retry");
			await builderPolicy.HandleAsync(MightThrowAsync, token);

			// Resolve another builder-based keyed policy under a different key
			var anotherBuilderPolicy = _serviceProvider.GetRequiredKeyedService<IPolicy>("another-retry");
			await anotherBuilderPolicy.HandleAsync(MightThrowAsync, token);
		}

		private async Task MightThrowAsync(CancellationToken token)
		{
			await Task.Delay(100, token); // Simulate async work
			throw new SomeException("Something went wrong in MightThrowAsync.");
		}
	}
}
