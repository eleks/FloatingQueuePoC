using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel;
using System.Text;

namespace FloatingQueue.Common.TCP
{
    public interface ITCPClient
    {
        void Initialize(EndpointAddress endpointAddress);
    }

    public abstract class TCPClientBase : TCPCommunicationObjectBase, ITCPClient
    {
        private string m_Host;
        private int m_Port;
        private TcpClient m_TcpClient;

        public void Initialize(EndpointAddress endpointAddress)
        {
            if (m_TcpClient != null)
                throw new InvalidOperationException("TCPClient is already initialized");
            m_Host = endpointAddress.Uri.Host;
            m_Port = endpointAddress.Uri.Port;
            m_TcpClient = new TcpClient();
            m_TcpClient.SendTimeout = 1000;
            m_TcpClient.ReceiveTimeout = 3000;
        }

        private bool IsConnected()
        {
            var client = m_TcpClient;
            return client != null && client.Client != null && client.Connected;
        }

        public override void Open()
        {
            if (IsConnected())
                throw new InvalidOperationException("Socket already openned");
            m_TcpClient.Connect(m_Host, m_Port);
        }

        private void OpenIfClosed()
        {
            if (!IsConnected())
                Open();
        }

        public override void Close()
        {
            if (IsConnected())
            {
                SendCloseCommand();
                m_TcpClient.Close();
            }
        }

        public override void Abort()
        {
            Close();
        }

        private int ReadBuffer(byte[] buf)
        {
            var stream = m_TcpClient.GetStream();
            var done = 0;
            try
            {
                while (done < buf.Length)
                {
                    var justRead = stream.Read(buf, done, buf.Length - done);
                    if (justRead == 0)
                        break;
                    done += justRead;
                }
            }
            catch(IOException)
            {
            }
            return done;
        }

        private void SendCloseCommand()
        {
            var request = new TCPBinaryWriter(TCPCommunicationSignature.Request, -1);
            byte[] data;
            var dataSize = request.Finish(out data);
            var stream = m_TcpClient.GetStream();
            stream.Write(data, 0, dataSize);
            stream.Flush();
        }


        protected TCPBinaryWriter CreateRequest(string command)
        {
            return new TCPBinaryWriter(TCPCommunicationSignature.Request, command.GetHashCode());
        }

        protected TCPBinaryReader SendReceive(TCPBinaryWriter request)
        {
            OpenIfClosed();
            //
            byte[] data; 
            var dataSize = request.Finish(out data);
            var stream = m_TcpClient.GetStream();
            stream.Write(data, 0, dataSize);
            stream.Flush();
            //
            var recvData = new TCPBinaryReader(TCPCommunicationSignature.Response, ReadBuffer);
            if (!recvData.IsComplete)
                throw new IOException("Incomplete response received");
            if (recvData.Command != request.Command)
                throw new IOException("Invalid response command");
            return recvData;
        }
    }
}
