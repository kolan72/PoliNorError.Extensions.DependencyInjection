namespace PoliNorError.Extensions.DependencyInjection
{
	/// <summary>
	/// Non-generic contract consumed by services that use policies registered
	/// as keyed services via <c>services.AddKeyedPolicy(key, factory)</c> or
	/// <c>services.AddKeyedPolicy&lt;TBuilder&gt;(key)</c> on
	/// <see cref="ServiceCollectionExtensions"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Unlike <see cref="IPolicy{TBuilder}"/>, where the builder type parameter
	/// selects the policy at the type level, a keyed policy is selected at runtime
	/// by a key. Keyed services are always resolved through
	/// <c>IKeyedServiceProvider</c>; they cannot be injected straight from a
	/// constructor in plain DI. To keep consumer code type-safe, use
	/// <see cref="ServiceCollectionExtensions.GetKeyedPolicy(IServiceProvider, object)"/>
	/// and <see cref="ServiceCollectionExtensions.GetRequiredKeyedPolicy(IServiceProvider, object)"/>.
	/// </para>
	/// <para>
	/// Example:
	/// <code>
	/// const string HttpClientRetryKey = "http-retry";
	///
	/// services.AddKeyedPolicy(HttpClientRetryKey,
	/// 	sp =&gt; new RetryPolicy(3).WithPolicyName("HttpRetry"));
	///
	/// IPolicy policy = sp.GetRequiredKeyedPolicy(HttpClientRetryKey);
	/// await policy.HandleAsync(DoWorkAsync, token);
	/// </code>
	/// </para>
	/// </remarks>
	public interface IPolicy : IPolicyBase
	{
	}
}
