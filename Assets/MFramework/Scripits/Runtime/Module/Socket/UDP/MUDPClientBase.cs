using System.Net.Sockets;
using System.Net;
using System;

namespace MFramework
{
    public abstract class MUDPClientBase
    {
        protected Socket _client;
        public EndPoint serverEP { get; protected set; }//��������ַ

        /// <summary>
        /// �ͻ��˹ر�ʱ�ص�
        /// </summary>
        protected virtual void OnCloseInternal() { }

        protected abstract void Send(UDPSendContext context, Action<UDPDataPack> onTrigger);
        protected abstract void Send(UDPSendContext context, Action<byte[]> onTrigger);

        public MUDPClientBase(string ip, int port)
        {
            //��������������
            var ep = new IPEndPoint(IPAddress.Parse(ip), port);

            InitSettings(ep);
        }
        public MUDPClientBase(IPEndPoint ep)
        {
            InitSettings(ep);
        }

        private void InitSettings(IPEndPoint ep)
        {
            serverEP = ep;
            _client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }

        public void Close()
        {
            if (_client != null)
            {
                OnCloseInternal();

                _client.Close();
                _client = null;
            }
        }
    }
}
