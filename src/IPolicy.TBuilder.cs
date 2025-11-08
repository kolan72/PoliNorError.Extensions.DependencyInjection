namespace PoliNorError.Extensions.DependencyInjection
{
	/// <summary>
	/// Defines a contract for a policy that can be constructed using a builder pattern.
	/// </summary>
	/// <remarks>
	/// This interface is typically implemented by policy classes that need to expose
	/// a policy builder for configuration.
	/// </remarks>
	/// <typeparam name="TBuilder">The type of the policy builder associated with this policy, which must implement <see cref="IPolicyBuilder{TBuilder}"/>.</typeparam>
	public interface IPolicy<TBuilder> : IPolicyBase where TBuilder : IPolicyBuilder<TBuilder>;
}