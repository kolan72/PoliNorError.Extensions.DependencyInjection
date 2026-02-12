## 0.0.3

- Introduce abstract classes `PolicyConfigurator<TPolicy>` and `PolicyBuilder<TPolicy, TConfigurator>`.
- Update Microsoft nuget packages.
- Add *Advanced Usage* section to README.
- Move the `RetryLoggingErrorProcessor` to `Shared.csproj`.
- Add `ConfiguratorDemo.csproj` to `Samples.sln`.
- Create `Shared.csproj` in Samples and move `SomeException` into it.
- Move `Samples.csproj` into a new `Intro` folder and rename it to `Intro.csproj


## 0.0.3-preview

- Fix `ServiceCollectionExtensions` xml docs.
- Package 0.0.3-preview.
- Forward the `CancellationToken` parameter to `Task.Delay` in the `Samples.Worker` class.
- Use `ProcessingErrorInfo.GetAttemptCount()` instead of `GetRetryCount()` in `RetryLoggingErrorProcessor` and  NuGet.md.
- Edit NuGet.md.
- Edit README.md.
- Async-dispose `ServiceProvider` in Samples `Program.Main`.
- Simplifies `HandleAsync` usage in the Samples project by replacing explicit `ConfigureAwait(false)` calls with PoliNorError’s dedicated overloads.
- Make `RetryLoggingErrorProcessor` in Samples non-generic.
- Create CHANGELOG.md and `dev` branch.