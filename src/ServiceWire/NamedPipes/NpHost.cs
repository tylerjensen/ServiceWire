using System;
using System.IO;

namespace ServiceWire.NamedPipes
{
    public class NpHost : Host
    {
        private NpListener _listener;
        private string _pipeName;
        private bool _useThreadPool = false;

        /// <summary>
        /// Get or set whether the host should use regular or thread pool threads.
        /// </summary>
        public bool UseThreadPool
        {
            get { return _useThreadPool; }
            set
            {
                if (_isOpen)
                    throw new Exception("The host is already open");
                _useThreadPool = value;
            }
        }

        /// <summary>
        /// Constructs an instance of the host and starts listening for incoming connections.
        /// All listener threads are regular background threads.
        /// 
        /// NOTE: the instance is not automatically thread safe!
        /// </summary>
        /// <param name="pipeName">The pipe name for incoming requests</param>
        /// <param name="log"></param>
        /// <param name="stats"></param>
        /// <param name="serializer">Inject your own serializer for complex objects and avoid using the Newtonsoft JSON DefaultSerializer.</param>
        public NpHost(string pipeName, ILog log = null, IStats stats = null, ISerializer serializer = null)
            : base(serializer)
        {
            base.Log = log;
            base.Stats = stats;
            _pipeName = pipeName;
            _listener = new NpListener(_pipeName, log: base.Log, stats: base.Stats);
            _listener.RequestReieved += ClientConnectionMade;
        }

        /// <summary>
        /// Gets the end point this host is listening on
        /// </summary>
        public string PipeName
        {
            get { return _pipeName; }
        }

        protected override void StartListener()
        {
            _listener.Start(); //start listening in the background
        }

        /// <summary>
        /// This method handles all requests on separate thread per client connection.
        /// There is one thread running this method for each connected client.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void ClientConnectionMade(object sender, PipeClientConnectionEventArgs args)
        {
            var stream = new BufferedStream(args.PipeStream);
            base.ProcessRequest(stream);
        }

        #region IDisposable Members

        private bool _disposed = false;

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true; //prevent second call to Dispose
                if (disposing)
                {
                    _listener.Stop();
                }
            }
        }

        #endregion
    }
}
