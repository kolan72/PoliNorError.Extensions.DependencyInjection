namespace PoliNorError.Extensions.DependencyInjection
{
	/// <summary>
	/// Factory interface for creating policy instances.
	/// </summary>
	public interface IPolicyFactory
	{
		/// <summary>
		/// Creates and initializes a policy of the specified type.
		/// </summary>
		/// <typeparam name="TPolicy">The type of policy to create, must inherit from <see cref="PolicyBase"/>.</typeparam>
		/// <returns>The created and initialized policy instance.</returns>
		IPolicyBase CreatePolicy<TPolicy>() where TPolicy : PolicyBase;
	}
}
