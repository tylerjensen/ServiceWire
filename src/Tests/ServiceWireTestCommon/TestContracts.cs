#region File Creator

// This File Created By Ersin Tarhan
// For Project : ServiceWire - ServiceWireTestCommon
// On 2016 03 14 04:36

#endregion


#region Usings

using System;
using System.Collections.Generic;
using System.Net;

using ServiceWire.NamedPipes;
using ServiceWire.TcpIp;

#endregion


namespace ServiceWireTestCommon
{
    public interface IValTypes
    {
        #region Methods


        #region Public Methods

        decimal GetDecimal(decimal input);

        bool OutDecimal(decimal val);

        #endregion


        #endregion
    }

    public interface INetTester
    {
        #region Methods


        #region Public Methods

        Guid GetId(string source,double weight,int quantity,DateTime dt);

        TestResponse Get(Guid id,string label,double weight,out long quantity);

        long TestLong(out long id1,out long id2);

        List<string> GetItems(Guid id);

        #endregion


        #endregion
    }

    public interface IMyTester
    {
        #region Methods


        #region Public Methods

        Guid GetId(string source,double weight,int quantity);

        TestResponse Get(Guid id,string label,double weight,out int quantity);

        List<string> GetItems(Guid id,int[] vals);

        #endregion


        #endregion
    }

    public struct TestResponse
    {
        public Guid Id { get; set; }
        public string Label { get; set; }
        public long Quantity { get; set; }
    }

    public class NetTcpTesterProxy:TcpClient<INetTester>,INetTester
    {
        #region Constractor

        public NetTcpTesterProxy(TcpEndPoint endpoint):base(endpoint)
        {
        }

        public NetTcpTesterProxy(IPEndPoint endpoint):base(endpoint)
        {
        }

        #endregion


        #region Methods


        #region Public Methods

        public Guid GetId(string source,double weight,int quantity,DateTime dt)
        {
            return Proxy.GetId(source,weight,quantity,dt);
        }

        public TestResponse Get(Guid id,string label,double weight,out long quantity)
        {
            return Proxy.Get(id,label,weight,out quantity);
        }

        public List<string> GetItems(Guid id)
        {
            return Proxy.GetItems(id);
        }

        public long TestLong(out long id1,out long id2)
        {
            id1=23;
            id2=24;
            return 25;
        }

        #endregion


        #endregion
    }

    public class NetNpTesterProxy:NpClient<INetTester>,INetTester
    {
        #region Constractor

        public NetNpTesterProxy(NpEndPoint npAddress):base(npAddress)
        {
        }

        #endregion


        #region Methods


        #region Public Methods

        public Guid GetId(string source,double weight,int quantity,DateTime dt)
        {
            return Proxy.GetId(source,weight,quantity,dt);
        }

        public TestResponse Get(Guid id,string label,double weight,out long quantity)
        {
            return Proxy.Get(id,label,weight,out quantity);
        }

        public List<string> GetItems(Guid id)
        {
            return Proxy.GetItems(id);
        }

        public long TestLong(out long id1,out long id2)
        {
            return Proxy.TestLong(out id1,out id2);
        }

        #endregion


        #endregion
    }

    public class NetTcpMyTesterProxy:TcpClient<IMyTester>,IMyTester
    {
        #region Constractor

        public NetTcpMyTesterProxy(IPEndPoint endpoint):base(endpoint)
        {
        }

        #endregion


        #region Methods


        #region Public Methods

        public Guid GetId(string source,double weight,int quantity)
        {
            return Proxy.GetId(source,weight,quantity);
        }

        public TestResponse Get(Guid id,string label,double weight,out int quantity)
        {
            return Proxy.Get(id,label,weight,out quantity);
        }

        public List<string> GetItems(Guid id,int[] vals)
        {
            return Proxy.GetItems(id,vals);
        }

        #endregion


        #endregion
    }

    public class NetNpMyTesterProxy:NpClient<IMyTester>,IMyTester
    {
        #region Constractor

        public NetNpMyTesterProxy(NpEndPoint npAddress):base(npAddress)
        {
        }

        #endregion


        #region Methods


        #region Public Methods

        public Guid GetId(string source,double weight,int quantity)
        {
            return Proxy.GetId(source,weight,quantity);
        }

        public TestResponse Get(Guid id,string label,double weight,out int quantity)
        {
            return Proxy.Get(id,label,weight,out quantity);
        }

        public List<string> GetItems(Guid id,int[] vals)
        {
            return Proxy.GetItems(id,vals);
        }

        #endregion


        #endregion
    }
}