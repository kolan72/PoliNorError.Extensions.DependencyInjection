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

				// AddKeyedPolicy (lambda factory): register a keyed policy using a factory delegate
				.AddKeyedPolicy("lambda-retry", _ => new RetryPolicy(1)
					.WithPolicyName("LambdaRetryPolicy")
					.WithWait(new TimeSpan(0, 0, 1)))

				// AddKeyedPolicy<TBuilder>: register a keyed policy using an existing IPolicyBuilder<T>
				// The builder type must already be registered (via AddPoliNorError above)
				.AddKeyedPolicy<SomePolicyBuilder>("builder-retry")

				// Same builder type under a different key, with Singleton lifetime
				.AddKeyedPolicy<AnotherPolicyBuilder>("another-retry", ServiceLifetime.Singleton)

				.BuildServiceProvider();

			var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
			logger.LogInformation("Application starting...");

			var worker = serviceProvider.GetRequiredService<Worker>();
			await worker.DoWorkAsync(default);

			logger.LogInformation("Application finished.");
		}
	}
}
