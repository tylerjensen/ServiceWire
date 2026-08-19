using System.Net;
using System.Net.Sockets;

namespace ServiceWireTests
{
    /// <summary>
    /// Hands out a TCP port the operating system says is free.
    /// <para>
    /// Guessing a port from a range fails intermittently on Windows: parts of the
    /// ephemeral range are reserved by Hyper-V, WinNAT and WSL, and binding one of
    /// those returns WSAEACCES ("An attempt was made to access a socket in a way
    /// forbidden by its access permissions") rather than address-in-use. The reserved
    /// ranges differ per machine, so a range that is clean locally can be a third
    /// reserved on a CI runner. Run "netsh int ipv4 show excludedportrange
    /// protocol=tcp" to see them.
    /// </para>
    /// <para>
    /// Binding port 0 makes the OS pick, which skips reserved ranges and ports already
    /// in use, and avoids the TIME_WAIT rebinding failures a fixed port hits when a
    /// test class is constructed once per test method.
    /// </para>
    /// </summary>
    internal static class TestPorts
    {
        /// <summary>
        /// Connect timeout for test clients, deliberately far above the 2500 ms default.
        /// <para>
        /// A ServiceWire client connects through Socket.ConnectAsync and blocks until the
        /// SocketAsyncEventArgs.Completed callback signals it. That callback is dispatched
        /// on a thread-pool thread, so its latency tracks thread-pool health rather than
        /// the network: with the pool saturated, a loopback connect that completes in
        /// under a millisecond is not observed for hundreds of milliseconds. This suite
        /// runs xUnit collections in parallel and several tests block pool threads on
        /// purpose, so on a two-core CI runner the delay can exceed the default and the
        /// client reports a spurious TimeoutException against a host that was listening
        /// the whole time.
        /// </para>
        /// </summary>
        public const int ConnectTimeoutMs = 15000;

        public static int GetFreePort()
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
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
    }
}
