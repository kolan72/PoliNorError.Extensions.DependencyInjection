using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoliNorError.Extensions.DependencyInjection.Tests
{
	[TestFixture]
	public class ProxyPolicyTests
	{
		[Test]
		public void Should_Delegate_Handle_Action_To_InnerPolicy()
		{
			var mockPolicy = new TestPolicy();
			var factory = new TestPolicyBuilder(mockPolicy);
			var proxy = new ProxyPolicy<TestPolicyBuilder>(factory);

			proxy.Handle(() => { });

			Assert.That(mockPolicy.HandleActionCalled, Is.True);
		}

		[Test]
		public void Should_Delegate_Handle_Func_To_InnerPolicy()
		{
			var mockPolicy = new TestPolicy();
			var factory = new TestPolicyBuilder(mockPolicy);
			var proxy = new ProxyPolicy<TestPolicyBuilder>(factory);

			var result = proxy.Handle(() => 42);

			Assert.That(mockPolicy.HandleFuncCalled, Is.True);
		}

		[Test]
		public async Task Should_Delegate_HandleAsync_To_InnerPolicy()
		{
			var mockPolicy = new TestPolicy();
			var factory = new TestPolicyBuilder(mockPolicy);
			var proxy = new ProxyPolicy<TestPolicyBuilder>(factory);

			await proxy.HandleAsync(ct => Task.CompletedTask);

			Assert.That(mockPolicy.HandleAsyncActionCalled, Is.True);
		}

		[Test]
		public void Should_Return_InnerPolicy_Properties()
		{
			var mockPolicy = new TestPolicy("TestPolicy");
			var factory = new TestPolicyBuilder(mockPolicy);
			var proxy = new ProxyPolicy<TestPolicyBuilder>(factory);

			Assert.That(proxy.PolicyName, Is.EqualTo("TestPolicy"));
			Assert.That(proxy.PolicyProcessor, Is.SameAs(mockPolicy.PolicyProcessor));
		}
	}

}
