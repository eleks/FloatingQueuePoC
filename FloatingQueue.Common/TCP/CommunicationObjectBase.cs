using System;
using System.ServiceModel;

namespace FloatingQueue.Common.TCP
{
    public abstract class CommunicationObjectBase : IDisposable
    {
        public abstract void Close();
        public abstract void Open();

        public void Dispose()
        {
            Close();
        }
    }


    public static class TCPCommunicationSignature
    {
        public static readonly uint Request = 0x34567890;
        public static readonly uint Response = 0x67234519;
        public static readonly uint EndOfStream = 0xFFEEDDBB;

        //
        public static readonly uint CmdClose = 0xF0000000;
        public static readonly uint CmdException = 0x80000000;
    }


}
