using PoliNorError.Extensions.DependencyInjection;
using Samples.Builders;

namespace Samples
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
			await _somePolicy.HandleAsync(MightThrowAsync, false, token).ConfigureAwait(false);
			await _anotherPolicy.HandleAsync(MightThrowAsync, false, token).ConfigureAwait(false);
		}

		private async Task MightThrowAsync(CancellationToken token)
		{
			await Task.Delay(100); // Simulate async work
			throw new SomeException("Something went wrong in MightThrowAsync.");
		}

#pragma warning disable RCS1194 // Implement exception constructors
		public class SomeException : Exception
#pragma warning restore RCS1194 // Implement exception constructors
		{
			public SomeException(string message) : base(message) { }
		}
	}
}
