using System;
using System.IO;
using System.Text;

namespace FloatingQueue.Common.TCP
{
    public class TCPBinaryWriter
    {
        public readonly uint Command;
        private readonly uint m_Signature;
        private readonly MemoryStream m_Stream;
        private BinaryWriter m_Writer;
        private int m_Size;

        public TCPBinaryWriter(uint signature, uint command)
        {
            Command = command;
            m_Signature = signature;
            //
            m_Stream = new MemoryStream();
            m_Writer = new BinaryWriter(m_Stream);
            WriteHeader(command);
        }

        private void WriteHeader(uint command)
        {
            m_Writer.Seek(0, SeekOrigin.Begin);
            m_Size = 0;
            Write(m_Signature);
            const int size = 0;
            Write(size); // reserve place for packet size
            Write(command);
        }

        private void EnsureWriteAllowed()
        {
            if (m_Writer == null)
                throw new InvalidOperationException("Could not write to finalized TCPRequest");
        }

        public int Finish(out byte[] data)
        {
            EnsureWriteAllowed();
            m_Writer.Flush();
            m_Writer.Seek(4, SeekOrigin.Begin);
            m_Writer.Write(m_Size);
            m_Writer.Close();
            m_Writer = null;
            //
            data = m_Stream.GetBuffer();
            return m_Size;
        }

        #region Write Methods

        public void WriteErrorResponse(int code, string message)
        {
            EnsureWriteAllowed();
            WriteHeader(TCPCommunicationSignature.CmdException);
            Write(code);
            Write(message);
        }

        public void Write(bool value)
        {
            EnsureWriteAllowed();
            byte v = value ? (byte)1 : (byte)0;
            m_Writer.Write(v);
            m_Size += sizeof(byte);
        }

        public void Write(int value)
        {
            EnsureWriteAllowed();
            m_Writer.Write(value);
            m_Size += sizeof(int);
        }

        public void Write(uint value)
        {
            EnsureWriteAllowed();
            m_Writer.Write(value);
            m_Size += sizeof(uint);
        }

        public void Write(byte[] data)
        {
            EnsureWriteAllowed();
            m_Writer.Write(data.Length);
            m_Writer.Write(data);
            m_Size += sizeof(int) + data.Length;
        }

        public void Write(string value)
        {
            EnsureWriteAllowed();
            var data = Encoding.UTF8.GetBytes(value);
            m_Writer.Write(data.Length);
            m_Writer.Write(data);
            m_Size += sizeof(int) + data.Length;
        }

        public void WriteObject(object obj)
        {
            var type = obj.GetType();
            Write(type.Name);
            if (type == typeof(string))
                Write((string)obj);
            else if (type == typeof(byte[]))
                Write((byte[])obj);
            else
                throw new NotSupportedException("Serialization is not suuported for of object " + type.Name);
        }

        #endregion 
    }
}
