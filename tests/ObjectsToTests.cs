using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoliNorError.Extensions.DependencyInjection.Tests
{
	// Test doubles
	public class TestPolicyBuilder : IPolicyBuilder<TestPolicyBuilder>
	{
		private readonly IPolicyBase _policy;

		public TestPolicyBuilder(IPolicyBase policy)
		{
			_policy = policy;
		}

		public IPolicyBase Build() => _policy;
	}

	public class TestPolicy : IPolicyBase
	{
		public bool HandleActionCalled { get; private set; }
		public bool HandleFuncCalled { get; private set; }
		public bool HandleAsyncActionCalled { get; private set; }
		public bool HandleAsyncFuncCalled { get; private set; }

		public string PolicyName { get; }

		public IPolicyProcessor PolicyProcessor { get; } = new TestPolicyProcessor();

        public TestPolicy(string policyName)
        {
			PolicyName = policyName;
		}

        public TestPolicy(): this(nameof(TestPolicy)){}

		public PolicyResult Handle(Action action, CancellationToken token = default)
		{
			HandleActionCalled = true;
			action();
			return new PolicyResult();
		}

		public PolicyResult<T> Handle<T>(Func<T> func, CancellationToken token = default)
		{
			HandleFuncCalled = true;
			return new PolicyResult<T>();
		}

		public Task<PolicyResult> HandleAsync(Func<CancellationToken, Task> func, bool configureAwait = false, CancellationToken token = default)
		{
			HandleAsyncActionCalled = true;
			return Task.FromResult(new PolicyResult());
		}

		public Task<PolicyResult<T>> HandleAsync<T>(Func<CancellationToken, Task<T>> func, bool configureAwait = false, CancellationToken token = default)
		{
			HandleAsyncFuncCalled = true;
			return Task.FromResult(new PolicyResult<T>());
		}
	}

	public class TestPolicyProcessor : IPolicyProcessor
	{
		// Implement interface members as needed for testing
		public PolicyProcessor.ExceptionFilter ErrorFilter => throw new NotImplementedException();

		public void AddErrorProcessor(IErrorProcessor newErrorProcessor) => throw new NotImplementedException();
		public IEnumerator<IErrorProcessor> GetEnumerator() => throw new NotImplementedException();
		IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
	}

	// Test Policy Builders for testing
	public class TestPolicyBuilderA : IPolicyBuilder<TestPolicyBuilderA>
	{
		public IPolicyBase Build()
		{
			return new TestPolicy("TestPolicyA");
		}
	}

	public class TestPolicyBuilderB : IPolicyBuilder<TestPolicyBuilderB>
	{
		public IPolicyBase Build()
		{
			return new TestPolicy("TestPolicyB");
		}
	}

	// Multiple Test Policy Builder Implementations
	public class TestPolicyBuilder1 : IPolicyBuilder<TestPolicyBuilder1>
	{
		public IPolicyBase Build() => new TestPolicy1();
	}

	public class TestPolicyBuilder2 : IPolicyBuilder<TestPolicyBuilder2>
	{
		public IPolicyBase Build() => new TestPolicy2();
	}

	public class TestPolicyBuilder3 : IPolicyBuilder<TestPolicyBuilder3>
	{
		public IPolicyBase Build() => new TestPolicy3();
	}

	// Abstract class that should NOT be registered
	public abstract class AbstractPolicyBuilder : IPolicyBuilder<AbstractPolicyBuilder>
	{
		public abstract IPolicyBase Build();
	}

	// Generic class that should NOT be registered
	public class GenericPolicyBuilder<T> : IPolicyBuilder<GenericPolicyBuilder<T>>
	{
		public IPolicyBase Build() => new TestPolicy1();
	}

	// Corresponding Policy Implementations
	public class TestPolicy1 : IPolicyBase
	{
		public string PolicyName => "TestPolicy1";
		public IPolicyProcessor PolicyProcessor { get; } = new TestPolicyProcessor();

		public PolicyResult Handle(Action action, CancellationToken token = default)
		{
			action();
			return new PolicyResult();
		}

		public PolicyResult<T> Handle<T>(Func<T> func, CancellationToken token = default)
		{
			return new PolicyResult<T>();
		}

		public Task<PolicyResult> HandleAsync(Func<CancellationToken, Task> func, bool configureAwait = false, CancellationToken token = default)
		{
			return Task.FromResult(new PolicyResult());
		}

		public Task<PolicyResult<T>> HandleAsync<T>(Func<CancellationToken, Task<T>> func, bool configureAwait = false, CancellationToken token = default)
		{
			return Task.FromResult(new PolicyResult<T>());
		}
	}

	public class TestPolicy2 : IPolicyBase
	{
		public string PolicyName => "TestPolicy2";
		public IPolicyProcessor PolicyProcessor { get; } = new TestPolicyProcessor();

		public PolicyResult Handle(Action action, CancellationToken token = default)
		{
			action();
			return new PolicyResult();
		}

		public PolicyResult<T> Handle<T>(Func<T> func, CancellationToken token = default)
		{
			return new PolicyResult<T>();
		}

		public Task<PolicyResult> HandleAsync(Func<CancellationToken, Task> func, bool configureAwait = false, CancellationToken token = default)
		{
			return Task.FromResult(new PolicyResult());
		}

		public Task<PolicyResult<T>> HandleAsync<T>(Func<CancellationToken, Task<T>> func, bool configureAwait = false, CancellationToken token = default)
		{
			return Task.FromResult(new PolicyResult<T>());
		}
	}

	public class TestPolicy3 : IPolicyBase
	{
		public string PolicyName => "TestPolicy3";
		public IPolicyProcessor PolicyProcessor { get; } = new TestPolicyProcessor();

		public PolicyResult Handle(Action action, CancellationToken token = default)
		{
			action();
			return new PolicyResult();
		}

		public PolicyResult<T> Handle<T>(Func<T> func, CancellationToken token = default)
		{
			return new PolicyResult<T>();
		}

		public Task<PolicyResult> HandleAsync(Func<CancellationToken, Task> func, bool configureAwait = false, CancellationToken token = default)
		{
			return Task.FromResult(new PolicyResult());
		}

		public Task<PolicyResult<T>> HandleAsync<T>(Func<CancellationToken, Task<T>> func, bool configureAwait = false, CancellationToken token = default)
		{
			return Task.FromResult(new PolicyResult<T>());
		}
	}

	internal class TestConfigurator : PolicyConfigurator<RetryPolicy>
	{
		public override void Configure(RetryPolicy policy) { }
	}

	internal class AnotherConfigurator : PolicyConfigurator<RetryPolicy>
	{
		public override void Configure(RetryPolicy policy) { }
	}

	internal class MultipleInterfaceConfigurator : PolicyConfigurator<RetryPolicy>
	{
		public override void Configure(RetryPolicy policy) { }
	}

	internal abstract class AbstractConfigurator : PolicyConfigurator<RetryPolicy>
	{
		public abstract void Configure2(RetryPolicy policy);
	}

#pragma warning disable S2326 // Unused type parameters should be removed
	internal class GenericConfigurator<T> : PolicyConfigurator<RetryPolicy> where T : Policy
#pragma warning restore S2326 // Unused type parameters should be removed
	{
		public override void Configure(RetryPolicy policy) { }
	}

	internal class NotAConfigurator
	{
#pragma warning disable S1186 // Methods should not be empty
		public void DoSomething() { }
#pragma warning restore S1186 // Methods should not be empty
	}

	public class FakeConfigurator : PolicyConfigurator<RetryPolicy>
	{
		public override void Configure(RetryPolicy policy) { policy.WithErrorProcessorOf((_) => { }); }
	}


	internal class SomePolicyBuilder : PolicyBuilder<RetryPolicy, FakeConfigurator>, IPolicyBuilder<SomePolicyBuilder>
	{
		protected override RetryPolicy CreatePolicy()
		{
			var policy = new RetryPolicy(1);
			policy.WithPolicyName("SomePolicy");
			return policy;
		}
		
	}
}
