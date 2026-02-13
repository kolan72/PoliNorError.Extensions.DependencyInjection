using ConfiguratorDemo.Builders;
using PoliNorError;
using PoliNorError.Extensions.DependencyInjection;
using Shared;

namespace ConfiguratorDemo
{
	public class Worker
	{
		private readonly IPolicy<SomePolicyBuilder> _somePolicy;
		private readonly IPolicy<AnotherPolicyBuilder> _anotherPolicy;

		public Worker(IPolicy<SomePolicyBuilder> somePolicy, IPolicy<AnotherPolicyBuilder> anotherPolicy)
		{
			_somePolicy = somePolicy;
			_anotherPolicy = anotherPolicy;
		}

		public async Task DoWorkAsync(CancellationToken token)
		{
			var somePolicyResult = await _somePolicy.HandleAsync(MightThrowAsync, token).ConfigureAwait(false);
			if (somePolicyResult.IsFailed)
			{
				await _anotherPolicy.HandleAsync(MightThrowAsync, token).ConfigureAwait(false);
			}
		}

		private async Task MightThrowAsync(CancellationToken token)
		{
			await Task.Delay(100, token); // Simulate async work
			throw new SomeException("Something went wrong in MightThrowAsync.");
		}
	}
}
