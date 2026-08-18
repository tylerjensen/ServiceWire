using System;
using System.Net;
using ServiceWire.InteropContract;
using ServiceWire.NamedPipes;
using ServiceWire.TcpIp;

namespace Old601Peer
{
    /// <summary>
    /// A ServiceWire 6.0.1 peer run as a separate process by InteropTests.
    /// Usage: Old601Peer (hosttcp|hostnp|clienttcp|clientnp) (port|pipeName) (compress: 0|1)
    /// Hosts print READY and wait for a line on stdin to exit.
    /// Clients run the battery and print PASS or FAIL: reason (exit code 0/1).
    /// </summary>
    internal class Program
    {
        private static int Main(string[] args)
        {
            var mode = args[0];
            var address = args[1];
            var compress = args.Length > 2 && args[2] == "1";
            switch (mode)
            {
                case "hosttcp":
                {
                    using (var host = new TcpHost(int.Parse(address)))
                    {
                        host.UseCompression = compress;
                        if (compress) host.CompressionThreshold = 1024;
                        host.AddService<IInteropSvc>(new InteropSvc());
                        host.Open();
                        Console.WriteLine("READY");
                        Console.ReadLine();
                    }
                    return 0;
                }
                case "hostnp":
                {
                    using (var host = new NpHost(address))
                    {
                        host.UseCompression = compress;
                        if (compress) host.CompressionThreshold = 1024;
                        host.AddService<IInteropSvc>(new InteropSvc());
                        host.Open();
                        Console.WriteLine("READY");
                        Console.ReadLine();
                    }
                    return 0;
                }
                case "clienttcp":
                {
                    using (var client = new TcpClient<IInteropSvc>(new TcpEndPoint(
                        new IPEndPoint(IPAddress.Loopback, int.Parse(address)), 5000)))
                    {
                        //old 6.0.1 client: large compressed string[] hits the pre-7.0
                        //sender bug, so the battery keeps string arrays small here
                        var failure = InteropBattery.Run(client.Proxy, largeStringArrays: false, includeThrowTest: true);
                        Console.WriteLine(failure == null ? "PASS" : "FAIL: " + failure);
                        return failure == null ? 0 : 1;
                    }
                }
                case "clientnp":
                {
                    using (var client = new NpClient<IInteropSvc>(new NpEndPoint(address, 5000)))
                    {
                        var failure = InteropBattery.Run(client.Proxy, largeStringArrays: false, includeThrowTest: true);
                        Console.WriteLine(failure == null ? "PASS" : "FAIL: " + failure);
                        return failure == null ? 0 : 1;
                    }
                }
                default:
                    Console.WriteLine("FAIL: unknown mode " + mode);
                    return 2;
            }
        }
    }
}
