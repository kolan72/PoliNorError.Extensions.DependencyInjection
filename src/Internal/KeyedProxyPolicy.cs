namespace PoliNorError.Extensions.DependencyInjection
{
	/// <summary>
	/// Internal implementation of the non-generic <see cref="IPolicy"/> interface used for
	/// keyed service registrations created by
	/// <see cref="ServiceCollectionExtensions.AddKeyedPolicy"/>. Delegates all policy
	/// operations to the <see cref="IPolicyBase"/> instance produced by the configured
	/// builder or factory via the shared <see cref="ProxyPolicyBase"/> delegation.
	/// </summary>
	internal class KeyedProxyPolicy : ProxyPolicyBase
	{
		/// <summary>
		/// Initialises a new <see cref="KeyedProxyPolicy"/> that wraps the specified
		/// already-built <paramref name="innerPolicy"/>.
		/// </summary>
		/// <param name="innerPolicy">The <see cref="IPolicyBase"/> instance to delegate to. Must not be <see langword="null"/>; see <see cref="ProxyPolicyBase"/>.</param>
		public KeyedProxyPolicy(IPolicyBase innerPolicy)
			: base(innerPolicy)
		{
		}
	}
}
