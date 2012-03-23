using System;
using System.IO;
using System.Net.Sockets;
using System.ServiceModel;
using System.Threading;
using FloatingQueue.Common.Proxy;

namespace FloatingQueue.Common.TCP
{
    public interface ITCPClient
    {
        void Initialize(EndpointAddress endpointAddress);
    }

    public abstract class TCPClientBase : CommunicationObjectBase, ITCPClient
    {
        private string m_Host;
        private int m_Port;
        private TcpClient m_TcpClient;
        private object m_Lock = new object();

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
            lock (m_Lock)
            {
                if (IsConnected())
                    throw new InvalidOperationException("Socket already openned");
                m_TcpClient.Connect(m_Host, m_Port);
            }
        }

        private void OpenIfClosed()
        {
            if (!IsConnected())
                Open();
        }

        public override void Close()
        {
            lock (m_Lock)
            {
                if (IsConnected())
                {
                    SendCloseCommand();
                    m_TcpClient.Close();
                }
            }
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
            lock (m_Lock)
            {
                var request = new TCPBinaryWriter(TCPCommunicationSignature.Request, TCPCommunicationSignature.CmdClose);
                byte[] data;
                var dataSize = request.Finish(out data);
                var stream = m_TcpClient.GetStream();
                stream.Write(data, 0, dataSize);
                stream.Flush();
            }
        }


        protected TCPBinaryWriter CreateRequest(string command)
        {
            var hash = (uint) command.GetHashCode();
            hash &= 0x7FFFFFFF;
            return new TCPBinaryWriter(TCPCommunicationSignature.Request, hash);
        }

        protected TCPBinaryReader SendReceive(TCPBinaryWriter request)
        {
            lock (m_Lock)
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
                if (recvData.Command == TCPCommunicationSignature.CmdException)
                {
                    throw HandleErrorResponse(recvData);
                }
                if (recvData.Command != request.Command && recvData.Command != TCPCommunicationSignature.CmdException)
                    throw new InvalidProtocolException("Invalid response command");
                return recvData;
            }
        }

        protected Exception HandleErrorResponse(TCPBinaryReader response)
        {
            if (response.Command != 0x80000000)
                throw new InvalidOperationException("Call to HandleErrorResponse for non-error response");
            
            var exceptionClassCode = response.ReadInt32();
            var msg = response.ReadString();
            switch(exceptionClassCode)
            {
                case ServerInternalException.CODE: return new ServerInternalException(msg);
                case ServerInvalidArgumentException.CODE: return new ServerInvalidArgumentException(msg);
                case ServerStreamAlreadyChangedException.CODE: return new ServerStreamAlreadyChangedException(msg);
                case InvalidProtocolException.CODE: return new InvalidProtocolException(msg);
                default:
                    return new NotSupportedException("Unknown server exception code: " + exceptionClassCode);
            }
        }
    }
}
