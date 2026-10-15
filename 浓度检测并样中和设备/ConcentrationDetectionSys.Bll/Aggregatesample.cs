using ConcentrationDetectionSys.Common;
using ConcentrationDetectionSys.DAL;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConcentrationDetectionSys.Bll
{
    public class AggregatesampleParam
    {
        /// <summary>
        /// No.1   K3阈值运算符
        /// </summary>
        public string No1_Operator { get; set; } = ">=";
        /// <summary>
        /// No.1   K3阈值比较值
        /// </summary>
        public double No1_ComparisonValue { get; set; } = 10.0;
        /// <summary>
        /// No.2   Mt阈值运算符
        /// </summary>
        public string No2_Operator { get; set; } = ">=";
        /// <summary>
        /// No.2   Mt阈值比较值
        /// </summary>
        public double No2_ComparisonValue { get; set; } = 80.0;
        /// <summary>
        /// No.3   (M1+M2)/Mt阈值运算符
        /// </summary>
        public string No3_Operator { get; set; } = ">=";
        /// <summary>
        ///No.3   (M1+M2)/Mt阈值比较值
        /// </summary>
        public double No3_ComparisonValue { get; set; } = 10.0;
        /// <summary>
        /// No.4   (M1+M2)/Mt阈值运算符
        /// </summary>
        public string No4_Operator { get; set; } = ">=";
        /// <summary>
        /// No.4   (M1+M2)/Mt阈值比较值
        /// </summary>
        public double No4_ComparisonValue { get; set; } = 5.0;
        /// <summary>
        /// No.5   Mn/(M1+M2)阈值运算符
        /// </summary>
        public string No5_Operator { get; set; } = ">=";
        /// <summary>
        ///No.5   Mn/(M1+M2)阈值比较值
        /// </summary>
        public double No5_ComparisonValue { get; set; } = 95.0;
        /// <summary>
        /// No.6   Mn/(M1+M2)阈值运算符
        /// </summary>
        public string No6_Operator { get; set; } = ">=";
        /// <summary>
        /// No.6   Mn/(M1+M2)阈值比较值
        /// </summary>
        public double No6_ComparisonValue { get; set; } = 90.0;
        /// <summary>
        /// No.7   V1+V2阈值运算符
        /// </summary>
        public string No7_Operator { get; set; } = "<=";
        /// <summary>
        ///No.7   V1+V2阈值比较值
        /// </summary>
        public double No7_ComparisonValue { get; set; } = 30.0;
    }
    /// <summary>
    /// 合并样品
    /// </summary>
    public class Aggregatesample : BaseStep
    {
        public AggregatesampleParam aggregatesampleParam = new AggregatesampleParam();
        public Aggregatesample() : base("并样")
        {
           
        }
        private int _AggregatesampleStepIndex = 0;
        private string elutionRackID1_S2, elutionRackID2_S2, elutionRackID3_S2;
        public override void Execute(CancellationToken token)
        {
            // 合并样品执行逻辑
            Task.Run(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    if (!CommonParam.AutoRunCondition)
                    {
                        Thread.Sleep(200);
                        continue;
                    }

                    //请求并样托盘点位信号触发信号
                    short[] registerValues_1600 = CommonParam.s_PLC.Read_Register_State_Short(1, 1600, 1);
                    if (registerValues_1600 != null &&registerValues_1600[0] == 1)
                    {
                        CommonParam.s_PLC.Write_Register_Value(1, 1600, "0", "short");
                        LogEventManager.LogEvent(31, $"合并样品-并样托盘点位信号触发信号");
                        //判断托盘1码
                        elutionRackID1_S2 = CommonParam.s_PLC.Read_Register_State_ACSII(1, 1650, 20);
                        //elutionRackID1_S2 = "1E20260423-001";
                        if (elutionRackID1_S2 == "")
                        {
                            LogEventManager.LogEvent(30, $"合并样品-读取并样托盘1码 {elutionRackID1_S2} 失败");
                            continue;
                        }
                        LogEventManager.LogEvent(31, $"合并样品-读取并样托盘1码 {elutionRackID1_S2} 成功");
                        short[] shorts = new short[20];
                        CommonParam.s_PLC.Fun10(1, 1650, shorts);
                        if (CommonParam.s_MES_Mode == MES_Mode.Off_Line)//MES离线模式
                        {
                            //判断托盘1码与CommonParam.s_WorkOrderMapHoleInfoDic中是否存在
                            if (CommonParam.s_WorkOrderMapHoleInfoList.Exists(info => info.RackID == elutionRackID1_S2))
                            {
                                LogEventManager.LogEvent(31, $"合并样品-并样托盘1码 {elutionRackID1_S2} 在工单存在");
                                //判断是否是主托盘码
                                if (elutionRackID1_S2?.StartsWith("1E") == true)
                                {
                                    LogEventManager.LogEvent(31, $"合并样品-判断是并样主托盘码");
                                    CommonParam.s_MapHoleInfoListS2_1 = CommonParam.s_WorkOrderMapHoleInfoList.FindAll(info => info != null && info.RackID == elutionRackID1_S2);
                                    CommonParam.s_WorkOrderKeyS2 = elutionRackID1_S2;
                                    CommonParam.SaveMapHoleInfosToSQL(CommonParam.s_MapHoleInfoListS2_1);
                                    SqlHelper.SetStringValue("ElutionRackID1_S2", elutionRackID1_S2);
                                    SqlHelper.SetStringValue("WorkOrderKeyS2", CommonParam.s_WorkOrderKeyS2);
                                    CommonParam.s_MapHoleInfoListS2_1.ForEach(item =>
                                    {
                                        item.State = MapHoleInfoState.Running;
                                        item.Progress = "进站扫码完成";
                                    });
                                }
                                else
                                {
                                    //报警
                                    LogEventManager.LogEvent(30, $"合并样品-判断不是并样主托盘码");
                                    CommonParam.s_PLC.Write_Register_Value(1, 2600, "2", "short");
                                    continue;
                                }
                            }
                            else
                            {
                                //报警
                                LogEventManager.LogEvent(30, $"合并样品-并样托盘1码 {elutionRackID1_S2} 在工单不存在");
                                CommonParam.s_PLC.Write_Register_Value(1, 2600, "2", "short");
                                continue;
                            }
                        }
                        else//MES在线模式
                        {

                        }
                        //判断托盘2码
                        elutionRackID2_S2 = CommonParam.s_PLC.Read_Register_State_ACSII(1, 1700, 20);
                        //elutionRackID2_S2 = "2E20260423-001";
                        if (elutionRackID2_S2 == "")
                        {
                            LogEventManager.LogEvent(30, $"合并样品-读取并样托盘2码 {elutionRackID2_S2} 失败");
                            continue;
                        }
                        LogEventManager.LogEvent(31, $"合并样品-读取并样托盘2码 {elutionRackID2_S2} 成功");
                        shorts = new short[20];
                        CommonParam.s_PLC.Fun10(1, 1700, shorts);
                        if (CommonParam.s_MES_Mode == MES_Mode.Off_Line)//MES离线模式
                        {
                            //判断托盘2码与CommonParam.s_WorkOrderMapHoleInfoDic中是否存在
                            if (CommonParam.s_WorkOrderMapHoleInfoList.Exists(info => info.RackID == elutionRackID2_S2))
                            {
                                LogEventManager.LogEvent(31, $"合并样品-并样托盘2码 {elutionRackID2_S2} 在工单存在");
                                //判断是否是2号托盘码
                                if (elutionRackID2_S2?.StartsWith("2E") == true)
                                {
                                    LogEventManager.LogEvent(31, $"合并样品-判断是并样托盘码");
                                    CommonParam.s_MapHoleInfoListS2_2 = CommonParam.s_WorkOrderMapHoleInfoList.FindAll(info => info != null && info.RackID == elutionRackID2_S2);
                                    CommonParam.s_WorkOrderKeyS2 = elutionRackID1_S2 + "," + elutionRackID2_S2;
                                    CommonParam.SaveMapHoleInfosToSQL(CommonParam.s_MapHoleInfoListS2_2);
                                    SqlHelper.SetStringValue("ElutionRackID2_S2", elutionRackID2_S2);
                                    SqlHelper.SetStringValue("WorkOrderKeyS2", CommonParam.s_WorkOrderKeyS2);
                                    CommonParam.s_MapHoleInfoListS2_2.ForEach(item =>
                                    {
                                        item.State = MapHoleInfoState.Running;
                                        item.Progress = "进站扫码完成";
                                    });
                                }
                                else
                                {
                                    //报警
                                    LogEventManager.LogEvent(30, $"合并样品-判断不是并样托盘码");
                                    CommonParam.s_PLC.Write_Register_Value(1, 2600, "2", "short");
                                    continue;
                                }
                            }
                            else
                            {
                                //报警
                                LogEventManager.LogEvent(30, $"合并样品-并样托盘2码 {elutionRackID2_S2} 在工单不存在");
                                CommonParam.s_PLC.Write_Register_Value(1, 2600, "2", "short");
                                continue;
                            }
                        }
                        else//MES在线模式
                        {

                        }

                        //判断托盘3码
                        elutionRackID3_S2 = CommonParam.s_PLC.Read_Register_State_ACSII(1, 1750, 20);
                        //elutionRackID3_S2 = "3E20260423-001";
                        if (elutionRackID3_S2 == "")
                        {
                            LogEventManager.LogEvent(30, $"合并样品-读取并样托盘3码 {elutionRackID3_S2} 失败");
                            continue;
                        }
                        LogEventManager.LogEvent(31, $"合并样品-读取并样托盘3码 {elutionRackID3_S2} 成功");
                        shorts = new short[20];
                        CommonParam.s_PLC.Fun10(1, 1750, shorts);
                        if (CommonParam.s_MES_Mode == MES_Mode.Off_Line)//MES离线模式
                        {
                            //判断托盘3码与CommonParam.s_WorkOrderMapHoleInfoDic中是否存在
                            if (CommonParam.s_WorkOrderMapHoleInfoList.Exists(info => info.RackID == elutionRackID3_S2))
                            {
                                LogEventManager.LogEvent(31, $"合并样品-并样托盘3码 {elutionRackID3_S2} 在工单存在");
                                //判断是否是3号托盘码
                                if (elutionRackID3_S2?.StartsWith("3E") == true)
                                {
                                    LogEventManager.LogEvent(31, $"合并样品-判断是并样托盘3码");
                                    CommonParam.s_MapHoleInfoListS2_3 = CommonParam.s_WorkOrderMapHoleInfoList.FindAll(info => info != null && info.RackID == elutionRackID3_S2);
                                    CommonParam.s_WorkOrderKeyS2 = elutionRackID1_S2 + "," + elutionRackID2_S2 + "," + elutionRackID3_S2;
                                    CommonParam.SaveMapHoleInfosToSQL(CommonParam.s_MapHoleInfoListS2_3);
                                    SqlHelper.SetStringValue("ElutionRackID3_S2", elutionRackID3_S2);
                                    SqlHelper.SetStringValue("WorkOrderKeyS2", CommonParam.s_WorkOrderKeyS2);
                                    CommonParam.s_MapHoleInfoListS2_3.ForEach(item =>
                                    {
                                        item.State = MapHoleInfoState.Running;
                                        item.Progress = "进站扫码完成";
                                    });
                                }
                                else
                                {
                                    //报警
                                    LogEventManager.LogEvent(30, $"合并样品-判断不是并样托盘码");
                                    CommonParam.s_PLC.Write_Register_Value(1, 2600, "2", "short");
                                    continue;
                                }
                            }
                            else
                            {
                                //报警
                                LogEventManager.LogEvent(30, $"合并样品-并样托盘3码 {elutionRackID3_S2} 在工单不存在");
                                CommonParam.s_PLC.Write_Register_Value(1, 2600, "2", "short");
                                continue;
                            }
                        }
                        else//MES在线模式
                        {

                        }
                        //分析工单给PLC并样托盘点位+容量数据
                        CommonParam.s_MapHoleInfoListS2_1.ForEach(item => { item.Progress = "并样中"; });
                        CommonParam.s_MapHoleInfoListS2_2.ForEach(item => { item.Progress = "并样中"; });
                        CommonParam.s_MapHoleInfoListS2_3.ForEach(item => { item.Progress = "并样中"; });

                        LogEventManager.LogEvent(31, $"合并样品-分析数据给PLC并样托盘点位+容量数据");
                        float[] floats = new float[24];
                        //遍历每一条工单
                        foreach (WorkOrder wo in CommonParam.s_MapHoleInfoListS2_1)
                        {
                            switch (wo.Location)
                            {
                                case "A1":
                                    floats[0] = Logic("A1");
                                    break;
                                case "A2":
                                    floats[1] = Logic("A2");
                                    break;
                                case "A3":
                                    floats[2] = Logic("A3");
                                    break;
                                case "A4":
                                    floats[3] = Logic("A4");
                                    break;
                                case "A5":
                                    floats[4] = Logic("A5");
                                    break;
                                case "A6":
                                    floats[5] = Logic("A6");
                                    break;
                                case "B1":
                                    floats[6] = Logic("B1");
                                    break;
                                case "B2":
                                    floats[7] = Logic("B2");
                                    break;
                                case "B3":
                                    floats[8] = Logic("B3");
                                    break;
                                case "B4":
                                    floats[9] = Logic("B4");
                                    break;
                                case "B5":
                                    floats[10] = Logic("B5");
                                    break;
                                case "B6":
                                    floats[11] = Logic("B6");
                                    break;
                                case "C1":
                                    floats[12] = Logic("C1");
                                    break;
                                case "C2":
                                    floats[13] = Logic("C2");
                                    break;
                                case "C3":
                                    floats[14] = Logic("C3");
                                    break;
                                case "C4":
                                    floats[15] = Logic("C4");
                                    break;
                                case "C5":
                                    floats[16] = Logic("C5");
                                    break;
                                case "C6":
                                    floats[17] = Logic("C6");
                                    break;
                                case "D1":
                                    floats[18] = Logic("D1");
                                    break;
                                case "D2":
                                    floats[19] = Logic("D2");
                                    break;
                                case "D3":
                                    floats[20] = Logic("D3");
                                    break;
                                case "D4":
                                    floats[21] = Logic("D4");
                                    break;
                                case "D5":
                                    floats[22] = Logic("D5");
                                    break;
                                case "D6":
                                    floats[23] = Logic("D6");
                                    break;
                            }
                        }
                        //if (CommonParam.s_TestInjectCapacity)
                        //{
                        //    // 强制发固定测试值，便于 PLC 侧验证通讯
                        //    CommonParam.s_PLC.WriteFloatArray(1, 2650, new float[24] {
                        //      15f, 15f, 15f,  15f, 15f, 15f,  15f, 15f, 15f,  15f, 15f, 15f,
                        //       15f, 15f, 15f,  15f, 15f, 15f,  15f, 15f, 15f, 15f, 15f, 15f});
                        //}
                        //else
                        {
                            CommonParam.s_PLC.WriteFloatArray(1, 2650, floats);
                        }

                        CommonParam.s_PLC.Write_Register_Value(1, 2600, "1", "short");
                        CommonParam.SaveMapHoleInfosToSQL(CommonParam.s_MapHoleInfoListS2_1);
                        CommonParam.SaveMapHoleInfosToSQL(CommonParam.s_MapHoleInfoListS2_2);
                        CommonParam.SaveMapHoleInfosToSQL(CommonParam.s_MapHoleInfoListS2_3);
                        LogEventManager.LogEvent(31, $"合并样品-数据给PLC并样托盘点位+容量数据完成");
                    }


                    //PLC并样完成信号
                    short[] registerValues_2081 = CommonParam.s_PLC.Read_Register_State_Short(1, 2081, 1);
                    if (registerValues_2081 != null && registerValues_2081[0] == 1)
                    {
                        LogEventManager.LogEvent(31, $"合并样品-PLC并样完成信号触发");
                        CommonParam.s_PLC.Write_Register_Value(1, 2081, "0", "short");

                        if (CommonParam.s_MapHoleInfoListS2_1.Count == 0)
                        {
                            string code = SqlHelper.GetString("ElutionRackID1_S2");
                            CommonParam.s_MapHoleInfoListS2_1 = CommonParam.QueryMapHoleInfoByBatch(code);  
                        }
                        if (CommonParam.s_MapHoleInfoListS2_2.Count == 0)
                        {
                            string code = SqlHelper.GetString("ElutionRackID2_S2");
                            CommonParam.s_MapHoleInfoListS2_2 = CommonParam.QueryMapHoleInfoByBatch(code);
                        }
                        if (CommonParam.s_MapHoleInfoListS2_3.Count == 0)
                        {
                            string code = SqlHelper.GetString("ElutionRackID3_S2");
                            CommonParam.s_MapHoleInfoListS2_3 = CommonParam.QueryMapHoleInfoByBatch(code);
                        }
                        CommonParam.s_MapHoleInfoListS2_1.ForEach(item => { item.Progress = "并样完成"; });
                        CommonParam.s_MapHoleInfoListS2_2.ForEach(item => { item.Progress = "并样完成"; });
                        CommonParam.s_MapHoleInfoListS2_3.ForEach(item => { item.Progress = "并样完成"; });
                        CommonParam.SaveMapHoleInfosToSQL(CommonParam.s_MapHoleInfoListS2_1);                       
                        CommonParam.SaveMapHoleInfosToSQL(CommonParam.s_MapHoleInfoListS2_3);
                        string nowStr = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");
                        foreach (var item in CommonParam.s_MapHoleInfoListS2_2)
                        {
                            if (item.PoolingLogic == "合并")
                            {
                                item.PoolingTime = nowStr;
                                CommonParam.s_Capacity.DoneNum_BY += 1;
                            }
                        }
                        CommonParam.SaveMapHoleInfosToSQL(CommonParam.s_MapHoleInfoListS2_2);
                        SqlHelper.SetStringValue("DoneNum_BY", CommonParam.s_Capacity.DoneNum_BY.ToString());
                        LogEventManager.LogEvent(31, $"合并样品-PLC并样完成");
                    }

                    Thread.Sleep(100);
                }
            });
        }
        #region bf
        //public override void Execute(CancellationToken token)
        //{
        //    // 合并样品执行逻辑
        //    Task.Run(() =>
        //    {
        //        while (!token.IsCancellationRequested)
        //        {
        //            if (CommonParam.s_DeviceState != DeviceState.Running)
        //            {
        //                Thread.Sleep(200);
        //                continue;
        //            }
        //            switch (_AggregatesampleStepIndex)
        //            {
        //                case 0:
        //                    //等待请求并样托盘点位信号
        //                    LogEventManager.LogEvent(31, $"合并样品-等待请求并样托盘点位信号");
        //                    while (!token.IsCancellationRequested)
        //                    {
        //                        short[] registerValues_1600 = CommonParam.s_PLC.Read_Register_State_Short(1, 1600, 1);
        //                        if (registerValues_1600[0] == 1)
        //                        {
        //                            CommonParam.s_PLC.Write_Register_Value(1, 1600, "0", "short");
        //                            break;
        //                        }
        //                        Thread.Sleep(10);
        //                    }
        //                    _AggregatesampleStepIndex = 10;
        //                    break;
        //                case 10:
        //                    elutionRackID1_S2 = CommonParam.s_PLC.Read_Register_State_ACSII(1, 1650, 20);
        //                    if (elutionRackID1_S2 == null)
        //                    {
        //                        LogEventManager.LogEvent(30, $"合并样品-读取并样托盘1码 {elutionRackID1_S2} 失败");
        //                        _AggregatesampleStepIndex = 0;
        //                    }
        //                    else
        //                    {
        //                        LogEventManager.LogEvent(31, $"合并样品-读取并样托盘1码 {elutionRackID1_S2} 成功");
        //                        short[] shorts = new short[20];
        //                        CommonParam.s_PLC.Fun10(1, 1650, shorts);
        //                        _AggregatesampleStepIndex = 20;
        //                    }
        //                    break;
        //                case 20:
        //                    //判断托盘1码与CommonParam.s_WorkOrderMapHoleInfoDic中是否存在
        //                    if (CommonParam.s_WorkOrderMapHoleInfoList.Exists(info => info.RackID == elutionRackID1_S2))
        //                    {
        //                        LogEventManager.LogEvent(31, $"合并样品-并样托盘1码 {elutionRackID1_S2} 在工单存在");
        //                        CommonParam.s_MapHoleInfoListS2_1 = CommonParam.s_WorkOrderMapHoleInfoList.FindAll(info => info != null && info.RackID == elutionRackID1_S2);
        //                        CommonParam.s_WorkOrderKeyS2 = elutionRackID1_S2;
        //                        CommonParam.s_MapHoleInfoListS2_1.ForEach(item =>
        //                        {
        //                            item.State = MapHoleInfoState.Running;
        //                            item.Progress = "进站扫码完成";
        //                        });
        //                        _AggregatesampleStepIndex = 30;
        //                    }
        //                    else
        //                    {
        //                        //报警
        //                        LogEventManager.LogEvent(30, $"合并样品-并样托盘1码 {elutionRackID1_S2} 在工单不存在");
        //                        CommonParam.s_PLC.Write_Register_Value(1, 2600, "2", "short");
        //                        _AggregatesampleStepIndex = 0;
        //                    }
        //                    break;
        //                case 30:
        //                    //判断是否是主托盘码
        //                    if (elutionRackID1_S2?.StartsWith("1E") == true)
        //                    {
        //                        LogEventManager.LogEvent(31, $"合并样品-判断是并样主托盘码");
        //                        _AggregatesampleStepIndex = 40;
        //                    }
        //                    else
        //                    {
        //                        //报警
        //                        LogEventManager.LogEvent(30, $"合并样品-判断不是并样主托盘码");
        //                        CommonParam.s_PLC.Write_Register_Value(1, 2600, "2", "short");
        //                        _AggregatesampleStepIndex = 0;
        //                    }
        //                    break;
        //                case 40:
        //                    //判断托盘2码
        //                    elutionRackID2_S2 = CommonParam.s_PLC.Read_Register_State_ACSII(1, 1700, 20);
        //                    if (elutionRackID2_S2 == null)
        //                    {
        //                        LogEventManager.LogEvent(30, $"合并样品-读取并样托盘2码 {elutionRackID2_S2} 失败");
        //                    }
        //                    else
        //                    {
        //                        LogEventManager.LogEvent(31, $"合并样品-读取并样托盘2码 {elutionRackID2_S2} 成功");
        //                        short[] shorts = new short[20];
        //                        CommonParam.s_PLC.Fun10(1, 1700, shorts);
        //                        if (elutionRackID2_S2?.StartsWith("2E") == true)
        //                        {
        //                            LogEventManager.LogEvent(31, $"合并样品-判断是并样托盘码");
        //                            CommonParam.s_MapHoleInfoListS2_2 = CommonParam.s_WorkOrderMapHoleInfoList.FindAll(info => info != null && info.RackID == elutionRackID2_S2);
        //                            CommonParam.s_WorkOrderKeyS2 = elutionRackID1_S2 + "," + elutionRackID2_S2;
        //                            CommonParam.s_MapHoleInfoListS2_2.ForEach(item =>
        //                            {
        //                                item.State = MapHoleInfoState.Running;
        //                                item.Progress = "进站扫码完成";
        //                            });
        //                            _AggregatesampleStepIndex = 50;
        //                        }
        //                        else
        //                        {
        //                            //报警
        //                            LogEventManager.LogEvent(30, $"合并样品-判断不是并样托盘码");
        //                            CommonParam.s_PLC.Write_Register_Value(1, 2600, "2", "short");
        //                            _AggregatesampleStepIndex = 0;
        //                        }
        //                    }
        //                    break;
        //                case 50:
        //                    //判断托盘3码
        //                    elutionRackID3_S2 = CommonParam.s_PLC.Read_Register_State_ACSII(1, 1750, 20);
        //                    if (elutionRackID3_S2 == null)
        //                    {
        //                        LogEventManager.LogEvent(30, $"合并样品-读取并样托盘3码 {elutionRackID3_S2} 失败");
        //                    }
        //                    else
        //                    {
        //                        LogEventManager.LogEvent(31, $"合并样品-读取并样托盘3码 {elutionRackID3_S2} 成功");
        //                        short[] shorts = new short[20];
        //                        CommonParam.s_PLC.Fun10(1, 1750, shorts);
        //                        if (elutionRackID3_S2?.StartsWith("3E") == true)
        //                        {
        //                            LogEventManager.LogEvent(31, $"合并样品-判断是并样托盘3码");
        //                            CommonParam.s_MapHoleInfoListS2_3 = CommonParam.s_WorkOrderMapHoleInfoList.FindAll(info => info != null && info.RackID == elutionRackID3_S2);
        //                            CommonParam.s_WorkOrderKeyS2 = elutionRackID1_S2 + "," + elutionRackID2_S2 + "," + elutionRackID3_S2;
        //                            CommonParam.s_MapHoleInfoListS2_3.ForEach(item =>
        //                            {
        //                                item.State = MapHoleInfoState.Running;
        //                                item.Progress = "进站扫码完成";
        //                            });
        //                            _AggregatesampleStepIndex = 51;
        //                        }
        //                        else
        //                        {
        //                            //报警
        //                            LogEventManager.LogEvent(30, $"合并样品-判断不是并样托盘码");
        //                            CommonParam.s_PLC.Write_Register_Value(1, 2600, "2", "short");
        //                            _AggregatesampleStepIndex = 0;
        //                        }
        //                    }
        //                    break;
        //                case 51:
        //                    //判断是否并样
        //                    if (CommonParam.s_ModeBools[2])
        //                    {
        //                        CommonParam.s_MapHoleInfoListS2_1.ForEach(item => { item.Progress = "并样中"; });
        //                        CommonParam.s_MapHoleInfoListS2_2.ForEach(item => { item.Progress = "并样中"; });
        //                        CommonParam.s_MapHoleInfoListS2_3.ForEach(item => { item.Progress = "并样中"; });
        //                        _AggregatesampleStepIndex = 60;
        //                    }
        //                    else
        //                        _AggregatesampleStepIndex = 0;
        //                    break;
        //                case 60:
        //                    //分析工单给PLC并样托盘点位+容量数据
        //                    LogEventManager.LogEvent(31, $"合并样品-分析数据给PLC并样托盘点位+容量数据");
        //                    float[] floats = new float[24];
        //                    //遍历每一条工单
        //                    foreach (WorkOrder wo in CommonParam.s_MapHoleInfoListS2_1)
        //                    {
        //                        switch (wo.Location)
        //                        {
        //                            case "A1":
        //                                floats[0] = Logic("A1");
        //                                break;
        //                            case "A2":
        //                                floats[1] = Logic("A2");
        //                                break;
        //                            case "A3":
        //                                floats[2] = Logic("A3");
        //                                break;
        //                            case "A4":
        //                                floats[3] = Logic("A4");
        //                                break;
        //                            case "A5":
        //                                floats[4] = Logic("A5");
        //                                break;
        //                            case "A6":
        //                                floats[5] = Logic("A6");
        //                                break;
        //                            case "B1":
        //                                floats[6] = Logic("B1");
        //                                break;
        //                            case "B2":
        //                                floats[7] = Logic("B2");
        //                                break;
        //                            case "B3":
        //                                floats[8] = Logic("B3");
        //                                break;
        //                            case "B4":
        //                                floats[9] = Logic("B4");
        //                                break;
        //                            case "B5":
        //                                floats[10] = Logic("B5");
        //                                break;
        //                            case "B6":
        //                                floats[11] = Logic("B6");
        //                                break;
        //                            case "C1":
        //                                floats[12] = Logic("C1");
        //                                break;
        //                            case "C2":
        //                                floats[13] = Logic("C2");
        //                                break;
        //                            case "C3":
        //                                floats[14] = Logic("C3");
        //                                break;
        //                            case "C4":
        //                                floats[15] = Logic("C4");
        //                                break;
        //                            case "C5":
        //                                floats[16] = Logic("C5");
        //                                break;
        //                            case "C6":
        //                                floats[17] = Logic("C6");
        //                                break;
        //                            case "D1":
        //                                floats[18] = Logic("D1");
        //                                break;
        //                            case "D2":
        //                                floats[19] = Logic("D2");
        //                                break;
        //                            case "D3":
        //                                floats[20] = Logic("D3");
        //                                break;
        //                            case "D4":
        //                                floats[21] = Logic("D4");
        //                                break;
        //                            case "D5":
        //                                floats[22] = Logic("D5");
        //                                break;
        //                            case "D6":
        //                                floats[23] = Logic("D6");
        //                                break;
        //                        }
        //                    }
        //                    CommonParam.s_PLC.WriteFloatArray(1, 2650, floats);
        //                    CommonParam.s_PLC.Write_Register_Value(1, 2600, "1", "short");
        //                    LogEventManager.LogEvent(31, $"合并样品-数据给PLC并样托盘点位+容量数据完成");
        //                    _AggregatesampleStepIndex = 70;
        //                    break;
        //                case 70:
        //                    //等待PLC并样完成
        //                    LogEventManager.LogEvent(31, $"合并样品-等待PLC并样完成信号");
        //                    while (!token.IsCancellationRequested)
        //                    {
        //                        short[] registerValues_2081 = CommonParam.s_PLC.Read_Register_State_Short(1, 2081, 1);
        //                        if (registerValues_2081[0] == 1)
        //                        {
        //                            CommonParam.s_PLC.Write_Register_Value(1, 2081, "0", "short");
        //                            break;
        //                        }
        //                        Thread.Sleep(10);
        //                    }
        //                    CommonParam.s_MapHoleInfoListS2_1.ForEach(item => { item.Progress = "并样完成"; });
        //                    CommonParam.s_MapHoleInfoListS2_2.ForEach(item => { item.Progress = "并样完成"; });
        //                    CommonParam.s_MapHoleInfoListS2_3.ForEach(item => { item.Progress = "并样完成"; });
        //                    CommonParam.SaveMapHoleInfosToSQL(CommonParam.s_MapHoleInfoListS2_1);
        //                    CommonParam.SaveMapHoleInfosToSQL(CommonParam.s_MapHoleInfoListS2_3);
        //                    string nowStr = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");
        //                    foreach (var item in CommonParam.s_MapHoleInfoListS2_2)
        //                    {
        //                        if (item.PoolingLogic == "合并")
        //                        {
        //                            item.PoolingTime = nowStr;
        //                        }
        //                    }
        //                    CommonParam.SaveMapHoleInfosToSQL(CommonParam.s_MapHoleInfoListS2_2);
        //                    LogEventManager.LogEvent(31, $"合并样品-PLC并样完成");
        //                    _AggregatesampleStepIndex = 0;
        //                    break;

        //            }
        //        }

        //    });
        //}
        #endregion

        /// <summary>
        /// 并样逻辑
        /// </summary>
        private float Logic(string elutionTubeLocation)
        {
            MapHoleInfo MapHoleInfo1 = CommonParam.s_MapHoleInfoListS2_1.FirstOrDefault(h => h.Location == elutionTubeLocation);
            MapHoleInfo MapHoleInfo2 = CommonParam.s_MapHoleInfoListS2_2.FirstOrDefault(h => h.Location == elutionTubeLocation);
            MapHoleInfo MapHoleInfo3 = CommonParam.s_MapHoleInfoListS2_3.FirstOrDefault(h => h.Location == elutionTubeLocation);
            double.TryParse(MapHoleInfo1.CedexYield, out double CedexYield);
            double.TryParse(MapHoleInfo1.Cal_M_W, out double Cal_M_W);
            double.TryParse(MapHoleInfo1.ExpressionScale, out double ExpressionScale);
            double M = (CedexYield / 150) *Cal_M_W * ExpressionScale / 1000;
            double.TryParse(MapHoleInfo1.ReagentVolume, out double V1);
            double.TryParse(MapHoleInfo2.ReagentVolume, out double V2);
            double.TryParse(MapHoleInfo3.ReagentVolume, out double V3);
            double M1 = V1 * 1;//体积*水的密度
            double M2 = V2 * 1;//体积*水的密度
            double M3 = V3 * 1;//体积*水的密度
            double Mt = M1 + M2 + M3;
            double K1 = M1 / Mt * 100;
            double K2 = M2 / Mt * 100;
            double K3 = M3 / Mt * 100;
            float result = 0;
            //判断K3是否收集
            //if (K3 >= 10)
            if (Check(K3, aggregatesampleParam.No1_Operator, aggregatesampleParam.No1_ComparisonValue))
                MapHoleInfo3.PoolingLogic = "保留";
            else
            {
                MapHoleInfo3.PoolingLogic = "舍弃";
                MapHoleInfo3.Final_Protein_Mass = "";
                MapHoleInfo3.Final_Volume = "";
            }
            //判断Mt>=M*80%
            //if (Mt >= M * 0.8)
            if (Check(Mt, aggregatesampleParam.No2_Operator, M * aggregatesampleParam.No2_ComparisonValue))
            {
                //判断(M1+M2)/Mt>=10%
                //if ((M1 + M2) / Mt >= 0.1)
                if (Check((M1 + M2) / Mt, aggregatesampleParam.No3_Operator,aggregatesampleParam.No3_ComparisonValue))
                {
                    //判断MN/(M1+M2)>=90% N=1或2
                    //if ( M1/ (M1 + M2) >= 0.9)
                    if (Check(M1 / (M1 + M2), aggregatesampleParam.No6_Operator, aggregatesampleParam.No6_ComparisonValue))
                    {
                        MapHoleInfo1.PoolingLogic = "保留";
                        MapHoleInfo2.PoolingLogic = "舍弃";
                        MapHoleInfo2.Final_Protein_Mass = "";
                        MapHoleInfo2.Final_Volume = "";
                    }
                    else
                    {
                        //if (M2 / (M1 + M2) >= 0.9)
                        if (Check(M2 / (M1 + M2), aggregatesampleParam.No6_Operator, aggregatesampleParam.No6_ComparisonValue))
                        {
                            MapHoleInfo1.PoolingLogic = "舍弃";
                            MapHoleInfo1.Final_Protein_Mass = "";
                            MapHoleInfo1.Final_Volume = "";
                            MapHoleInfo2.PoolingLogic = "保留";
                        }
                        else
                        {
                            //收集合并第一、二管
                            //判断V1+V2≤30ml
                            //if (V1 + V2 <= 30)
                            if (Check(V1 + V2, aggregatesampleParam.No7_Operator, aggregatesampleParam.No7_ComparisonValue*100))
                            {
                                //合并
                                MapHoleInfo1.PoolingLogic = "保留";
                                MapHoleInfo1.Final_Volume = (V1 + V2).ToString();
                                double.TryParse(MapHoleInfo1.Final_Protein_Mass, out double ProteinMass1);
                                double.TryParse(MapHoleInfo1.Final_Protein_Mass, out double ProteinMass2);
                                MapHoleInfo1.Final_Protein_Mass = (ProteinMass1 + ProteinMass2).ToString();
                                MapHoleInfo2.PoolingLogic = "合并";
                                result = (float)V2;
                                MapHoleInfo2.Target = MapHoleInfo1.RackID;
                                MapHoleInfo2.Final_Protein_Mass = "";
                                MapHoleInfo2.Final_Volume = "";
                                MapHoleInfo2.Pooling_Operator = CommonParam.s_CurrentUser.UserName;
                            }
                            else
                            {
                                //不合并
                                MapHoleInfo1.PoolingLogic = "保留";
                                MapHoleInfo2.PoolingLogic = "保留";
                            }
                        }
                    }
                }
                else
                {
                    MapHoleInfo1.PoolingLogic = "舍弃";
                    MapHoleInfo1.Final_Protein_Mass = "";
                    MapHoleInfo1.Final_Volume = "";
                    MapHoleInfo2.PoolingLogic = "舍弃";
                    MapHoleInfo2.Final_Protein_Mass = "";
                    MapHoleInfo2.Final_Volume = "";
                }
            }
            else
            {
                //判断(M1+M2)/Mt≥5%
                //if ((M1 + M2)/ Mt >= 0.05)
                if (Check((M1 + M2) / Mt, aggregatesampleParam.No4_Operator, aggregatesampleParam.No4_ComparisonValue))
                {
                    //判断MN/(M1+M2)>=95% N=1或2
                    //if (M1 / (M1 + M2) >= 0.95)
                    if (Check(M1 / (M1 + M2), aggregatesampleParam.No5_Operator, aggregatesampleParam.No5_ComparisonValue))
                    {
                        MapHoleInfo1.PoolingLogic = "保留";
                        MapHoleInfo2.PoolingLogic = "舍弃";
                        MapHoleInfo2.Final_Protein_Mass = "";
                        MapHoleInfo2.Final_Volume = "";
                    }
                    else
                    {
                        //if (M2 / (M1 + M2) >= 0.95)
                        if (Check(M2 / (M1 + M2), aggregatesampleParam.No5_Operator, aggregatesampleParam.No5_ComparisonValue))
                        {
                            MapHoleInfo1.PoolingLogic = "舍弃";
                            MapHoleInfo1.Final_Protein_Mass = "";
                            MapHoleInfo1.Final_Volume = "";
                            MapHoleInfo2.PoolingLogic = "保留";
                        }
                        else
                        {
                            //收集合并第一、二管
                            //判断V1+V2≤30ml
                            if (V1 + V2 <= 30)
                            {
                                //合并
                                MapHoleInfo1.PoolingLogic = "保留";
                                MapHoleInfo2.PoolingLogic = "合并";
                                result = (float)V2;
                                MapHoleInfo2.Target = MapHoleInfo1.RackID;
                                MapHoleInfo2.Final_Protein_Mass = "";
                                MapHoleInfo2.Final_Volume = "";
                                MapHoleInfo2.Pooling_Operator = CommonParam.s_CurrentUser.UserName;
                                MapHoleInfo1.Final_Volume = (V1 + V2).ToString();
                                double.TryParse(MapHoleInfo1.Final_Protein_Mass, out double ProteinMass1);
                                double.TryParse(MapHoleInfo1.Final_Protein_Mass, out double ProteinMass2);
                                MapHoleInfo1.Final_Protein_Mass = (ProteinMass1 + ProteinMass2).ToString();
                            }
                            else
                            {
                                //不合并
                                MapHoleInfo1.PoolingLogic = "保留";
                                MapHoleInfo2.PoolingLogic = "保留";
                            }
                        }
                    }
                    
                }
                else
                {
                    MapHoleInfo1.PoolingLogic = "舍弃";
                    MapHoleInfo1.Final_Protein_Mass = "";
                    MapHoleInfo1.Final_Volume = "";
                    MapHoleInfo2.PoolingLogic = "舍弃";
                    MapHoleInfo2.Final_Protein_Mass = "";
                    MapHoleInfo2.Final_Volume = "";
                }
            }
            return result;

        }
        public bool Check(double value, string Operator, double comparisonValue)
        {
            bool result = false;
            comparisonValue = comparisonValue * 0.01;
            switch (Operator)
            {
                case ">":
                    result = value > comparisonValue;
                    break;
                case ">=":
                    result = value >= comparisonValue;
                    break;
                case "<":
                    result = value < comparisonValue;
                    break;
                case "<=":
                    result = value <= comparisonValue;
                    break;
                case "==":
                    result = value == comparisonValue;
                    break;
                case "!=":
                    result = value != comparisonValue;
                    break;
                default:
                    result = false;
                    break;
            }
            return result;
        }
    }

}
