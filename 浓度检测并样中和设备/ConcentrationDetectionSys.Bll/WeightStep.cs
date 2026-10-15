using ConcentrationDetectionSys.Common;
using ConcentrationDetectionSys.DAL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConcentrationDetectionSys.Bll
{
    /// <summary>
    /// 称重
    /// </summary>
    public class WeightStep : BaseStep
    {
        public WeightStep() : base("称重")
        {
        }
        private int _weightStepStepIndex = 0;
        private string[] rackIdArr;
        public override void Execute(CancellationToken token)
        {
            // 称重执行逻辑
            Task.Run(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    if (!CommonParam.AutoRunCondition)
                    {
                        Thread.Sleep(200);
                        continue;
                    }

                    //称重1结果上报触发
                    short[] registerValues_1400 = CommonParam.s_PLC.Read_Register_State_Short(1, 1400, 1);
                    if (registerValues_1400 != null && registerValues_1400[0] == 1)
                    {
                        LogEventManager.LogEvent(31, $"称重-等待称重1结果上报触发");
                        CommonParam.s_PLC.Write_Register_Value(1, 1400, "0", "short");
                        if(SqlHelper.GetString("RackID2WeightStep") == "")
                        {
                            //报警
                            LogEventManager.LogEvent(30, $"称重-托盘1码记忆为空");
                            continue;
                        }
                        rackIdArr = SqlHelper.GetString("RackID2WeightStep").Split(',');
                        List<MapHoleInfo> findList = new List<MapHoleInfo>();
                        if (CommonParam.s_MES_Mode == MES_Mode.Off_Line)//MES离线模式
                        {
                            //判断托盘1码与CommonParam.s_WorkOrderMapHoleInfoList中是否存在
                            if (CommonParam.s_WorkOrderMapHoleInfoList.Exists(info => info.RackID == rackIdArr[0]))
                            {
                                findList = CommonParam.s_WorkOrderMapHoleInfoList.FindAll(info => info != null && info.RackID ==  rackIdArr[0]);
                            }
                            else
                            {
                                //报警
                                LogEventManager.LogEvent(30, $"称重-测浓托盘1码 {rackIdArr[0]} 在工单不存在");
                                continue;
                            }
                        }
                        else//MES在线模式  传码MES下发
                        {

                        }
                        findList.ForEach(item =>
                        {
                            item.State = MapHoleInfoState.Running;
                            item.Progress = "称重完成";
                        });
                        float[] registerValues_1401_1424 = CommonParam.s_PLC.Read_Register_State_Float(1, 1401, 24);
                        
                        UpdateCollectionVolume(registerValues_1401_1424, findList);
                        CommonParam.SaveMapHoleInfosToSQL(findList);
                        CommonParam.s_MapHoleInfoListS1_1 = findList;
                        LogEventManager.LogEvent(31, $"称重-接收PLC称重托盘1数据成功");
                        float[] floats = new float[24];
                        CommonParam.s_PLC.WriteFloatArray(1, 1401, floats);
                        CommonParam.s_PLC.Write_Register_Value(1, 2400, "1", "short");
                    }


                    //称重2结果上报触发
                    short[] registerValues_1500 = CommonParam.s_PLC.Read_Register_State_Short(1, 1500, 1);
                    if (registerValues_1500 != null && registerValues_1500[0] == 1)
                    {
                        CommonParam.s_PLC.Write_Register_Value(1, 1500, "0", "short");
                        LogEventManager.LogEvent(31, $"称重-等待称重2结果上报触发");
                        if (rackIdArr.Length < 2)
                        {
                            //报警
                            LogEventManager.LogEvent(30, $"称重-测浓托盘2码记忆为空");
                            continue;
                        }
                        List<MapHoleInfo> findList = new List<MapHoleInfo>();
                        if (CommonParam.s_MES_Mode == MES_Mode.Off_Line)//MES离线模式
                        {
                            //判断托盘2码与CommonParam.s_WorkOrderMapHoleInfoList中是否存在
                            if (CommonParam.s_WorkOrderMapHoleInfoList.Exists(info => info.RackID == rackIdArr[1]))
                            {
                                findList = CommonParam.s_WorkOrderMapHoleInfoList.FindAll(info => info != null && info.RackID == rackIdArr[1]);
                            }
                            else
                            {
                                //报警
                                LogEventManager.LogEvent(30, $"称重-测浓托盘2码 {rackIdArr[1]} 在工单不存在");
                                continue;
                            }
                        }
                        else//MES在线模式  传码MES下发
                        {

                        }

                        findList.ForEach(item =>
                        {
                            item.State = MapHoleInfoState.Running;
                            item.Progress = "称重完成";
                        });
                        CommonParam.s_MapHoleInfoListS1_2 = findList;
                        float[] registerValues_1501_1524 = CommonParam.s_PLC.Read_Register_State_Float(1, 1501, 24);
                        UpdateCollectionVolume(registerValues_1501_1524, CommonParam.s_MapHoleInfoListS1_2);
                        CommonParam.SaveMapHoleInfosToSQL(CommonParam.s_MapHoleInfoListS1_2);
                        LogEventManager.LogEvent(31, $"称重-接收PLC称重托盘2数据成功");
                        float[] floats = new float[24];
                        CommonParam.s_PLC.WriteFloatArray(1, 1501, floats);
                        CommonParam.s_PLC.Write_Register_Value(1, 2500, "1", "short");

                        ////判断是否有测浓模块
                        //if (!CommonParam.s_ModeBools[1])
                        //{
                        //    CommonParam.s_MapHoleInfoListS1_1.Clear();
                        //    CommonParam.s_MapHoleInfoListS1_2.Clear();
                        //    CommonParam.s_MapHoleInfoListS1_3.Clear();
                        //    SqlHelper.SetStringValue("ElutionRackID1_S1", "");
                        //    SqlHelper.SetStringValue("ElutionRackID2_S1", "");
                        //    SqlHelper.SetStringValue("ElutionRackID3_S1", "");
                        //    SqlHelper.SetStringValue("WorkOrderKeyS1", "");
                        //}


                    }

                    Thread.Sleep(100);
                }
            });
        }

        private void UpdateCollectionVolume(float[] registerValues, List<MapHoleInfo> mapHoleInfos)
        {
            foreach (var item in mapHoleInfos)
            {
                switch (item.Location)
                {
                    case "A1":
                        item.ReagentVolume = registerValues[0].ToString();
                        if (!CommonParam.s_ModeBools[2])
                            item.Final_Volume = registerValues[0].ToString();
                        break;
                    case "A2":
                        item.ReagentVolume = registerValues[1].ToString();
                        if (!CommonParam.s_ModeBools[2])
                            item.Final_Volume = registerValues[1].ToString();
                        break;
                    case "A3":
                        item.ReagentVolume = registerValues[2].ToString();
                        if (!CommonParam.s_ModeBools[2])
                            item.Final_Volume = registerValues[2].ToString();
                        break;                                
                    case "A4":                               
                        item.ReagentVolume = registerValues[3].ToString();
                        if (!CommonParam.s_ModeBools[2])
                            item.Final_Volume = registerValues[3].ToString();
                        break;                               
                    case "A5":                               
                        item.ReagentVolume = registerValues[4].ToString();
                        if (!CommonParam.s_ModeBools[2])
                            item.Final_Volume = registerValues[4].ToString();
                        break;                               
                    case "A6":                                
                        item.ReagentVolume = registerValues[5].ToString();
                        if (!CommonParam.s_ModeBools[2])
                            item.Final_Volume = registerValues[5].ToString();
                        break;                               
                    case "B1":                               
                        item.ReagentVolume = registerValues[6].ToString();
                        if (!CommonParam.s_ModeBools[2])
                            item.Final_Volume = registerValues[6].ToString();
                        break;                               
                    case "B2":                               
                        item.ReagentVolume = registerValues[7].ToString();
                        if (!CommonParam.s_ModeBools[2])
                            item.Final_Volume = registerValues[7].ToString();
                        break;                                
                    case "B3":                               
                        item.ReagentVolume = registerValues[8].ToString();
                        if (!CommonParam.s_ModeBools[2])
                            item.Final_Volume = registerValues[8].ToString();
                        break;                                
                    case "B4":                             
                        item.ReagentVolume = registerValues[9].ToString();
                        if (!CommonParam.s_ModeBools[2])
                            item.Final_Volume = registerValues[9].ToString();
                        break;                              
                    case "B5":                                
                        item.ReagentVolume = registerValues[10].ToString();
                        if (!CommonParam.s_ModeBools[2])
                            item.Final_Volume = registerValues[10].ToString();
                        break;                               
                    case "B6":                               
                        item.ReagentVolume = registerValues[11].ToString();
                        if (!CommonParam.s_ModeBools[2])
                            item.Final_Volume = registerValues[11].ToString();
                        break;                              
                    case "C1":                                
                        item.ReagentVolume = registerValues[12].ToString();
                        if (!CommonParam.s_ModeBools[2])
                            item.Final_Volume = registerValues[12].ToString();
                        break;                                
                    case "C2":                               
                        item.ReagentVolume = registerValues[13].ToString();
                        if (!CommonParam.s_ModeBools[2])
                            item.Final_Volume = registerValues[13].ToString();
                        break;                               
                    case "C3":                               
                        item.ReagentVolume = registerValues[14].ToString();
                        if (!CommonParam.s_ModeBools[2])
                            item.Final_Volume = registerValues[14].ToString();
                        break;                                
                    case "C4":                               
                        item.ReagentVolume = registerValues[15].ToString();
                        if (!CommonParam.s_ModeBools[2])
                            item.Final_Volume = registerValues[15].ToString();
                        break;                                
                    case "C5":                                
                        item.ReagentVolume = registerValues[16].ToString();
                        if (!CommonParam.s_ModeBools[2])
                            item.Final_Volume = registerValues[16].ToString();
                        break;                                
                    case "C6":                               
                        item.ReagentVolume = registerValues[17].ToString();
                        if (!CommonParam.s_ModeBools[2])
                            item.Final_Volume = registerValues[17].ToString();
                        break;                               
                    case "D1":                                
                        item.ReagentVolume = registerValues[18].ToString();
                        if (!CommonParam.s_ModeBools[2])
                            item.Final_Volume = registerValues[18].ToString();
                        break;                               
                    case "D2":                                
                        item.ReagentVolume = registerValues[19].ToString();
                        if (!CommonParam.s_ModeBools[2])
                            item.Final_Volume = registerValues[19].ToString();
                        break;                               
                    case "D3":                               
                        item.ReagentVolume = registerValues[20].ToString();
                        if (!CommonParam.s_ModeBools[2])
                            item.Final_Volume = registerValues[20].ToString();
                        break;                               
                    case "D4":                               
                        item.ReagentVolume = registerValues[21].ToString();
                        if (!CommonParam.s_ModeBools[2])
                            item.Final_Volume = registerValues[21].ToString();
                        break;                                
                    case "D5":                               
                        item.ReagentVolume = registerValues[22].ToString();
                        if (!CommonParam.s_ModeBools[2])
                            item.Final_Volume = registerValues[22].ToString();
                        break;                                
                    case "D6":                              
                        item.ReagentVolume = registerValues[23].ToString();
                        if (!CommonParam.s_ModeBools[2])
                            item.Final_Volume = registerValues[23].ToString();
                        break;
                }
            }
        }

        #region bf
        //public override void Execute(CancellationToken token)
        //{
        //    // 称重执行逻辑
        //    Task.Run(() =>
        //    {
        //        while (!token.IsCancellationRequested)
        //        {
        //            if (CommonParam.s_DeviceState != DeviceState.Running)
        //            {
        //                Thread.Sleep(200);
        //                continue;
        //            }
        //            switch (_weightStepStepIndex)
        //            {
        //                case 0:
        //                    //等待设备1进站完成信号
        //                    LogEventManager.LogEvent(31, $"称重-等待设备1进站完成信号");
        //                    while (!token.IsCancellationRequested)
        //                    {
        //                        if (CommonParam.s_BoardingCompleteS1)
        //                        {
        //                            CommonParam.s_BoardingCompleteS1 = false;
        //                            break;
        //                        }
        //                        Thread.Sleep(10);
        //                    }
        //                    _weightStepStepIndex = 1;
        //                    break;
        //                case 1:
        //                    //等待称重1结果上报触发
        //                    LogEventManager.LogEvent(31, $"称重-等待称重1结果上报触发");
        //                    while (!token.IsCancellationRequested)
        //                    {
        //                        short[] registerValues_1400 = CommonParam.s_PLC.Read_Register_State_Short(1, 1400, 1);
        //                        if (registerValues_1400[0] == 1)
        //                        {
        //                            CommonParam.s_PLC.Write_Register_Value(1, 1400, "0", "short");
        //                            break;
        //                        }
        //                        Thread.Sleep(10);
        //                    }
        //                    CommonParam.s_MapHoleInfoListS1_1.ForEach(item => { item.Progress = "称重完成"; });
        //                    _weightStepStepIndex = 10;
        //                    break;
        //                case 10:
        //                    float[] registerValues_1401_1424 = CommonParam.s_PLC.Read_Register_State_Float(1, 1401, 24);
        //                    rackIdArr = CommonParam.s_WorkOrderKeyS1.Split(',');
        //                    UpdateCollectionVolume(registerValues_1401_1424, CommonParam.s_MapHoleInfoListS1_1);
        //                    CommonParam.SaveMapHoleInfosToSQL(CommonParam.s_MapHoleInfoListS1_1);
        //                    _weightStepStepIndex = 20;
        //                    break;
        //                case 20:
        //                    LogEventManager.LogEvent(31, $"称重-接收PLC称重托盘1数据成功");
        //                    float[] floats = new float[24];
        //                    CommonParam.s_PLC.WriteFloatArray(1, 1401, floats);
        //                    CommonParam.s_PLC.Write_Register_Value(1, 2400, "1", "short");
        //                    _weightStepStepIndex = 30;
        //                    break;
        //                case 30:
        //                    if (rackIdArr.Length > 1)
        //                    {
        //                        _weightStepStepIndex = 40;
        //                    }
        //                    else
        //                        _weightStepStepIndex = 0;
        //                    break;
        //                case 40:
        //                    //等待称重2结果上报触发
        //                    LogEventManager.LogEvent(31, $"称重-等待称重2结果上报触发");
        //                    while (!token.IsCancellationRequested)
        //                    {
        //                        short[] registerValues_1500 = CommonParam.s_PLC.Read_Register_State_Short(1, 1500, 1);
        //                        if (registerValues_1500[0] == 1)
        //                        {
        //                            CommonParam.s_PLC.Write_Register_Value(1, 1500, "0", "short");
        //                            break;
        //                        }
        //                        Thread.Sleep(10);
        //                    }
        //                    CommonParam.s_MapHoleInfoListS1_2.ForEach(item => { item.Progress = "称重完成"; });
        //                    _weightStepStepIndex = 50;
        //                    break;
        //                case 50:
        //                    float[] registerValues_1501_1524 = CommonParam.s_PLC.Read_Register_State_Float(1, 1501, 24);
        //                    UpdateCollectionVolume(registerValues_1501_1524, CommonParam.s_MapHoleInfoListS1_2);
        //                    CommonParam.SaveMapHoleInfosToSQL(CommonParam.s_MapHoleInfoListS1_2);
        //                    _weightStepStepIndex = 60;
        //                    break;
        //                case 60:
        //                    LogEventManager.LogEvent(31, $"称重-接收PLC称重托盘2数据成功");
        //                    floats = new float[24];
        //                    CommonParam.s_PLC.WriteFloatArray(1, 1501, floats);
        //                    CommonParam.s_PLC.Write_Register_Value(1, 2500, "1", "short");
        //                    _weightStepStepIndex = 70;
        //                    break;
        //                case 70:
        //                    //if (rackIdArr.Length > 2)
        //                    //{
        //                    //    LogEventManager.LogEvent(30, $"判断称重超过两个托盘");
        //                    //    _weightStepStepIndex = 0;
        //                    //}
        //                    //else
        //                    _weightStepStepIndex = 0;
        //                    break;
        //            }
        //        }

        //    });
        //}
        #endregion
    }
}
