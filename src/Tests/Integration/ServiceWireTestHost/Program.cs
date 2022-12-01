using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ServiceWire;
using ServiceWire.NamedPipes;
using ServiceWire.TcpIp;
using ServiceWireTestCommon;

namespace ServiceWireTestHost
{
	class Program
	{
		static void Main(string[] args)
		{
            var logger = new Logger(logLevel: LogLevel.Debug);
            var stats = new Stats();

            var ip = ConfigurationManager.AppSettings["ip"];
            var port = Convert.ToInt32(ConfigurationManager.AppSettings["port"]);
            var ipEndpoint = new IPEndPoint(IPAddress.Any, port);

		    var useCompression = false;
            var compressionThreshold = 131072; //128KB
            var pipeName = "ServiceWireTestHost";
			
            var nphost = new NpHost(pipeName, logger, stats);
			var tester = new NetTester();
			nphost.AddService<INetTester>(tester);
			var mytester = new MyTester();
            nphost.UseCompression = useCompression;
            nphost.CompressionThreshold = compressionThreshold;
			nphost.AddService<IMyTester>(mytester);
			nphost.Open();

			var tcphost = new TcpHost(ipEndpoint, logger, stats);
            tcphost.UseCompression = useCompression;
            tcphost.CompressionThreshold = compressionThreshold;
			tcphost.AddService<INetTester>(tester);
			tcphost.AddService<IMyTester>(mytester);

		    var valTypes = new ValTypes();
            tcphost.AddService<IValTypes>(valTypes);

			tcphost.Open();

			Console.WriteLine("Press Enter to stop the dual host test.");
			Console.ReadLine();

			nphost.Close();
			tcphost.Close();

			Console.WriteLine("Press Enter to quit.");
			Console.ReadLine();
		}
	}

    public class ValTypes : IValTypes
    {
        public decimal GetDecimal(decimal input)
        {
            return input += 456.44m;
        }

        public Task<decimal> GetDecimalAsync(decimal input)
        {
			return Task.FromResult(GetDecimal(input));
		}

        public bool OutDecimal(decimal val)
        {
            val = 45.66m;
            return true;
        }

        public Task<bool> OutDecimalAsync(decimal val)
        {
	        return Task.FromResult(OutDecimal(val));
        }
    }

	public class NetTester : INetTester
	{
        public Guid GetId(string source, double weight, int quantity, DateTime dt)
		{
			return Guid.NewGuid();
		}

        public TestResponse Get(Guid id, string label, double weight, out long quantity)
		{
		    quantity = 42;
			return new TestResponse { Id = id, Label = "Hello, world.", Quantity = quantity };
		}

		public List<string> GetItems(Guid id)
		{
			var list = new List<string>();
			list.Add("42");
			list.Add(id.ToString());
			list.Add("Test");
			return list;
		}

		public Task<List<string>> GetItemsAsync(Guid id)
		{
			return Task.FromResult(GetItems(id));
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

    public class MyTester : IMyTester
	{
		private string longLabel = string.Empty;
        private const int totalKilobytes = 140;
        private Random rand = new Random(DateTime.Now.Millisecond);

		public MyTester()
		{
			var sb = new StringBuilder();
            for (int i = 0; i < totalKilobytes; i++)
			{
                for (int k = 0; k < 1024; k++) sb.Append(((char)rand.Next(32, 126)));
			}
			longLabel = sb.ToString();
		}


		public Guid GetId(string source, double weight, int quantity)
		{
			return Guid.NewGuid();
		}

		public TestResponse Get(Guid id, string label, double weight, out int quantity)
		{
		    quantity = 44;
			return new TestResponse { Id = id, Label = longLabel, Quantity = quantity };
		}

        public List<string> GetItems(Guid id, int[] vals)
		{
			var list = new List<string>();
			list.Add("42");
			list.Add(id.ToString());
			list.Add("MyTest");
			list.Add(longLabel);
			return list;
		}
	}
}
