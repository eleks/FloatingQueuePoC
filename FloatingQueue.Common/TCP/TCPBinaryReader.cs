using System;
using System.IO;
using System.Text;

namespace FloatingQueue.Common.TCP
{
    public class TCPBinaryReader
    {
        public static readonly int HeaderSize = 3*sizeof (int);

        public readonly int Command;
        public readonly bool IsComplete;

        private readonly MemoryStream m_Stream;
        private readonly BinaryReader m_Reader;

        public TCPBinaryReader(int expectedSignature, Func<byte[], int> dataReceiveCallback)
        {
            var header = new byte[HeaderSize];
            if (dataReceiveCallback(header) == header.Length)
            {
                var hdr = ParseHeader(header, expectedSignature);
                Command = hdr.Item1;
                var dataSize = hdr.Item2;
                //
                var data = new byte[dataSize];
                IsComplete = dataReceiveCallback(data) == data.Length;
                m_Stream = new MemoryStream(data);
                m_Reader = new BinaryReader(m_Stream);
            }
        }

        private static Tuple<int, int> ParseHeader(byte[] header, int expectedSignature)
        {
            if (header.Length != HeaderSize)
                throw new ArgumentException("Invalid header size");
            int dataSize;
            int command;
            using (var tempStream = new MemoryStream(header))
            {
                using (var tempReader = new BinaryReader(tempStream))
                {
                    var signature = tempReader.ReadInt32();
                    if (signature != expectedSignature)
                        throw new InvalidDataException("Invalid TCP packet signature");
                    dataSize = tempReader.ReadInt32();
                    command = tempReader.ReadInt32();
                }
            }
            return new Tuple<int, int>(command, dataSize - HeaderSize);
        }

        //
        public bool ReadBoolean()
        {
            byte b = m_Reader.ReadByte();
            return b == 0 ? false : true;
        }

        public int ReadInt32()
        {
            return m_Reader.ReadInt32();
        }

        public byte[] ReadBytes()
        {
            var sz = m_Reader.ReadInt32();
            return m_Reader.ReadBytes(sz);
        }

        public string ReadString()
        {
            var sz = m_Reader.ReadInt32();
            var data = m_Reader.ReadBytes(sz);
            return Encoding.UTF8.GetString(data);
        }

        public object ReadObject()
        {
            var typeName = ReadString();
            if (typeName == typeof(string).Name)
                return ReadString();
            if (typeName == typeof(byte[]).Name)
                return ReadBytes();
            throw new NotSupportedException("Serialization is not suuported for of object " + typeName);
        }

    }
}
    