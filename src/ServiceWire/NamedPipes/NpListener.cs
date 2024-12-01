using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceWire.NamedPipes
{
    public class NpListener
    {
        private bool _running;
        private readonly EventWaitHandle _terminateHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
        private readonly int _maxConnections = 254;
        private readonly ILog _log;
        private readonly IStats _stats;
        private readonly INamedPipeServerStreamFactory _streamFactory;

        public string PipeName { get; set; }

        public event EventHandler<PipeClientConnectionEventArgs> RequestReieved;

        public NpListener(string pipeName, int maxConnections = 254, ILog log = null, IStats stats = null, INamedPipeServerStreamFactory streamFactory = null)
        {
            _log = log ?? new NullLogger();
            _stats = stats ?? new NullStats();
            if (maxConnections > 254) maxConnections = 254;
            _maxConnections = maxConnections;
            this.PipeName = pipeName;
            _streamFactory = streamFactory ?? new DefaultNamedPipeServerStreamFactory();
        }

        public void Start()
        {
            _running = true;
            Task.Factory.StartNew(() => ServerLoop(), TaskCreationOptions.LongRunning);
        }

        public void Stop()
        {
            if (_running)
            {
                _running = false;
                //make fake connection to terminate the waiting stream
                try
                {
                    using (var client = new NamedPipeClientStream(PipeName))
                    {
                        client.Connect(50);
                    }
                }
                catch (Exception e)
                {
                    _log.Error("Stop error: {0}", e.ToString().Flatten());
                }
                _terminateHandle.WaitOne();
            }
        }

        private void ServerLoop()
        {
            try
            {
                while (_running)
                {
                    ProcessNextClient();
                }
            }
            catch (Exception e)
            {
                _log.Fatal("ServerLoop fatal error: {0}", e.ToString().Flatten());
            }
            finally
            {
                _terminateHandle.Set();
            }
        }

        private void ProcessClientThread(NamedPipeServerStream pipeStream)
        {
            try
            {
                if (this.RequestReieved != null) //has event subscribers
                {
                    var args = new PipeClientConnectionEventArgs(pipeStream);
                    RequestReieved(this, args);
                }
            }
            catch (Exception e)
            {
                _log.Error("ProcessClientThread error: {0}", e.ToString().Flatten());
            }
            finally
            {
                if (pipeStream.IsConnected) pipeStream.Close();
                pipeStream.Dispose();
            }
        }

        public void ProcessNextClient()
        {
            try
            {
                var pipeStream = _streamFactory.Create(PipeName, PipeDirection.InOut, _maxConnections, PipeTransmissionMode.Byte, PipeOptions.None, 512, 512);
                pipeStream.WaitForConnection();
                //Task.Factory.StartNew(() => ProcessClientThread(pipeStream), TaskCreationOptions.LongRunning);
                Task.Factory.StartNew(() => ProcessClientThread(pipeStream));
            }
            catch (Exception e)
            {
                //If there are no more avail connections (254 is in use already) then just keep looping until one is avail
                _log.Error("ProcessNextClient error: {0}", e.ToString().Flatten());
            }
        }
    }

    // Defines the data protocol for reading and writing strings on our stream

    // Contains the method executed in the context of the impersonated user
}
