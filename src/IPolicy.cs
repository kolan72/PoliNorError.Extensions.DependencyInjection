namespace PoliNorError.Extensions.DependencyInjection
{
	/// <summary>
	/// Non-generic policy interface used for keyed service registration and resolution.
	/// Allows policies to be registered and retrieved by string key via the
	/// .NET 8+ keyed services API.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Resolve via <c>sp.GetRequiredKeyedService&lt;IPolicy&gt;("key")</c> or inject with
	/// <c>[FromKeyedServices("key")] IPolicy policy</c>.
	/// </para>
	/// <para>
	/// This interface extends <see cref="IPolicyBase"/> so consumers can call
	/// <see cref="IPolicyBase.Handle(Action, CancellationToken)"/>,
	/// <see cref="IPolicyBase.HandleAsync"/>, etc. directly.
	/// </para>
	/// </remarks>
	public interface IPolicy : IPolicyBase
	{
	}
}
