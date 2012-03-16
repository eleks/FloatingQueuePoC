using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using FloatingQueue.Common.Common;

namespace FloatingQueue.Common.TCP
{
    public interface ITCPServer
    {
        void Initialize(string address);
    }



    public abstract class TCPServerBase : TCPCommunicationObjectBase, ITCPServer
    {
        public int Port { get; private set; }
        private ConnectionListenerThread m_ListenerThread;

        public void Initialize(string address)
        {
            var uri = new Uri(address);
            Port = uri.Port;
            //
            m_ListenerThread = new ConnectionListenerThread(this);
        }

        public abstract bool Dispatch(TCPBinaryReader request, TCPBinaryWriter response);

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
            //private readonly List<ThreadBase> m_IdleThreads = new List<ThreadBase>();

            public ConnectionListenerThread(TCPServerBase server)
            {
                m_Server = server;
                m_Listener = new TcpListener(IPAddress.Any, m_Server.Port);
            }

            protected override void DoRun()
            {
                m_Listener.Start(100);
                DbgLogger.WriteLine("Listening");
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
                        DbgLogger.WriteLine("  Socket listener: " + e.Message);
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

            protected override void DoStop()
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
                while (done < buf.Length)
                {
                    var justRead = m_TcpClient.GetStream().Read(buf, done, buf.Length - done);
                    if (justRead == 0)
                        break;
                    done += justRead;
                }
                return done;
            }


            protected override void DoRun()
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
                        if (req.Command == -1)
                        {
                            break;
                        }

                        var res = new TCPBinaryWriter(TCPCommunicationSignature.Response, req.Command);
                        var isOk = m_Server.Dispatch(req, res);
                        if (!isOk)
                        {
                            break;
                        }
                        byte[] data;
                        var dataSize = res.Finish(out data);
                        m_TcpClient.GetStream().Write(data, 0, dataSize);
                    }
                }
                catch (Exception e)
                {
                    DbgLogger.LogException(e);

                }
                CloseAll();
                m_TcpClient = null;
            }

            protected override void DoStop()
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

    }
}
