using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Timers;

namespace MFramework
{
    public class MUDPServer : MUDPServerBase
    {
        public event Action<EndPoint> OnConnect;
        public event Action<EndPoint> OnDisconnect;
        public event Action<EndPoint, UDPDataPack> OnReceive;
        public event Action<EndPoint, UDPDataPack> OnSend;

        public Dictionary<EndPoint, UDPClientSocketInfo> ClientInfoDic =
            new Dictionary<EndPoint, UDPClientSocketInfo>();

        private const int HEAD_CHECKTIME = 5000;//��������ʱ���Ƶ��

        private System.Timers.Timer _headCheckTimer;

        public MUDPServer(string ip, int port) : base(ip, port)
        {
            StartHeadCheckTimer();
        }
        public MUDPServer(IPEndPoint ep) : base(ep)
        {
            StartHeadCheckTimer();
        }

        //===����===
        public override void Open()
        {
            base.Open();
            if (_headCheckTimer == null)
            {
                StartHeadCheckTimer();
            }
        }
        private void StartHeadCheckTimer()
        {
            //��������ʱ���
            _headCheckTimer = new System.Timers.Timer(HEAD_CHECKTIME);
            _headCheckTimer.AutoReset = true;
            _headCheckTimer.Elapsed += delegate (object sender, ElapsedEventArgs args)
            {
                CheckHeadTimeOut();
            };
            _headCheckTimer.Start();
        }

        //=====����=====
        protected override void ReceiveData()
        {
            //1024---С����·�㸺��(1472)��ֵ
            byte[] bytes = new byte[1024];//��������С
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
                    if (!ClientInfoDic.ContainsKey(endPoint))//���Ӵ���
                    {
                        byte[] verificationBytes = new byte[4] { 18, 203, 59, 38 };
                        byte[] receviedBytes = new byte[4];
                        Array.Copy(bytes, 0, receviedBytes, 0, 4);
                        if (receviedBytes.SequenceEqual(verificationBytes))//��֤ͨ��
                        {
                            //�ͻ������ӻ�Ӧ
                            byte[] buff = new byte[1] { 1 };
                            _server.SendTo(buff, endPoint);
                            //ͨ�������
                            MainThreadUtility.Post<EndPoint>(OnConnect, endPoint);//OnConnect�ص�
                            ClientInfoDic.Add(endPoint, new UDPClientSocketInfo()
                            {
                                Client = endPoint,
                                DataBuffer = new DataBuffer(),
                                HeadTime = MTimeUtility.GetNowTime()
                            });

                            MLog.Print($"{typeof(MUDPServer)}���ͻ���<{endPoint}>������");
                        }
                    }
                    else//һ�㴦��
                    {
                        //���ݼ��뻺������(���ݿ��ܷ�������Ҳ����ͬʱ������)
                        ClientInfoDic[endPoint].DataBuffer.AddBuffer(bytes, len);
                        //��ȡ����(�����ȡ)
                        TryUnpack(endPoint);
                    }
                }

