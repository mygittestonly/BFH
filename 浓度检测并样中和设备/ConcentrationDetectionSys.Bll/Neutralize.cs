using ConcentrationDetectionSys.Common;
using ConcentrationDetectionSys.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConcentrationDetectionSys.Bll
{
    public class NeutralizeParam
    {
        /// <summary>
        /// 中和系数 e
        /// </summary>
        public double e { get; set; } = 1.0;
    }
    /// <summary>
    /// 中和
    /// </summary>
    public class Neutralize : BaseStep
    {
        public NeutralizeParam neutralizeParam = new NeutralizeParam();
        public Neutralize() : base("中和")
        {

        }
        private int _NeutralizeStepIndex = 0;
        private string elutionRackID1_S2, elutionRackID2_S2, elutionRackID3_S2;
        private List<MapHoleInfo> _mapHoleInfoListS2_1, _mapHoleInfoListS2_2, _mapHoleInfoListS2_3;
        private int _neutralizeCount = 0;
        public override void Execute(CancellationToken token)
        {
            // 中和执行逻辑
            Task.Run(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    if (!CommonParam.AutoRunCondition)
                    {
                        Thread.Sleep(200);
                        continue;
                    }
                    //请求托盘中和剂信号触发
                    short[] registerValues_1800 = CommonParam.s_PLC.Read_Register_State_Short(1, 1800, 1);
                    if (registerValues_1800 != null && registerValues_1800[0] == 1)
                    {
                        CommonParam.s_PLC.Write_Register_Value(1, 1800, "0", "short");
                        LogEventManager.LogEvent(31, $"中和-请求托盘中和信号触发");
                        //判断是否需要分析条码工单
                        if (!CommonParam.s_ModeBools[2])
                        {
                            //判断托盘1码
                            elutionRackID1_S2 = CommonParam.s_PLC.Read_Register_State_ACSII(1, 1801, 20);
                            //elutionRackID1_S2 = "1E20260423-001";
                            if (elutionRackID1_S2 == "")
                            {
                                LogEventManager.LogEvent(30, $"中和-读取中和托盘1码 {elutionRackID1_S2} 失败");
                                continue;
                            }
                            LogEventManager.LogEvent(31, $"中和-读取中和托盘1码 {elutionRackID1_S2} 成功");
                            short[] shorts = new short[20];
                            CommonParam.s_PLC.Fun10(1, 1801, shorts);
                            //判断托盘1码与CommonParam.s_WorkOrderMapHoleInfoDic中是否存在
                            if (CommonParam.s_WorkOrderMapHoleInfoList.Exists(info => info.RackID == elutionRackID1_S2))
                            {
                                LogEventManager.LogEvent(31, $"中和-中和托盘1码 {elutionRackID1_S2} 在工单存在");
                                //判断是否是主托盘码
                                if (elutionRackID1_S2?.StartsWith("1E") == true)
                                {
                                    LogEventManager.LogEvent(31, $"中和-判断是中和主托盘码");
                                    CommonParam.s_MapHoleInfoListS2_1 = CommonParam.s_WorkOrderMapHoleInfoList.FindAll(info => info != null && info.RackID == elutionRackID1_S2);
                                    CommonParam.s_WorkOrderKeyS2 = elutionRackID1_S2;
                                    CommonParam.SaveMapHoleInfosToSQL(CommonParam.s_MapHoleInfoListS2_1);
                                    SqlHelper.SetStringValue("ElutionRackID1_S2", elutionRackID1_S2);
                                    SqlHelper.SetStringValue("WorkOrderKeyS2", CommonParam.s_WorkOrderKeyS2);
                                    _mapHoleInfoListS2_1 = CommonParam.s_MapHoleInfoListS2_1.FindAll(info => info != null && info.PoolingLogic == "保留");
                                    _mapHoleInfoListS2_1.ForEach(item =>
                                    {
                                        item.State = MapHoleInfoState.Running;
                                        item.Progress = "进站扫码完成";
                                    });
                                }
                                else
                                {
                                    //报警
                                    LogEventManager.LogEvent(30, $"中和-判断不是中和主托盘码");
                                    CommonParam.s_PLC.Write_Register_Value(1, 2800, "2", "short");
                                    continue;
                                }
                                
                            }
                            else
                            {
                                //报警
                                LogEventManager.LogEvent(30, $"中和-中和托盘1码 {elutionRackID1_S2} 在工单不存在");
                                CommonParam.s_PLC.Write_Register_Value(1, 2800, "2", "short");
                                continue;
                            }

                            //判断托盘2码
                            elutionRackID2_S2 = CommonParam.s_PLC.Read_Register_State_ACSII(1, 1851, 20);
                           // elutionRackID2_S2 = "2E20260423-001";
                            if (elutionRackID2_S2 == "")
                            {
                                LogEventManager.LogEvent(30, $"中和-读取中和托盘2码 {elutionRackID2_S2} 失败");
                                continue;
                            }
                            LogEventManager.LogEvent(31, $"中和-读取中和托盘2码 {elutionRackID2_S2} 成功");
                            shorts = new short[20];
                            CommonParam.s_PLC.Fun10(1, 1851, shorts);
                            //判断托盘2码与CommonParam.s_WorkOrderMapHoleInfoDic中是否存在
                            if (CommonParam.s_WorkOrderMapHoleInfoList.Exists(info => info.RackID == elutionRackID2_S2))
                            {
                                LogEventManager.LogEvent(31, $"中和-中和托盘2码 {elutionRackID2_S2} 在工单存在");
                                if (elutionRackID2_S2?.StartsWith("2E") == true)
                                {
                                    LogEventManager.LogEvent(31, $"中和-判断是中和托盘2码");
                                    CommonParam.s_MapHoleInfoListS2_2 = CommonParam.s_WorkOrderMapHoleInfoList.FindAll(info => info != null && info.RackID == elutionRackID2_S2);
                                    CommonParam.s_WorkOrderKeyS2 = elutionRackID1_S2 + "," + elutionRackID2_S2;
                                    _mapHoleInfoListS2_2 = CommonParam.s_MapHoleInfoListS2_2.FindAll(info => info != null && info.PoolingLogic == "保留");
                                    CommonParam.SaveMapHoleInfosToSQL(CommonParam.s_MapHoleInfoListS2_2);
                                    SqlHelper.SetStringValue("ElutionRackID2_S2", elutionRackID2_S2);
                                    SqlHelper.SetStringValue("WorkOrderKeyS2", CommonParam.s_WorkOrderKeyS2);
                                    _mapHoleInfoListS2_2.ForEach(item =>
                                    {
                                        item.State = MapHoleInfoState.Running;
                                        item.Progress = "进站扫码完成";
                                    });
                                }
                                else
                                {
                                    //报警
                                    LogEventManager.LogEvent(30, $"中和-判断不是中和托盘码");
                                    CommonParam.s_PLC.Write_Register_Value(1, 2800, "2", "short");
                                    continue;
                                }
                            }
                            else
                            {
                                //报警
                                LogEventManager.LogEvent(30, $"中和-中和托盘2码 {elutionRackID2_S2} 在工单不存在");
                                CommonParam.s_PLC.Write_Register_Value(1, 2800, "2", "short");
                                continue;
                            }

                            //判断托盘3码
                            elutionRackID3_S2 = CommonParam.s_PLC.Read_Register_State_ACSII(1, 1901, 20);
                           // elutionRackID3_S2 = "3E20260423-001";
                            if (elutionRackID3_S2 == "")
                            {
                                LogEventManager.LogEvent(30, $"中和-读取中和托盘3码 {elutionRackID3_S2} 失败");
                                continue;
                            }
                            LogEventManager.LogEvent(31, $"中和-读取中和托盘3码 {elutionRackID3_S2} 成功");
                            shorts = new short[20];
                            CommonParam.s_PLC.Fun10(1, 1901, shorts);
                            //判断托盘3码与CommonParam.s_WorkOrderMapHoleInfoDic中是否存在
                            if (CommonParam.s_WorkOrderMapHoleInfoList.Exists(info => info.RackID == elutionRackID3_S2))
                            {
                                if (elutionRackID3_S2?.StartsWith("3E") == true)
                                {
                                    LogEventManager.LogEvent(31, $"中和-判断是中和托盘3码");
                                    CommonParam.s_MapHoleInfoListS2_3 = CommonParam.s_WorkOrderMapHoleInfoList.FindAll(info => info != null && info.RackID == elutionRackID3_S2);
                                    CommonParam.s_WorkOrderKeyS2 = elutionRackID1_S2 + "," + elutionRackID2_S2 + "," + elutionRackID3_S2;
                                    _mapHoleInfoListS2_3 = CommonParam.s_MapHoleInfoListS2_3.FindAll(info => info != null && info.PoolingLogic == "保留");
                                    CommonParam.SaveMapHoleInfosToSQL(CommonParam.s_MapHoleInfoListS2_3);
                                    SqlHelper.SetStringValue("ElutionRackID2_S2", elutionRackID3_S2);
                                    SqlHelper.SetStringValue("WorkOrderKeyS2", CommonParam.s_WorkOrderKeyS2);
                                    _mapHoleInfoListS2_3.ForEach(item =>
                                    {
                                        item.State = MapHoleInfoState.Running;
                                        item.Progress = "进站扫码完成";
                                    });
                                }
                                else
                                {
                                    //报警
                                    LogEventManager.LogEvent(30, $"中和-判断不是中和托盘码");
                                    CommonParam.s_PLC.Write_Register_Value(1, 2800, "2", "short");
                                    continue;
                                }
                            }
                            else
                            {
                                //报警
                                LogEventManager.LogEvent(30, $"中和-中和托盘3码 {elutionRackID3_S2} 在工单不存在");
                                CommonParam.s_PLC.Write_Register_Value(1, 2800, "2", "short");
                                continue;
                            }
                        }
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
                        _mapHoleInfoListS2_1 = CommonParam.s_MapHoleInfoListS2_1.FindAll(info => info != null && info.PoolingLogic == "保留");
                        _mapHoleInfoListS2_1.ForEach(item => { item.Progress = "中和中"; });
                        _mapHoleInfoListS2_2 = CommonParam.s_MapHoleInfoListS2_2.FindAll(info => info != null && info.PoolingLogic == "保留");
                        _mapHoleInfoListS2_2.ForEach(item => { item.Progress = "中和中"; });
                        _mapHoleInfoListS2_3 = CommonParam.s_MapHoleInfoListS2_3.FindAll(info => info != null && info.PoolingLogic == "保留");
                        _mapHoleInfoListS2_3.ForEach(item => { item.Progress = "中和中"; });
                        //分析工单给PLC中和托盘点位+容量数据
                        _neutralizeCount = 0;
                        LogEventManager.LogEvent(31, $"中和-分析托盘1数据给PLC中和托盘点位+容量数据");
                        float[] floats1 = Logic(_mapHoleInfoListS2_1);
                        CommonParam.s_PLC.WriteFloatArray(1, 2801, floats1);
                        LogEventManager.LogEvent(31, $"中和-托盘1数据给PLC中和托盘点位+容量数据完成");

                        LogEventManager.LogEvent(31, $"中和-分析托盘2数据给PLC中和托盘点位+容量数据");
                        float[] floats2 = Logic(_mapHoleInfoListS2_2);
                        CommonParam.s_PLC.WriteFloatArray(1, 2851, floats2);
                        LogEventManager.LogEvent(31, $"中和-托盘2数据给PLC中和托盘点位+容量数据完成");

                        LogEventManager.LogEvent(31, $"中和-分析托盘3数据给PLC中和托盘点位+容量数据");
                        float[] floats3 = Logic(_mapHoleInfoListS2_3);
                        CommonParam.s_PLC.WriteFloatArray(1, 2901, floats3);
                        LogEventManager.LogEvent(31, $"中和-托盘3数据给PLC中和托盘点位+容量数据完成");
                        CommonParam.s_PLC.Write_Register_Value(1, 2800, "1", "short");
                    }


                    //PLC中和完成信号触发
                    short[] registerValues_2082 = CommonParam.s_PLC.Read_Register_State_Short(1, 2082, 1);
                    if (registerValues_2082 != null && registerValues_2082[0] == 1)
                    {
                        CommonParam.s_PLC.Write_Register_Value(1, 2082, "0", "short");
                        LogEventManager.LogEvent(31, $"中和-PLC中和完成");
                        string nowStr = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");
                        if (CommonParam.s_MapHoleInfoListS2_1.Count == 0)
                        {
                            string code = SqlHelper.GetString("ElutionRackID1_S2");
                            CommonParam.s_MapHoleInfoListS2_1 = CommonParam.QueryMapHoleInfoByBatch(code);
                            
                        }
                        if (CommonParam.s_MapHoleInfoListS2_2.Count == 0)
                        {
                            string code = SqlHelper.GetString("ElutionRackID2_S2");
                            CommonParam.s_MapHoleInfoListS2_2 = CommonParam.QueryMapHoleInfoByBatch(code);
                            _mapHoleInfoListS2_2 = CommonParam.s_MapHoleInfoListS2_2.FindAll(info => info != null && info.PoolingLogic == "保留");
                        }
                        if (CommonParam.s_MapHoleInfoListS2_3.Count == 0)
                        {
                            string code = SqlHelper.GetString("ElutionRackID3_S2");
                            CommonParam.s_MapHoleInfoListS2_3 = CommonParam.QueryMapHoleInfoByBatch(code);
                            _mapHoleInfoListS2_3 = CommonParam.s_MapHoleInfoListS2_3.FindAll(info => info != null && info.PoolingLogic == "保留");
                        }
                        if (_mapHoleInfoListS2_1 == null)
                            _mapHoleInfoListS2_1 = CommonParam.s_MapHoleInfoListS2_1.FindAll(info => info != null && info.PoolingLogic == "保留");
                        if (_mapHoleInfoListS2_2 == null)
                            _mapHoleInfoListS2_2 = CommonParam.s_MapHoleInfoListS2_2.FindAll(info => info != null && info.PoolingLogic == "保留");
                        if (_mapHoleInfoListS2_3 == null)
                            _mapHoleInfoListS2_3 = CommonParam.s_MapHoleInfoListS2_3.FindAll(info => info != null && info.PoolingLogic == "保留");
                        _mapHoleInfoListS2_1.ForEach(item => { item.Progress = "中和完成"; item.Neutralization_Time = nowStr; });
                        _mapHoleInfoListS2_2.ForEach(item => { item.Progress = "中和完成"; item.Neutralization_Time = nowStr; });
                        _mapHoleInfoListS2_3.ForEach(item => { item.Progress = "中和完成"; item.Neutralization_Time = nowStr; });
                        CommonParam.SaveMapHoleInfosToSQL(CommonParam.s_MapHoleInfoListS2_1);
                        CommonParam.SaveMapHoleInfosToSQL(CommonParam.s_MapHoleInfoListS2_2);
                        CommonParam.SaveMapHoleInfosToSQL(CommonParam.s_MapHoleInfoListS2_3);
                        CommonParam.s_Capacity.DoneNum_ZH += _neutralizeCount;
                        SqlHelper.SetStringValue("DoneNum_ZH", CommonParam.s_Capacity.DoneNum_ZH.ToString());
                    }

                    Thread.Sleep(100);
                }

            });
        }

        #region bf
        //public override void Execute(CancellationToken token)
        //{
        //    // 中和执行逻辑
        //    Task.Run(() =>
        //    {
        //        while (!token.IsCancellationRequested)
        //        {
        //            if (CommonParam.s_DeviceState != DeviceState.Running)
        //            {
        //                Thread.Sleep(200);
        //                continue;
        //            }
        //            switch (_NeutralizeStepIndex)
        //            {
        //                case 0:
        //                    //等待请求托盘中和剂信号
        //                    LogEventManager.LogEvent(31, $"中和-等待请求托盘中和信号");
        //                    while (!token.IsCancellationRequested)
        //                    {
        //                        short[] registerValues_1800 = CommonParam.s_PLC.Read_Register_State_Short(1, 1800, 1);
        //                        if (registerValues_1800[0] == 1)
        //                        {
        //                            CommonParam.s_PLC.Write_Register_Value(1, 1800, "0", "short");
        //                            break;
        //                        }
        //                        Thread.Sleep(10);
        //                    }

        //                    _NeutralizeStepIndex = 10;
        //                    break;
        //                case 10:
        //                    //判断是否需要分析条码工单
        //                    if (CommonParam.s_ModeBools[2])
        //                    {
        //                        //直接传PLC数据
        //                        _NeutralizeStepIndex = 70;
        //                    }
        //                    else
        //                    {
        //                        //扫码分析
        //                        _NeutralizeStepIndex = 20;
        //                    }
        //                    break;
        //                case 20:
        //                    elutionRackID1_S2 = CommonParam.s_PLC.Read_Register_State_ACSII(1, 1801, 20);
        //                    if (elutionRackID1_S2 == null)
        //                    {
        //                        LogEventManager.LogEvent(30, $"中和-读取中和托盘1码 {elutionRackID1_S2} 失败");
        //                        _NeutralizeStepIndex = 0;
        //                    }
        //                    else
        //                    {
        //                        LogEventManager.LogEvent(31, $"中和-读取中和托盘1码 {elutionRackID1_S2} 成功");
        //                        short[] shorts = new short[20];
        //                        CommonParam.s_PLC.Fun10(1, 1801, shorts);
        //                        _NeutralizeStepIndex = 30;
        //                    }
        //                    break;
        //                case 30:
        //                    //判断托盘1码与CommonParam.s_WorkOrderMapHoleInfoDic中是否存在
        //                    if (CommonParam.s_WorkOrderMapHoleInfoList.Exists(info => info.RackID == elutionRackID1_S2))
        //                    {
        //                        LogEventManager.LogEvent(31, $"中和-中和托盘1码 {elutionRackID1_S2} 在工单存在");
        //                        CommonParam.s_MapHoleInfoListS2_1 = CommonParam.s_WorkOrderMapHoleInfoList.FindAll(info => info != null && info.RackID == elutionRackID1_S2);
        //                        CommonParam.s_WorkOrderKeyS2 = elutionRackID1_S2;
        //                        _mapHoleInfoListS2_1 = CommonParam.s_MapHoleInfoListS2_1.FindAll(info => info != null && info.PoolingLogic == "保留");
        //                        _mapHoleInfoListS2_1.ForEach(item =>
        //                        {
        //                            item.State = MapHoleInfoState.Running;
        //                            item.Progress = "进站扫码完成";
        //                        });
        //                        _NeutralizeStepIndex = 40;
        //                    }
        //                    else
        //                    {
        //                        //报警
        //                        LogEventManager.LogEvent(30, $"中和-中和托盘1码 {elutionRackID1_S2} 在工单不存在");
        //                        CommonParam.s_PLC.Write_Register_Value(1, 2800, "2", "short");
        //                        _NeutralizeStepIndex = 0;
        //                    }
        //                    break;
        //                case 40:
        //                    //判断是否是主托盘码
        //                    if (elutionRackID1_S2?.StartsWith("1E") == true)
        //                    {
        //                        LogEventManager.LogEvent(31, $"中和-判断是中和主托盘码");
        //                        _NeutralizeStepIndex = 50;
        //                    }
        //                    else
        //                    {
        //                        //报警
        //                        LogEventManager.LogEvent(30, $"中和-判断不是中和主托盘码");
        //                        CommonParam.s_PLC.Write_Register_Value(1, 2800, "2", "short");
        //                        _NeutralizeStepIndex = 0;
        //                    }
        //                    break;
        //                case 50:
        //                    //判断托盘2码
        //                    elutionRackID2_S2 = CommonParam.s_PLC.Read_Register_State_ACSII(1, 1851, 20);
        //                    if (elutionRackID2_S2 == null)
        //                    {
        //                        LogEventManager.LogEvent(30, $"中和-读取中和托盘2码 {elutionRackID2_S2} 失败");
        //                    }
        //                    else
        //                    {
        //                        LogEventManager.LogEvent(31, $"中和-读取中和托盘2码 {elutionRackID2_S2} 成功");
        //                        short[] shorts = new short[20];
        //                        CommonParam.s_PLC.Fun10(1, 1851, shorts);
        //                        if (elutionRackID2_S2?.StartsWith("2E") == true)
        //                        {
        //                            LogEventManager.LogEvent(31, $"中和-判断是中和托盘码");
        //                            CommonParam.s_MapHoleInfoListS2_2 = CommonParam.s_WorkOrderMapHoleInfoList.FindAll(info => info != null && info.RackID == elutionRackID2_S2);
        //                            CommonParam.s_WorkOrderKeyS2 = elutionRackID1_S2 + "," + elutionRackID2_S2;
        //                            _mapHoleInfoListS2_2 = CommonParam.s_MapHoleInfoListS2_2.FindAll(info => info != null && info.PoolingLogic == "保留");
        //                            _mapHoleInfoListS2_2.ForEach(item =>
        //                            {
        //                                item.State = MapHoleInfoState.Running;
        //                                item.Progress = "进站扫码完成";
        //                            });
        //                            _NeutralizeStepIndex = 60;
        //                        }
        //                        else
        //                        {
        //                            //报警
        //                            LogEventManager.LogEvent(30, $"中和-判断不是中和托盘码");
        //                            CommonParam.s_PLC.Write_Register_Value(1, 2800, "2", "short");
        //                            _NeutralizeStepIndex = 0;
        //                        }
        //                    }
        //                    break;
        //                case 60:
        //                    //判断托盘3码
        //                    elutionRackID3_S2 = CommonParam.s_PLC.Read_Register_State_ACSII(1, 1901, 20);
        //                    if (elutionRackID3_S2 == null)
        //                    {
        //                        LogEventManager.LogEvent(30, $"中和-读取中和托盘3码 {elutionRackID3_S2} 失败");
        //                    }
        //                    else
        //                    {
        //                        LogEventManager.LogEvent(31, $"中和-读取中和托盘3码 {elutionRackID3_S2} 成功");
        //                        short[] shorts = new short[20];
        //                        CommonParam.s_PLC.Fun10(1, 1901, shorts);
        //                        if (elutionRackID3_S2?.StartsWith("3E") == true)
        //                        {
        //                            LogEventManager.LogEvent(31, $"中和-判断是中和托盘3码");
        //                            CommonParam.s_MapHoleInfoListS2_3 = CommonParam.s_WorkOrderMapHoleInfoList.FindAll(info => info != null && info.RackID == elutionRackID3_S2);
        //                            CommonParam.s_WorkOrderKeyS2 = elutionRackID1_S2 + "," + elutionRackID2_S2 + "," + elutionRackID3_S2;
        //                            _mapHoleInfoListS2_3 = CommonParam.s_MapHoleInfoListS2_3.FindAll(info => info != null && info.PoolingLogic == "保留");
        //                            _mapHoleInfoListS2_3.ForEach(item =>
        //                            {
        //                                item.State = MapHoleInfoState.Running;
        //                                item.Progress = "进站扫码完成";
        //                            });
        //                            _NeutralizeStepIndex = 70;
        //                        }
        //                        else
        //                        {
        //                            //报警
        //                            LogEventManager.LogEvent(30, $"中和-判断不是中和托盘码");
        //                            CommonParam.s_PLC.Write_Register_Value(1, 2800, "2", "short");
        //                            _NeutralizeStepIndex = 0;
        //                        }
        //                    }
        //                    break;
        //                case 70:
        //                    _mapHoleInfoListS2_1 = CommonParam.s_MapHoleInfoListS2_1.FindAll(info => info != null && info.PoolingLogic == "保留");
        //                    _mapHoleInfoListS2_1.ForEach(item => { item.Progress = "中和中"; });
        //                    _mapHoleInfoListS2_2 = CommonParam.s_MapHoleInfoListS2_2.FindAll(info => info != null && info.PoolingLogic == "保留");
        //                    _mapHoleInfoListS2_2.ForEach(item => { item.Progress = "中和中"; });
        //                    _mapHoleInfoListS2_3 = CommonParam.s_MapHoleInfoListS2_3.FindAll(info => info != null && info.PoolingLogic == "保留");
        //                    _mapHoleInfoListS2_3.ForEach(item => { item.Progress = "中和中"; });
        //                    //分析工单给PLC中和托盘点位+容量数据
        //                    LogEventManager.LogEvent(31, $"中和-分析托盘1数据给PLC中和托盘点位+容量数据");
        //                    float[] floats1 = Logic(_mapHoleInfoListS2_1);
        //                    CommonParam.s_PLC.WriteFloatArray(1, 2801, floats1);
        //                    LogEventManager.LogEvent(31, $"中和-托盘1数据给PLC中和托盘点位+容量数据完成");

        //                    LogEventManager.LogEvent(31, $"中和-分析托盘2数据给PLC中和托盘点位+容量数据");
        //                    float[] floats2 = Logic(_mapHoleInfoListS2_2);
        //                    CommonParam.s_PLC.WriteFloatArray(1, 2851, floats2);
        //                    LogEventManager.LogEvent(31, $"中和-托盘2数据给PLC中和托盘点位+容量数据完成");

        //                    LogEventManager.LogEvent(31, $"中和-分析托盘3数据给PLC中和托盘点位+容量数据");
        //                    float[] floats3 = Logic(_mapHoleInfoListS2_3);
        //                    CommonParam.s_PLC.WriteFloatArray(1, 2901, floats3);
        //                    LogEventManager.LogEvent(31, $"中和-托盘3数据给PLC中和托盘点位+容量数据完成");
        //                    CommonParam.s_PLC.Write_Register_Value(1, 2800, "1", "short");
        //                    _NeutralizeStepIndex = 80;
        //                    break;
        //                case 80:
        //                    //等待PLC中和完成
        //                    LogEventManager.LogEvent(31, $"中和-等待PLC中和完成信号");
        //                    while (!token.IsCancellationRequested)
        //                    {
        //                        short[] registerValues_2082 = CommonParam.s_PLC.Read_Register_State_Short(1, 2082, 1);
        //                        if (registerValues_2082[0] == 1)
        //                        {
        //                            CommonParam.s_PLC.Write_Register_Value(1, 2082, "0", "short");
        //                            break;
        //                        }
        //                        Thread.Sleep(10);
        //                    }
        //                    LogEventManager.LogEvent(31, $"中和-PLC中和完成");
        //                    string nowStr = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");
        //                    _mapHoleInfoListS2_1.ForEach(item => { item.Progress = "中和完成"; item.Neutralization_Time = nowStr; });
        //                    _mapHoleInfoListS2_2.ForEach(item => { item.Progress = "中和完成"; item.Neutralization_Time = nowStr; });
        //                    _mapHoleInfoListS2_3.ForEach(item => { item.Progress = "中和完成"; item.Neutralization_Time = nowStr; });
        //                    CommonParam.SaveMapHoleInfosToSQL(CommonParam.s_MapHoleInfoListS2_1);
        //                    CommonParam.SaveMapHoleInfosToSQL(CommonParam.s_MapHoleInfoListS2_2);
        //                    CommonParam.SaveMapHoleInfosToSQL(CommonParam.s_MapHoleInfoListS2_3);
        //                    _NeutralizeStepIndex = 0;
        //                    break;
        //            }
        //        }

        //    });
        //}
        #endregion
        /// <summary>
        /// 中和逻辑
        /// </summary>
        private float[] Logic(List<MapHoleInfo> mapHoleInfos)
        {
            
            float[] floats = new float[24];
            //遍历每一条工单
            foreach (MapHoleInfo mapHoleInfo in mapHoleInfos)
            {
                switch (mapHoleInfo.Location)
                {
                    case "A1":
                            floats[0] = NeutralizeLogic(mapHoleInfo);
                        break;
                    case "A2":
                            floats[1] = NeutralizeLogic(mapHoleInfo);
                        break;
                    case "A3":
                            floats[2] = NeutralizeLogic(mapHoleInfo);
                        break;
                    case "A4":
                            floats[3] = NeutralizeLogic(mapHoleInfo);
                        break;
                    case "A5":
                            floats[4] = NeutralizeLogic(mapHoleInfo);         
                        break;
                    case "A6":
                        
                            floats[5] = NeutralizeLogic(mapHoleInfo);
                        break;
                    case "B1":
                            floats[6] = NeutralizeLogic(mapHoleInfo);
                        break;
                    case "B2":
                            floats[7] = NeutralizeLogic(mapHoleInfo);
                        break;
                    case "B3":
                            floats[8] = NeutralizeLogic(mapHoleInfo);
                        break;
                    case "B4":
                            floats[9] = NeutralizeLogic(mapHoleInfo);
                        break;
                    case "B5":
                            floats[10] = NeutralizeLogic(mapHoleInfo);
                        break;
                    case "B6":
                            floats[11] = NeutralizeLogic(mapHoleInfo);
                        break;
                    case "C1":
                            floats[12] = NeutralizeLogic(mapHoleInfo);
                        break;
                    case "C2":
                            floats[13] = NeutralizeLogic(mapHoleInfo);
                        break;
                    case "C3":
                            floats[14] = NeutralizeLogic(mapHoleInfo);
                        break;
                    case "C4":
                            floats[15] = NeutralizeLogic(mapHoleInfo);
                        break;
                    case "C5":
                            floats[16] = NeutralizeLogic(mapHoleInfo);
                        break;
                    case "C6":
                            floats[17] = NeutralizeLogic(mapHoleInfo);
                        break;
                    case "D1":
                            floats[18] = NeutralizeLogic(mapHoleInfo);
                        break;
                    case "D2":
                            floats[19] = NeutralizeLogic(mapHoleInfo);
                        break;
                    case "D3":
                            floats[20] = NeutralizeLogic(mapHoleInfo);
                        break;
                    case "D4":
                            floats[21] = NeutralizeLogic(mapHoleInfo);
                        break;
                    case "D5":
                            floats[22] = NeutralizeLogic(mapHoleInfo);
                        break;
                    case "D6":
                            floats[23] = NeutralizeLogic(mapHoleInfo);
                        break;
                }
            }
            return floats;

        }

        private float NeutralizeLogic(MapHoleInfo mapHoleInfo)
        {
            //中和液Volume(ml) = (Reagent Volume(ml) + 磁珠添加量(ml) * 0.15
            //磁珠添加量 = (Cedex yield/150) * Cal.M.W.(KDa) * 1.2)/ 30 * Expression Scale(ml) / 1000, 若有小数，向上进位0.5即可，比如计算结果是3.1，
            //                                                                                         那就进位到3.5，3.7就进位到4.0;
            double.TryParse(mapHoleInfo.CedexYield, out double cedexyield);
            double.TryParse(mapHoleInfo.Cal_M_W, out double Cal_M_W);
            double.TryParse(mapHoleInfo.ExpressionScale, out double expressionScale);
            double.TryParse(mapHoleInfo.ReagentVolume, out double reagentVolume);
            double magneticBeadValue = Math.Ceiling(cedexyield / 150 * Cal_M_W * 1.2 / 30 * expressionScale / 1000 * 2) / 2;
            double neutralizeVolume = (reagentVolume + magneticBeadValue) * 0.15 * neutralizeParam.e;
            mapHoleInfo.NeutralizationBufferVolume = neutralizeVolume.ToString();
            mapHoleInfo.Neutralization_Operator = CommonParam.s_CurrentUser.UserName;
            mapHoleInfo.NeutralizationBuffer = "NeutralizationBuffer";
            mapHoleInfo.NeutralizationBufferBatchNumber = "NeutralizationBufferBatchNumber";
            _neutralizeCount++;
            return (float)neutralizeVolume;
        }
    }
}
