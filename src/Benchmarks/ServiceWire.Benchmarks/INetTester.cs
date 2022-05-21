using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceWire.Benchmarks
{
    public interface INetTester
    {
        int Min(int a, int b);
        Dictionary<int, int> Range(int start, int count);
        TestResponse Get(Guid id, string label, double weight, out int quantity);
        Task<int> CalculateAsync(int a, int b);
    }

    [Serializable]
    public struct TestResponse
    {
        public Guid Id { get; set; }
        public string Label { get; set; }
        public long Quantity { get; set; }
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

        public TestResponse Get(Guid id, string label, double weight, out int quantity)
        {
            quantity = 44;
            return new TestResponse { Id = id, Label = "MyLabel", Quantity = quantity };
        }
    }
}