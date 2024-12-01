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
    }
}