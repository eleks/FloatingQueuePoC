using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using FloatingQueue.Common.Common;
using FloatingQueue.Common.Proxy;

namespace FloatingQueue.Common.TCP
{
    public interface ITCPServer
    {
        void Initialize(string displayName, string address);
    }

    public abstract class TCPServerBase : TCPCommunicationObjectBase, ITCPServer
    {
        public string DisplayName { get; private set; }
        public int Port { get; private set; }
        private ConnectionListenerThread m_ListenerThread;

        public void Initialize(string displayName, string address)
        {
            DisplayName = displayName;
            //
            var uri = new Uri(address);
            Port = uri.Port;
            //
            m_ListenerThread = new ConnectionListenerThread(this);
        }

        public override void Open()
        {
            m_ListenerThread.Start(null);
        }

        public override void Close()
        {
            m_ListenerThread.Stop();
        }

        public override void Abort()
        {
            Close();
        }

        // Implementation

        private class ConnectionListenerThread : ThreadBase
        {
            private readonly TCPServerBase m_Server;
            private readonly TcpListener m_Listener;
            private readonly List<ConnectionWorkerThread> m_WorkingThreads = new List<ConnectionWorkerThread>();

            public ConnectionListenerThread(TCPServerBase server)
                : base(server.DisplayName + " - Listener for " + server.Port)
            {
                m_Server = server;
                m_Listener = new TcpListener(IPAddress.Any, m_Server.Port);
            }

            protected override void RunCore()
            {
                m_Listener.Start(100);
                Logger.Instance.Info("Listening");
                while (!IsStopping)
                {
                    Thread.Sleep(0);
                    try
                    {
                        var acceptTcpClient = m_Listener.AcceptTcpClient();
                        var idleThread = m_WorkingThreads.FirstOrDefault(th => th.IsIdle && th.IsAlive);
                        if (idleThread == null)
                        {
                            Console.Out.WriteLine("New working thread created");
                            idleThread = new ConnectionWorkerThread(m_Server);
                            idleThread.Start(null);
                            m_WorkingThreads.Add(idleThread);
                        }
                        idleThread.AttachClient(acceptTcpClient);
                    }
                    catch (SocketException e)
                    {
                        Logger.Instance.Debug("  Socket listener: " + e.Message);
                    }
                    // clean-up dead threads
                    m_WorkingThreads.RemoveAll(t => !t.IsAlive);
                }

                m_WorkingThreads.ForEach(t => t.Stop());
                m_WorkingThreads.ForEach(t => t.Wait());
                m_WorkingThreads.RemoveAll(t => !t.IsAlive);

                if (m_WorkingThreads.Count > 0)
                    throw new Exception("m_workingThreads.Count > 0");
            }

            protected override void StopCore()
            {
                m_Listener.Stop();
            }
        }

        public class ConnectionWorkerThread : ThreadBase
        {
            private readonly TCPServerBase m_Server;
            private readonly AutoResetEvent m_NewClientAttachedEvent = new AutoResetEvent(false);
            private TcpClient m_TcpClient;

            public ConnectionWorkerThread(TCPServerBase server)
                : base(server.DisplayName + " - Worker")
            {
                m_Server = server;
            }

            public bool IsIdle
            {
                get { return m_TcpClient == null; }
            }

            public void AttachClient(TcpClient client)
            {
                m_TcpClient = client;
                m_NewClientAttachedEvent.Set();
            }

            private int ReadBuffer(byte[] buf)
            {
                var done = 0;
                try
                {
                    while (done < buf.Length)
                    {
                        var justRead = m_TcpClient.GetStream().Read(buf, done, buf.Length - done);
                        if (justRead == 0)
                            break;
                        done += justRead;
                    }
                }
                catch (IOException)
                {
                }
                return done;
            }


            protected override void RunCore()
            {
                while (!IsStopping)
                {
                    m_NewClientAttachedEvent.WaitOne();
                    if (IsStopping)
                        break;
                    ProcessClientRequests();
                }
            }

