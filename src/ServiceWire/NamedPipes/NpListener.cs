#region File Creator

// This File Created By Ersin Tarhan
// For Project : ServiceWire - ServiceWire
// On 2016 03 14 04:36

#endregion


#region Usings

using System;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

#endregion


namespace ServiceWire.NamedPipes
{
    public class NpListener
    {
        #region Constractor

        public NpListener(string pipeName,int maxConnections=254,ILog log=null,IStats stats=null)
        {
            _log=log??_log;
            _stats=stats??_stats;
            if(maxConnections>254)
            {
                maxConnections=254;
            }
            _maxConnections=maxConnections;
            PipeName=pipeName;
            _pipeSecurity=new PipeSecurity();
            _pipeSecurity.AddAccessRule(new PipeAccessRule(@"Everyone",PipeAccessRights.ReadWrite,AccessControlType.Allow));
            _pipeSecurity.AddAccessRule(new PipeAccessRule(WindowsIdentity.GetCurrent().User,PipeAccessRights.FullControl,AccessControlType.Allow));
            _pipeSecurity.AddAccessRule(new PipeAccessRule(@"SYSTEM",PipeAccessRights.FullControl,AccessControlType.Allow));
        }

        #endregion


        #region Fields

        private bool running;
        private Thread runningThread;
        private readonly EventWaitHandle terminateHandle=new EventWaitHandle(false,EventResetMode.AutoReset);
        private readonly int _maxConnections=254;
        private readonly PipeSecurity _pipeSecurity;
        private readonly ILog _log=new NullLogger();
        private readonly IStats _stats=new NullStats();

        #endregion


        #region  Proporties

        public string PipeName { get; set; }

        #endregion


        #region Methods


        #region Public Methods

        public void Start()
        {
            running=true;
            Task.Factory.StartNew(() => ServerLoop(),TaskCreationOptions.LongRunning);
        }

        public void Stop()
        {
            if(running)
            {
                running=false;

                //make fake connection to terminate the waiting stream
                try
                {
                    using(var client=new NamedPipeClientStream(PipeName))
                    {
                        client.Connect(50);
                    }
                }
                catch(Exception e)
                {
                    _log.Error("Stop error: {0}",e.ToString().Flatten());
                }
                terminateHandle.WaitOne();
            }
        }

        public void ProcessNextClient()
        {
            try
            {
                var pipeStream=new NamedPipeServerStream(PipeName,PipeDirection.InOut,_maxConnections,PipeTransmissionMode.Byte,PipeOptions.None,512,512,_pipeSecurity);
                pipeStream.WaitForConnection();

                //Task.Factory.StartNew(() => ProcessClientThread(pipeStream), TaskCreationOptions.LongRunning);
                Task.Factory.StartNew(() => ProcessClientThread(pipeStream));
            }
            catch(Exception e)
            {
                //If there are no more avail connections (254 is in use already) then just keep looping until one is avail
                _log.Error("ProcessNextClient error: {0}",e.ToString().Flatten());
            }
        }

        #endregion


        #region Private Methods

        private void ServerLoop()
        {
            try
            {
                while(running)
                {
                    ProcessNextClient();
                }
            }
            catch(Exception e)
            {
                _log.Fatal("ServerLoop fatal error: {0}",e.ToString().Flatten());
            }
            finally
            {
                terminateHandle.Set();
            }
        }

        private void ProcessClientThread(NamedPipeServerStream pipeStream)
        {
            try
            {
                if(RequestReieved!=null) //has event subscribers
                {
                    var args=new PipeClientConnectionEventArgs(pipeStream);
                    RequestReieved(this,args);
                }
            }
            catch(Exception e)
            {
                _log.Error("ProcessClientThread error: {0}",e.ToString().Flatten());
            }
            finally
            {
                if(pipeStream.IsConnected)
                {
                    pipeStream.Close();
                }
                pipeStream.Dispose();
            }
        }

        #endregion


        #endregion


        #region Events

        public event EventHandler<PipeClientConnectionEventArgs> RequestReieved;

        #endregion
    }

    // Defines the data protocol for reading and writing strings on our stream

    // Contains the method executed in the context of the impersonated user
}