namespace PoliNorError.Extensions.DependencyInjection
{
	/// <summary>
	/// Defines a contract for building policies.
	/// </summary>
	/// <typeparam name="TBuilder">The type that implements this interface, which allows for method chaining in the builder pattern.</typeparam>
	public interface IPolicyBuilder<TBuilder> where TBuilder : IPolicyBuilder<TBuilder>
	{
		/// <summary>
		/// Creates and returns the built policy instance.
		/// </summary>
		/// <returns>A policy object that implements <see cref="IPolicyBase"/>.</returns>
		IPolicyBase Build();
	}
}
