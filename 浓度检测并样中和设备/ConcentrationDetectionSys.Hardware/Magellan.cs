using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ConcentrationDetectionSys.Hardware
{
    public class MagellanELIASA
    {

        private Magellan2.IOleMagellan _iMagellan;
        private dynamic _iReaderServer;
        private dynamic _iInstrument;
        private Magellan2.IOleWorkspace _iWorkspace;
        public string _DeviceName = "Sunrise";
        public bool _IsConnected = false;
        public MagellanELIASA()
        {
        }
        //public static MagellanELIASA Instance { get; } = new MagellanELIASA();
        public bool Connect()
        {
            try
            {
                if (_IsConnected) return true;

                // 1. 先彻底清理上次可能残留的 COM 对象
                Disconnect();
                System.Threading.Thread.Sleep(10000);
                // 注意：既然你删了 Process.Kill，这里的 Disconnect 只是优雅释放 COM

                // 2. 创建顶层对象
                if (_iMagellan == null)
                {
                    _iMagellan = new Magellan2.Application();
                }

                // 3. 加上重试机制
                int retryCount = 0;
                while (retryCount < 3)
                {
                    try
                    {
                        if (_iMagellan == null)
                        {
                            _iMagellan = new Magellan2.Application();
                        }
                        _iReaderServer = _iMagellan.CreateRdrOleObject();
                        _iReaderServer.InstrumentName = _DeviceName;
                        _iReaderServer.PortNr = 0;

                        // 最容易卡死的地方，如果卡死，下次重试前必须重置环境
                        _iInstrument = _iReaderServer.CreateInstrumentObject();

                        _iMagellan.InstrumentObject = _iInstrument;
                        _iWorkspace = _iMagellan.CreateWorkspaceObject();
                        _IsConnected = true;
                        return true;
                    }
                    catch (Exception ex)
                    {
                        retryCount++;
                        System.Diagnostics.Debug.WriteLine($"第 {retryCount} 次连接失败，原因: {ex.Message}");
                        //System.Threading.Thread.Sleep(2000);
                        
                        Disconnect();
                        System.Threading.Thread.Sleep(5000);
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                // 处理 RPC 服务器不可用
                if (ex is COMException comEx && (uint)comEx.HResult == 0x800706BA)
                {
                    System.Diagnostics.Debug.WriteLine("处理 RPC 服务器不可用");
                }
                Disconnect();
                System.Threading.Thread.Sleep(5000);
                return false;
            }
        }
        //public bool Connect()
        //{
        //    try
        //    {
        //        if (_IsConnected)
        //        {
        //            return true;
        //        }
        //        else
        //        {
        //            if (_iMagellan == null)
        //            {
        //                _iMagellan = new Magellan2.Application();
        //            }
        //            _iReaderServer = _iMagellan.CreateRdrOleObject();
        //            _iReaderServer.InstrumentName = _DeviceName;
        //            _iReaderServer.PortNr = 0;
        //            _iInstrument = _iReaderServer.CreateInstrumentObject();
        //            _iMagellan.InstrumentObject = _iInstrument;
        //            _iWorkspace = _iMagellan.CreateWorkspaceObject();
        //            _IsConnected = true;
        //            return _IsConnected;
        //        }
        //    }
        //    catch (System.Runtime.InteropServices.COMException comEx)
        //    {
        //        //RPC服务器不可用：Magellan进程已经挂掉，顶层COM作废，置null
        //        if ((uint)comEx.HResult == 0x800706BA)
        //        {
        //            _iMagellan = null;
        //        }

        //        //释放子对象
        //        ReleaseComObj(_iWorkspace);
        //        _iWorkspace = null;
        //        ReleaseComObj(_iInstrument);
        //        _iInstrument = null;
        //        ReleaseComObj(_iReaderServer);
        //        _iReaderServer = null;

        //        _IsConnected = false;
        //        return false;
        //    }
        //    catch (Exception ex)
        //    {
        //        ReleaseComObj(_iWorkspace);
        //        _iWorkspace = null;
        //        ReleaseComObj(_iInstrument);
        //        _iInstrument = null;
        //        ReleaseComObj(_iReaderServer);
        //        _iReaderServer = null;

        //        _IsConnected = false;
        //        return false;
        //    }
        //    finally
        //    {

        //    }
        //}
        /// <summary>
        /// 释放COM RCW对象
        /// </summary>
        //private void ReleaseComObj(object comObj)
        //{
        //    if (comObj != null)
        //    {
        //        System.Runtime.InteropServices.Marshal.ReleaseComObject(comObj);
        //    }
        //}
        private void ReleaseComObj(object comObj)
        {
            if (comObj == null) return;
            try
            {
                int refCount = Marshal.ReleaseComObject(comObj);
                //// 循环释放，直到引用计数归零
                //while (refCount > 0)
                //{
                //    refCount = Marshal.ReleaseComObject(comObj);
                //}
            }
            catch
            {
                // 已经释放的COM对象会抛异常，忽略
            }
            //finally
            //{
            //    // 置空
            //    comObj = null;
            //}
        }
        public void Disconnect()
        {
            try
            {
                _iWorkspace = null;
                _iInstrument = null;
                _iReaderServer = null;
                _iMagellan = null;
                _IsConnected = false;
                GC.Collect();
            }
            catch (Exception ex)
            {
            }
            finally
            {    
            }
        }
        /// <summary>
        /// 清空工作空间
        /// </summary>
        /// <returns></returns>
        public bool WorkSpaceSetEmpty()
        {
            if (_IsConnected)
            {
                _iWorkspace.Empty = true;
                return true;
            }
            return false;
        }
        /// <summary>
        /// 获取可用方法列表
        /// </summary>
        /// <returns></returns>
        public string[] AvailableMethods()
        {
            if (_IsConnected)
            {
                string sMethods = _iMagellan.AvailableMethodFiles(_iInstrument.InstrumentName);
                string[] sMethodsArray = sMethods.Split('\t');
                return sMethodsArray;
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 插入方法
        /// </summary>
        /// <param name="methodName"></param>
        /// <returns></returns>
        public bool InsertMethod(string methodName)
        {
            if (_IsConnected)
            {
                if (!string.IsNullOrEmpty(methodName))
                {
                    bool bOk = _iWorkspace.InsertMethod(methodName);
                    return bOk;
                }
                else
                    return false;
            }
            return false;
        }
        /// <summary>
        /// 设置文件名
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public bool SetFileName(string fileName)
        {
            if (_IsConnected)
            {
                if (!string.IsNullOrEmpty(fileName))
                {
                    _iWorkspace.FileName = fileName;
                    return true;
                }
                else
                    return false;
            }
            return false;
        }
        /// <summary>
        /// 开始运行
        /// </summary>
        /// <returns></returns>
        public bool StartRun()
        {
            if (_IsConnected)
            {
                _iWorkspace.ErrorProtocol = null;
                bool bOk = _iWorkspace.StartRun();
                return bOk;
            }
            return false;
        }
        /// <summary>
        /// 停止运行
        /// </summary>
        /// <returns></returns>
        public bool StopRun()
        {
            if (_IsConnected)
            {
                _iInstrument.CreateMeasurementObject().StopMeasurement();
                return true;
            }
            return false;
        }
        /// <summary>
        /// 获取运行状态
        /// </summary>
        /// <param name="nStatus"></param>
        /// <returns></returns>
        public string GetRunStatusAsText()
        {
            if (_IsConnected)
            {
                string sStatus = "";
                switch (_iWorkspace.Status)
                {
                    case 0:
                        sStatus = "测量进行中";
                        break;
                    case 1:
                        sStatus = "测量成功结束";
                        break;
                    case 2:
                        sStatus = "测量异常结束";
                        break;
                    case 3:
                        sStatus = "无原始数据";
                        break;
                    case 4:
                        sStatus = "验证失败";
                        break;
                    case 5:
                        sStatus = "标准曲线非单调";
                        break;
                    case 6:
                        sStatus = "验证失败 + 标准曲线非单调";
                        break;
                    case 7:
                        sStatus = "质控点偏移";
                        break;
                    case 8:
                        sStatus = "质控点偏移 + 验证失败";
                        break;
                    case 9:
                        sStatus = "质控点偏移 + 标准曲线非单调";
                        break;
                    case 10:
                        sStatus = "质控点偏移 + 验证失败 + 标准曲线非单调";
                        break;
                    default:
                        sStatus = "未知状态";
                        break;
                }
                return sStatus;
            }
            else
            {
                return "未连接";

            }
        }
        /// <summary>
        /// 数据导出
        /// </summary>
        /// <returns></returns>
        public bool DataExport()
        {
            if (_IsConnected)
            {
                _iWorkspace.ErrorProtocol = null;
                bool bOk = _iWorkspace.AutomatedDataHandling();
                return bOk;
            }
            else
                return false;
        }
        /// <summary>
        /// 保存工作空间
        /// </summary>
        /// <returns></returns>
        public bool SaveWorkspace()
        {
            if (_IsConnected)
            {
                bool bOk = _iWorkspace.DoSerialization(1);
                return bOk;
            }
            else
                return false;

        }
        /// <summary>
        /// 获取数据名称列表
        /// </summary>
        /// <returns></returns>
        public string[] GetDataNames()
        {
            if (_IsConnected)
            {
                string sNames = _iWorkspace.DataNames;
                string[] sDataArray = sNames.Split('\t');
                return sDataArray;
            }
            else
            {
                return null;
            }
                
        }
        /// <summary>
        /// 获取数据表格
        /// </summary>
        /// <param name="sDataName"></param>
        /// <returns></returns>
        public string DataAsTable(string sDataName)
        {
            if (_IsConnected)
            {
                string sDataAsTable = _iWorkspace.DataAsTable(sDataName, 0);
                return sDataAsTable;
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 插入板子
        /// </summary>
        /// <returns></returns>
        public bool PlateIn()
        {
            if (_IsConnected)
            {
                _iInstrument.PlateInOut = "In";
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// 取出板子
        /// </summary>
        /// <returns></returns>
        public bool PlateOut()
        {
            if (_IsConnected)
            {
                _iMagellan.InstrumentObject.PlateInOut = "Out";
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
