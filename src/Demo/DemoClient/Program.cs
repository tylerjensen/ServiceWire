using DemoCommon;
using ServiceWire;
using ServiceWire.TcpIp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace DemoClient
{
    class Program
    {
        private static async Task Main(string[] args)
        {
            var logger = new Logger(logLevel: LogLevel.Debug);
            var stats = new Stats();

            var addr = new[] { "127.0.0.1", "8098" }; //defaults
            if (null != args && args.Length > 0)
            {
                var parts = args[0].Split(':');
                if (parts.Length > 1) addr[1] = parts[1];
                addr[0] = parts[0];
            }

            var ip = addr[0];
            var port = Convert.ToInt32(addr[1]);
            var zkEndpoint = new TcpZkEndPoint("username", "password", 
                new IPEndPoint(IPAddress.Parse(ip), port), connectTimeOutMs: 2500);

            Console.WriteLine("Iteration 1");
            await RunTest(zkEndpoint, ip, logger, stats);

            Console.WriteLine("Iteration 2");
            await RunTest(zkEndpoint, ip, logger, stats);

            Console.ReadLine();
        }

        private static async Task RunTest(TcpZkEndPoint zkEndpoint, string ip, Logger logger, Stats stats)
        {
            var sw = Stopwatch.StartNew();
			using (var client = new TcpClient<ITest>(zkEndpoint, null, null, null, null, logger, stats))
			{
				await client.Proxy.SetAsync(1);
				int value = await client.Proxy.GetAsync();
			}

			using (var client = new TcpClient<IDataContract>(zkEndpoint, null, null, null, null, logger, stats))
			{
				decimal abc = client.Proxy.GetDecimal(4.5m);
				bool result = client.Proxy.OutDecimal(abc);
			}

			using (var client = new TcpClient<IComplexDataContract>(zkEndpoint, null, null, null, null, logger, stats))
			{
				var id = client.Proxy.GetId("test1", 3.314, 42, DateTime.Now);
				long q = 3;
				var response = client.Proxy.Get(id, "mirror", 4.123, out q);
				var list = client.Proxy.GetItems(id);
			}

			Console.WriteLine("elapsed ms: {0}", sw.ElapsedMilliseconds);
        }
    }
}