                //������������
                ReceiveData();
            }
            catch (SocketException)
            {

            }
        }
        private void ReceiveHead(EndPoint client)
        {
            if (ClientInfoDic.TryGetValue(client, out var info))
            {
                long now = MTimeUtility.GetNowTime();
                long offset = now - info.HeadTime;
                MLog.Print($"�ͻ���<{client}>����������ʱ��� >>>{now}    ��� >>>{offset}");

                if (offset > HEAD_CHECKTIME)
                {
                    //��ʱ(��ʱ�߳��߼�����������ʱ�����ʵ��)
                }
                info.HeadTime = now;//���ģ�����ʱ��
            }
        }
        private void ReceiveCloseRequest(EndPoint client) 
        {
            CloseClient(client);
            SendEvent(client, SocketEvent.S2C_DISCONNECTREPLY);
        }
        private void ReceiveCloseReply(EndPoint client)
        {
            CloseClient(client);
        }

        private void TryUnpack(EndPoint ep)
        {
            //����������а�(�������⵼�µĻ�ѹ)
            if (ClientInfoDic[endPoint].DataBuffer.haveBuff)
            {
                var dataPack = new UDPDataPack();
                if (ClientInfoDic[endPoint].DataBuffer.TryUnpack(out dataPack))
                {
                    //������
                    if (dataPack.Type == (UInt16)SocketEvent.C2S_HEAD)
                    {
                        ReceiveHead(endPoint);
                    }
                    //�رհ�(�ͻ�������ر�)
                    else if (dataPack.Type == (UInt16)SocketEvent.C2S_DISCONNECTREQUEST)
                    {
                        ReceiveCloseRequest(endPoint);
                    }
                    //�رհ�(�ͻ��˹رջظ�)
                    else if (dataPack.Type == (UInt16)SocketEvent.C2S_DISCONNECTREPLY)
                    {
                        ReceiveCloseReply(endPoint);
                    }
                    else
                    {
                        MainThreadUtility.Post<EndPoint, UDPDataPack>(OnReceive, ep, dataPack);//OnReceive�ص�
                    }
                }
            }
        }

        //=====���������=====
        private void CheckHeadTimeOut()
        {
            foreach (var ep in ClientInfoDic.Keys)
            {
                var info = ClientInfoDic[ep];
                long now = MTimeUtility.GetNowTime();
                long offset = now - info.HeadTime;
                if (offset > HEAD_CHECKTIME)
                {
                    //��������ʱ
                    KickOut(ep);
                }
            }
        }



        //=====����=====
        public void SendUTF(EndPoint endPoint, SocketEvent type, string message, Action<EndPoint, UDPDataPack> onTrigger = null)
        {
            byte[] buff = Encoding.UTF8.GetBytes(message);
            UDPSendContext context = new UDPSendContext() { EndPoint = endPoint, Type = (ushort)type, Buff = buff };

            Send(context, onTrigger);
        }
        public void SendASCII(EndPoint endPoint, SocketEvent type, string message, Action<EndPoint, UDPDataPack> onTrigger = null)
        {
            byte[] buff = Encoding.ASCII.GetBytes(message);
            UDPSendContext context = new UDPSendContext() { EndPoint = endPoint, Type = (ushort)type, Buff = buff };

            Send(context, onTrigger);
        }
        public void SendBytes(EndPoint endPoint, SocketEvent type, byte[] buff, Action<EndPoint, UDPDataPack> onTrigger = null)
        {
            UDPSendContext context = new UDPSendContext() { EndPoint = endPoint, Type = (ushort)type, Buff = buff };

            Send(context, onTrigger);
        }
        public void SendEvent(EndPoint endPoint, SocketEvent type, Action<EndPoint, UDPDataPack> onTrigger = null)
        {
            UDPSendContext context = new UDPSendContext() { EndPoint = endPoint, Type = (ushort)type, Buff = null };

            Send(context, onTrigger);
        }
        protected override void Send(UDPSendContext context, Action<EndPoint, UDPDataPack> onTrigger)
        {
            //��ɰ���ȡ��Buff
            context.Buff = context.Buff ?? new byte[] { };
            var dataPack = new UDPDataPack(context.Type, context.Buff);

            foreach (var packet in dataPack.Packets)
            {
                //����Buff
                _server.BeginSendTo(packet, 0, packet.Length, SocketFlags.None, context.EndPoint, new AsyncCallback((asyncSend) =>
                {
                    Socket c = (Socket)asyncSend.AsyncState;
                    c.EndSend(asyncSend);

                    MainThreadUtility.Post<EndPoint, UDPDataPack>(onTrigger, endPoint, dataPack);
                    MainThreadUtility.Post<EndPoint, UDPDataPack>(OnSend, endPoint, dataPack);//OnSend�ص�
                }), _server);
            }
        }
        protected override void Send(UDPSendContext context, Action<EndPoint, byte[]> onTrigger = null)
        {
            throw new NotSupportedException();
        }



        //=====����=====
        protected override void OnCloseInternal()
        {
            if (ClientInfoDic.Keys.Count == 0) return;

            //�ȶ����пͻ��˽��ж�������(���Ͷ�������)
            foreach (var ep in ClientInfoDic.Keys)
            {
                SendEvent(ep, SocketEvent.S2C_DISCONNECTREQUEST);
            }

            isWaiting = true;//Э�̵ȴ�

            //�������ȴ�5�� �� �յ����лر�
            //Tip��ֻ��ʹ���̣߳���OnApplicationQuit()ʱЭ����ʧЧ
            ThreadPool.QueueUserWorkItem(_ =>
            {
                Check();
            });
        }

        private void Check()
        {
            int elapsed = 0;
            while (elapsed < 5000)//�ȴ�5��
            {
                if (ClientInfoDic.Keys.Count == 0)
                {
                    break;
                }

                Thread.Sleep(100);//ÿ100ms���һ��
                elapsed += 100;
            }

            CloseInternal();
        }
        private void CloseInternal()
        {
            ClientInfoDic = null;

            if (_headCheckTimer != null)
            {
                _headCheckTimer.Stop();
                _headCheckTimer = null;
            }

            isWaiting = false;//����ִ��
        }

        public void KickOutAll()
        {
            foreach (var ep in ClientInfoDic.Keys)
            {
                KickOut(ep);
            }
        }
        public void KickOut(EndPoint client)
        {
            SendEvent(client, SocketEvent.S2C_KICKOUT, (ep, dataPack) =>
            {
                CloseClient(client);
            });
        }

        private void CloseClient(EndPoint ep)
        {
            //Tip��Ϊprivate�����������������Ͽ���ͻ��˵���ϵ�����Ƿ������(��������/�ͻ�������Ҫ��)
            MainThreadUtility.Post<EndPoint>((socket) =>
            {
                MLog.Print($"�������Ͽ���ͻ���<{ep}>������");

                try
                {
                    OnDisconnect?.Invoke(ep);
                    ClientInfoDic.Remove(ep);
                }
                catch { }
            }, ep);
        }
    }
}
