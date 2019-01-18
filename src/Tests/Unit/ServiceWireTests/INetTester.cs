using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceWireTests
{
    public interface INetTester
    {
        int Min(int a, int b);
        Dictionary<int, int> Range(int start, int count);
        Task<int> CalculateAsync(int a, int b);
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

        public Task<int> CalculateAsync(int a, int b)
        {
	        return Task.FromResult(a + b);
        }
    }
}