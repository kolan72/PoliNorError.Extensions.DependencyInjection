using KeyedPolicyDemo.Builders;
using PoliNorError;
using PoliNorError.Extensions.DependencyInjection;
using Shared;

namespace KeyedPolicyDemo
{
	public class Worker
	{
		private readonly IPolicyBase _somePolicy;
		private readonly IPolicyBase _anotherPolicy;
		private readonly IPolicyBase _thirdPolicy;

		public Worker(IPolicyRegistry policyRegistry)
		{
			_somePolicy = policyRegistry.GetPolicy<SomePolicyBuilder>();
			_anotherPolicy = policyRegistry.GetPolicy<AnotherPolicyBuilder>();
			_thirdPolicy = policyRegistry.GetPolicy<ThirdPolicyBuilder>();
		}

		public async Task DoWorkAsync(CancellationToken token)
		{
			await _somePolicy.HandleAsync(MightThrowAsync, token);
			await _anotherPolicy.HandleAsync(MightThrowAsync, token);
			await _thirdPolicy.HandleAsync(MightThrowAsync, token);
		}

		private async Task MightThrowAsync(CancellationToken token)
		{
			await Task.Delay(100, token); // Simulate async work
			throw new SomeException("Something went wrong in MightThrowAsync.");
		}
	}
}
