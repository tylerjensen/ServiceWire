using System.Configuration;
using ServiceWire.NamedPipes;
using ServiceWire.TcpIp;
using ServiceWireTestCommon;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ServiceWireTestClient2
{
    class Program
    {
        private static void Main(string[] args)
        {
            Thread.Sleep(1200);
            var ip = ConfigurationManager.AppSettings["ip"];
            var port = Convert.ToInt32(ConfigurationManager.AppSettings["port"]);
            var ipEndpoint = new IPEndPoint(IPAddress.Parse(ip), port);
            var tasks = new List<Task>();
            for (int i = 0; i < 1; i++) tasks.Add(RunTest(ipEndpoint, ip));
            Task.WaitAll(tasks.ToArray());
            Console.ReadLine();
        }

        private static async Task RunTest(IPEndPoint ipEndpoint, string ip)
        {
            using (var client = new TcpClient<IValTypes>(ipEndpoint))
            {
                decimal abc = client.Proxy.GetDecimal(4.5m);
                bool result = client.Proxy.OutDecimal(abc);
            }

            using (var client = new TcpClient<IValTypes>(ipEndpoint))
            {
	            decimal abc = await client.Proxy.GetDecimalAsync(4.5m);
	            bool result = await client.Proxy.OutDecimalAsync(abc);
            }

			using (var client = new NetTcpTesterProxy(ipEndpoint))
            {
                var id = client.GetId("test1", 3.314, 42, DateTime.Now);
                long q = 3;
                var response = client.Get(id, "mirror", 4.123, out q);
                var list = client.GetItems(id);
                var listFromAsync = await client.GetItemsAsync(id);
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

            if ("127.0.0.1" == ip) //only run np tests when testing locally
            {
                var pipeName = "ServiceWireTestHost";
                using (var client = new NetNpTesterProxy(new NpEndPoint(pipeName)))
                {
                    var id = client.GetId("test1", 3.314, 42, DateTime.Now);
                    long q = 2;
                    var response = client.Get(id, "mirror", 4.123, out q);
                    var list = client.GetItems(id);
                }

                sw = Stopwatch.StartNew();
                Parallel.For(from, to, index =>
                {
                    using (var client = new NetNpTesterProxy(new NpEndPoint(pipeName)))
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            var id = client.GetId("test1", 3.314, 42, DateTime.Now);
                            long q = 4;
                            var response = client.Get(id, "mirror", 4.123, out q);
                            var list = client.GetItems(id);

                            long id1;
                            long id2;
                            long id3 = client.TestLong(out id1, out id2);

                            eResult e1 = eResult.FAILED;
                            eResult e2 = eResult.FAILED;
                            eResult eRet =  client.TestEnum(out e1, ref e2);
                        }
                    }
                    using (var client = new NetNpMyTesterProxy(new NpEndPoint(pipeName)))
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            var id = client.GetId("test1", 3.314, 42);
                            int q2 = 4;
                            var response = client.Get(id, "mirror", 4.123, out q2);
                            var list = client.GetItems(id, new int[] { 3, 6, 9 });
                        }
                    }
                });
                sw.Stop();
                msperop = sw.ElapsedMilliseconds / 24000.0;
                Console.WriteLine("pip: {0}, {1}", sw.ElapsedMilliseconds, msperop);
                Thread.Sleep(2000);
            }
        }
    }
}
