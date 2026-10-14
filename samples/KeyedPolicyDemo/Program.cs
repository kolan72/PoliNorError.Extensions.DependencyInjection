using KeyedPolicyDemo.Builders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using PoliNorError;
using PoliNorError.Extensions.DependencyInjection;
using System.Reflection;
using Shared;

namespace KeyedPolicyDemo
{
#pragma warning disable S1118 // Utility classes should not have public constructors
	internal class Program
#pragma warning restore S1118 // Utility classes should not have public constructors
	{
#pragma warning disable RCS1163 // Unused parameter
		static async Task Main(string[] args)
#pragma warning restore RCS1163 // Unused parameter
		{
#pragma warning disable S125 // Sections of code should not be commented out
			await using var serviceProvider = new ServiceCollection()
				.AddLogging(builder =>
				{
					builder.AddConsole();
					builder.SetMinimumLevel(LogLevel.Information);
				})
				.Configure<ConsoleFormatterOptions>(options =>
				{
					options.IncludeScopes = true;
					options.TimestampFormat = "HH:mm:ss ";
				})

				.AddTransient<Worker>()

				// Register RetryLoggingErrorProcessor as Transient
				.AddTransient(typeof(RetryLoggingErrorProcessor<>))

				// Register all IPolicyBuilder<T> implementations from the current assembly
				.AddPoliNorError(Assembly.GetExecutingAssembly())

				// Builder-based keyed policies: the same builder registered under two keys.
				// "retry-primary" follows the default (Transient) lifetime;
				// "retry-relaxed" is a Singleton, so the built policy is cached.
				.AddKeyedPolicy<SomePolicyBuilder>("retry-primary")
				.AddKeyedPolicy<SomePolicyBuilder>("retry-relaxed", ServiceLifetime.Singleton)

				// Lambda-based ad-hoc keyed policy: no builder class, the factory builds the policy directly.
				.AddKeyedPolicy("orders-policy",
					sp =>
					{
						var logger = sp.GetRequiredService<ILogger<Program>>();
						return new RetryPolicy(2)
							.WithPolicyName("OrdersPolicy")
							.WithErrorProcessor(new RetryLoggingErrorProcessor(logger))
							.WithWait(TimeSpan.FromSeconds(1))
							.AddPolicyResultHandler(pr =>
							{
								Log.PolicyFailedToHandleException(
									logger,
									pr.UnprocessedError,
									pr.PolicyName);
							});
					},
					ServiceLifetime.Singleton)

				.BuildServiceProvider();
#pragma warning restore S125 // Sections of code should not be commented out

			var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
			logger.LogInformation("Application starting...");

			var worker = serviceProvider.GetRequiredService<Worker>();
			await worker.DoWorkAsync(default);

			logger.LogInformation("Application finished.");
		}
	}
}
