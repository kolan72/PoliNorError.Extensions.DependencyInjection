using Microsoft.Extensions.DependencyInjection;

namespace PoliNorError.Extensions.DependencyInjection
{
	/// <summary>
	/// Factory implementation for creating and initializing policy instances from the service provider.
	/// </summary>
	public class PolicyFactory : IPolicyFactory
	{
		private readonly IServiceProvider _serviceProvider;

		/// <summary>
		/// Initializes a new instance of the <see cref="PolicyFactory"/> class.
		/// </summary>
		/// <param name="serviceProvider">The service provider used to resolve policy instances.</param>
		public PolicyFactory(IServiceProvider serviceProvider)
		{
			_serviceProvider = serviceProvider;
		}

		/// <inheritdoc />
		public IPolicyBase CreatePolicy<TPolicy>() where TPolicy : PolicyBase
		{
			var policy = _serviceProvider.GetRequiredService<TPolicy>();
			policy.Create();
			return policy;
		}
	}
}
