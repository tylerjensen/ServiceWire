using ServiceWire.NamedPipes;
using ServiceWire.TcpIp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace ServiceWireTestCommon
{
    public enum eResult : UInt16
    {
        FAILED = 0,
        OK = 1
    }

    public interface IValTypes
    {
        decimal GetDecimal(decimal input);
        Task<decimal> GetDecimalAsync(decimal input);
        bool OutDecimal(decimal val);
        Task<bool> OutDecimalAsync(decimal val);
    }
    
    public interface INetTester
    {
        Guid GetId(string source, double weight, int quantity, DateTime dt);
        TestResponse Get(Guid id, string label, double weight, out long quantity);
        long TestLong(out long id1, out long id2);
        eResult TestEnum(out eResult e1, ref eResult e2);
        List<string> GetItems(Guid id);
        Task<List<string>> GetItemsAsync(Guid id);
    }

    public interface IMyTester
    {
        Guid GetId(string source, double weight, int quantity);
        TestResponse Get(Guid id, string label, double weight, out int quantity);
        List<string> GetItems(Guid id, int[] vals);
    }

    [Serializable]
    public struct TestResponse
    {
        public Guid Id { get; set; }
        public string Label { get; set; }
        public long Quantity { get; set; }
        public IList<string> Values { get; set; }
    }

    public class NetTcpTesterProxy : TcpClient<INetTester>, INetTester
    {
        public NetTcpTesterProxy(TcpEndPoint endpoint) : base(endpoint)
        {
        }

        public NetTcpTesterProxy(IPEndPoint endpoint) : base(endpoint)
        {
        }

        public Guid GetId(string source, double weight, int quantity, DateTime dt)
        {
            return Proxy.GetId(source, weight, quantity, dt);
        }

        public TestResponse Get(Guid id, string label, double weight, out long quantity)
        {
            return Proxy.Get(id, label, weight, out quantity);
        }

        public List<string> GetItems(Guid id)
        {
            return Proxy.GetItems(id);
        }

        public Task<List<string>> GetItemsAsync(Guid id)
        {
	        return Task.FromResult(Proxy.GetItems(id));
        }

        public long TestLong(out long id1, out long id2)
        {
            id1 = 23;
            id2 = 24;
            return 25;
        }

        public eResult TestEnum(out eResult e1, ref eResult e2)
        {
            e1 = eResult.OK;
            e2 = eResult.OK;
            return eResult.OK;
        }
    }

    public class NetNpTesterProxy : NpClient<INetTester>, INetTester
    {
        public NetNpTesterProxy(NpEndPoint npAddress) : base(npAddress)
        {
        }

        public Guid GetId(string source, double weight, int quantity, DateTime dt)
        {
            return Proxy.GetId(source, weight, quantity, dt);
        }

        public TestResponse Get(Guid id, string label, double weight, out long quantity)
        {
            return Proxy.Get(id, label, weight, out quantity);
        }

        public List<string> GetItems(Guid id)
        {
            return Proxy.GetItems(id);
        }

        public Task<List<string>> GetItemsAsync(Guid id)
        {
	        return Task.FromResult(Proxy.GetItems(id));
        }

        public long TestLong(out long id1, out long id2)
        {
            return Proxy.TestLong(out id1, out id2);
        }

        public eResult TestEnum(out eResult e1, ref eResult e2)
        {
            return Proxy.TestEnum(out e1, ref e2);
        }
    }

    public class NetTcpMyTesterProxy : TcpClient<IMyTester>, IMyTester
    {
        public NetTcpMyTesterProxy(IPEndPoint endpoint)
            : base(endpoint)
        {
        }

        public Guid GetId(string source, double weight, int quantity)
        {
            return Proxy.GetId(source, weight, quantity);
        }

        public TestResponse Get(Guid id, string label, double weight, out int quantity)
        {
            return Proxy.Get(id, label, weight, out quantity);
        }

        public List<string> GetItems(Guid id, int[] vals)
        {
            return Proxy.GetItems(id, vals);
        }
    }

    public class NetNpMyTesterProxy : NpClient<IMyTester>, IMyTester
    {
        public NetNpMyTesterProxy(NpEndPoint npAddress)
            : base(npAddress)
        {
        }

        public Guid GetId(string source, double weight, int quantity)
        {
            return Proxy.GetId(source, weight, quantity);
        }

        public TestResponse Get(Guid id, string label, double weight, out int quantity)
        {
            return Proxy.Get(id, label, weight, out quantity);
        }

        public List<string> GetItems(Guid id, int[] vals)
        {
            return Proxy.GetItems(id, vals);
        }
    }
}