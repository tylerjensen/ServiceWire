using DuoVia.Net.Aspects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace DuoVia.ConsoleTests
{
    internal static class InterceptionPerfTester
    {
        public static void RunPerfTest()
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

            Console.WriteLine("Ticks plain vs intercepted first ctr:  {0}, {1}", plainCtorTicks, interceptCtorTicks);
            Console.WriteLine("Ticks plain vs intercepted first add:  {0}, {1}", plainAddTicks, interceptAddTicks);
            Console.WriteLine("Ticks plain vs intercepted second ctr: {0}, {1}", plainCtorTicks, interceptCtorTicks2);
            Console.WriteLine("Ticks plain vs intercepted second add: {0}, {1}", plainAddTicks, interceptAddTicks2);

            Console.WriteLine("TS plain vs intercepted first ctr:  {0}, {1}", plainCtorTs, interceptCtorTs);
            Console.WriteLine("TS plain vs intercepted first add:  {0}, {1}", plainAddTs, interceptAddTs);

            Console.WriteLine("TS plain vs intercepted second ctr: {0}, {1}", plainCtorTs, interceptCtorTs2);
            Console.WriteLine("TS plain vs intercepted second add: {0}, {1}", plainAddTs, interceptAddTs2);

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
