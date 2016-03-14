#region File Creator

// This File Created By Ersin Tarhan
// For Project : ServiceWire - ServiceWire
// On 2016 03 14 04:36

#endregion


#region Usings

using System;
using System.IO;

#endregion


namespace ServiceWire.NamedPipes
{
    public class NpHost:Host
    {
        #region Constractor

        /// <summary>
        ///     Constructs an instance of the host and starts listening for incoming connections.
        ///     All listener threads are regular background threads.
        ///     NOTE: the instance is not automatically thread safe!
        /// </summary>
        /// <param name="pipeName">The pipe name for incoming requests</param>
        /// <param name="log"></param>
        /// <param name="stats"></param>
        public NpHost(string pipeName,ILog log=null,IStats stats=null)
        {
            Log=log;
            Stats=stats;
            PipeName=pipeName;
            _listener=new NpListener(PipeName,log:Log,stats:Stats);
            _listener.RequestReieved+=ClientConnectionMade;
        }

        #endregion


        #region Fields

        private readonly NpListener _listener;
        private bool _useThreadPool;

        #endregion


        #region  Proporties

        /// <summary>
        ///     Gets the end point this host is listening on
        /// </summary>
        public string PipeName { get; }

        #endregion


        #region Methods


        #region Private Methods

        /// <summary>
        ///     This method handles all requests on separate thread per client connection.
        ///     There is one thread running this method for each connected client.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void ClientConnectionMade(object sender,PipeClientConnectionEventArgs args)
        {
            var stream=new BufferedStream(args.PipeStream);
            ProcessRequest(stream);
        }

        #endregion


        #region Protected Methods

        protected override void StartListener()
        {
            _listener.Start(); //start listening in the background
        }

        #endregion


        #endregion


        #region  Others

        /// <summary>
        ///     Get or set whether the host should use regular or thread pool threads.
        /// </summary>
        public bool UseThreadPool
        {
            get { return _useThreadPool; }
            set
            {
                if(_isOpen)
                {
                    throw new Exception("The host is already open");
                }
                _useThreadPool=value;
            }
        }

        #endregion


        #region IDisposable Members

        private bool _disposed;

        protected override void Dispose(bool disposing)
        {
            if(!_disposed)
            {
                _disposed=true; //prevent second call to Dispose
                if(disposing)
                {
                    _listener.Stop();
                }
            }
        }

        #endregion
    }
}