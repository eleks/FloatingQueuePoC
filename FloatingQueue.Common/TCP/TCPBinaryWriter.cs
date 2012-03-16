using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FloatingQueue.Common.TCP
{
    public class TCPBinaryWriter
    {
        public readonly int Command;
        private readonly MemoryStream m_Stream;
        private BinaryWriter m_Writer;
        private int m_Size;

        public TCPBinaryWriter(int signature, int command)
        {
            Command = command;
            //
            m_Stream = new MemoryStream();
            m_Writer = new BinaryWriter(m_Stream);
            Write(signature);
            const int size = 0;
            Write(size); // reserve place for packet size
            Write(Command);
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
