using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using NUnit.Framework;
using ServiceWire.Aspects;
using System.Diagnostics;

namespace ServiceWireTests
{
    [TestFixture]
    public class InterceptionTests
    {
        [Test]
        public void SimpleTest()
        {
            var preInvokeInfo = string.Empty;
            var postInvokeInfo = string.Empty;
            var exceptionHandlerInfo = string.Empty;
            var cc = new CrossCuttingConcerns();
            cc.PreInvoke = (instanceId, methodName, parameters) => 
            {
                preInvokeInfo = methodName + "_preInvokeInfo";
            };
            cc.PostInvoke = (instanceId, methodName, parameters) =>
            {
                postInvokeInfo = methodName + "_postInvokeInfo";
            };
            cc.ExceptionHandler = (instanceId, methodName, parameters, exception) =>
            {
                exceptionHandlerInfo = methodName + "_exceptionHandlerInfo";
                return false; //do not throw
            };
            var t = Interceptor.Intercept<ISimpleMath>(new SimpleMath(), cc);
            var a = t.Add(1, 2);
            Assert.IsNotNullOrEmpty(preInvokeInfo);
            Assert.IsNotNullOrEmpty(postInvokeInfo);
            Assert.IsNullOrEmpty(exceptionHandlerInfo);
            var b = t.Divide(4, 0);
            Assert.IsNotNullOrEmpty(preInvokeInfo);
            Assert.IsNotNullOrEmpty(postInvokeInfo);
            Assert.IsNotNullOrEmpty(exceptionHandlerInfo);
        }
    
        [Test]
        public void SimpleTimingTest()
        {
            var preInvokeInfo = string.Empty;
            var postInvokeInfo = string.Empty;
            var exceptionHandlerInfo = string.Empty;
            var cc = new CrossCuttingConcerns();
            cc.PreInvoke = (instanceId, methodName, parameters) =>
            {
                preInvokeInfo = methodName + "_preInvokeInfo";
            };
            cc.PostInvoke = (instanceId, methodName, parameters) =>
            {
                postInvokeInfo = methodName + "_postInvokeInfo";
            };
            cc.ExceptionHandler = (instanceId, methodName, parameters, exception) =>
            {
                exceptionHandlerInfo = methodName + "_exceptionHandlerInfo";
                return false; //do not throw
            };
            var t = Interceptor.Intercept<ISimpleMath>(new SimpleMath(), cc);
            var a = t.Add(1, 2);
            Assert.IsNotNullOrEmpty(preInvokeInfo);
            Assert.IsNotNullOrEmpty(postInvokeInfo);
            Assert.IsNullOrEmpty(exceptionHandlerInfo);

            var sw = Stopwatch.StartNew();
            var t2 = Interceptor.Intercept<ISimpleMath2>(new SimpleMath2(), cc);
            sw.Stop();
            var interceptCtorTicks = sw.ElapsedTicks;
            var interceptCtorTs = sw.Elapsed;
            sw.Restart();
            var c = t2.Add(2, 3);
            sw.Stop();
            var interceptAddTicks = sw.ElapsedTicks;
            var interceptAddTs = sw.Elapsed;

            sw.Restart();
            var t3 = Interceptor.Intercept<ISimpleMath2>(new SimpleMath2(), cc);
            sw.Stop();
            var interceptCtorTicks2 = sw.ElapsedTicks;
            var interceptCtorTs2 = sw.Elapsed;
            sw.Restart();
            var c2 = t3.Add(2, 3);
            sw.Stop();
            var interceptAddTicks2 = sw.ElapsedTicks;
            var interceptAddTs2 = sw.Elapsed;

            sw.Restart();
            var t4 = new SimpleMath2();
            sw.Stop();
            var plainCtorTicks = sw.ElapsedTicks;
            var plainCtorTs = sw.Elapsed;
            sw.Restart();
            var c3 = t4.Add(2, 3);
            sw.Stop();
            var plainAddTicks = sw.ElapsedTicks;
            var plainAddTs = sw.Elapsed;

            Assert.IsTrue(plainCtorTicks < interceptCtorTicks);
            Assert.IsTrue(plainAddTicks < interceptAddTicks);

            Assert.IsTrue(plainCtorTicks < interceptCtorTicks2);
            Assert.IsTrue(plainAddTicks < interceptAddTicks2);

            Assert.IsTrue(plainCtorTs < interceptCtorTs);
            Assert.IsTrue(plainAddTs < interceptAddTs);

            Assert.IsTrue(plainCtorTs < interceptCtorTs2);
            Assert.IsTrue(plainAddTs < interceptAddTs2);
        
        }
    }

    public interface ISimpleMath
    {
        int Add(int a, int b);
        int Divide(int a, int b);
    }

    public class SimpleMath : ISimpleMath
    {
        public int Add(int a, int b)
        {
            return a + b;
        }

        public int Divide(int a, int b)
        {
            return a / b;
        }
    }

    public interface ISimpleMath2
    {
        int Add(int a, int b);
        int Divide(int a, int b);
    }

    public class SimpleMath2 : ISimpleMath2
    {
        public int Add(int a, int b)
        {
            return a + b;
        }

        public int Divide(int a, int b)
        {
            return a / b;
        }
    }

}
