namespace ServiceWire.NamedPipes
{
    public class NpEndPoint
    {
        public NpEndPoint(string pipeName, int connectTimeOutMs = 2500) 
            : this(".", pipeName, connectTimeOutMs)
        {
        }

        public NpEndPoint(string serverName, string pipeName, int connectTimeOutMs = 2500)
        {
            this.ServerName = serverName;
            this.PipeName = pipeName;
            this.ConnectTimeOutMs = connectTimeOutMs;
        }

        public string ServerName { get; private set; }
        public string PipeName { get; private set; }
        public int ConnectTimeOutMs { get; private set; }

        /// <summary>
        /// When true (the default) the client uses the v2 wire protocol when the
        /// server advertises it: framed messages with correlation ids that survive
        /// request decode errors, plus binary DateTime transfer. Named-pipe handles
        /// cannot overlap reads and writes, so v2 calls on a pipe serialize the
        /// whole exchange (no pipelining). Set false to force the classic v1 wire
        /// (lowest per-call overhead for strictly sequential local callers).
        /// </summary>
        public bool UseWireV2 { get; set; } = true;
    }
}