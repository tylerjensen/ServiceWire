using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceWireTests
{
    public interface INetTester
    {
        int Min(int a, int b);
        Dictionary<int, int> Range(int start, int count);
    }

    public class NetTester : INetTester
    {
        public int Min(int a, int b)
        {
            return Math.Min(a, b);
        }

        public Dictionary<int, int> Range(int start, int count)
        {
            return Enumerable.Range(start, count).ToDictionary(key => key, el => el);
        }
    }
}