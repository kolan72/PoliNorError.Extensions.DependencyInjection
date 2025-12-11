using ConfiguratorDemo.Builders;
using PoliNorError.Extensions.DependencyInjection;
using Shared;
using PoliNorError;

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
			await _somePolicy.HandleAsync(MightThrowAsync, token);
			await _anotherPolicy.HandleAsync(MightThrowAsync, token);
		}

		private async Task MightThrowAsync(CancellationToken token)
		{
			await Task.Delay(100, token); // Simulate async work
			throw new SomeException("Something went wrong in MightThrowAsync.");
		}
	}
}
