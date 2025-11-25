using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using PoliNorError.Extensions.DependencyInjection;
using System.Reflection;

namespace Intro
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

				// Register all IPolicyBuilder<T> implementations from the current assembly
				.AddPoliNorError(Assembly.GetExecutingAssembly())

				.BuildServiceProvider();

			var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
			logger.LogInformation("Application starting...");

			var worker = serviceProvider.GetRequiredService<Worker>();
			await worker.DoWorkAsync(default);

			logger.LogInformation("Application finished.");
		}
	}
}