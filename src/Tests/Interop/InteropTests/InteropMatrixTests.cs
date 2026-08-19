using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using ServiceWire;
using ServiceWire.InteropContract;
using ServiceWire.NamedPipes;
using ServiceWire.TcpIp;
using Xunit;

namespace InteropTests
{
    /// <summary>
    /// Cross-version compatibility matrix: this process runs ServiceWire 7.0 (project
    /// reference); Old601Peer processes run the published ServiceWire 6.0.1 package.
    /// Every mixed pairing must work over the v1 wire path.
    /// </summary>
    public class InteropMatrixTests
    {
        private static readonly string PeerDll = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "Old601Peer", "bin",
            ConfigurationName, "net8.0", "Old601Peer.dll"));

        private static string ConfigurationName =>
#if DEBUG
            "Debug";
#else
            "Release";
#endif

        private static Process StartPeer(string args)
        {
            var psi = new ProcessStartInfo("dotnet", $"\"{PeerDll}\" {args}")
            {
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            return Process.Start(psi);
        }

        private static Process StartPeerHost(string args)
        {
            var process = StartPeer(args);
            var line = process.StandardOutput.ReadLine();
            Assert.Equal("READY", line);
            return process;
        }

        private static void StopPeerHost(Process process)
        {
            try
            {
                process.StandardInput.WriteLine();
                if (!process.WaitForExit(5000)) process.Kill();
            }
            finally
            {
                process.Dispose();
            }
        }

        private static string RunPeerClient(string args)
        {
            using (var process = StartPeer(args))
            {
                var output = process.StandardOutput.ReadToEnd();
                var stderr = process.StandardError.ReadToEnd();
                process.WaitForExit(30000);
                return (output + stderr).Trim();
            }
        }

        /// <summary>
        /// Asks the OS for a free port rather than guessing one. Parts of the Windows
        /// ephemeral range are reserved by Hyper-V, WinNAT and WSL, and binding one of
        /// those fails with WSAEACCES; the reserved ranges differ per machine, so a
        /// guessed range that is clean locally can be partly reserved on a CI runner.
        /// </summary>
        private static int NextPort()
        {
            //fully qualified: ServiceWire.TcpIp is in scope and also defines Tcp* types
            var probe = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            try
            {
                return ((IPEndPoint)probe.LocalEndpoint).Port;
            }
            finally
            {
                probe.Stop();
            }
        }

        // ---- old 6.0.1 server, new 7.0 client ----

        [Fact]
        public void OldServerTcp_NewClient_Plain()
        {
            var port = NextPort();
            var host = StartPeerHost($"hosttcp {port} 0");
            try
            {
                StreamingChannel.ClearCachedSyncInfo();
                using (var client = new TcpClient<IInteropSvc>(new TcpEndPoint(new IPEndPoint(IPAddress.Loopback, port), 5000)))
                {
                    //throw test skipped: the 6.0.1 server cannot serialize thrown exceptions (fixed in 7.0)
                    var failure = InteropBattery.Run(client.Proxy, largeStringArrays: false, includeThrowTest: false);
                    Assert.Null(failure);
                }
            }
            finally
            {
                StopPeerHost(host);
                StreamingChannel.ClearCachedSyncInfo();
            }
        }

        [Fact]
        public void OldServerTcp_NewClient_Compression()
        {
            var port = NextPort();
            var host = StartPeerHost($"hosttcp {port} 1");
            try
            {
                StreamingChannel.ClearCachedSyncInfo();
                using (var client = new TcpClient<IInteropSvc>(new TcpEndPoint(new IPEndPoint(IPAddress.Loopback, port), 5000)))
                {
                    //large string arrays force the CompressedUnknown path the 7.0 sender fixed;
                    //6.0.1 receivers have read that code since v1.5.0
                    var failure = InteropBattery.Run(client.Proxy, largeStringArrays: true, includeThrowTest: false);
                    Assert.Null(failure);
                }
            }
            finally
            {
                StopPeerHost(host);
                StreamingChannel.ClearCachedSyncInfo();
            }
        }

        [Fact]
        public void OldServerNp_NewClient_Plain()
        {
            var pipe = "InteropOldNp" + Guid.NewGuid().ToString("N");
            var host = StartPeerHost($"hostnp {pipe} 0");
            try
            {
                StreamingChannel.ClearCachedSyncInfo();
                using (var client = new NpClient<IInteropSvc>(new NpEndPoint(pipe, 5000)))
                {
                    var failure = InteropBattery.Run(client.Proxy, largeStringArrays: false, includeThrowTest: false);
                    Assert.Null(failure);
                }
            }
            finally
            {
                StopPeerHost(host);
                StreamingChannel.ClearCachedSyncInfo();
            }
        }

        // ---- new 7.0 server, old 6.0.1 client ----

        [Fact]
        public void NewServerTcp_OldClient_Plain()
        {
            var port = NextPort();
            using (var host = new TcpHost(port))
            {
                host.AddService<IInteropSvc>(new InteropSvc());
                host.Open();
                var result = RunPeerClient($"clienttcp {port} 0");
                Assert.Equal("PASS", result);
            }
        }

        [Fact]
        public void NewServerTcp_OldClient_Compression()
        {
            var port = NextPort();
            using (var host = new TcpHost(port))
            {
                host.UseCompression = true;
                host.CompressionThreshold = 1024;
                host.AddService<IInteropSvc>(new InteropSvc());
                host.Open();
                var result = RunPeerClient($"clienttcp {port} 1");
                Assert.Equal("PASS", result);
            }
        }

        [Fact]
        public void NewServerNp_OldClient_Plain()
        {
            var pipe = "InteropNewNp" + Guid.NewGuid().ToString("N");
            using (var host = new NpHost(pipe))
            {
                host.AddService<IInteropSvc>(new InteropSvc());
                host.Open();
                var result = RunPeerClient($"clientnp {pipe} 0");
                Assert.Equal("PASS", result);
            }
        }

        // ---- capability-cache staleness on in-place downgrade ----

        [Fact]
        public void DowngradedServer_StaleCachedCapability_EvictsAndRenegotiates()
        {
            var port = NextPort();

            //prime the process-wide capability cache against a 7.0 server
            StreamingChannel.ClearCachedSyncInfo();
            using (var host = new TcpHost(port))
            {
                host.AddService<IInteropSvc>(new InteropSvc());
                host.Open();
                using (var client = new TcpClient<IInteropSvc>(new TcpEndPoint(new IPEndPoint(IPAddress.Loopback, port), 5000)))
                {
                    Assert.Equal(42, client.Proxy.Add(2, 40));
                }
            }

            //swap the endpoint for a 6.0.1 server; a new channel hits the stale cache
            var oldHost = StartPeerHost($"hosttcp {port} 0");
            try
            {
                using (var staleClient = new TcpClient<IInteropSvc>(new TcpEndPoint(new IPEndPoint(IPAddress.Loopback, port), 5000)))
                {
                    var ex = Assert.Throws<InvalidOperationException>(() => staleClient.Proxy.Add(1, 1));
                    Assert.Contains("evicted", ex.Message);
                }

                //the eviction lets the next channel renegotiate and run over v1
                using (var freshClient = new TcpClient<IInteropSvc>(new TcpEndPoint(new IPEndPoint(IPAddress.Loopback, port), 5000)))
                {
                    var failure = InteropBattery.Run(freshClient.Proxy, largeStringArrays: false, includeThrowTest: false);
                    Assert.Null(failure);
                }
            }
            finally
            {
                StopPeerHost(oldHost);
                StreamingChannel.ClearCachedSyncInfo();
            }
        }
    }
}
