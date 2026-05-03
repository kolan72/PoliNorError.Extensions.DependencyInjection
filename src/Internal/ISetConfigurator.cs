namespace PoliNorError.Extensions.DependencyInjection
{
	/// <summary>
	/// Internal seam that allows <see cref="ProxyPolicy{TBuilder}"/> to inject a
	/// <see cref="PolicyConfigurator{TPolicy}"/> into a
	/// <see cref="PolicyBuilder{TPolicy,TConfigurator,TBuilder}"/> before
	/// <see cref="IPolicyBuilder{TBuilder}.Build"/> is called.
	/// </summary>
	internal interface ISetConfigurator
	{
		/// <summary>
		/// Resolves the required <see cref="PolicyConfigurator{TPolicy}"/> from the DI container
		/// and stores it for use during <see cref="IPolicyBuilder{TBuilder}.Build"/>.
		/// </summary>
		/// <param name="serviceProvider">The DI service provider.</param>
		void SetConfigurator(IServiceProvider serviceProvider);
	}
}
