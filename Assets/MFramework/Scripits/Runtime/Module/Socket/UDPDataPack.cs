using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MFramework
{
    public class UDPDataPack
    {
        /// <summary>
        /// С��װ����
        /// </summary>
        private class PacketAssembler
        {
            private readonly UInt16 _totalFrag;
            private readonly byte[][] _fragments;

            public bool isComplete { get; private set; }

            public PacketAssembler(UInt16 totalFrag)
            {
                _totalFrag = totalFrag;
                _fragments = new byte[totalFrag][];
            }

            public void AddFragment(UInt16 index, byte[] data)
            {
                if (_fragments[index] == null)
                {
                    _fragments[index] = data;
                }

                //����Ƿ�����С��������
                //�ڴ˴�ֻ����һ��
                isComplete = _fragments.All(fragment => fragment != null);
            }

            public byte[] Assemble()
            {
                //ֻ�����а��������װ��
                if (!isComplete)
                {
                    return null;
                }

                //ƴ�����з�Ƭ(ʹ���ڴ���)
                using (var ms = new MemoryStream())
                {
                    foreach (var fragment in _fragments)
                    {
                        ms.Write(fragment, 0, fragment.Length);
                    }
                    return ms.ToArray();
                }
            }
        }

        private static Dictionary<UInt16, PacketAssembler> PacketPool = new Dictionary<UInt16, PacketAssembler>();

        public static int MAX_SEG_LEN = 1024;//�������ܳ�
        public static int HEAD_PACKETID_LEN = 2;//��ID
        public static int HEAD_FRAGINDEX_LEN = 2;//��ǰ��Ƭ����
        public static int HEAD_TOTALFRAG_LEN = 2;//�ܷ�Ƭ��
        public static int HEAD_DATA_LEN = 2;//���ݳ���(UDP���ݲ���������2bytes�㹻)
        public static int HEAD_TYPE_LEN = 2;//��������
        /// <summary>
        /// ��ͷ�ܳ�
        /// </summary>
        public static int HEAD_LEN
        {
            get { return HEAD_PACKETID_LEN + HEAD_FRAGINDEX_LEN + 
                    HEAD_TOTALFRAG_LEN + HEAD_DATA_LEN + HEAD_TYPE_LEN; }
        }
        /// <summary>
        /// ��������ܳ�
        /// </summary>
        public static int MAX_DATA_LEN
        {
            get { return MAX_SEG_LEN - HEAD_LEN; }
        }

        public UInt16 ID;
        public UInt16 TotalFrag;
        public UInt16 Type;
        public byte[] Data;//������

        public int DataLength
        {
            get { return Data.Length; }
        }

        public List<byte[]> Packets;//����������(������ͷ)

        public UDPDataPack() { }
        public UDPDataPack(UInt16 type, byte[] data)
        {
            Type = type;
            Data = data;

            CreatePackets(Type, Data);
        }

        private void CreatePackets(UInt16 type, byte[] data)
        {
            if (Packets != null) return;
            Packets = new List<byte[]>();

            int maxDataSize = MAX_DATA_LEN;//�����
            if (data.Length == 0) TotalFrag = 1;//��Ƭ��---����Event�����ֻ��1��С��
            else TotalFrag = (UInt16)((data.Length - 1 + maxDataSize) / maxDataSize);//��Ƭ��---һ�����
            ID = GenerateID();

            //����ÿһ��С��
            for (int i = 0; i < TotalFrag; i++)
            {
                int offset = i * maxDataSize;
                //С����(ǰ������ΪmaxDataSize�����һ����Ϊdata.Length - offset����ʣ�೤��)
                int length = Math.Min(maxDataSize, data.Length - offset);

                byte[] packetData = new byte[length];
                Array.Copy(data, offset, packetData, 0, length);

                var packet = CreatePacket(packetData, ID, (UInt16)i, TotalFrag, type);
                Packets.Add(packet);
            }
        }

        private byte[] CreatePacket(byte[] packetData, UInt16 id, UInt16 index, UInt16 totalFrag, UInt16 type)
        {
            //��ͷ˳�����£�
            //0-2   2-4       4-6     6-8     8-10      10-N
            //ID    С������   �ܰ���   С����   ��������   ����               
            byte[] packet = new byte[packetData.Length + HEAD_LEN];

            byte[] temp;
            temp = BitConverter.GetBytes(id);
            Array.Copy(temp, 0, packet, 0, 2);
            temp = BitConverter.GetBytes(index);
            Array.Copy(temp, 0, packet, 2, 2);
            temp = BitConverter.GetBytes(totalFrag);
            Array.Copy(temp, 0, packet, 4, 2);
            temp = BitConverter.GetBytes((UInt16)packetData.Length);
            Array.Copy(temp, 0, packet, 6, 2);
            temp = BitConverter.GetBytes(type);
            Array.Copy(temp, 0, packet, 8, 2);

            //Data
            Array.Copy(packetData, 0, packet, 10, packetData.Length);

            return packet;
        }

        public static UDPDataPack Unpack(byte[] buff, out int packetLength)
        {
            packetLength = -1;
            try
            {
                //��δ�����������
                if (buff.Length < HEAD_LEN) return null;

                UInt16 id = BitConverter.ToUInt16(buff, 0);
                UInt16 index = BitConverter.ToUInt16(buff, 2);
                UInt16 totalFrag = BitConverter.ToUInt16(buff, 4);
                UInt16 dataLength = BitConverter.ToUInt16(buff, 6);
                UInt16 type = BitConverter.ToUInt16(buff, 8);

                packetLength = dataLength + HEAD_LEN;
                if (buff.Length < packetLength) return null;//����������

                //��ȡ��
                byte[] packetData = new byte[dataLength];
                Array.Copy(buff, HEAD_LEN, packetData, 0, dataLength);

                //��С��ת��װ����
                if (!PacketPool.ContainsKey(id))
                {
                    PacketPool.Add(id, new PacketAssembler(totalFrag));
                }
                var assembler = PacketPool[id];
                assembler.AddFragment(index, packetData);
                //���ռ�ȫ��С���������������
                if (assembler.isComplete)
                {
                    var data = assembler.Assemble();
                    var dataPack = new UDPDataPack(type, data);

                    PacketPool.Remove(id);
                    IDTable.Remove(id);

                    return dataPack;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static HashSet<int> IDTable = new HashSet<int>();
        private static Random random = new Random();
        private static UInt16 GenerateID()
        {
            UInt16 value = (UInt16)random.Next(0, UInt16.MaxValue);
            if (IDTable.Contains(value))
            {
                value = GenerateID();
            }
            else
            {
                IDTable.Add(value);
            }

            return value;
        }
    }
}
