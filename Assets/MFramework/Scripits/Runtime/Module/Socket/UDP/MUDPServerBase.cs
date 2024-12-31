using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MFramework
{
    public abstract class MUDPServerBase
    {
        public string IP;//������IP
        public int Port;//������Port
        public EndPoint EP;//������EP

        protected Socket _server;
        protected EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);//����ͻ���EndPoint

        protected bool isWaiting = false;

        public bool isValid { get; private set; }

        /// <summary>
        /// �������ر�ʱ�ص���ע�⣺�����ӳٲ������뽫isWaiting����Ϊtrue����ɺ���Ϊfalse
        /// </summary>
        protected virtual void OnCloseInternal() { }

        protected abstract void Send(UDPSendContext context, Action<EndPoint, UDPDataPack> onTrigger);//ͨ������
        protected abstract void Send(UDPSendContext context, Action<EndPoint, byte[]> onTrigger);//EZ����
        protected abstract void ReceiveData();

        public MUDPServerBase(string ip, int port)
        {
            IP = ip;
            Port = port;
            EP = new IPEndPoint(IPAddress.Parse(ip), port);

            //InitSettings((IPEndPoint)EP);
        }
        public MUDPServerBase(IPEndPoint ep)
        {
            EP = ep;
            IP = ep.Address.ToString();
            Port = ep.Port;

            //InitSettings(ep);
        }

        private void InitSettings(IPEndPoint ep)
        {
            _server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _server.Bind(ep);

            isValid = true;
            MLog.Print($"{typeof(MUDPServerBase)}��������<{EP}>�ѿ�ʼ����");

            ReceiveData();
        }

        public virtual void Open()
        {
            InitSettings((IPEndPoint)EP);
        }

        public void Close()
        {
            string ep = EP.ToString();

            if (!isValid)
            {
                MLog.Print($"{typeof(MUDPServerBase)}��������<{ep}>�ѹرգ��������¹ر�", MLogType.Warning);
                return;
            }

            OnCloseInternal();

            //Tip��ֻ��ʹ���̣߳���OnApplicationQuit()ʱЭ����ʧЧ
            ThreadPool.QueueUserWorkItem(_ =>
            {
                WaitForDisconnect();
            });
        }

        private void WaitForDisconnect()
        {
            //���OnCloseInternal()������isWaiting=true�����ѭ���ȴ���ΪisWaiting=false
            while (isWaiting)
            {
                Thread.Sleep(100);//100ms���һ��
            }

            _server.Close();
            _server = null;

            isValid = false;
            MLog.Print($"{typeof(MUDPServerBase)}���������ѹر�");
        }
    }
}
