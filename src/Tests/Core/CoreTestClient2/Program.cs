using CoreTestCommon;
using ServiceWire.TcpIp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CoreTestClient2
{
    class Program
    {
        private static void Main(string[] args)
        {
            var addr = new[] { "127.0.0.1", "8098" }; //defaults
            if (null != args && args.Length > 0)
            {
                var parts = args[0].Split(':');
                if (parts.Length > 1) addr[1] = parts[1];
                addr[0] = parts[0];
            }

            var ip = addr[0];
            var port = Convert.ToInt32(addr[1]);
            var ipEndpoint = new IPEndPoint(IPAddress.Parse(ip), port);
            for (int i = 0; i < 1; i++) RunTest(ipEndpoint, ip);

            Console.ReadLine();
        }

        private static void RunTest(IPEndPoint ipEndpoint, string ip)
        {
            using (var client = new TcpClient<IValTypes>(ipEndpoint))
            {
                decimal abc = client.Proxy.GetDecimal(4.5m);
                bool result = client.Proxy.OutDecimal(abc);
            }

            using (var client = new NetTcpTesterProxy(ipEndpoint))
            {
                var id = client.GetId("test1", 3.314, 42, DateTime.Now);
                long q = 3;
                var response = client.Get(id, "mirror", 4.123, out q);
                var list = client.GetItems(id);
            }
            using (var client = new NetTcpMyTesterProxy(ipEndpoint))
            {
                var id = client.GetId("test1", 3.314, 42);
                int q2 = 4;
                var response = client.Get(id, "mirror", 4.123, out q2);
                var list = client.GetItems(id, new int[] { 3, 6, 9 });
            }

            var sw = Stopwatch.StartNew();
            var from = 0;
            var to = 400;
            Parallel.For(from, to, index =>
            {
                using (var client = new NetTcpTesterProxy(ipEndpoint))
                {
                    for (int i = 0; i < 10; i++)
                    {
                        var id = client.GetId("test1", 3.314, 42, DateTime.Now);
                        long q = 2;
                        var response = client.Get(id, "mirror", 4.123, out q);
                        var list = client.GetItems(id);
                    }
                }

                using (var client = new NetTcpMyTesterProxy(ipEndpoint))
                {
                    for (int i = 0; i < 10; i++)
                    {
                        var id = client.GetId("test1", 3.314, 42);
                        int q2 = 6;
                        var response = client.Get(id, "mirror", 4.123, out q2);
                        var list = client.GetItems(id, new int[] { 3, 6, 9 });
                    }
                }
            });
            sw.Stop();
            var msperop = sw.ElapsedMilliseconds / 24000.0;
            Console.WriteLine("tcp: {0}, {1}", sw.ElapsedMilliseconds, msperop);
        }
    }
}
