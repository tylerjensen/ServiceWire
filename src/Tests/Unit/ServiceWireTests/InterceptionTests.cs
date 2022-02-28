using ServiceWire.Aspects;
using System;
using System.Diagnostics;
using Xunit;

namespace ServiceWireTests
{
    public class InterceptionTests
    {
        [Fact]
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
            Assert.False(string.IsNullOrEmpty(preInvokeInfo));
            Assert.False(string.IsNullOrEmpty(postInvokeInfo));
            Assert.True(string.IsNullOrEmpty(exceptionHandlerInfo));
            var b = t.Divide(4, 0);
            Assert.False(string.IsNullOrEmpty(preInvokeInfo));
            Assert.False(string.IsNullOrEmpty(postInvokeInfo));
            Assert.False(string.IsNullOrEmpty(exceptionHandlerInfo));
        }

        [Fact]
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
            Assert.False(string.IsNullOrEmpty(preInvokeInfo));
            Assert.False(string.IsNullOrEmpty(postInvokeInfo));
            Assert.True(string.IsNullOrEmpty(exceptionHandlerInfo));

            var sw = Stopwatch.StartNew();
            var t2 = Interceptor.Intercept<ISimpleMath2>(new SimpleMath2(), cc);
            sw.Stop();
            var interceptCtorTicks = sw.ElapsedTicks;
            var interceptCtorTs = sw.Elapsed;
            sw.Reset();
            var c = t2.Add(2, 3);
            sw.Stop();
            var interceptAddTicks = sw.ElapsedTicks;
            var interceptAddTs = sw.Elapsed;

            sw.Reset();
            var t3 = Interceptor.Intercept<ISimpleMath2>(new SimpleMath2(), cc);
            sw.Stop();
            var interceptCtorTicks2 = sw.ElapsedTicks;
            var interceptCtorTs2 = sw.Elapsed;
            sw.Reset();
            var c2 = t3.Add(2, 3);
            sw.Stop();
            var interceptAddTicks2 = sw.ElapsedTicks;
            var interceptAddTs2 = sw.Elapsed;

            sw.Reset();
            var t4 = new SimpleMath2();
            sw.Stop();
            var plainCtorTicks = sw.ElapsedTicks;
            var plainCtorTs = sw.Elapsed;
            sw.Reset();
            var c3 = t4.Add(2, 3);
            sw.Stop();
            var plainAddTicks = sw.ElapsedTicks;
            var plainAddTs = sw.Elapsed;

            Assert.True(plainCtorTicks <= interceptCtorTicks);
            Assert.True(plainAddTicks <= interceptAddTicks);

            Assert.True(plainCtorTicks <= interceptCtorTicks2);
            Assert.True(plainAddTicks <= interceptAddTicks2);

            Assert.True(plainCtorTs <= interceptCtorTs);
            Assert.True(plainAddTs <= interceptAddTs);

            Assert.True(plainCtorTs <= interceptCtorTs2);
            Assert.True(plainAddTs <= interceptAddTs2);
        }

        [Fact]
        public void SimpleErrorTestNoThrow()
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
            var t = Interceptor.Intercept<ISimpleError>(new SimpleError(), cc);
            t.RaiseAnError();
            Assert.False(string.IsNullOrEmpty(preInvokeInfo));
            Assert.False(string.IsNullOrEmpty(postInvokeInfo));
            Assert.False(string.IsNullOrEmpty(exceptionHandlerInfo));
        }

        [Fact]
        public void SimpleErrorTestThrowOriginalError()
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
                return true; //do throw
            };
            var t = Interceptor.Intercept<ISimpleError>(new SimpleError(), cc);
            try
            {
                t.RaiseAnError();
            }
            catch (Exception ex)
            {
                var isCorrectError = ex.Message.Contains("eliberate");
                Assert.True(isCorrectError);
            }
            Assert.False(string.IsNullOrEmpty(preInvokeInfo));
            Assert.False(string.IsNullOrEmpty(postInvokeInfo));
            Assert.False(string.IsNullOrEmpty(exceptionHandlerInfo));
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

    public interface ISimpleError
    {
        void RaiseAnError();
    }

    public class SimpleError : ISimpleError
    {
        public void RaiseAnError()
        {
            throw new Exception("Deliberate Exception for Test");
        }
    }

}
