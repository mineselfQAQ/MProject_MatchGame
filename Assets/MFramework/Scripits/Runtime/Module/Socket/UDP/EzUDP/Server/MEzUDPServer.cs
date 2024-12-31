using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MFramework
{
    /// <summary>
    /// ���װ�UDP�����������ṩ�κζ��๦�ܣ���֧��ֱ�Ӵ���
    /// </summary>
    public class MEzUDPServer : MUDPServerBase
    {
        public MEzUDPServer(string ip, int port) : base(ip, port)
        {
            //һ���Դ��䣬����Ϊ���
            _server.SendBufferSize = 65507;
            _server.ReceiveBufferSize = 65507;
        }
        public MEzUDPServer(IPEndPoint ep) : base(ep)
        {
            //һ���Դ��䣬����Ϊ���
            _server.SendBufferSize = 65507;
            _server.ReceiveBufferSize = 65507;
        }

        public event Action<EndPoint, string> OnReceive;
        public event Action<EndPoint, byte[]> OnSend;

        protected override void ReceiveData()
        {
            //UDPֻҪ���ͳ���������ʧ�ܣ���������Ϊ���ֵ
            byte[] bytes = new byte[64 * 1024 - 20 - 8];//��󻺳�����С
            _server.BeginReceiveFrom(bytes, 0, bytes.Length, SocketFlags.None, ref endPoint, new AsyncCallback(OnReceiveData), bytes);
        }
        private void OnReceiveData(IAsyncResult result)
        {
            try
            {
                byte[] bytes = (byte[])result.AsyncState;
                int len = _server.EndReceiveFrom(result, ref endPoint);
                if (len > 0)
                {
                    string message = Encoding.UTF8.GetString(bytes);

                    MLog.Print($"�յ����Կͻ���<{endPoint}>����Ϣ��{message}");
                    MainThreadUtility.Post<EndPoint, string>(OnReceive, endPoint, message);//OnReceive�ص�
                }
            }
            catch (SocketException ex)
            {
                MLog.Print("���ݽ���ʧ�ܣ�" + ex.Message, MLogType.Warning);
            }
            finally
            {
                //������������
                ReceiveData();
            }
        }

        public void SendUTF(EndPoint endPoint, string message, Action<EndPoint, byte[]> onTrigger = null)
        {
            byte[] buff = Encoding.UTF8.GetBytes(message);
            UDPSendContext context = new UDPSendContext() { EndPoint = endPoint, Buff = buff };

            Send(context, onTrigger);
        }
        public void SendASCII(EndPoint endPoint, string message, Action<EndPoint, byte[]> onTrigger = null)
        {
            byte[] buff = Encoding.ASCII.GetBytes(message);
            UDPSendContext context = new UDPSendContext() { EndPoint = endPoint, Buff = buff };

            Send(context, onTrigger);
        }
        public void SendBytes(EndPoint endPoint, byte[] buff, Action<EndPoint, byte[]> onTrigger = null)
        {
            UDPSendContext context = new UDPSendContext() { EndPoint = endPoint, Buff = buff };

            Send(context, onTrigger);
        }
        public void SendEvent(EndPoint endPoint, Action<EndPoint, byte[]> onTrigger = null)
        {
            UDPSendContext context = new UDPSendContext() { EndPoint = endPoint, Buff = null };

            Send(context, onTrigger);
        }
        protected override void Send(UDPSendContext context, Action<EndPoint, byte[]> onTrigger)
        {
            try
            {
                //����Buff
                _server.BeginSendTo(context.Buff, 0, context.Buff.Length, SocketFlags.None, context.EndPoint, new AsyncCallback((asyncSend) =>
                {
                    Socket c = (Socket)asyncSend.AsyncState;
                    c.EndSend(asyncSend);

                    MainThreadUtility.Post<EndPoint, byte[]>(onTrigger, context.EndPoint, context.Buff);
                    MainThreadUtility.Post<EndPoint, byte[]>(OnSend, context.EndPoint, context.Buff);//OnSend�ص�
                }), _server);
            }
            catch (SocketException ex)
            {
                MLog.Print(ex);
            }
        }
        protected override void Send(UDPSendContext context, Action<EndPoint, UDPDataPack> onTrigger = null) 
        {
            throw new NotSupportedException();
        }
    }
}
