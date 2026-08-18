using System;
using System.Threading.Tasks;
using ServiceWire.NamedPipes;
using Xunit;

namespace ServiceWireTests
{
    public interface IDispatchTester
    {
        int Add(int a, int b);
        void VoidMethod();
        string Concat(string a, int b, Guid c);
        int OutAndRef(int a, out int doubled, ref int accumulated);
        void ThrowSync(string message);
        Task ThrowAsync(string message);
        Task<int> AddAsync(int a, int b);
        Task PlainTaskAsync();
    }

    public class DispatchTester : IDispatchTester
    {
        public int Add(int a, int b) => a + b;
        public void VoidMethod() { }
        public string Concat(string a, int b, Guid c) => a + b + c.ToString("N");

        public int OutAndRef(int a, out int doubled, ref int accumulated)
        {
            doubled = a * 2;
            accumulated += a;
            return a;
        }

        public void ThrowSync(string message) => throw new InvalidOperationException(message);

        public Task ThrowAsync(string message) => throw new InvalidOperationException(message);

        public async Task<int> AddAsync(int a, int b)
        {
            await Task.Yield();
            return a + b;
        }

        public async Task PlainTaskAsync()
        {
            await Task.Yield();
        }
    }

    /// <summary>
    /// Proves compiled-delegate dispatch behaves identically to MethodInfo.Invoke:
    /// same results, same client-visible exceptions (raw, unwrapped), and a working
    /// reflection fallback for byref methods that are not compiled.
    /// </summary>
    public class CompiledDispatchTests : IDisposable
    {
        private readonly NpHost _host;
        private readonly NpClient<IDispatchTester> _client;

        public CompiledDispatchTests()
        {
            var pipeName = "CompiledDispatchTests" + Guid.NewGuid().ToString("N");
            _host = new NpHost(pipeName);
            _host.AddService<IDispatchTester>(new DispatchTester());
            _host.Open();
            _client = new NpClient<IDispatchTester>(new NpEndPoint(pipeName));
        }

        [Fact]
        public void CompiledPath_ReturnsCorrectResults()
        {
            Assert.Equal(7, _client.Proxy.Add(3, 4));
            var g = Guid.NewGuid();
            Assert.Equal("x5" + g.ToString("N"), _client.Proxy.Concat("x", 5, g));
            _client.Proxy.VoidMethod(); // must not throw
        }

        [Fact]
        public void ByRefMethod_FallsBackToReflectionAndWorks()
        {
            int accumulated = 10;
            int result = _client.Proxy.OutAndRef(4, out int doubled, ref accumulated);
            Assert.Equal(4, result);
            Assert.Equal(8, doubled);
            Assert.Equal(14, accumulated);
        }

        [Fact]
        public void SyncException_SurfacesWithOriginalTypeAndMessage()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => _client.Proxy.ThrowSync("sync boom"));
            Assert.Equal("sync boom", ex.Message);
        }

        [Fact]
        public void AsyncException_SurfacesWithOriginalTypeAndMessage()
        {
            var ex = Assert.Throws<InvalidOperationException>(
                () => _client.Proxy.ThrowAsync("async boom").GetAwaiter().GetResult());
            Assert.Equal("async boom", ex.Message);
        }

        [Fact]
        public void AsyncResult_RoundTrips()
        {
            Assert.Equal(9, _client.Proxy.AddAsync(4, 5).GetAwaiter().GetResult());
            _client.Proxy.PlainTaskAsync().GetAwaiter().GetResult(); // must not throw
        }

        public void Dispose()
        {
            _client.Dispose();
            _host.Close();
        }
    }
}
