using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace ConcentrationDetectionSys.DAL
{
    public class ModbusTcp
    {
        TcpClient tcpClient;
        private string serverIP;
        private int serverPort;
        private byte[] buffer = new byte[1024];
        private readonly object lockObj = new object();
        private int sendIndex = 0;

        //
        private readonly System.Timers.Timer _reconnectTimer;
        //private readonly object _reconnectLock = new object();
        private bool _isReconnecting = false;
        // 重连次数控制
        private int _retryCount = 0;
        private const int _maxRetry = 5;
        private bool _disposed = false;

        /// <summary>
        /// 连接断开标志位，true表示连接断开，false表示连接正常
        /// </summary>
        private bool _isDisconnectFlag = false;
        public bool _OnReconnectAlarmFlag = false;

        public bool connected
        {
            get
            {
                if (tcpClient == null || tcpClient.Client == null || !tcpClient.Client.Connected)
                {
                    return false;
                }
                if (_isDisconnectFlag)
                    return false;
                
                return tcpClient.Connected;
                

            }
        }
        public ModbusTcp(string serverIP, int serverPort)
        {
            this.serverIP = serverIP;
            this.serverPort = serverPort;

            tcpClient = new TcpClient();

            tcpClient.SendTimeout = 300;
            tcpClient.ReceiveTimeout = 300;

            _reconnectTimer = new System.Timers.Timer(1000)
            {
                AutoReset = true
            };
            _reconnectTimer.Elapsed += ReconnectTimer_Elapsed;
            _reconnectTimer.Start();
        }

        //public bool Connect()
        //{
        //    if (!tcpClient.Connected)
        //    {
        //        try
        //        {
        //            Close();
                    
        //            tcpClient = new TcpClient(serverIP, serverPort);
        //        }
        //        catch
        //        {
        //            return false;
        //        }
        //    }
        //    return tcpClient.Connected;
        //}
        public bool Connect()
        {
            lock (lockObj) // Connect加同一把锁
            {
                if (tcpClient != null && tcpClient.Connected)
                {
                    _isDisconnectFlag = false;
                    return true;
                }
                // 先安全释放旧实例
                if (tcpClient != null)
                {
                    try
                    {
                        tcpClient.Close();
                        tcpClient.Dispose();
                    }
                    catch { }
                }
                tcpClient = null;

                try
                {
                    var newClient = new TcpClient();
                    newClient.SendTimeout = 300;
                    newClient.ReceiveTimeout = 300;
                    newClient.Connect(serverIP, serverPort);
                    tcpClient = newClient;
                    _isDisconnectFlag = false;
                    _retryCount = 0;
                    _OnReconnectAlarmFlag = false;
                    return true;
                }
                catch
                {
                    tcpClient = new TcpClient(); // 空实例，标记未连接
                    _isDisconnectFlag = true;
                    return false;
                }
            }
        }
        public void Close()
        {
            lock (lockObj)
            {
                if (tcpClient != null)
                {
                    try
                    {
                        tcpClient.Close();
                        tcpClient.Dispose();
                    }
                    catch { }
                    tcpClient = null;
                }
                _isDisconnectFlag = true;
            }
        }
        //private void HeartbeatConnect()
        //{
        //    for (int i = 0; i < 3; i++)
        //    {
        //        Connect();
        //        if (connected)
        //        {
        //            break;
        //        }
        //    }
        //}
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                // 顺序：解绑事件 → Stop → Dispose
                if (_reconnectTimer != null)
                {
                    _reconnectTimer.Elapsed -= ReconnectTimer_Elapsed;
                    _reconnectTimer.Stop();
                    _reconnectTimer.Dispose();
                }
                Close();
                tcpClient?.Dispose();
            }
            _disposed = true;
        }
        private void ReconnectTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (lockObj)
            {
                if (_disposed) return; // 已经释放直接退出
                if (_isReconnecting)//如果正在重连，直接返回
                    return;
                if (_OnReconnectAlarmFlag)//如果已经触发过重连报警，直接返回
                    return;
                if (_isReconnecting) return;
                _isReconnecting = true;
                try
                {
                    Close();
                    tcpClient = new TcpClient
                    {
                        SendTimeout = 300,
                        ReceiveTimeout = 300
                    };
                    bool ok = Connect();
                    if (ok)
                    {
                        _isDisconnectFlag = false;
                        _retryCount = 0;
                        _OnReconnectAlarmFlag = false;
                    }
                    else
                    {
                        _retryCount++;
                        if (_retryCount >= _maxRetry)
                        {
                            _OnReconnectAlarmFlag = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _retryCount++;
                    if (_retryCount >= _maxRetry)
                    {
                        _OnReconnectAlarmFlag = true;
                    }
                }
                finally
                {
                    _isReconnecting = false;
                }
            }
        }
        #region 串口发送/获取返回数据
        public byte[] SendData(byte[] SendBuf)
        {
            byte[] receiveBytes = null;
            Monitor.Enter(lockObj);
            try
            {
                if (Connect())
                {
                    NetworkStream networkStream = tcpClient.GetStream();
                    if (networkStream.CanWrite)
                    {
                        byte[] sendBytes = new byte[SendBuf.Length + 6];
                        sendIndex++;
                        if (sendIndex >= 0x10000) sendIndex = 1;
                        sendBytes[0] = (byte)(sendIndex >> 8 & 0xFF);
                        sendBytes[1] = (byte)(sendIndex & 0xFF);
                        sendBytes[2] = 0;
                        sendBytes[3] = 0;
                        sendBytes[4] = (byte)(SendBuf.Length >> 8 & 0xFF);
                        sendBytes[5] = (byte)(SendBuf.Length & 0xFF);

                        //Array.Copy(SendBuf, 0, sendBytes, 6, SendBuf.Length);
                        SendBuf.CopyTo(sendBytes, 6);
                        networkStream.Write(sendBytes, 0, sendBytes.Length);

                        if (networkStream.CanRead)
                        {
                            int len = networkStream.Read(buffer, 0, 1024);

                            if (len >= 10)
                            {
                                //报头正确
                                if (buffer[0] == sendBytes[0] && buffer[1] == sendBytes[1] && buffer[2] == sendBytes[2] && buffer[3] == sendBytes[3] && (len - 6) == (buffer[4] * 0x100 + buffer[5]))
                                {
                                    int receiveLen = 12;
                                    if (SendBuf[1] == 0x01 || SendBuf[1] == 0x02)//0x01读多个线圈 0X; 0x02只读多个输入离散量 1X
                                    {
                                        receiveLen = (SendBuf[4] << 5) + (SendBuf[5] >> 3);
                                        if ((SendBuf[5] & 0x07) > 0) receiveLen++;
                                        receiveLen += 9;
                                    }
                                    else if (SendBuf[1] == 0x03 || SendBuf[1] == 0x04 || SendBuf[1] == 0x17)//0x03读多个寄存器 4X; 0x04只读多个输入寄存器 3X
                                    {
                                        receiveLen = ((SendBuf[4] << 8) + SendBuf[5]) * 2 + 9;
                                    }

                                    //站号、功能码、报文长度正确
                                    if (buffer[7] == SendBuf[1] && receiveLen == len)
                                    {
                                        receiveBytes = buffer.Skip(6).Take(len - 6).ToArray();
                                        _isDisconnectFlag = false;
                                        _retryCount = 0;
                                        _OnReconnectAlarmFlag = false;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _isDisconnectFlag = true;
            }
            finally
            {
                Monitor.Exit(lockObj); //无论正常还是报错，锁一定释放
            }
            return receiveBytes;
        }
        //public byte[] SendData(byte[] SendBuf)
        //{
        //    byte[] receiveBytes = null;
        //    Monitor.Enter(lockObj);
        //    try
        //    {
        //        if (Connect())
        //        {
        //            NetworkStream networkStream = tcpClient.GetStream();
        //            tcpClient.ReceiveTimeout = 1000; //接收超时1秒
        //            if (networkStream.CanWrite)
        //            {
        //                byte[] sendBytes = new byte[SendBuf.Length + 6];
        //                sendIndex++;
        //                if (sendIndex >= 0x10000) sendIndex = 1;
        //                sendBytes[0] = (byte)(sendIndex >> 8 & 0xFF);
        //                sendBytes[1] = (byte)(sendIndex & 0xFF);
        //                sendBytes[2] = 0;
        //                sendBytes[3] = 0;
        //                sendBytes[4] = (byte)(SendBuf.Length >> 8 & 0xFF);
        //                sendBytes[5] = (byte)(SendBuf.Length & 0xFF);
        //                SendBuf.CopyTo(sendBytes, 6);

        //                networkStream.Write(sendBytes, 0, sendBytes.Length);
        //                networkStream.Flush();

        //                // ==========循环接收完整ModbusTCP帧==========
        //                byte[] buffer = new byte[1024];
        //                int totalRead = 0;
        //                int remainLen = -1;
        //                DateTime start = DateTime.Now;
        //                while ((DateTime.Now - start).TotalMilliseconds < 1000)
        //                {
        //                    if (networkStream.DataAvailable == false)
        //                    {
        //                        System.Threading.Thread.Sleep(5);
        //                        continue;
        //                    }
        //                    int read = networkStream.Read(buffer, totalRead, 1024 - totalRead);
        //                    if (read <= 0) break;
        //                    totalRead += read;

        //                    //收到MBAP头前6字节就可以解析报文总长度
        //                    if (totalRead >= 6 && remainLen == -1)
        //                    {
        //                        ushort msgLen = (ushort)(buffer[4] * 256 + buffer[5]);
        //                        remainLen = 6 + msgLen; //完整帧总字节数
        //                    }
        //                    //已经收够完整报文
        //                    if (remainLen > 0 && totalRead >= remainLen)
        //                    {
        //                        break;
        //                    }
        //                }

        //                int len = totalRead;
        //                if (len >= 10)
        //                {
        //                    //事务ID校验
        //                    if (buffer[0] == sendBytes[0] && buffer[1] == sendBytes[1] && buffer[2] == sendBytes[2] && buffer[3] == sendBytes[3])
        //                    {
        //                        int receiveLen = 0;
        //                        if (SendBuf[1] == 0x01 || SendBuf[1] == 0x02)
        //                        {
        //                            int bitCount = (SendBuf[4] << 8) + SendBuf[5];
        //                            receiveLen = (bitCount / 8) + ((bitCount % 8) > 0 ? 1 : 0);
        //                            receiveLen += 10;
        //                        }
        //                        else if (SendBuf[1] == 0x03 || SendBuf[1] == 0x04 || SendBuf[1] == 0x17)
        //                        {
        //                            int regNum = (SendBuf[4] << 8) + SendBuf[5];
        //                            receiveLen = regNum * 2 + 10; //✅修正 +10
        //                        }
        //                        //长度匹配校验
        //                        if (receiveLen == len)
        //                        {
        //                            //检查是否异常码
        //                            //if (buffer[7] >= 0x80)
        //                            //{
        //                                //PLC返回错误，打印异常码 buffer[8]
        //                                //continue;
        //                           // }
        //                            //✅跳过7字节MBAP头，取出PDU数据
        //                            receiveBytes = buffer.Skip(7).Take(len - 7).ToArray();
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {

        //    }
        //    finally
        //    {
        //        Monitor.Exit(lockObj);
        //    }
        //    return receiveBytes;
        //}
        #endregion


        #region 读线圈Fun01 0X
        public bool[] Fun01(byte station, ushort inAddr, ushort inNum)
        {
            bool[] outBools = null;
            Byte[] subuffer = new Byte[6];
            subuffer[0] = station;    //站号
            subuffer[1] = 1;    //功能码
            IntTo2Bytes(inAddr).CopyTo(subuffer, 2);    //起始地址
            IntTo2Bytes(inNum).CopyTo(subuffer, 4);    //读取数量

            byte[] ob = SendData(subuffer);

            if (ob != null)
            {
                int inN = inNum >> 3;
                if ((inNum & 0x07) > 0) inN++;
                if (ob[2] == inN)
                {
                    bool[] recBool = new bool[inN * 8];
                    for (int i = 0; i < inN; i++)
                    {
                        ByteToBools(ob[i + 3]).CopyTo(recBool, i * 8);
                    }
                    outBools = new bool[inNum];
                    Array.Copy(recBool, 0, outBools, 0, inNum);
                }
            }
            return outBools;
        }
        #endregion


        #region 读离散量Fun02 1X
        public bool[] Fun02(byte station, ushort inAddr, ushort inNum)
        {
            bool[] outBools = null;
            Byte[] subuffer = new Byte[6];
            subuffer[0] = station;    //站号
            subuffer[1] = 2;    //功能码
            IntTo2Bytes(inAddr).CopyTo(subuffer, 2);    //起始地址
            IntTo2Bytes(inNum).CopyTo(subuffer, 4);    //读取数量

            byte[] ob = SendData(subuffer);

            if (ob != null)
            {
                int inN = inNum >> 3;
                if ((inNum & 0x07) > 0) inN++;
                if (ob[2] == inN)
                {
                    bool[] recBool = new bool[inN * 8];
                    for (int i = 0; i < inN; i++)
                    {
                        ByteToBools(ob[i + 3]).CopyTo(recBool, i * 8);
                    }
                    outBools = new bool[inNum];
                    Array.Copy(recBool, 0, outBools, 0, inNum);
                }
            }

            return outBools;
        }
        #endregion


        #region 写单个线圈Fun05 0X
        public bool Fun05(byte station, ushort inAddr, bool inBool)
        {
            Byte[] subuffer = new Byte[6];
            subuffer[0] = station;
            subuffer[1] = 5;
            IntTo2Bytes(inAddr).CopyTo(subuffer, 2);
            subuffer[4] = inBool ? (byte)0xff : (byte)0x00;
            subuffer[5] = 0x00;

            return CompareArray(subuffer, SendData(subuffer));
        }
        #endregion


        #region 写多个线圈Fun0F 0X
        public bool Fun0F(byte station, ushort inAddr, bool[] inBools)
        {
            int len = inBools.Length;
            int byteNum = (len >> 3);
            if ((len & 0x07) > 0) byteNum++;

            byte[] sendBytes = new byte[byteNum];
            int bt = 0, j = 0;
            for (int i = 0; i < len; i++)
            {
                if (inBools[i]) bt |= (1 << j);
                j++;
                if (j > 7)
                {
                    sendBytes[i / 8] = (byte)bt;
                    j = 0;
                    bt = 0;
                }
            }
            if (j > 0) sendBytes[byteNum - 1] = (byte)bt;

            Byte[] subuffer = new Byte[byteNum + 7];
            subuffer[0] = station;
            subuffer[1] = 0x0F;
            IntTo2Bytes(inAddr).CopyTo(subuffer, 2);    //起始地址
            IntTo2Bytes(len).CopyTo(subuffer, 4);    //线圈数量
            subuffer[6] = (byte)byteNum;    //字节数
            Array.Copy(sendBytes, 0, subuffer, 7, byteNum);    //传送数据

            byte[] ob = SendData(subuffer);

            if (ob != null && ob.Length == 6)
            {
                byte[] send_data = new byte[6];
                Array.Copy(subuffer, 0, send_data, 0, 6);
                return CompareArray(send_data, ob);
            }
            else
            {
                return false;
            }
        }
        #endregion


        #region 读多个寄存器 Fun03 4X
        public byte[] Fun03(byte station, ushort inAddr, ushort inNum)
        {
            Byte[] subuffer = new Byte[6];
            subuffer[0] = station;    //站号
            subuffer[1] = 0x03;    //功能码
            IntTo2Bytes(inAddr).CopyTo(subuffer, 2);    //起始地址
            IntTo2Bytes(inNum).CopyTo(subuffer, 4);    //数据个数

            return SendData(subuffer);
        }
        #endregion


        #region 读多个输入寄存器 Fun04 3X
        public byte[] Fun04(byte station, ushort inAddr, ushort inNum)
        {
            Byte[] subuffer = new Byte[6];
            subuffer[0] = station;    //站号
            subuffer[1] = 0x04;    //功能码
            IntTo2Bytes(inAddr).CopyTo(subuffer, 2);    //起始地址
            IntTo2Bytes(inNum).CopyTo(subuffer, 4);    //数据个数

            return SendData(subuffer);
        }
        #endregion


        #region 写单个寄存器 Fun06 4X
        public bool Fun06(byte station, ushort inAddr, short inShort)
        {
            Byte[] subuffer = new Byte[6];
            subuffer[0] = station;    //站号
            subuffer[1] = 0x06;    //功能码
            IntTo2Bytes(inAddr).CopyTo(subuffer, 2);    //起始地址
            IntTo2Bytes(inShort).CopyTo(subuffer, 4);    //传送数据

            return CompareArray(subuffer, SendData(subuffer));
        }
        #endregion


        #region 写多个寄存器Fun10 4X
        public bool Fun10(byte station, ushort inAddr, short[] inShorts)
        {
            try
            {
                int len = inShorts.Length;
                byte[] sendByte = new byte[len * 2];
                for (int i = 0; i < len; i++)
                {
                    IntTo2Bytes(inShorts[i]).CopyTo(sendByte, i * 2);
                }

                Byte[] subuffer = new Byte[len * 2 + 7];
                subuffer[0] = station;    //站号
                subuffer[1] = 0x10;    //功能码
                IntTo2Bytes(inAddr).CopyTo(subuffer, 2);    //起始地址
                IntTo2Bytes(len).CopyTo(subuffer, 4);    //数据个数
                subuffer[6] = (byte)(len * 2);   //字节长度
                sendByte.CopyTo(subuffer, 7);

                byte[] ob = SendData(subuffer);

                if (ob == null)
                {
                    return false;
                }
                else
                {
                    byte[] send_data = new byte[4];
                    byte[] rec_data = new byte[4];
                    Array.Copy(subuffer, 2, send_data, 0, 4);
                    Array.Copy(ob, 2, rec_data, 0, 4);
                    return CompareArray(send_data, rec_data);
                }

            }
            catch (Exception)
            {
                //HeartbeatConnect();
                return false;
            }

        }
        #endregion


        #region 写多个字节数组
        public bool SendBytes(byte station, ushort inAddr, Byte[] inBytes)
        {
            int len = inBytes.Length;
            Byte[] subuffer = new Byte[len + 7];
            subuffer[0] = station;    //站号
            subuffer[1] = 0x10;    //功能码
            IntTo2Bytes(inAddr).CopyTo(subuffer, 2);    //起始地址
            IntTo2Bytes(len / 2).CopyTo(subuffer, 4);    //数据个数
            subuffer[6] = (byte)(len);   //字节长度
            inBytes.CopyTo(subuffer, 7);

            byte[] ob = SendData(subuffer);

            if (ob == null)
            {
                return false;
            }
            else
            {
                byte[] send_data = new byte[4];
                byte[] rec_data = new byte[4];
                Array.Copy(subuffer, 2, send_data, 0, 4);
                Array.Copy(ob, 2, rec_data, 0, 4);

                return CompareArray(send_data, rec_data);
            }
        }
        #endregion


        #region 读写多个寄存器Fun17 4X
        public byte[] Fun17(byte station, ushort readAddress, ushort readNum, ushort writeAddress, byte[] writeBytes)
        {
            int len = (writeBytes.Length + 1) / 2;
            Byte[] subuffer = new Byte[len * 2 + 11];
            subuffer[0] = station;    //站号
            subuffer[1] = 0x17;    //功能码
            IntTo2Bytes(readAddress).CopyTo(subuffer, 2);    //读取起始地址
            IntTo2Bytes(readNum).CopyTo(subuffer, 4);    //读取数据个数
            IntTo2Bytes(writeAddress).CopyTo(subuffer, 6);    //写入起始地址
            IntTo2Bytes(len).CopyTo(subuffer, 8);    //写入数据个数
            subuffer[10] = (byte)(len * 2);   //写入字节长度
            writeBytes.CopyTo(subuffer, 11); //写入数据

            return SendData(subuffer);
        }
        #endregion


        #region 判断两个数组是否相等
        public bool CompareArray(byte[] bt1, byte[] bt2)
        {
            if (bt1 == null && bt2 == null)
            {
                return true;
            }
            else if (bt1 == null || bt2 == null)
            {
                return false;
            }

            int len = bt1.Length;
            if (len != bt2.Length)
            {
                return false;
            }
            for (var i = 0; i < len; i++)
            {
                if (bt1[i] != bt2[i])
                    return false;
            }
            return true;
        }
        #endregion


        #region 浮点数、整形、字节、位互相转化

        //整形转字节数组
        public static Byte[] IntTo2Bytes(int inInt)
        {
            Byte[] outByte = new byte[2];
            if (inInt < 0) inInt += 0x10000;
            outByte[1] = (byte)inInt;
            outByte[0] = (byte)(inInt >> 8);
            return outByte;
        }

        public static Byte[] IntTo4Bytes(int inInt)
        {
            Byte[] outByte = new byte[4];
            outByte[1] = (byte)inInt;
            outByte[0] = (byte)(inInt >> 8);
            outByte[3] = (byte)(inInt >> 16);
            outByte[2] = (byte)(inInt >> 24);
            return outByte;
        }

        public static Byte[] UIntTo4Bytes(uint inInt)
        {
            Byte[] outByte = new byte[4];
            outByte[1] = (byte)inInt;
            outByte[0] = (byte)(inInt >> 8);
            outByte[3] = (byte)(inInt >> 16);
            outByte[2] = (byte)(inInt >> 24);
            return outByte;
        }


        public static Byte[] FloatTo4Bytes(float inFloat)
        {
            byte[] bts = BitConverter.GetBytes(inFloat);
            Byte[] outByte = new byte[4] { bts[1], bts[0], bts[3], bts[2] };

            return outByte;
        }

        //字节数组转Short，先校验两个字节
        public static short BytesToShort(byte[] inBytes)
        {
            inBytes[0] = inBytes[1];
            inBytes[1] = inBytes[0];
            return BitConverter.ToInt16(inBytes, 0);
        }

        public static ushort BytesToUShort(byte[] inBytes)
        {
            inBytes[0] = inBytes[1];
            inBytes[1] = inBytes[0];
            return BitConverter.ToUInt16(inBytes, 0);
        }

        //字节数组转整形，先校验四个字节
        public static int BytesToInt(byte[] inBytes)
        {
            inBytes[0] = inBytes[1];
            inBytes[1] = inBytes[0];
            inBytes[2] = inBytes[3];
            inBytes[3] = inBytes[2];
            return BitConverter.ToInt32(inBytes, 0);
            //return inBytes[1] | inBytes[0] >> 8 | inBytes[3] >> 16 | inBytes[2] >> 24;
        }

        public static uint BytesToUInt(byte[] inBytes)
        {
            inBytes[0] = inBytes[1];
            inBytes[1] = inBytes[0];
            inBytes[2] = inBytes[3];
            inBytes[3] = inBytes[2];
            return BitConverter.ToUInt32(inBytes, 0);
        }

        //字节数组转浮点型，先校验四个字节
        public static float BytesToFloat(byte[] inBytes)
        {
            inBytes[0] = inBytes[1];
            inBytes[1] = inBytes[0];
            inBytes[2] = inBytes[3];
            inBytes[3] = inBytes[2];
            return BitConverter.ToSingle(inBytes, 0);
        }

        //Bool数组转Int
        public static int BoolsToInt(bool[] bools)
        {
            if (bools != null && bools.Length > 0)
            {
                int b = 0;
                int len = bools.Length;
                if (len > 32) len = 32;
                for (int i = 0; i < len; i++)
                {
                    if (bools[i]) b |= (1 << i);
                }
                return b;
            }
            return 0;
        }

        //Byte转Bool数组
        public static bool[] ByteToBools(byte inByte)
        {
            bool[] bls = new bool[8];

            bls[0] = (inByte & 0x01) == 0x01;
            bls[1] = (inByte & 0x02) == 0x02;
            bls[2] = (inByte & 0x04) == 0x04;
            bls[3] = (inByte & 0x08) == 0x08;

            bls[4] = (inByte & 0x10) == 0x10;
            bls[5] = (inByte & 0x20) == 0x20;
            bls[6] = (inByte & 0x40) == 0x40;
            bls[7] = (inByte & 0x80) == 0x80;

            return bls;
        }
        #endregion


        #region 字符串和数组互转
        public static string HexStrCheck(string str)
        {
            string strResult = string.Empty;
            bool checkResult = true;
            str = str.ToUpper();
            string checkStr = "0123456789ABCDEF ";
            for (int i = 0; i < str.Length; i++)
            {
                string s = str.Substring(i, 1);
                if (!checkStr.Contains(s))
                {
                    checkResult = false;
                    break;
                }
            }

            if (checkResult)
            {

                for (int i = 0; i < str.Length - 1; i++)
                {
                    while (i < str.Length && str[i] == ' ')
                    {
                        i++;
                    }

                    if (i < str.Length)
                    {
                        string addstring = string.Empty;
                        if (strResult != string.Empty)
                        {
                            addstring = " ";
                        }
                        if (str.Substring(i + 1, 1) == " ")
                        {
                            addstring += ("0" + str.Substring(i, 1));
                        }
                        else
                        {
                            addstring += str.Substring(i, 2);
                        }

                        strResult += addstring;
                        i++;
                    }
                }
            }

            return strResult;
        }

        public static byte[] HexStrToBytes(string str)
        {
            string hexStr = HexStrCheck(str);
            if (hexStr == string.Empty)
            {
                return null;
            }
            else
            {
                byte[] bts = new byte[str.Length / 2];
                for (int i = 0; i < str.Length / 2; i++)
                {
                    bts[i] = Convert.ToByte(str.Substring(i * 2, 2), 16);
                }
                return bts;
            }
        }

        public static string BytesToHexStr(byte[] bts)
        {
            if (bts == null)
                return string.Empty;
            else
                return BitConverter.ToString(bts).Replace("-", " ");
        }
        public static string BytesToHexNoSpace(byte[] bts)
        {
            if (bts == null) return string.Empty;
            return BitConverter.ToString(bts).Replace("-", "");
        }
        public static string BytesToASCII(byte[] bts)
        {
            if (bts == null) return string.Empty;
            return Encoding.ASCII.GetString(bts).TrimEnd('\0');
        }
        public static string BoolStrCheck(string inStr)
        {
            string str = inStr.Replace(" ", "");
            string outStr = string.Empty;
            foreach (char c in str)
            {
                if (c == '0' || c == '1')
                {
                    if (string.IsNullOrEmpty(outStr))
                    {
                        outStr = c.ToString();
                    }
                    else
                    {
                        outStr += (" " + c.ToString());
                    }
                }
                else
                {
                    return string.Empty;
                }
            }

            return outStr;
        }

        public static bool[] StrToBools(string inStr)
        {
            string str = BoolStrCheck(inStr);
            if (string.IsNullOrEmpty(str))
            {
                return null;
            }
            else
            {
                string[] strs = str.Split(' ');
                bool[] bls = new bool[strs.Length];
                for (int i = 0; i < strs.Length; i++)
                {
                    bls[i] = strs[i] == "1";
                }
                return bls;
            }
        }

        public static string BoolsToStr(bool[] bls)
        {
            if (bls == null)
                return string.Empty;
            else
            {
                string str = string.Empty;
                foreach (bool b in bls)
                {
                    if (!string.IsNullOrEmpty(str))
                    {
                        str += " ";
                    }
                    if (b)
                    {
                        str += "1";
                    }
                    else
                    {
                        str += "0";
                    }
                }
                return str;
            }

        }

        #endregion

        #region 读写寄存器类型处理
        public string[] Read_Register_String(byte station, ushort address, ushort num, string type)
        {
            ushort recNum;
            byte[] bts;
            if (!connected)
            {
                return null;
            }

            if (type == "short" || type == "ushort")
            {
                recNum = num;
            }
            else
            {
                recNum = (ushort)(num * 2);
            }
            bts = Fun03(station, address, recNum);

            string[] str = new string[num];
            if (bts != null)
            {
                switch (type)
                {
                    case "short":
                        short[] shorts = new short[num];
                        for (int i = 3; i < bts.Length; i = i + 2)
                        {
                            byte[] bytes = new byte[2] { bts[i + 1], bts[i] };
                            shorts[(i - 3) / 2] = BitConverter.ToInt16(bytes, 0);
                        }
                        for (int i = 0; i < num; i++)
                        {
                            str[i] = shorts[i].ToString();
                        }
                        break;
                    case "ushort":
                        ushort[] ushorts = new ushort[num];
                        for (int i = 3; i < bts.Length; i = i + 2)
                        {
                            byte[] bytes = new byte[2] { bts[i + 1], bts[i] };
                            ushorts[(i - 3) / 2] = BitConverter.ToUInt16(bytes, 0);
                        }
                        for (int i = 0; i < num; i++)
                        {
                            str[i] = ushorts[i].ToString();
                        }
                        break;
                    case "int":
                        int[] ints = new int[num];
                        for (int i = 3; i < bts.Length; i = i + 4)
                        {
                            byte[] bytes = new byte[4] { bts[i + 1], bts[i], bts[i + 3], bts[i + 2] };
                            ints[(i - 3) / 4] = BitConverter.ToInt32(bytes, 0);
                        }
                        for (int i = 0; i < num; i++)
                        {
                            str[i] = ints[i].ToString();
                        }
                        break;
                    case "uint":
                        uint[] uints = new uint[num];
                        for (int i = 3; i < bts.Length; i = i + 4)
                        {
                            byte[] bytes = new byte[4] { bts[i + 1], bts[i], bts[i + 3], bts[i + 2] };
                            uints[(i - 3) / 4] = BitConverter.ToUInt32(bytes, 0);
                        }
                        for (int i = 0; i < num; i++)
                        {
                            str[i] = uints[i].ToString();
                        }
                        break;
                    case "float":
                        float[] floats = new float[num];
                        for (int i = 3; i < bts.Length; i = i + 4)
                        {
                            byte[] bytes = new byte[4] { bts[i + 1], bts[i], bts[i + 3], bts[i + 2] };
                            floats[(i - 3) / 4] = BitConverter.ToSingle(bytes, 0);
                        }
                        for (int i = 0; i < num; i++)
                        {
                            str[i] = floats[i].ToString("0.000");
                        }
                        break;
                }
            }
            return str;
        }
        public short[] Read_Register_State_Short(byte station, ushort address, ushort num)
        {
            try
            {
                byte[] bts;
                if (!connected)
                {
                    return null;
                }
                bts = Fun03(station, address, num);
                if (bts == null)
                {
                    return null;
                }
                short[] shorts = new short[num];
                for (int i = 3; i < bts.Length; i = i + 2)
                {
                    byte[] bytes = new byte[2] { bts[i + 1], bts[i] };
                    shorts[(i - 3) / 2] = BitConverter.ToInt16(bytes, 0);
                }
                return shorts;
            }
            catch (Exception)
            {
                //HeartbeatConnect();
                return null;
            }

        }
        public ushort[] Read_Register_State_Ushort(byte station, ushort address, ushort num)
        {
            byte[] bts;
            if (!connected)
            {
                return null;
            }
            bts = Fun03(station, address, num);
            if (bts == null)
            {
                return null;
            }
            ushort[] ushorts = new ushort[num];
            for (int i = 3; i < bts.Length; i = i + 2)
            {
                byte[] bytes = new byte[2] { bts[i + 1], bts[i] };
                ushorts[(i - 3) / 2] = BitConverter.ToUInt16(bytes, 0);
            }
            return ushorts;
        }

        public int[] Read_Register_State_Int(byte station, ushort address, ushort num)
        {
            byte[] bts;
            ushort recNum;
            if (!connected)
            {
                return null;
            }
            recNum = (ushort)(num * 2);
            bts = Fun03(station, address, recNum);
            if (bts == null)
            {
                return null;
            }
            int[] ints = new int[num];
            for (int i = 3; i < bts.Length; i = i + 4)
            {
                byte[] bytes = new byte[4] { bts[i + 1], bts[i], bts[i + 3], bts[i + 2] };
                ints[(i - 3) / 4] = BitConverter.ToInt32(bytes, 0);
            }
            return ints;
        }

        public float[] Read_Register_State_Float(byte station, ushort address, ushort num)
        {
            try
            {
                byte[] bts;
                ushort recNum;
                if (!connected)
                {
                    return null;
                }
                recNum = (ushort)(num * 2);
                bts = Fun03(station, address, recNum);
                if (bts == null)
                {
                    return null;
                }
                float[] floats = new float[num];
                for (int i = 3; i < bts.Length; i = i + 4)
                {
                    byte[] bytes = new byte[4] { bts[i + 1], bts[i], bts[i + 3], bts[i + 2] };
                    floats[(i - 3) / 4] = BitConverter.ToSingle(bytes, 0);
                }
                return floats;
            }
            catch (Exception)
            {
                //HeartbeatConnect();
                return null;
            }

        }

        public string Read_Register_State_ACSII(byte station, ushort address, ushort num)
        {
            try
            {
                byte[] bts;
                StringBuilder sbText = new StringBuilder();
                if (!connected)
                {
                    return null;
                }
                bts = Fun03(station, address, num);
                if (bts == null)
                {
                    return "";
                }
                string str = string.Empty;
                for (int i = 3; i < bts.Length; i = i + 2)
                {
                    //char ch1 = (char)bts[i];      //高字节 第一个字符
                    //char ch2 = (char)bts[i + 1];  //低字节 第二个字符
                    char ch1 = (char)bts[i + 1];      //低字节 第一个字符
                    char ch2 = (char)bts[i];          //高字节 第二个字符
                    sbText.Append(ch1);
                    sbText.Append(ch2);
                }
                str = sbText.ToString().Replace("\0", "");
                return str;
            }
            catch (Exception)
            {
                //HeartbeatConnect();
                return "";
            }

        }
        public string Read_Register_State_ACSII_1(byte station, ushort address, ushort num)
        {
            try
            {
                byte[] bts;
                StringBuilder sbText = new StringBuilder();
                if (!connected)
                {
                    return null;
                }
                bts = Fun03(station, address, num);
                string str = string.Empty;
                for (int i = 3; i < bts.Length; i = i + 2)
                {
                    char ch1 = (char)bts[i];      //高字节 第一个字符
                    char ch2 = (char)bts[i + 1];  //低字节 第二个字符
                    sbText.Append(ch1);
                    sbText.Append(ch2);
                }
                //// 保留第0位，后面的全部把\0替换为空
                //string firstChar = sbText[0].ToString();
                str = sbText.ToString().Replace("\0", "");
                return /*firstChar +*/ str;
            }
            catch (Exception)
            {
                //HeartbeatConnect();
                return null;
            }

        }
        public void Write_Register_Value(byte station, ushort address, string value, string type)
        {
            try
            {
                byte[] sendBytes = null;
                string str = value;
                if (!connected)
                {
                    return;
                }
                if (string.IsNullOrEmpty(str))
                {
                    return;
                }
                str = str.Replace(" ", "");
                switch (type)
                {
                    case "short":
                        sendBytes = new byte[2];
                        ModbusTcp.IntTo2Bytes(Convert.ToInt16(str)).CopyTo(sendBytes, 0);
                        break;
                    case "ushort":
                        sendBytes = new byte[4];
                        ModbusTcp.IntTo2Bytes(Convert.ToUInt16(str)).CopyTo(sendBytes, 0);
                        break;
                    case "int":
                        sendBytes = new byte[4];
                        ModbusTcp.IntTo4Bytes(Convert.ToInt32(str)).CopyTo(sendBytes, 0);
                        break;
                    case "uint":
                        sendBytes = new byte[4];
                        ModbusTcp.UIntTo4Bytes(Convert.ToUInt32(str)).CopyTo(sendBytes, 0);
                        break;
                    case "float":
                        sendBytes = new byte[4];
                        ModbusTcp.FloatTo4Bytes(Convert.ToSingle(str)).CopyTo(sendBytes, 0);
                        break;
                }
                if (sendBytes != null)
                {
                    SendBytes(station, address, sendBytes);
                }
            }
            catch (Exception)
            {
               // HeartbeatConnect();
            }


        }
        #endregion
        /// <summary>
        /// bool数组转字
        /// 数组下标0对应bit0，下标7对应bit7
        /// </summary>
        /// <param name="arr"></param>
        /// <returns></returns>
        public ushort BoolArrayToWord(bool[] arr)
        {
            ushort w = 0;
            for (int i = 0; arr.Length < 8; i++)
            {
                if (arr[i])
                    w |= (ushort)(1 << i);
            }
            return w;
        }
        /// <summary>
        /// 32个bool数组 → 拆成2个16位ushort字
        /// arr[0]=bit0 , arr[31]=bit31
        /// </summary>
        /// <param name="bools">长度必须等于32</param>
        /// <param name="lowWord">D2000 低16位</param>
        /// <param name="highWord">D2001 高16位</param>
        public void Bool32ToTwoWord(bool[] bools, out ushort lowWord, out ushort highWord)
        {
            lowWord = 0;
            highWord = 0;

            for (int i = 0; i < 16; i++)
            {
                if (bools[i])
                    lowWord |= (ushort)(1 << i);
            }

            for (int i = 16; i < 32; i++)
            {
                if (bools[i])
                    highWord |= (ushort)(1 << (i - 16));
            }
        }
        /// <summary>
        /// 批量写入任意数量float到modbus寄存器（功能码0x10）
        /// </summary>
        /// <param name="modbus">ModbusTcp实例</param>
        /// <param name="station">站号</param>
        /// <param name="startAddr">起始寄存器地址</param>
        /// <param name="floatArr">float数组，任意长度</param>
        /// <returns>true成功 false失败</returns>
        public bool WriteFloatArray(byte station, ushort startAddr, float[] floatArr)
        {
            try
            {
                if (floatArr == null || floatArr.Length == 0)
                {
                    return false;
                }

                // 1个float占4字节
                int totalByteCount = floatArr.Length * 4;
                byte[] totalBytes = new byte[totalByteCount];

                for (int i = 0; i < floatArr.Length; i++)
                {
                    byte[] fBytes = ModbusTcp.FloatTo4Bytes(floatArr[i]);
                    Array.Copy(fBytes, 0, totalBytes, i * 4, 4);
                }

                // Modbus 0x10单帧最大写123个寄存器，1寄存器=2字节 → 最大246字节
                const int MaxRegisterPerFrame = 123;
                int totalRegCount = totalByteCount / 2;

                // 单帧可写完
                if (totalRegCount <= MaxRegisterPerFrame)
                {
                    return SendBytes(station, startAddr, totalBytes);
                }
                else
                {
                    // 需要分包发送
                    int floatPerFrame = MaxRegisterPerFrame / 2; //每帧最多多少个float
                    ushort currentAddr = startAddr;
                    int index = 0;
                    while (index < floatArr.Length)
                    {
                        int takeCnt = Math.Min(floatPerFrame, floatArr.Length - index);
                        float[] part = floatArr.Skip(index).Take(takeCnt).ToArray();

                        int partByteLen = takeCnt * 4;
                        byte[] partBytes = new byte[partByteLen];
                        for (int i = 0; i < part.Length; i++)
                        {
                            byte[] fBytes = ModbusTcp.FloatTo4Bytes(part[i]);
                            Array.Copy(fBytes, 0, partBytes, i * 4, 4);
                        }

                        bool ret = SendBytes(station, currentAddr, partBytes);
                        if (!ret)
                            return false;

                        currentAddr += (ushort)(takeCnt * 2);
                        index += takeCnt;
                    }
                    return true;
                }
            }
            catch (Exception)
            {
                //HeartbeatConnect();
                return false;
            }

        }
    }
}
