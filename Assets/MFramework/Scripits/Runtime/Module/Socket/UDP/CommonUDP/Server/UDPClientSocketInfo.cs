using System.Net;

namespace MFramework
{
    //Tip��������class���������Ͳ��ܼ򵥸�ֵ����������info.HeadTime = time;û��ref�޷���ֵ
    public class UDPClientSocketInfo
    {
        public EndPoint Client;//�����ͻ���
        public DataBuffer DataBuffer;//���ݻ���
        public long HeadTime;//������ʱ���
    }
}