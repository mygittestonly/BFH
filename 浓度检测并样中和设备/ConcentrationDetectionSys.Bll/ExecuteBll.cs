using ConcentrationDetectionSys.Common;
using ConcentrationDetectionSys.DAL;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConcentrationDetectionSys.Bll
{
    public class ExecuteBll
    {
        public CancellationTokenSource cts;
        private ProcessScheme _runningProcessScheme = new ProcessScheme();
        public bool isRunning = false;
        private int _pullInScanCodesStation1StepIndex = 0;
        public ExecuteBll()
        {
        }
        public void Execute(ProcessScheme runningProcessScheme)
        {
            isRunning = true;
            cts = new CancellationTokenSource();
            _runningProcessScheme = runningProcessScheme;
            CommonParam.s_ModeBools = new bool[5];
            foreach (BaseStep step in _runningProcessScheme.Steps)
            {
                switch (step.StepTypeName)
                {
                    case "称重":
                        CommonParam.s_ModeBools[0] = true;
                        break;
                    case "随样测浓":
                        CommonParam.s_ModeBools[1] = true;
                        break;
                    case "并样":
                        CommonParam.s_ModeBools[2] = true;
                        break;
                    case "中和":
                        CommonParam.s_ModeBools[3] = true;
                        break;
                    case "振荡":
                        CommonParam.s_ModeBools[4] = true;
                        break;
                }
            }
            if (CommonParam.s_ModeBools[0] || CommonParam.s_ModeBools[1])
            {
                //设备1进站扫码
                PullInScanCodesStation1(cts.Token);
            }
            foreach (BaseStep step in _runningProcessScheme.Steps)
            {
                step.Execute(cts.Token);
            }
        }
        #region 进站扫码分析工单

        private string elutionRackID1, elutionRackID2, elutionRackID3, WorkOrderKeyS1;
        private int _rackIdCount = 0;
        private bool[] locationInfo = new bool[32];
        /// <summary>
        /// 第一台进站扫码分析工单
        /// </summary>
        public void PullInScanCodesStation1(CancellationToken token)
        {
            Task.Run(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    if (!CommonParam.AutoRunCondition)
                    {
                        Thread.Sleep(200);
                        continue;
                    }

                    //请求测浓托盘1点样点位信号
                    short[] registerValues_1200 = CommonParam.s_PLC.Read_Register_State_Short(1, 1200, 1);
                    if (registerValues_1200 != null && registerValues_1200[0] == 1)
                    {
                        SqlHelper.SetStringValue("RackID_S1", "");
                        CommonParam.s_PLC.Write_Register_Value(1, 1200, "0", "short");
                        LogEventManager.LogEvent(31, $"第一台进站-测浓托盘1点样点位信号触发");
                        elutionRackID1 = CommonParam.s_PLC.Read_Register_State_ACSII(1, 1201, 20);
                        //elutionRackID1 = "1E20260423-001";
                        if (elutionRackID1 == "")
                        {
                            LogEventManager.LogEvent(30, $"第一台进站-读取测浓托盘1码 {elutionRackID1} 失败");
                        }
                        else
                        {
                            LogEventManager.LogEvent(31, $"第一台进站-读取测浓托盘1码 {elutionRackID1} 成功");
                            short[] shorts = new short[20];
                            CommonParam.s_PLC.Fun10(1, 1201, shorts);
                            if (CommonParam.s_MES_Mode == MES_Mode.Off_Line)//MES离线模式
                            {
                                //判断托盘1码与CommonParam.s_WorkOrderMapHoleInfoList中是否存在
                                if (CommonParam.s_WorkOrderMapHoleInfoList.Exists(info => info.RackID == elutionRackID1))
                                {
                                    LogEventManager.LogEvent(31, $"第一台进站-测浓托盘1码 {elutionRackID1} 在工单存在");
                                    List<MapHoleInfo> findList = CommonParam.s_WorkOrderMapHoleInfoList.FindAll(info => info != null && info.RackID == elutionRackID1);
                                    findList.ForEach(item =>
                                    {
                                        item.State = MapHoleInfoState.Running;
                                        item.Progress = "进站扫码完成";
                                    });
                                    CommonParam.SaveMapHoleInfosToSQL(findList);
                                    CommonParam.s_MapHoleInfoListS1_1 = findList;
                                    WorkOrderKeyS1 = elutionRackID1;
                                    SqlHelper.SetStringValue("RackID_S1", WorkOrderKeyS1);
                                    LogEventManager.LogEvent(31, $"第一台进站-准备给PLC发送测浓托盘1位置信息");
                                    //遍历每一条工单
                                    foreach (WorkOrder wo in findList)
                                    {
                                        switch (wo.Location)
                                        {
                                            case "A1":
                                                locationInfo[0] = true;
                                                break;
                                            case "A2":
                                                locationInfo[1] = true;
                                                break;
                                            case "A3":
                                                locationInfo[2] = true;
                                                break;
                                            case "A4":
                                                locationInfo[3] = true;
                                                break;
                                            case "A5":
                                                locationInfo[4] = true;
                                                break;
                                            case "A6":
                                                locationInfo[5] = true;
                                                break;
                                            case "B1":
                                                locationInfo[6] = true;
                                                break;
                                            case "B2":
                                                locationInfo[7] = true;
                                                break;
                                            case "B3":
                                                locationInfo[8] = true;
                                                break;
                                            case "B4":
                                                locationInfo[9] = true;
                                                break;
                                            case "B5":
                                                locationInfo[10] = true;
                                                break;
                                            case "B6":
                                                locationInfo[11] = true;
                                                break;
                                            case "C1":
                                                locationInfo[12] = true;
                                                break;
                                            case "C2":
                                                locationInfo[13] = true;
                                                break;
                                            case "C3":
                                                locationInfo[14] = true;
                                                break;
                                            case "C4":
                                                locationInfo[15] = true;
                                                break;
                                            case "C5":
                                                locationInfo[16] = true;
                                                break;
                                            case "C6":
                                                locationInfo[17] = true;
                                                break;
                                            case "D1":
                                                locationInfo[18] = true;
                                                break;
                                            case "D2":
                                                locationInfo[19] = true;
                                                break;
                                            case "D3":
                                                locationInfo[20] = true;
                                                break;
                                            case "D4":
                                                locationInfo[21] = true;
                                                break;
                                            case "D5":
                                                locationInfo[22] = true;
                                                break;
                                            case "D6":
                                                locationInfo[23] = true;
                                                break;
                                        }
                                    }
                                    ushort registerValues_2301 = 0, registerValues_2302 = 0;
                                    CommonParam.s_PLC.Bool32ToTwoWord(locationInfo, out registerValues_2301, out registerValues_2302);
                                    CommonParam.s_PLC.Write_Register_Value(1, 2301, registerValues_2301.ToString(), "ushort");
                                    CommonParam.s_PLC.Write_Register_Value(1, 2302, registerValues_2302.ToString(), "ushort");
                                    CommonParam.s_PLC.Write_Register_Value(1, 2300, "1", "short");
                                    LogEventManager.LogEvent(31, $"第一台进站-给PLC发送测浓托盘1位置信息完成");
                                }
                                else
                                {
                                    //报警
                                    LogEventManager.LogEvent(30, $"第一台进站-测浓托盘1码 {elutionRackID1} 在工单不存在");
                                    CommonParam.s_PLC.Write_Register_Value(1, 2300, "2", "short");
                                }
                            }
                            else//MES在线模式
                            {

                            }
                        }
                    }

                    //请求测浓托盘2点样点位信号
                    short[] registerValues_1250 = CommonParam.s_PLC.Read_Register_State_Short(1, 1250, 1);
                    if (registerValues_1250 != null && registerValues_1250[0] == 1)
                    {
                        if (SqlHelper.GetString("RackID_S1").Split(',').Length != 1)
                        {
                            continue;
                        }
                        CommonParam.s_PLC.Write_Register_Value(1, 1250, "0", "short");
                        elutionRackID2 = CommonParam.s_PLC.Read_Register_State_ACSII(1, 1251, 20);
                        //elutionRackID2 = "2E20260423-001";
                        if (elutionRackID2 == "")
                        {
                            LogEventManager.LogEvent(30, $"第一台进站-读取测浓托盘2码 {elutionRackID2} 失败");
                        }
                        else
                        {
                            LogEventManager.LogEvent(31, $"第一台进站-读取测浓托盘2码 {elutionRackID2} 成功");
                            short[] shorts = new short[20];
                            CommonParam.s_PLC.Fun10(1, 1251, shorts);
                            if (CommonParam.s_MES_Mode == MES_Mode.Off_Line)//MES离线模式
                            {
                                //判断托盘2码与CommonParam.s_WorkOrderMapHoleInfoList中是否存在
                                if (CommonParam.s_WorkOrderMapHoleInfoList.Exists(info => info.RackID == elutionRackID2))
                                {
                                    LogEventManager.LogEvent(31, $"第一台进站-测浓托盘2码 {elutionRackID2} 在工单存在");
                                    List<MapHoleInfo> findList = CommonParam.s_WorkOrderMapHoleInfoList.FindAll(info => info != null && info.RackID == elutionRackID2);
                                    findList.ForEach(item =>
                                    {
                                        item.State = MapHoleInfoState.Running;
                                        item.Progress = "进站扫码完成";
                                    });
                                    CommonParam.SaveMapHoleInfosToSQL(findList);
                                    CommonParam.s_MapHoleInfoListS1_2 = findList;
                                    WorkOrderKeyS1 = SqlHelper.GetString("RackID_S1") + "," + elutionRackID2;
                                    string[] RackID_S1 = SqlHelper.GetString("RackID_S1").Split(',');
                                    if (!RackID_S1.Contains(elutionRackID2))
                                    {
                                        SqlHelper.SetStringValue("RackID_S1", WorkOrderKeyS1);
                                    }
                                    LogEventManager.LogEvent(31, $"第一台进站-准备给PLC发送测浓托盘2位置信息");
                                    //遍历每一条工单
                                    foreach (WorkOrder wo in findList)
                                    {
                                        switch (wo.Location)
                                        {
                                            case "A1":
                                                locationInfo[0] = true;
                                                break;
                                            case "A2":
                                                locationInfo[1] = true;
                                                break;
                                            case "A3":
                                                locationInfo[2] = true;
                                                break;
                                            case "A4":
                                                locationInfo[3] = true;
                                                break;
                                            case "A5":
                                                locationInfo[4] = true;
                                                break;
                                            case "A6":
                                                locationInfo[5] = true;
                                                break;
                                            case "B1":
                                                locationInfo[6] = true;
                                                break;
                                            case "B2":
                                                locationInfo[7] = true;
                                                break;
                                            case "B3":
                                                locationInfo[8] = true;
                                                break;
                                            case "B4":
                                                locationInfo[9] = true;
                                                break;
                                            case "B5":
                                                locationInfo[10] = true;
                                                break;
                                            case "B6":
                                                locationInfo[11] = true;
                                                break;
                                            case "C1":
                                                locationInfo[12] = true;
                                                break;
                                            case "C2":
                                                locationInfo[13] = true;
                                                break;
                                            case "C3":
                                                locationInfo[14] = true;
                                                break;
                                            case "C4":
                                                locationInfo[15] = true;
                                                break;
                                            case "C5":
                                                locationInfo[16] = true;
                                                break;
                                            case "C6":
                                                locationInfo[17] = true;
                                                break;
                                            case "D1":
                                                locationInfo[18] = true;
                                                break;
                                            case "D2":
                                                locationInfo[19] = true;
                                                break;
                                            case "D3":
                                                locationInfo[20] = true;
                                                break;
                                            case "D4":
                                                locationInfo[21] = true;
                                                break;
                                            case "D5":
                                                locationInfo[22] = true;
                                                break;
                                            case "D6":
                                                locationInfo[23] = true;
                                                break;
                                        }
                                    }
                                    ushort registerValues_2311 = 0, registerValues_2312 = 0;
                                    CommonParam.s_PLC.Bool32ToTwoWord(locationInfo, out registerValues_2311, out registerValues_2312);
                                    CommonParam.s_PLC.Write_Register_Value(1, 2311, registerValues_2311.ToString(), "ushort");
                                    CommonParam.s_PLC.Write_Register_Value(1, 2312, registerValues_2312.ToString(), "ushort");
                                    CommonParam.s_PLC.Write_Register_Value(1, 2310, "1", "short");
                                    LogEventManager.LogEvent(31, $"第一台进站-给PLC发送测浓托盘2位置信息完成");
                                }
                                else
                                {
                                    //报警
                                    LogEventManager.LogEvent(30, $"第一台进站-测浓托盘2码 {elutionRackID2} 在工单不存在");
                                    CommonParam.s_PLC.Write_Register_Value(1, 2310, "2", "short");
                                }
                            }
                            else//MES在线模式
                            {

                            }
                            if (CommonParam.s_ModeBools[0])
                            {
                                SqlHelper.SetStringValue("RackID2WeightStep", WorkOrderKeyS1);
                            }
                            if (CommonParam.s_ModeBools[1])
                            {
                                SqlHelper.SetStringValue("RackID2ConcentrationMeasurement", WorkOrderKeyS1);
                                LogEventManager.LogEvent(30, $"第一台进站-给测浓托盘码记忆{WorkOrderKeyS1}");
                            }
                        }
                    }

                    //请求测浓托盘3点样点位信号
                    short[] registerValues_1300 = CommonParam.s_PLC.Read_Register_State_Short(1, 1300, 1);
                    if (registerValues_1300 != null && registerValues_1300[0] == 1)
                    {
                        if (SqlHelper.GetString("RackID_S1").Split(',').Length != 2)
                        {
                            continue;
                        }
                        CommonParam.s_PLC.Write_Register_Value(1, 1300, "0", "short");
                        elutionRackID3 = CommonParam.s_PLC.Read_Register_State_ACSII(1, 1301, 20);
                        //elutionRackID3 = "3E20260423-001";
                        if (elutionRackID3 == "")
                        {
                            LogEventManager.LogEvent(30, $"第一台进站-读取测浓托盘3码 {elutionRackID3} 失败");
                        }
                        else
                        {
                            LogEventManager.LogEvent(31, $"第一台进站-读取测浓托盘3码 {elutionRackID3} 成功");
                            short[] shorts = new short[20];
                            CommonParam.s_PLC.Fun10(1, 1301, shorts);
                            if (CommonParam.s_MES_Mode == MES_Mode.Off_Line)//MES离线模式
                            {
                                //判断托盘3码与CommonParam.s_WorkOrderMapHoleInfoList中是否存在
                                if (CommonParam.s_WorkOrderMapHoleInfoList.Exists(info => info.RackID == elutionRackID3))
                                {
                                    LogEventManager.LogEvent(31, $"第一台进站-测浓托盘3码 {elutionRackID3} 在工单存在");
                                    List<MapHoleInfo> findList = CommonParam.s_WorkOrderMapHoleInfoList.FindAll(info => info != null && info.RackID == elutionRackID3);
                                    findList.ForEach(item =>
                                    {
                                        item.State = MapHoleInfoState.Running;
                                        item.Progress = "进站扫码完成";
                                    });
                                    CommonParam.SaveMapHoleInfosToSQL(findList);
                                    CommonParam.s_MapHoleInfoListS1_3 = findList;
                                    WorkOrderKeyS1 = SqlHelper.GetString("RackID_S1") + "," + elutionRackID3;
                                    string[] RackID_S1 = SqlHelper.GetString("RackID_S1").Split(',');
                                    if (!RackID_S1.Contains(elutionRackID3))
                                    {
                                        SqlHelper.SetStringValue("RackID_S1", WorkOrderKeyS1);
                                    }
                                    LogEventManager.LogEvent(31, $"第一台进站-准备给PLC发送测浓托盘3位置信息");
                                    //遍历每一条工单
                                    foreach (WorkOrder wo in findList)
                                    {
                                        switch (wo.Location)
                                        {
                                            case "A1":
                                                locationInfo[0] = true;
                                                break;
                                            case "A2":
                                                locationInfo[1] = true;
                                                break;
                                            case "A3":
                                                locationInfo[2] = true;
                                                break;
                                            case "A4":
                                                locationInfo[3] = true;
                                                break;
                                            case "A5":
                                                locationInfo[4] = true;
                                                break;
                                            case "A6":
                                                locationInfo[5] = true;
                                                break;
                                            case "B1":
                                                locationInfo[6] = true;
                                                break;
                                            case "B2":
                                                locationInfo[7] = true;
                                                break;
                                            case "B3":
                                                locationInfo[8] = true;
                                                break;
                                            case "B4":
                                                locationInfo[9] = true;
                                                break;
                                            case "B5":
                                                locationInfo[10] = true;
                                                break;
                                            case "B6":
                                                locationInfo[11] = true;
                                                break;
                                            case "C1":
                                                locationInfo[12] = true;
                                                break;
                                            case "C2":
                                                locationInfo[13] = true;
                                                break;
                                            case "C3":
                                                locationInfo[14] = true;
                                                break;
                                            case "C4":
                                                locationInfo[15] = true;
                                                break;
                                            case "C5":
                                                locationInfo[16] = true;
                                                break;
                                            case "C6":
                                                locationInfo[17] = true;
                                                break;
                                            case "D1":
                                                locationInfo[18] = true;
                                                break;
                                            case "D2":
                                                locationInfo[19] = true;
                                                break;
                                            case "D3":
                                                locationInfo[20] = true;
                                                break;
                                            case "D4":
                                                locationInfo[21] = true;
                                                break;
                                            case "D5":
                                                locationInfo[22] = true;
                                                break;
                                            case "D6":
                                                locationInfo[23] = true;
                                                break;
                                        }
                                    }
                                    ushort registerValues_2321 = 0, registerValues_2322 = 0;
                                    CommonParam.s_PLC.Bool32ToTwoWord(locationInfo, out registerValues_2321, out registerValues_2322);
                                    CommonParam.s_PLC.Write_Register_Value(1, 2321, registerValues_2321.ToString(), "ushort");
                                    CommonParam.s_PLC.Write_Register_Value(1, 2322, registerValues_2322.ToString(), "ushort");
                                    CommonParam.s_PLC.Write_Register_Value(1, 2320, "1", "short");
                                    LogEventManager.LogEvent(31, $"第一台进站-给PLC发送测浓托盘3位置信息完成");
                                    CommonParam.s_BoardingCompleteS1 = true;
                                }
                                else
                                {
                                    //报警
                                    LogEventManager.LogEvent(30, $"第一台进站-测浓托盘3码 {elutionRackID3} 在工单不存在");
                                    CommonParam.s_PLC.Write_Register_Value(1, 2320, "2", "short");
                                }
                            }
                            else//MES在线模式
                            {

                            }
                            if (CommonParam.s_ModeBools[0])
                            {
                                SqlHelper.SetStringValue("RackID2WeightStep", WorkOrderKeyS1);
                            }
                            if (CommonParam.s_ModeBools[1])
                            {
                                SqlHelper.SetStringValue("RackID2ConcentrationMeasurement", WorkOrderKeyS1);
                                LogEventManager.LogEvent(30, $"第一台进站-给测浓托盘码记忆{WorkOrderKeyS1}");
                            }
                        }
                    }

                    Thread.Sleep(100);
                }
            });
        }

        #endregion

        #region bf
        //public void PullInScanCodesStation1(CancellationToken token)
        //{
        //    Task.Run(() =>
        //    {
        //        while (!token.IsCancellationRequested)
        //        {
        //            if (CommonParam.s_DeviceState != DeviceState.Running)
        //            {
        //                Thread.Sleep(200);
        //                continue;
        //            }
        //            switch (_pullInScanCodesStation1StepIndex)
        //            {
        //                case 0:
        //                    //CommonParam.s_BoardingCompleteS1 = false;
        //                    locationInfo = new bool[32];
        //                    //判断几个托盘
        //                    if (CommonParam.s_ModeBools[2] || CommonParam.s_ModeBools[3])
        //                        _rackIdCount = 3;
        //                    else
        //                        _rackIdCount = 2;
        //                    //等待请求测浓托盘1点样点位信号
        //                    while (!token.IsCancellationRequested)
        //                    {
        //                        short[] registerValues_1200 = CommonParam.s_PLC.Read_Register_State_Short(1, 1200, 1);
        //                        if (registerValues_1200[0] == 1)
        //                        {
        //                            CommonParam.s_PLC.Write_Register_Value(1, 1200, "0", "short");
        //                            _pullInScanCodesStation1StepIndex = 2;
        //                            break;
        //                        }
        //                        Thread.Sleep(10);
        //                    }
        //                    break;
        //                case 2:
        //                    elutionRackID1 = CommonParam.s_PLC.Read_Register_State_ACSII(1, 1201, 20);
        //                    elutionRackID1 = "1E20260423-001";
        //                    if (elutionRackID1 == null)
        //                    {
        //                        LogEventManager.LogEvent(30, $"第一台进站-读取测浓托盘1码 {elutionRackID1} 失败");
        //                        _pullInScanCodesStation1StepIndex = 0;
        //                    }
        //                    else
        //                    {
        //                        LogEventManager.LogEvent(31, $"第一台进站-读取测浓托盘1码 {elutionRackID1} 成功");
        //                        short[] shorts = new short[20];
        //                        CommonParam.s_PLC.Fun10(1, 1201, shorts);
        //                        _pullInScanCodesStation1StepIndex = 10;
        //                    }
        //                    break;
        //                case 10:
        //                    //判断托盘1码与CommonParam.s_WorkOrderMapHoleInfoList中是否存在
        //                    if (CommonParam.s_WorkOrderMapHoleInfoList.Exists(info => info.RackID == elutionRackID1))
        //                    {
        //                        LogEventManager.LogEvent(31, $"第一台进站-测浓托盘1码 {elutionRackID1} 在工单存在");
        //                        CommonParam.s_MapHoleInfoListS1_1 = CommonParam.s_WorkOrderMapHoleInfoList.FindAll(info => info != null && info.RackID == elutionRackID1);
        //                        CommonParam.s_MapHoleInfoListS1_1.ForEach(item =>
        //                        {
        //                            item.State = MapHoleInfoState.Running;
        //                            item.Progress = "进站扫码完成";
        //                        });
        //                        CommonParam.s_WorkOrderKeyS1 = elutionRackID1;
        //                        _pullInScanCodesStation1StepIndex = 20;
        //                    }
        //                    else
        //                    {
        //                        //报警
        //                        LogEventManager.LogEvent(30, $"第一台进站-测浓托盘1码 {elutionRackID1} 在工单不存在");
        //                        CommonParam.s_PLC.Write_Register_Value(1, 2300, "2", "short");
        //                        _pullInScanCodesStation1StepIndex = 0;
        //                    }

        //                    break;
        //                case 20:
        //                    _pullInScanCodesStation1StepIndex = 30;
        //                    break;
        //                case 30:
        //                    LogEventManager.LogEvent(31, $"第一台进站-准备给PLC发送测浓托盘1位置信息");

        //                    //遍历每一条工单
        //                    foreach (WorkOrder wo in CommonParam.s_MapHoleInfoListS1_1)
        //                    {
        //                        switch (wo.Location)
        //                        {
        //                            case "A1":
        //                                locationInfo[0] = true;
        //                                break;
        //                            case "A2":
        //                                locationInfo[1] = true;
        //                                break;
        //                            case "A3":
        //                                locationInfo[2] = true;
        //                                break;
        //                            case "A4":
        //                                locationInfo[3] = true;
        //                                break;
        //                            case "A5":
        //                                locationInfo[4] = true;
        //                                break;
        //                            case "A6":
        //                                locationInfo[5] = true;
        //                                break;
        //                            case "B1":
        //                                locationInfo[6] = true;
        //                                break;
        //                            case "B2":
        //                                locationInfo[7] = true;
        //                                break;
        //                            case "B3":
        //                                locationInfo[8] = true;
        //                                break;
        //                            case "B4":
        //                                locationInfo[9] = true;
        //                                break;
        //                            case "B5":
        //                                locationInfo[10] = true;
        //                                break;
        //                            case "B6":
        //                                locationInfo[11] = true;
        //                                break;
        //                            case "C1":
        //                                locationInfo[12] = true;
        //                                break;
        //                            case "C2":
        //                                locationInfo[13] = true;
        //                                break;
        //                            case "C3":
        //                                locationInfo[14] = true;
        //                                break;
        //                            case "C4":
        //                                locationInfo[15] = true;
        //                                break;
        //                            case "C5":
        //                                locationInfo[16] = true;
        //                                break;
        //                            case "C6":
        //                                locationInfo[17] = true;
        //                                break;
        //                            case "D1":
        //                                locationInfo[18] = true;
        //                                break;
        //                            case "D2":
        //                                locationInfo[19] = true;
        //                                break;
        //                            case "D3":
        //                                locationInfo[20] = true;
        //                                break;
        //                            case "D4":
        //                                locationInfo[21] = true;
        //                                break;
        //                            case "D5":
        //                                locationInfo[22] = true;
        //                                break;
        //                            case "D6":
        //                                locationInfo[23] = true;
        //                                break;
        //                        }
        //                    }
        //                    ushort registerValues_2301 = 0, registerValues_2302 = 0;
        //                    CommonParam.s_PLC.Bool32ToTwoWord(locationInfo, out registerValues_2301, out registerValues_2302);
        //                    CommonParam.s_PLC.Write_Register_Value(1, 2301, registerValues_2301.ToString(), "ushort");
        //                    CommonParam.s_PLC.Write_Register_Value(1, 2302, registerValues_2302.ToString(), "ushort");
        //                    CommonParam.s_PLC.Write_Register_Value(1, 2300, "1", "short");
        //                    LogEventManager.LogEvent(31, $"第一台进站-给PLC发送测浓托盘1位置信息完成");
        //                    _pullInScanCodesStation1StepIndex = 40;
        //                    break;
        //                case 40:
        //                    if (_rackIdCount > 1)
        //                    {
        //                        _pullInScanCodesStation1StepIndex = 50;
        //                    }
        //                    else
        //                    {
        //                        _pullInScanCodesStation1StepIndex = 0;
        //                    }
        //                    break;
        //                case 50:
        //                    //等待请求测浓托盘2点样点位信号
        //                    while (!token.IsCancellationRequested)
        //                    {
        //                        short[] registerValues_1250 = CommonParam.s_PLC.Read_Register_State_Short(1, 1250, 1);
        //                        if (registerValues_1250[0] == 1)
        //                        {
        //                            CommonParam.s_PLC.Write_Register_Value(1, 1250, "0", "short");
        //                            _pullInScanCodesStation1StepIndex = 52;
        //                            break;
        //                        }
        //                        Thread.Sleep(10);
        //                    }

        //                    break;
        //                case 52:
        //                    elutionRackID2 = CommonParam.s_PLC.Read_Register_State_ACSII(1, 1251, 20);
        //                    elutionRackID2 = "2E20260423-001";
        //                    if (elutionRackID2 == null)
        //                    {
        //                        LogEventManager.LogEvent(30, $"第一台进站-读取测浓托盘2码 {elutionRackID2} 失败");
        //                        _pullInScanCodesStation1StepIndex = 50;
        //                    }
        //                    else
        //                    {
        //                        LogEventManager.LogEvent(31, $"第一台进站-读取测浓托盘2码 {elutionRackID2} 成功");
        //                        short[] shorts = new short[20];
        //                        CommonParam.s_PLC.Fun10(1, 1251, shorts);
        //                        _pullInScanCodesStation1StepIndex = 60;
        //                    }
        //                    break;
        //                case 60:
        //                    //判断托盘2码与CommonParam.s_WorkOrderMapHoleInfoList中是否存在
        //                    if (CommonParam.s_WorkOrderMapHoleInfoList.Exists(info => info.RackID == elutionRackID2))
        //                    {
        //                        LogEventManager.LogEvent(31, $"第一台进站-测浓托盘2码 {elutionRackID2} 在工单存在");
        //                        CommonParam.s_MapHoleInfoListS1_2 = CommonParam.s_WorkOrderMapHoleInfoList.FindAll(info => info != null && info.RackID == elutionRackID2);
        //                        CommonParam.s_MapHoleInfoListS1_2.ForEach(item =>
        //                        {
        //                            item.State = MapHoleInfoState.Running;
        //                            item.Progress = "进站扫码完成";
        //                        });
        //                        CommonParam.s_WorkOrderKeyS1 = elutionRackID1 + "," + elutionRackID2;
        //                        _pullInScanCodesStation1StepIndex = 70;
        //                    }
        //                    else
        //                    {
        //                        //报警
        //                        LogEventManager.LogEvent(30, $"第一台进站-测浓托盘2码 {elutionRackID2} 在工单不存在");
        //                        CommonParam.s_PLC.Write_Register_Value(1, 2310, "2", "short");
        //                        _pullInScanCodesStation1StepIndex = 0;
        //                    }
        //                    break;
        //                case 70:
        //                    LogEventManager.LogEvent(31, $"第一台进站-准备给PLC发送测浓托盘2位置信息");
        //                    ushort registerValues_2311 = 0, registerValues_2312 = 0;
        //                    CommonParam.s_PLC.Bool32ToTwoWord(locationInfo, out registerValues_2311, out registerValues_2312);
        //                    CommonParam.s_PLC.Write_Register_Value(1, 2311, registerValues_2311.ToString(), "ushort");
        //                    CommonParam.s_PLC.Write_Register_Value(1, 2312, registerValues_2312.ToString(), "ushort");
        //                    CommonParam.s_PLC.Write_Register_Value(1, 2310, "1", "short");
        //                    LogEventManager.LogEvent(31, $"第一台进站-给PLC发送测浓托盘2位置信息完成");
        //                    _pullInScanCodesStation1StepIndex = 80;
        //                    break;
        //                case 80:
        //                    if (_rackIdCount > 2)
        //                    {
        //                        _pullInScanCodesStation1StepIndex = 90;
        //                    }
        //                    else
        //                    {
        //                        _pullInScanCodesStation1StepIndex = 0;
        //                    }
        //                    break;
        //                case 90:
        //                    //等待请求测浓托盘3点样点位信号
        //                    while (!token.IsCancellationRequested)
        //                    {
        //                        short[] registerValues_1300 = CommonParam.s_PLC.Read_Register_State_Short(1, 1300, 1);
        //                        if (registerValues_1300[0] == 1)
        //                        {
        //                            CommonParam.s_PLC.Write_Register_Value(1, 1300, "0", "short");
        //                            _pullInScanCodesStation1StepIndex = 92;
        //                            break;
        //                        }
        //                        Thread.Sleep(10);
        //                    }
        //                    break;
        //                case 92:
        //                    elutionRackID3 = CommonParam.s_PLC.Read_Register_State_ACSII(1, 1301, 20);
        //                    elutionRackID3 = "3E20260423-001";
        //                    if (elutionRackID3 == null)
        //                    {
        //                        LogEventManager.LogEvent(30, $"第一台进站-读取测浓托盘3码 {elutionRackID3} 失败");
        //                        _pullInScanCodesStation1StepIndex = 90;
        //                    }
        //                    else
        //                    {
        //                        LogEventManager.LogEvent(31, $"第一台进站-读取测浓托盘3码 {elutionRackID3} 成功");
        //                        short[] shorts = new short[20];
        //                        CommonParam.s_PLC.Fun10(1, 1301, shorts);
        //                        _pullInScanCodesStation1StepIndex = 100;
        //                    }
        //                    break;
        //                case 100:
        //                    //判断托盘3码与CommonParam.s_WorkOrderMapHoleInfoList中是否存在
        //                    if (CommonParam.s_WorkOrderMapHoleInfoList.Exists(info => info.RackID == elutionRackID3))
        //                    {
        //                        LogEventManager.LogEvent(31, $"第一台进站-测浓托盘3码 {elutionRackID3} 在工单存在");
        //                        CommonParam.s_MapHoleInfoListS1_3 = CommonParam.s_WorkOrderMapHoleInfoList.FindAll(info => info != null && info.RackID == elutionRackID3);
        //                        CommonParam.s_MapHoleInfoListS1_3.ForEach(item =>
        //                        {
        //                            item.State = MapHoleInfoState.Running;
        //                            item.Progress = "进站扫码完成";
        //                        });
        //                        CommonParam.s_WorkOrderKeyS1 = elutionRackID1 + "," + elutionRackID2 + "," + elutionRackID3;
        //                        _pullInScanCodesStation1StepIndex = 110;
        //                    }
        //                    else
        //                    {
        //                        //报警
        //                        LogEventManager.LogEvent(30, $"第一台进站-测浓托盘3码 {elutionRackID3} 在工单不存在");
        //                        CommonParam.s_PLC.Write_Register_Value(1, 2320, "2", "short");
        //                        _pullInScanCodesStation1StepIndex = 0;
        //                    }
        //                    break;
        //                case 110:
        //                    LogEventManager.LogEvent(31, $"第一台进站-准备给PLC发送测浓托盘3位置信息");
        //                    ushort registerValues_2321 = 0, registerValues_2322 = 0;
        //                    CommonParam.s_PLC.Bool32ToTwoWord(locationInfo, out registerValues_2321, out registerValues_2322);
        //                    CommonParam.s_PLC.Write_Register_Value(1, 2321, registerValues_2321.ToString(), "ushort");
        //                    CommonParam.s_PLC.Write_Register_Value(1, 2322, registerValues_2322.ToString(), "ushort");
        //                    CommonParam.s_PLC.Write_Register_Value(1, 2320, "1", "short");
        //                    LogEventManager.LogEvent(31, $"第一台进站-给PLC发送测浓托盘3位置信息完成");
        //                    _pullInScanCodesStation1StepIndex = 0;
        //                    CommonParam.s_BoardingCompleteS1 = true;
        //                    break;
        //            }
        //        }
        //    });
        //}
        #endregion
    }
}
