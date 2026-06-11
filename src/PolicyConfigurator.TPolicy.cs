namespace PoliNorError.Extensions.DependencyInjection
{
	// Abstract class (not interface) is intentional: preserves binary compatibility and
	// allows future concrete members (e.g., shared helper methods) to be added without
	// introducing a breaking change to consumers who inherit from this type.
#pragma warning disable S1694 // An abstract class should have both abstract and concrete methods — suppressed by design; see comment above.
	/// <summary>
	/// Abstract base class for encapsulating cross-cutting policy configuration logic
	/// (e.g., adding error processors, result handlers, or logging).
	/// Subclasses are discovered and registered automatically by
	/// <see cref="ServiceCollectionExtensions"/>.
	/// </summary>
	/// <typeparam name="TPolicy">The PoliNorError policy type this configurator targets.</typeparam>
	public abstract class PolicyConfigurator<TPolicy> where TPolicy : Policy, IPolicyBase
#pragma warning restore S1694
	{
		/// <summary>
		/// Applies configuration to the specified <typeparamref name="TPolicy"/> instance.
		/// Called by <see cref="PolicyBuilder{TPolicy,TConfigurator,TBuilder}.Build"/> after the
		/// policy has been created.
		/// </summary>
		/// <param name="policy">The policy instance to configure.</param>
		public abstract void Configure(TPolicy policy);
	}
}