            private void ProcessClientRequests()
            {
                try
                {
                    while (!IsStopping)
                    {
                        if (m_TcpClient == null || !m_TcpClient.Connected)
                        {
                            break;
                        }
                        var req = new TCPBinaryReader(TCPCommunicationSignature.Request, ReadBuffer);
                        if (!req.IsComplete) // incomplete request received
                        {
                            break;
                        }
                        if (req.Command == TCPCommunicationSignature.CmdClose) // Close command
                        {
                            break;
                        }

                        var res = new TCPBinaryWriter(TCPCommunicationSignature.Response, req.Command);
                        var status = m_Server.SafeDispatch(req, res);
                        if (status == DispatchStatus.Close)
                            break;
                        //
                        byte[] data;
                        var dataSize = res.Finish(out data);
                        m_TcpClient.GetStream().Write(data, 0, dataSize);
                        //
                        if (status == DispatchStatus.SendAndClose)
                            break;
                    }
                }
                catch (Exception e)
                {
                    Logger.Instance.Debug("{0}\n{1}", e.Message, e.StackTrace);

                }
                CloseAll();
                m_TcpClient = null;
            }

            protected override void StopCore()
            {
                CloseAll();
                m_NewClientAttachedEvent.Set();
            }

            private void CloseAll()
            {
                if (m_TcpClient != null && m_TcpClient.Connected)
                {
                    m_TcpClient.Close();
                }
            }

        }

        protected enum DispatchStatus
        {
            SendAndContinue,
            SendAndClose,
            Close
        }


        protected abstract DispatchStatus SafeDispatch(TCPBinaryReader request, TCPBinaryWriter response);
    }


    public abstract class TCPServerAutoDispatchBase<T> : TCPServerBase
        where T : class
    {
        private readonly Dictionary<uint, Action<T, TCPBinaryReader, TCPBinaryWriter>> m_DispatchMap =
            new Dictionary<uint, Action<T, TCPBinaryReader, TCPBinaryWriter>>();

        private bool m_InitializingDispatcher;

        protected TCPServerAutoDispatchBase()
        {
            DoInitializeDispatcher();
        }

        protected void AddDispatcher(string name, Action<T, TCPBinaryReader, TCPBinaryWriter> dispatcherFunc)
        {
            if (!m_InitializingDispatcher)
                throw new InvalidOperationException("AddDispatcher can be invoked only in Initializing stage");
            var hash = (uint)name.GetHashCode();
            hash &= 0x7FFFFFFF;
            m_DispatchMap.Add(hash, dispatcherFunc);
        }

        private void DoInitializeDispatcher()
        {
            m_InitializingDispatcher = true;
            try
            {
                InitializeDispatcher();
            }
            finally
            {
                m_InitializingDispatcher = false;
            }
        }

        protected override DispatchStatus SafeDispatch(TCPBinaryReader request, TCPBinaryWriter response)
        {
            try
            {
                DoDispatch(request, response);
                return DispatchStatus.SendAndContinue;
            }
            catch(InvalidProtocolException e)
            {
                response.WriteErrorResponse(InvalidProtocolException.CODE, e.Message);
                return DispatchStatus.SendAndClose;
            }
            catch(Exception e)
            {
                response.WriteErrorResponse(ServerInternalException.CODE, e.Message);
                return DispatchStatus.SendAndContinue;
            }
        }

        protected void DoDispatch(TCPBinaryReader request, TCPBinaryWriter response)
        {
            Action<T, TCPBinaryReader, TCPBinaryWriter> method;
            if (!m_DispatchMap.TryGetValue(request.Command, out method))
                throw new InvalidProtocolException("Invalid command code: " + request.Command);
            var service = CreateService();
            method(service, request, response);
        }

        protected abstract void InitializeDispatcher();
        protected abstract T CreateService();

    }
}
    