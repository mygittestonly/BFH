using ConcentrationDetectionSys.Common;
using ConcentrationDetectionSys.DAL;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace ConcentrationDetectionSys.Bll
{
    public class ConcentrationMeasurementParam
    {
        /// <summary>
        /// A260/A280下限阈值
        /// </summary>
        public double RatioLowerLimit { get; set; } = 0.0;
        /// <summary>
        /// 下限比较运算符：">" 或 ">="
        /// </summary>
        public string LowerOperator { get; set; } = ">=";

        /// <summary>
        /// A260/A280上限阈值
        /// </summary>
        public double RatioUpperLimit { get; set; } = 1.0;
        /// <summary>
        /// 上限比较运算符："<" 或 "<="
        /// </summary>
        public string UpperOperator { get; set; } = "<=";
        /// <summary>
        /// 光程
        /// </summary>
        public double OpticalDistance { get; set; } = 0.2;
        /// <summary>
        /// 人工干预
        /// </summary>
        public bool HumanIntervention { get; set; } = false;
    }
    /// <summary>
    /// 浓度测量
    /// </summary>
    public class ConcentrationMeasurement : BaseStep
    {
        /// <summary>
        /// 人工确认委托（三个托盘的数据一起传给 UI）。
        /// 返回 true = 用户确认继续；false = 用户取消。
        /// 由 UI 层订阅实现；业务线程同步调用（内部会 Invoke 到主线程阻塞等待）。
        /// </summary>
        public static Func<List<MapHoleInfo>, List<MapHoleInfo>, List<MapHoleInfo>, bool> OnHumanConfirm;
        public static event Action<string, string, string> OnDataRestChanged;
        public ConcentrationMeasurementParam concentrationMeasurementParam = new ConcentrationMeasurementParam();
        public ConcentrationMeasurement() : base("随样测浓")
        {
        }
        private int _ConcentrationMeasurementStepIndex = 0;
        private string _testResult;
        private string[,] tableArrA260;
        private string[,] tableArrA280;
        private string[,] tableArrA340;

        private string dataA260Str, dataA280Str, dataA340Str;
        private int RackCount;
        public override void Execute(CancellationToken token)
        {
            // 浓度测量执行逻辑
            Task.Run(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    if (!CommonParam.AutoRunCondition)
                    {
                        Thread.Sleep(200);    
                        continue;
                    }
                    //酶标板扫码完成信号
                    short[] registerValues_1100 = CommonParam.s_PLC.Read_Register_State_Short(1, 1100, 1);
                    if (registerValues_1100 != null && registerValues_1100[0] == 1)
                    {
                        CommonParam.s_PLC.Write_Register_Value(1, 1100, "0", "short");
                        //读取酶标板二维码
                        string code = CommonParam.s_PLC.Read_Register_State_ACSII(1, 1101, 20);
                        if (code == "")
                        {
                            LogEventManager.LogEvent(30, $"浓度测量-酶标板扫码完成信号触发，读取酶标板二维码为空");
                            continue;
                        }
                        LogEventManager.LogEvent(31, $"浓度测量-酶标板扫码完成: {code}");
                        SqlHelper.SetStringValue("MicroplateID", code);
                    }


                    //酶标板点样完成信号
                    short[] registerValues_2080 = CommonParam.s_PLC.Read_Register_State_Short(1, 2080, 1);
                    if (registerValues_2080 != null && registerValues_2080[0] == 1)
                    {
                        CommonParam.s_PLC.Write_Register_Value(1, 2080, "0", "short");
                        LogEventManager.LogEvent(31, $"浓度测量-酶标板点样完成信号触发");
                        
                        string code = SqlHelper.GetString("MicroplateID");
                        string nowStr = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");
                        RackCount = GetMapHoleInfoListS1();
                        if (RackCount > 0)
                        {
                            findListTest1.ForEach(item => { item.Progress = "测浓中"; item.MicroplateID = code; item.LoadTime = nowStr; });
                            CommonParam.SaveMapHoleInfosToSQL(findListTest1);
                            CommonParam.s_MapHoleInfoListS1_1 = findListTest1;
                            findListTest2.ForEach(item => { item.Progress = "测浓中"; item.MicroplateID = code; item.LoadTime = nowStr; });
                            CommonParam.SaveMapHoleInfosToSQL(findListTest2);
                            CommonParam.s_MapHoleInfoListS1_2 = findListTest2;
                            findListTest3.Clear();
                            if (RackCount > 2)
                            {
                                findListTest3.ForEach(item => { item.Progress = "测浓中"; item.MicroplateID = code; item.LoadTime = nowStr; });
                                CommonParam.SaveMapHoleInfosToSQL(findListTest3);
                                CommonParam.s_MapHoleInfoListS1_3 = findListTest3;
                            }
                        }
                        else
                        {
                            continue;
                        }
                        SqlHelper.SetStringValue("MicroplateID","");
                    }
                   
                    //请求打开酶标仪仓门信号
                    short[] registerValues_1951 = CommonParam.s_PLC.Read_Register_State_Short(1, 1951, 1);
                    if (registerValues_1951 != null && registerValues_1951[0] == 1)
                    {
                        LogEventManager.LogEvent(31, $"浓度测量-请求打开酶标仪仓门信号触发");
                        CommonParam.s_PLC.Write_Register_Value(1, 1951, "0", "short");
                        LogEventManager.LogEvent(31, $"浓度测量-打开酶标仪仓门");
                        //调用酶标仪开门方法
                        CommonParam.s_Magellan.PlateOut();
                        //暂时给延时
                        Thread.Sleep(1000);
                        CommonParam.s_PLC.Write_Register_Value(1, 2951, "1", "short");
                    }

                    //酶标板放料ok信号
                    short[] registerValues_1952 = CommonParam.s_PLC.Read_Register_State_Short(1, 1952, 1);
                    if (registerValues_1952 != null && registerValues_1952[0] == 1)
                    {
                        CommonParam.s_PLC.Write_Register_Value(1, 1952, "0", "short");
                        LogEventManager.LogEvent(31, $"浓度测量-收到PLC酶标板放料ok信号");
                        
                        //调用酶标仪关门方法
                        LogEventManager.LogEvent(31, $"浓度测量-关闭酶标仪仓门");
                        CommonParam.s_Magellan.PlateIn();
                        //暂时给延时
                        Thread.Sleep(2000);
                        LogEventManager.LogEvent(31, $"浓度测量-酶标仪开始检测");
                        CommonParam.s_Magellan.StartRun();
                        //等待检测完成
                        while (!token.IsCancellationRequested)
                        {
                            _testResult = CommonParam.s_Magellan.GetRunStatusAsText();
                            if (_testResult != "测量进行中")
                            {
                                break;
                            }
                            Thread.Sleep(10);
                        }
                        //检测完成
                        LogEventManager.LogEvent(31, $"浓度测量-酶标仪检测完成，结果: {_testResult}");
                        string nowStr = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");

                        string code = SqlHelper.GetString("ConcentrationMeasurementRackID1");
                        mapHoleInfoListS2_1 = CommonParam.QueryMapHoleInfoByBatch(code);
                        mapHoleInfoListS2_1.ForEach(item => { item.Progress = "测浓完成"; item.Concentration_Test_Time = nowStr; item.Test_Operator = CommonParam.s_CurrentUser.UserName; });
                        CommonParam.SaveMapHoleInfosToSQL(mapHoleInfoListS2_1);
                        code = SqlHelper.GetString("ConcentrationMeasurementRackID2");
                        mapHoleInfoListS2_2 = CommonParam.QueryMapHoleInfoByBatch(code);
                        mapHoleInfoListS2_2.ForEach(item => { item.Progress = "测浓完成"; item.Concentration_Test_Time = nowStr; item.Test_Operator = CommonParam.s_CurrentUser.UserName; });
                        CommonParam.SaveMapHoleInfosToSQL(mapHoleInfoListS2_2);
                        CommonParam.s_MapHoleInfoListS1_1 = mapHoleInfoListS2_1;
                        CommonParam.s_MapHoleInfoListS1_2 = mapHoleInfoListS2_2;
                        LogEventManager.LogEvent(30, $"浓度测量-检测1 {mapHoleInfoListS2_1.First().RackID}");
                        LogEventManager.LogEvent(30, $"浓度测量-检测2 {mapHoleInfoListS2_2.First().RackID}");
                        if (!CommonParam.s_ModeBools[0])
                        {
                            code = SqlHelper.GetString("ConcentrationMeasurementRackID3");
                            mapHoleInfoListS2_3 = CommonParam.QueryMapHoleInfoByBatch(code);
                            mapHoleInfoListS2_3.ForEach(item => { item.Progress = "测浓完成"; item.Concentration_Test_Time = nowStr; item.Test_Operator = CommonParam.s_CurrentUser.UserName; });
                            CommonParam.SaveMapHoleInfosToSQL(mapHoleInfoListS2_3);
                            CommonParam.s_MapHoleInfoListS1_3 = mapHoleInfoListS2_3;
                            LogEventManager.LogEvent(30, $"浓度测量-检测3 {mapHoleInfoListS2_3.First().RackID}");
                        }
                       
                       
                        if (_testResult == "测量成功结束")
                        {
                            //获取检测结果
                            string[] dataNamesStr = CommonParam.s_Magellan.GetDataNames();
                            bool hasAsc = dataNamesStr.Contains("A260");
                            hasAsc = dataNamesStr.Contains("A280");
                            hasAsc = dataNamesStr.Contains("A340");
                            if (!hasAsc)
                            {
                                LogEventManager.LogEvent(30, $"浓度测量-酶标仪检测方法错误，检查是否是A260,A280,A340的方法");
                                continue;
                            }
                            tableArrA260 = new string[16, 24];
                            tableArrA280 = new string[16, 24];
                            tableArrA340 = new string[16, 24];
                            dataA260Str = CommonParam.s_Magellan.DataAsTable(dataNamesStr[0]);
                            dataA280Str = CommonParam.s_Magellan.DataAsTable(dataNamesStr[1]);
                            dataA340Str = CommonParam.s_Magellan.DataAsTable(dataNamesStr[2]);
                            if (dataA260Str != null && dataA280Str != null && dataA340Str != null)
                            {
                                tableArrA260 = BuildTableStringArray(dataA260Str);
                                tableArrA280 = BuildTableStringArray(dataA280Str);
                                tableArrA340 = BuildTableStringArray(dataA340Str);
                                OnDataRestChanged?.Invoke(dataA260Str, dataA280Str, dataA340Str);
                            }
                            LogEventManager.LogEvent(31, $"浓度测量-解析检测结果");
                            //更新 OD_Value Test_Operator Concentration_Test_Time Final_Protein_Mass 参数
                            UpdateParameter();
                        }
                        else
                        {
                            //异常处理
                            if (mapHoleInfoListS2_1.Count > 0)
                            {
                                mapHoleInfoListS2_1.ForEach(item => { item.State = MapHoleInfoState.Abnormal; });
                                CommonParam.s_Capacity.DoneNGNum_CN += mapHoleInfoListS2_1.Count;
                            } 
                            if (mapHoleInfoListS2_2.Count > 0)
                            {
                                mapHoleInfoListS2_2.ForEach(item => { item.State = MapHoleInfoState.Abnormal; });
                                CommonParam.s_Capacity.DoneNGNum_CN += mapHoleInfoListS2_2.Count;
                            }  
                            if (mapHoleInfoListS2_3.Count > 0)
                            {
                                mapHoleInfoListS2_3.ForEach(item => { item.State = MapHoleInfoState.Abnormal; });
                                CommonParam.s_Capacity.DoneNGNum_CN += mapHoleInfoListS2_3.Count;
                            } 
                            LogEventManager.LogEvent(30, $"浓度测量-酶标仪检测结果异常结束，不解析");
                            continue;
                        }
                       
                        
                        
                        LogEventManager.LogEvent(31, $"浓度测量-测量完成打开酶标仪仓门");
                        //调用酶标仪开门方法
                        CommonParam.s_Magellan.PlateOut();
                        //等待酶标仪仓门打开完成信号
                        //暂时给延时
                        Thread.Sleep(2000);

                        // ================= 人工确认 =================
                        if (concentrationMeasurementParam.HumanIntervention)
                        {
                            var handler = OnHumanConfirm;
                            if (handler != null)
                            {
                                LogEventManager.LogEvent(31, "浓度测量-等待人工确认测浓值");

                                bool confirmed = handler(
                                    mapHoleInfoListS2_1,
                                    mapHoleInfoListS2_2,
                                    mapHoleInfoListS2_3);

                                if (!confirmed)
                                {
                                    LogEventManager.LogEvent(31, "浓度测量-人工确认取消流程");
                                    continue;   
                                }
                                CommonParam.SaveMapHoleInfosToSQL(mapHoleInfoListS2_1);
                                CommonParam.SaveMapHoleInfosToSQL(mapHoleInfoListS2_2);
                                if (RackCount > 2)
                                    CommonParam.SaveMapHoleInfosToSQL(mapHoleInfoListS2_3);

                                LogEventManager.LogEvent(31, "浓度测量-人工确认完成继续");
                            }
                        }
                        // ================= 人工确认结束 =================
                        mapHoleInfoListS2_1.ForEach(item => 
                        {
                            if (item.State == MapHoleInfoState.Running) 
                                CommonParam.s_Capacity.DoneOKNum_CN += 1;
                            if (item.State == MapHoleInfoState.Abnormal)
                                CommonParam.s_Capacity.DoneNGNum_CN += 1;
                        });
                        mapHoleInfoListS2_2.ForEach(item =>
                        {
                            if (item.State == MapHoleInfoState.Running)
                                CommonParam.s_Capacity.DoneOKNum_CN += 1;
                            if (item.State == MapHoleInfoState.Abnormal)
                                CommonParam.s_Capacity.DoneNGNum_CN += 1;
                        });
                        if (mapHoleInfoListS2_3.Count > 0)
                        {
                            mapHoleInfoListS2_3.ForEach(item =>
                            {
                                if (item.State == MapHoleInfoState.Running)
                                    CommonParam.s_Capacity.DoneOKNum_CN += 1;
                                if (item.State == MapHoleInfoState.Abnormal)
                                    CommonParam.s_Capacity.DoneNGNum_CN += 1;
                            });
                        }
                        CommonParam.s_PLC.Write_Register_Value(1, 2952, "1", "short");
                    }


                    //酶标板取料ok信号触发
                    short[] registerValues_1953 = CommonParam.s_PLC.Read_Register_State_Short(1, 1953, 1);
                    if (registerValues_1953 != null && registerValues_1953[0] == 1)
                    {
                        CommonParam.s_PLC.Write_Register_Value(1, 1953, "0", "short");
                        LogEventManager.LogEvent(31, $"浓度测量-收到PLC酶标板取料ok信号，酶标仪关门");
                        //调用酶标仪关门方法
                        CommonParam.s_Magellan.PlateIn();
                        //CommonParam.s_MapHoleInfoListS1_1.Clear();
                        //CommonParam.s_MapHoleInfoListS1_2.Clear();
                        //CommonParam.s_MapHoleInfoListS1_3.Clear();
                        //SqlHelper.SetStringValue("ElutionRackID1_S1", "");
                        //SqlHelper.SetStringValue("ElutionRackID2_S1", "");
                        //SqlHelper.SetStringValue("ElutionRackID3_S1", "");
                        //SqlHelper.SetStringValue("WorkOrderKeyS1", "");
                       
                    }

                    Thread.Sleep(100);
                    
                }

            });
        }
        List<MapHoleInfo> mapHoleInfoListS2_1 = new List<MapHoleInfo>();
        List<MapHoleInfo> mapHoleInfoListS2_2 = new List<MapHoleInfo>();
        List<MapHoleInfo> mapHoleInfoListS2_3 = new List<MapHoleInfo>();
        List<MapHoleInfo> findListTest1 = new List<MapHoleInfo>();
        List<MapHoleInfo> findListTest2 = new List<MapHoleInfo>();
        List<MapHoleInfo> findListTest3 = new List<MapHoleInfo>();
        private int GetMapHoleInfoListS1()
        {
            //托盘1
            string rackId = SqlHelper.GetString("RackID2ConcentrationMeasurement");
            if (rackId == "")
            {
                //报警
                LogEventManager.LogEvent(30, $"浓度测量-托盘码记忆为空");
                return 0;
            }
            string[] rackIdArr = rackId.Split(',');
            if (CommonParam.s_MES_Mode == MES_Mode.Off_Line)//MES离线模式
            {
                //判断托盘1码与CommonParam.s_WorkOrderMapHoleInfoList中是否存在
                if (CommonParam.s_WorkOrderMapHoleInfoList.Exists(info => info.RackID == rackIdArr[0]))
                {
                    LogEventManager.LogEvent(30, $"浓度测量-入站1 {rackIdArr[0]}");
                    findListTest1 = CommonParam.s_WorkOrderMapHoleInfoList.FindAll(info => info != null && info.RackID == rackIdArr[0]);
                    SqlHelper.SetStringValue("ConcentrationMeasurementRackID1", rackIdArr[0]);
                }
                else
                {
                    //报警
                    LogEventManager.LogEvent(30, $"浓度测量-测浓托盘1码 {rackIdArr[0]} 在工单不存在");
                    return 0;
                }
            }
            else//MES在线模式  传码MES下发
            {

            }

            //托盘2
            if (rackIdArr.Length < 2)
            {
                //报警
                LogEventManager.LogEvent(30, $"浓度测量-测浓托盘2码记忆为空");
                return 0;
            }
            if (CommonParam.s_MES_Mode == MES_Mode.Off_Line)//MES离线模式
            {
                //判断托盘2码与CommonParam.s_WorkOrderMapHoleInfoList中是否存在
                if (CommonParam.s_WorkOrderMapHoleInfoList.Exists(info => info.RackID == rackIdArr[1]))
                {
                    findListTest2 = CommonParam.s_WorkOrderMapHoleInfoList.FindAll(info => info != null && info.RackID == rackIdArr[1]);
                    SqlHelper.SetStringValue("ConcentrationMeasurementRackID2", rackIdArr[1]);
                    LogEventManager.LogEvent(30, $"浓度测量-入站2 {rackIdArr[1]}");
                }
                else
                {
                    //报警
                    LogEventManager.LogEvent(30, $"浓度测量-测浓托盘2码 {rackIdArr[1]} 在工单不存在");
                    return 0;
                }
            }
            else//MES在线模式  传码MES下发
            {

            }

            //判断是否有托盘3
            if (rackIdArr.Length > 2)
            {
                //托盘3
                if (CommonParam.s_MES_Mode == MES_Mode.Off_Line)//MES离线模式
                {
                    //判断托盘3码与CommonParam.s_WorkOrderMapHoleInfoList中是否存在
                    if (CommonParam.s_WorkOrderMapHoleInfoList.Exists(info => info.RackID == rackIdArr[2]))
                    {
                        findListTest3 = CommonParam.s_WorkOrderMapHoleInfoList.FindAll(info => info != null && info.RackID == rackIdArr[2]);
                        SqlHelper.SetStringValue("ConcentrationMeasurementRackID3", rackIdArr[2]);
                        LogEventManager.LogEvent(30, $"浓度测量-入站3 {rackIdArr[2]}");
                    }
                    else
                    {
                        //报警
                        LogEventManager.LogEvent(30, $"浓度测量-测浓托盘3码 {rackIdArr[2]} 在工单不存在");
                        return 0;
                    }
                }
                else//MES在线模式  传码MES下发
                {

                }
            }
            else
            {
                return 2;
            }
            return 3;
        }

      

        #region bf
        //public override void Execute(CancellationToken token)
        //{
        //    // 浓度测量执行逻辑
        //    Task.Run(() =>
        //    {
        //        while (!token.IsCancellationRequested)
        //        {
        //            if (CommonParam.s_DeviceState != DeviceState.Running)
        //            {
        //                Thread.Sleep(200);
        //                continue;
        //            }
        //            switch (_ConcentrationMeasurementStepIndex)
        //            {
        //                case 0:
        //                    if (!CommonParam.s_ModeBools[0])
        //                    {
        //                        //等待设备1进站完成信号
        //                        LogEventManager.LogEvent(31, $"浓度测量-等待设备1进站完成信号");
        //                        while (!token.IsCancellationRequested)
        //                        {
        //                            if (CommonParam.s_BoardingCompleteS1)
        //                            {
        //                                CommonParam.s_BoardingCompleteS1 = false;
        //                                _ConcentrationMeasurementStepIndex = 1;
        //                                break;
        //                            }
        //                            Thread.Sleep(10);
        //                        }
        //                    }
        //                    break;
        //                case 1:
        //                    //等待酶标板扫码完成信号
        //                    LogEventManager.LogEvent(31, $"浓度测量-等待酶标板扫码完成信号");
        //                    while (!token.IsCancellationRequested)
        //                    {
        //                        short[] registerValues_1100 = CommonParam.s_PLC.Read_Register_State_Short(1, 1100, 1);
        //                        if (registerValues_1100[0] == 1)
        //                        {
        //                            CommonParam.s_PLC.Write_Register_Value(1, 1100, "0", "short");
        //                            _ConcentrationMeasurementStepIndex = 10;
        //                            break;
        //                        }
        //                        Thread.Sleep(10);
        //                    }
        //                    break;
        //                case 10:
        //                    //读取酶标板二维码
        //                    string code = CommonParam.s_PLC.Read_Register_State_ACSII(1, 1101, 20);
        //                    LogEventManager.LogEvent(31, $"浓度测量-酶标板扫码完成: {code}");
        //                    //short[] shorts = new short[20];
        //                    //CommonParam.s_PLC.Fun10(1, 1101, shorts);
        //                    //更新 Microplate ID 参数
        //                    RackCount = CommonParam.s_WorkOrderKeyS1.Split(',').Length;
        //                    if (CommonParam.s_WorkOrderKeyS1.Length > 0)
        //                        CommonParam.s_MapHoleInfoListS1_1.ForEach(item => { item.Progress = "测浓中"; item.MicroplateID = code; });
        //                    if (CommonParam.s_WorkOrderKeyS1.Length > 1)
        //                        CommonParam.s_MapHoleInfoListS1_2.ForEach(item => { item.Progress = "测浓中"; item.MicroplateID = code; });
        //                    if (CommonParam.s_WorkOrderKeyS1.Length > 2)
        //                        CommonParam.s_MapHoleInfoListS1_3.ForEach(item => { item.Progress = "测浓中"; item.MicroplateID = code; });
        //                    _ConcentrationMeasurementStepIndex = 11;
        //                    break;
        //                case 11:
        //                    LogEventManager.LogEvent(31, $"浓度测量-等待酶标板点样完成信号");
        //                    string nowStr = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");
        //                    while (!token.IsCancellationRequested)
        //                    {
        //                        short[] registerValues_2080 = CommonParam.s_PLC.Read_Register_State_Short(1, 2080, 1);
        //                        if (registerValues_2080[0] == 1)
        //                        {
        //                            CommonParam.s_PLC.Write_Register_Value(1, 2080, "0", "short");
        //                            //更新 Load Time 参数
        //                            if (CommonParam.s_WorkOrderKeyS1.Length > 0)
        //                                CommonParam.s_MapHoleInfoListS1_1.ForEach(item => { item.LoadTime = nowStr; });
        //                            if (CommonParam.s_WorkOrderKeyS1.Length > 1)
        //                                CommonParam.s_MapHoleInfoListS1_2.ForEach(item => { item.LoadTime = nowStr; });
        //                            if (CommonParam.s_WorkOrderKeyS1.Length > 2)
        //                                CommonParam.s_MapHoleInfoListS1_3.ForEach(item => { item.LoadTime = nowStr; });
        //                            _ConcentrationMeasurementStepIndex = 20;
        //                            break;
        //                        }
        //                        Thread.Sleep(10);
        //                    }
        //                    break;
        //                case 20:
        //                    LogEventManager.LogEvent(31, $"浓度测量-等待请求打开酶标仪仓门信号");
        //                    while (!token.IsCancellationRequested)
        //                    {
        //                        short[] registerValues_1951 = CommonParam.s_PLC.Read_Register_State_Short(1, 1951, 1);
        //                        if (registerValues_1951[0] == 1)
        //                        {
        //                            CommonParam.s_PLC.Write_Register_Value(1, 1951, "0", "short");
        //                            _ConcentrationMeasurementStepIndex = 30;
        //                            break;
        //                        }
        //                        Thread.Sleep(10);
        //                    }
        //                    break;
        //                case 30:
        //                    LogEventManager.LogEvent(31, $"浓度测量-打开酶标仪仓门");
        //                    //调用酶标仪开门方法
        //                    CommonParam.s_Magellan.PlateOut();
        //                    //等待酶标仪仓门打开完成信号
        //                    //暂时给延时
        //                    Thread.Sleep(1000);
        //                    CommonParam.s_PLC.Write_Register_Value(1, 2951, "1", "short");
        //                    _ConcentrationMeasurementStepIndex = 40;
        //                    break;
        //                case 40:
        //                    //等待酶标板放料ok信号
        //                    LogEventManager.LogEvent(31, $"浓度测量-等待酶标板放料ok信号");
        //                    while (!token.IsCancellationRequested)
        //                    {
        //                        short[] registerValues_1952 = CommonParam.s_PLC.Read_Register_State_Short(1, 1952, 1);
        //                        if (registerValues_1952[0] == 1)
        //                        {
        //                            CommonParam.s_PLC.Write_Register_Value(1, 1952, "0", "short");
        //                            LogEventManager.LogEvent(31, $"浓度测量-收到PLC酶标板放料ok信号");
        //                            _ConcentrationMeasurementStepIndex = 50;
        //                            break;
        //                        }
        //                        Thread.Sleep(10);
        //                    }
        //                    break;
        //                case 50:
        //                    //调用酶标仪关门方法
        //                    LogEventManager.LogEvent(31, $"浓度测量-关闭酶标仪仓门");
        //                    CommonParam.s_Magellan.PlateIn();
        //                    //暂时给延时
        //                    Thread.Sleep(1000);
        //                    _ConcentrationMeasurementStepIndex = 60;
        //                    break;
        //                case 60:
        //                    LogEventManager.LogEvent(31, $"浓度测量-酶标仪开始检测");
        //                    CommonParam.s_Magellan.StartRun();
        //                    _ConcentrationMeasurementStepIndex = 70;
        //                    break;
        //                case 70:
        //                    //等待检测完成
        //                    while (!token.IsCancellationRequested)
        //                    {
        //                        _testResult = CommonParam.s_Magellan.GetRunStatusAsText();
        //                        if (_testResult != "测量进行中")
        //                        {
        //                            _ConcentrationMeasurementStepIndex = 80;
        //                            break;
        //                        }
        //                        Thread.Sleep(10);
        //                    }
        //                    break;
        //                case 80:
        //                    //检测完成
        //                    LogEventManager.LogEvent(31, $"浓度测量-酶标仪检测完成，结果: {_testResult}");
        //                    nowStr = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");
        //                    if (RackCount > 0)
        //                    {
        //                        CommonParam.s_MapHoleInfoListS1_1.ForEach(item => { item.Progress = "测浓完成"; item.Concentration_Test_Time = nowStr; item.Test_Operator = CommonParam.s_CurrentUser.UserName; });
        //                    }
        //                    if (RackCount > 1)
        //                    {
        //                        CommonParam.s_MapHoleInfoListS1_2.ForEach(item => { item.Progress = "测浓完成"; item.Concentration_Test_Time = nowStr; item.Test_Operator = CommonParam.s_CurrentUser.UserName; });
        //                    }
        //                    if (RackCount > 2)
        //                    {
        //                        CommonParam.s_MapHoleInfoListS1_3.ForEach(item => { item.Progress = "测浓完成"; item.Concentration_Test_Time = nowStr; item.Test_Operator = CommonParam.s_CurrentUser.UserName; });
        //                    }
        //                    if (_testResult == "测量成功结束")
        //                    {
        //                        //获取检测结果
        //                        string[] dataNamesStr = CommonParam.s_Magellan.GetDataNames();
        //                        bool hasAsc = dataNamesStr.Contains("A260");
        //                        hasAsc = dataNamesStr.Contains("A280");
        //                        hasAsc = dataNamesStr.Contains("A340");
        //                        if (!hasAsc)
        //                        {
        //                            LogEventManager.LogEvent(30, $"浓度测量-酶标仪检测方法错误，检查是否是A260,A280,A340的方法");
        //                        }
        //                        tableArrA260 = new string[16, 24];
        //                        tableArrA280 = new string[16, 24];
        //                        tableArrA340 = new string[16, 24];
        //                        dataA260Str = CommonParam.s_Magellan.DataAsTable(dataNamesStr[0]);
        //                        dataA280Str = CommonParam.s_Magellan.DataAsTable(dataNamesStr[1]);
        //                        dataA340Str = CommonParam.s_Magellan.DataAsTable(dataNamesStr[2]);
        //                        if (dataA260Str != null && dataA280Str != null && dataA340Str != null)
        //                        {
        //                            tableArrA260 = BuildTableStringArray(dataA260Str);
        //                            tableArrA280 = BuildTableStringArray(dataA280Str);
        //                            tableArrA340 = BuildTableStringArray(dataA340Str);
        //                            OnDataRestChanged?.Invoke(dataA260Str, dataA280Str, dataA340Str);
        //                        }
        //                        _ConcentrationMeasurementStepIndex = 90;
        //                    }
        //                    else
        //                    {
        //                        //异常处理
        //                        if (RackCount > 0)
        //                            CommonParam.s_MapHoleInfoListS1_1.ForEach(item => { item.State = MapHoleInfoState.Abnormal; });
        //                        if (RackCount > 1)
        //                            CommonParam.s_MapHoleInfoListS1_2.ForEach(item => { item.State = MapHoleInfoState.Abnormal; });
        //                        if (RackCount > 2)
        //                            CommonParam.s_MapHoleInfoListS1_3.ForEach(item => { item.State = MapHoleInfoState.Abnormal; });
        //                        LogEventManager.LogEvent(30, $"浓度测量-酶标仪检测结果异常结束，不解析");
        //                        _ConcentrationMeasurementStepIndex = 100;
        //                    }
        //                    break;
        //                case 90:
        //                    LogEventManager.LogEvent(31, $"浓度测量-解析检测结果");
        //                    //更新 OD_Value Test_Operator Concentration_Test_Time Final_Protein_Mass 参数
        //                    UpdateParameter();
        //                    _ConcentrationMeasurementStepIndex = 100;
        //                    break;
        //                case 100:
        //                    LogEventManager.LogEvent(31, $"浓度测量-测量完成打开酶标仪仓门");
        //                    //调用酶标仪开门方法
        //                    CommonParam.s_Magellan.PlateOut();
        //                    //等待酶标仪仓门打开完成信号
        //                    //暂时给延时
        //                    Thread.Sleep(1000);
        //                    CommonParam.s_PLC.Write_Register_Value(1, 2952, "1", "short");
        //                    _ConcentrationMeasurementStepIndex = 110;
        //                    break;
        //                case 110:
        //                    //等待酶标板放料ok信号
        //                    LogEventManager.LogEvent(31, $"浓度测量-等待酶标板取料ok信号");
        //                    while (!token.IsCancellationRequested)
        //                    {
        //                        short[] registerValues_1953 = CommonParam.s_PLC.Read_Register_State_Short(1, 1953, 1);
        //                        if (registerValues_1953[0] == 1)
        //                        {
        //                            CommonParam.s_PLC.Write_Register_Value(1, 1953, "0", "short");
        //                            LogEventManager.LogEvent(31, $"浓度测量-收到PLC酶标板取料ok信号，酶标仪关门");
        //                            //调用酶标仪关门方法
        //                            CommonParam.s_Magellan.PlateIn();
        //                            _ConcentrationMeasurementStepIndex = 0;
        //                            break;
        //                        }
        //                        Thread.Sleep(10);
        //                    }
        //                    break;
        //            }
        //        }

        //    });
        //}
        #endregion
        public static DataTable ParseGridToDataTable(string tableText)
        {
            DataTable dt = new DataTable();
            try
            {
                // 1. 添加列：第一列=行标签，后面24列 1~24
                dt.Columns.Add("<>", typeof(string));
                for (int col = 1; col <= 24; col++)
                {
                    dt.Columns.Add(col.ToString(), typeof(double));
                }
                // 按换行分割所有行
                string[] lines = tableText.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    string trimLine = line.Trim();
                    //跳过表头第一行 <>	1	2	3...
                    if (trimLine.StartsWith("<>"))
                        continue;
                    //按制表符\t分割单元格
                    string[] cells = trimLine.Split(new[] { '\t' }, StringSplitOptions.None);

                    DataRow dr = dt.NewRow();
                    //第一个单元格：行标签 A/B/C...
                    dr["<>"] = cells[0];

                    //循环24列
                    for (int c = 1; c <= 24; c++)
                    {
                        //if (c >= cells.Length)
                        //{
                        //    dr[c.ToString()] = DBNull.Value;
                        //    continue;
                        //}
                        string cellValue = cells[c].Trim();
                        if (cellValue == "###")
                        {
                            dr[c.ToString()] = DBNull.Value;
                        }
                        else
                        {
                            //转成double数值
                            if (double.TryParse(cellValue, out double val))
                            {
                                dr[c.ToString()] = val;
                            }
                            else
                            {
                                dr[c.ToString()] = DBNull.Value;
                            }
                        }
                    }
                    dt.Rows.Add(dr);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"解析行异常：{ex.Message}");
                LogEventManager.LogEvent(31, $"Magellan-解析异常：{ex.Message}");
            }
            return dt;
        }

        /// <summary>
        /// 根据行号字母(A‑P)，列号(1‑24) 获取单元格值，###返回null
        /// </summary>
        private double? GetCellValue(string tableText, string rowTag, int colNum)
        {
            string[] lines = tableText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var trimLine = line.Trim();
                if (trimLine.StartsWith("<>")) continue; //跳过表头

                string[] cells = trimLine.Split(new[] { '\t' }, StringSplitOptions.None);

                //第一格就是行标签 A / F / G
                string currentRow = cells[0].Trim();
                if (currentRow.Equals(rowTag, StringComparison.OrdinalIgnoreCase))
                {
                    //列号从1开始，数组下标=colNum
                    int index = colNum;
                    if (index >= cells.Length) return null;

                    string cell = cells[index].Trim();
                    if (cell == "###")
                        return null;

                    if (double.TryParse(cell, out double val))
                        return val;
                    return null;
                }
            }
            //没有找到这一行
            return null;
        }

        /// <summary>
        /// 根据文本表格生成二维string数组
        /// 行：A-P，列：1~24
        /// </summary>
        /// <param name="tableText">原始制表符分隔文本</param>
        /// <returns>string[行索引,列索引]，找不到/### 返回null</returns>
        public static string[,] BuildTableStringArray(string tableText)
        {
            string[] lines = tableText.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);

            // A-P 一共16行：A=0, B=1 ... P=15
            int rowCount = 16;
            // 列 1~24，共24列，下标0对应第1列
            int colCount = 24;
            string[,] result = new string[rowCount, colCount];

            foreach (var line in lines)
            {
                var trimLine = line.Trim();
                if (trimLine.StartsWith("<>")) continue;

                string[] cells = trimLine.Split(new[] { '\t' }, StringSplitOptions.None);
                string currentRowTag = cells[0].Trim();

                // 把行字母A-P转为数组下标 A=0, B=1 ... P=15
                if (currentRowTag.Length != 1) continue;
                char rowChar = char.ToUpper(currentRowTag[0]);
                if (rowChar < 'A' || rowChar > 'P') continue;
                int rowIndex = rowChar - 'A';

                // 遍历1~24列
                for (int col = 1; col <= colCount; col++)
                {
                    int cellIndex = col;
                    if (cellIndex >= cells.Length)
                    {
                        result[rowIndex, col - 1] = null;
                        continue;
                    }
                    string cell = cells[cellIndex].Trim();
                    if (cell == "###")
                    {
                        result[rowIndex, col - 1] = null;
                    }
                    else
                    {
                        result[rowIndex, col - 1] = cell;
                    }
                }
            }
            return result;
        }

        private void UpdateParameter()
        {
            //校正A260 = A260 - A340
            //蛋白浓度 = 校正 A260 ÷（消光系数 × 光程）
            //自动判定 A260/A280 纯度比值是否在合格区间；
            //tableArrA260
            //tableArrA280
            //tableArrA340
            //读取单元格（下标规则）
            //行：A = 0，B = 1 … P = 15
            //列：第 1 列 = 0，第 2 列 = 1 … 第 24 列 = 23
            if (mapHoleInfoListS2_1.Count > 0)
            {
                foreach (var item in mapHoleInfoListS2_1)
                {
                    double A260 = 0, A280 = 0, A340 = 0, OD = 0, Protein_Mass = 0, ExtinctionCoefficient = 0, ReagentVolume = 0;
                    switch (item.Location)
                    {
                        case "A1":
                            UpdateParameterLogic(0, 6, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "A2":
                            UpdateParameterLogic(0, 7, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "A3":
                            UpdateParameterLogic(1, 6, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "A4":
                            UpdateParameterLogic(1, 7, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "A5":
                            UpdateParameterLogic(2, 6, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "A6":
                            UpdateParameterLogic(2, 7, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "B1":
                            UpdateParameterLogic(0, 8, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "B2":
                            UpdateParameterLogic(0, 9, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "B3":
                            UpdateParameterLogic(1, 8, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "B4":
                            UpdateParameterLogic(1, 9, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "B5":
                            UpdateParameterLogic(2, 8, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "B6":
                            UpdateParameterLogic(2, 9, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "C1":
                            UpdateParameterLogic(0, 10, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "C2":
                            UpdateParameterLogic(0, 11, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "C3":
                            UpdateParameterLogic(1, 10, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "C4":
                            UpdateParameterLogic(1, 11, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "C5":
                            UpdateParameterLogic(2, 10, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "C6":
                            UpdateParameterLogic(2, 11, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "D1":
                            UpdateParameterLogic(0, 12, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "D2":
                            UpdateParameterLogic(0, 13, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "D3":
                            UpdateParameterLogic(1, 12, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "D4":
                            UpdateParameterLogic(1, 13, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "D5":
                            UpdateParameterLogic(2, 12, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "D6":
                            UpdateParameterLogic(2, 13, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                    }
                }
                CommonParam.SaveMapHoleInfosToSQL(mapHoleInfoListS2_1);
                CommonParam.s_MapHoleInfoListS1_1 = mapHoleInfoListS2_1;

            }
            if (mapHoleInfoListS2_2.Count > 0)
            {
                foreach (var item in mapHoleInfoListS2_2)
                {
                    double A260 = 0, A280 = 0, A340 = 0, OD = 0, Protein_Mass = 0, ExtinctionCoefficient = 0, ReagentVolume = 0;
                    switch (item.Location)
                    {
                        case "A1":
                            UpdateParameterLogic(3, 6, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "A2":
                            UpdateParameterLogic(3, 7, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "A3":
                            UpdateParameterLogic(4, 6, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "A4":
                            UpdateParameterLogic(4, 7, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "A5":
                            UpdateParameterLogic(5, 6, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "A6":
                            UpdateParameterLogic(5, 7, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "B1":
                            UpdateParameterLogic(3, 8, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "B2":
                            UpdateParameterLogic(3, 9, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "B3":
                            UpdateParameterLogic(4, 8, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "B4":
                            UpdateParameterLogic(4, 9, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "B5":
                            UpdateParameterLogic(5, 8, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "B6":
                            UpdateParameterLogic(5, 9, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "C1":
                            UpdateParameterLogic(3, 10, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "C2":
                            UpdateParameterLogic(3, 11, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "C3":
                            UpdateParameterLogic(4, 10, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "C4":
                            UpdateParameterLogic(4, 11, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "C5":
                            UpdateParameterLogic(5, 10, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "C6":
                            UpdateParameterLogic(5, 11, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "D1":
                            UpdateParameterLogic(3, 12, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "D2":
                            UpdateParameterLogic(3, 13, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "D3":
                            UpdateParameterLogic(4, 12, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "D4":
                            UpdateParameterLogic(4, 13, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "D5":
                            UpdateParameterLogic(5, 12, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "D6":
                            UpdateParameterLogic(5, 13, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                    }
                }
                CommonParam.SaveMapHoleInfosToSQL(mapHoleInfoListS2_2);
                CommonParam.s_MapHoleInfoListS1_2 = mapHoleInfoListS2_2;
            }
            if (mapHoleInfoListS2_3.Count > 0)
            {
                foreach (var item in mapHoleInfoListS2_3)
                {
                    double A260 = 0, A280 = 0, A340 = 0, OD = 0, Protein_Mass = 0, ExtinctionCoefficient = 0, ReagentVolume = 0;
                    switch (item.Location)
                    {
                        case "A1":
                            UpdateParameterLogic(6, 6, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "A2":
                            UpdateParameterLogic(6, 7, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "A3":
                            UpdateParameterLogic(7, 6, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "A4":
                            UpdateParameterLogic(7, 7, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "A5":
                            UpdateParameterLogic(8, 6, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "A6":
                            UpdateParameterLogic(8, 7, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "B1":
                            UpdateParameterLogic(6, 8, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "B2":
                            UpdateParameterLogic(6, 9, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "B3":
                            UpdateParameterLogic(7, 8, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "B4":
                            UpdateParameterLogic(7, 9, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "B5":
                            UpdateParameterLogic(8, 8, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "B6":
                            UpdateParameterLogic(8, 9, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "C1":
                            UpdateParameterLogic(6, 10, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "C2":
                            UpdateParameterLogic(6, 11, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "C3":
                            UpdateParameterLogic(7, 10, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "C4":
                            UpdateParameterLogic(7, 11, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "C5":
                            UpdateParameterLogic(8, 10, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "C6":
                            UpdateParameterLogic(8, 11, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "D1":
                            UpdateParameterLogic(6, 12, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "D2":
                            UpdateParameterLogic(6, 13, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "D3":
                            UpdateParameterLogic(7, 12, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "D4":
                            UpdateParameterLogic(7, 13, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "D5":
                            UpdateParameterLogic(8, 12, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                        case "D6":
                            UpdateParameterLogic(8, 13, A260, A280, A340, OD, Protein_Mass, ExtinctionCoefficient, ReagentVolume, item);
                            break;
                    }
                }
                CommonParam.SaveMapHoleInfosToSQL(mapHoleInfoListS2_3);
                CommonParam.s_MapHoleInfoListS1_3 = mapHoleInfoListS2_3;
            }      
        }
        private void UpdateParameterLogic(int row,int col,double A260, double A280, double A340, double OD, 
            double Protein_Mass, double ExtinctionCoefficient, double ReagentVolume, MapHoleInfo mapHoleInfo)
        {
            double.TryParse(tableArrA260[row, col], out A260);
            double.TryParse(tableArrA280[row, col], out A280);
            double.TryParse(tableArrA340[row, col], out A340);
            if (!Check(A260/ A280, concentrationMeasurementParam.RatioLowerLimit, concentrationMeasurementParam.LowerOperator,
                concentrationMeasurementParam.RatioUpperLimit, concentrationMeasurementParam.UpperOperator))
            {
                mapHoleInfo.State = MapHoleInfoState.Abnormal;
            }
            double.TryParse(mapHoleInfo.ExtinctionCoefficient, out ExtinctionCoefficient);
            double.TryParse(mapHoleInfo.ReagentVolume, out ReagentVolume);
            OD = A260 - A340;
            Protein_Mass = OD / (ExtinctionCoefficient * concentrationMeasurementParam.OpticalDistance) * ReagentVolume;
            mapHoleInfo.OD_Value = OD.ToString();
            mapHoleInfo.Final_Protein_Mass = Protein_Mass.ToString();

        }

        public bool Check(double value,double ratioLowerLimit, string lowerOperator, double ratioUpperLimit, string upperOperator)
        {
            bool result = false;
            switch (lowerOperator)
            {
                case ">":
                    result = value > ratioLowerLimit;
                    break;
                case ">=":
                    result = value >= ratioLowerLimit;
                    break;
                default:
                    result = false;
                    break;
            }
            if (result)
            {
                switch (upperOperator)
                {
                    case "<":
                        result = value > ratioUpperLimit;
                        break;
                    case "<=":
                        result = value >= ratioUpperLimit;
                        break;
                    default:
                        result = false;
                        break;
                }
            }
            return result;
        }

    }
}
