using System.Net.Sockets;
using System.Threading;

namespace MFramework
{
    //Tip��������class���������Ͳ��ܼ򵥸�ֵ����������info.HeadTime = time;û��ref�޷���ֵ
    public class TCPClientSocketInfo
    {
        public Socket Client;//�����ͻ���
        public Thread ReceiveThread;//�����߳�
        public long HeadTime;//������ʱ���
    }
}