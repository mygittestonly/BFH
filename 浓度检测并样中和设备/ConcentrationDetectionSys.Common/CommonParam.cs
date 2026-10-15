using ConcentrationDetectionSys.DAL;
using ConcentrationDetectionSys.Hardware;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConcentrationDetectionSys.Common
{
    public static class CommonParam
    {
        public static bool s_TestInjectCapacity = true;   // 【临时调试】true = 并样容量强制发测试值
        public static CapacityClass s_Capacity = new CapacityClass();
        public static User s_CurrentUser = new User();
        public static RunMode s_RunMode = RunMode.Manual;
        public static MES_Mode s_MES_Mode = MES_Mode.Off_Line;
        public static DeviceState s_DeviceState = DeviceState.Stop;
        public static bool AutoRunCondition => s_DeviceState == DeviceState.Running && s_RunMode== RunMode.Auto;
        public static MagellanELIASA s_Magellan = new MagellanELIASA();
        public static ModbusTcp s_PLC;
        public static bool[] s_StationInfo = new bool[5];
        public static DataTable s_WorkOrderTable = new DataTable();
        public static bool s_UpDataWorkOrderTableFinish = false;
        /// <summary>
        /// 设备1进站完成信号
        /// </summary>
        public static bool s_BoardingCompleteS1 = false;
        /// <summary>
        /// 设备2进站完成信号
        /// </summary>
        public static bool s_BoardingCompleteS2 = false;
        public static bool[] s_ModeBools;
        /// <summary>
        /// 整个工单的Map孔位信息字典，key为托盘号，value为对应组内所有的Map孔位信息列表
        /// </summary>
        public static List<MapHoleInfo> s_WorkOrderMapHoleInfoList = new List<MapHoleInfo>();
        /// <summary>
        /// 第一台设备做料的托盘组号
        /// </summary>
        public static List<string> s_WorkOrderKeyS1 = new List<string>();
        public static object lock_WorkOrderKeyS1 = new object();
        /// <summary>
        /// 第一台设备第一个托盘的Map孔位信息列表
        /// </summary>
        public static List<MapHoleInfo> s_MapHoleInfoListS1_1 = new List<MapHoleInfo>();
        public static object lock_MapHoleS1_1 = new object();
        /// <summary>
        /// 第一台设备第二个托盘的Map孔位信息列表
        /// </summary>
        public static List<MapHoleInfo> s_MapHoleInfoListS1_2 = new List<MapHoleInfo>();
        public static object lock_MapHoleS1_2 = new object();
        /// <summary>
        /// 第一台设备第三个托盘的Map孔位信息列表
        /// </summary>
        public static List<MapHoleInfo> s_MapHoleInfoListS1_3 = new List<MapHoleInfo>();
        public static object lock_MapHoleS1_3 = new object();
        /// <summary>
        /// 第二台设备做料的托盘组号
        /// </summary>
        public static string s_WorkOrderKeyS2 = "";
        public static object lock_WorkOrderKeyS2 = new object();
        /// <summary>
        /// 第二台设备第一个托盘的Map孔位信息列表
        /// </summary>
        public static List<MapHoleInfo> s_MapHoleInfoListS2_1 = new List<MapHoleInfo>();
        /// <summary>
        /// 第二台设备第二个托盘的Map孔位信息列表
        /// </summary>
        public static List<MapHoleInfo> s_MapHoleInfoListS2_2 = new List<MapHoleInfo>();
        /// <summary>
        /// 第二台设备第三个托盘的Map孔位信息列表
        /// </summary>
        public static List<MapHoleInfo> s_MapHoleInfoListS2_3 = new List<MapHoleInfo>();
        //耗材数量
        public static Consumableitems s_Consumableitems = new Consumableitems();
        //静态锁对象
        private static readonly object _mapHoleSaveLocker = new object();
        public static void SaveMapHoleInfosToSQL(List<MapHoleInfo> dataList)
        {
            lock(_mapHoleSaveLocker)
            {
                if (dataList == null || dataList.Count == 0) return;
                // 开启事务，一次性全量写入
                SqlHelper.ExecuteTransaction((conn, trans) =>
                {
                    foreach (var item in dataList)
                    {
                        SqlParameter[] pms = new SqlParameter[]
                        {
                        new SqlParameter("@ID", item.ID ?? (object)DBNull.Value),
                        new SqlParameter("@TubeID", item.TubeID ?? (object)DBNull.Value),
                        new SqlParameter("@RackID", item.RackID ?? (object)DBNull.Value),
                        new SqlParameter("@Location", item.Location ?? (object)DBNull.Value),
                        new SqlParameter("@OrderID", item.OrderID ?? (object)DBNull.Value),
                        new SqlParameter("@OrderRepeatID", item.OrderRepeatID ?? (object)DBNull.Value),
                        new SqlParameter("@BatchID", item.BatchID ?? (object)DBNull.Value),
                        new SqlParameter("@ExpressionScale", item.ExpressionScale ?? (object)DBNull.Value),
                        new SqlParameter("@Cal_M_W", item.Cal_M_W ?? (object)DBNull.Value),
                        new SqlParameter("@CedexYield", item.CedexYield ?? (object)DBNull.Value),
                        new SqlParameter("@ExtinctionCoefficient", item.ExtinctionCoefficient ?? (object)DBNull.Value),
                        new SqlParameter("@ReagentTypes", item.ReagentTypes ?? (object)DBNull.Value),
                        new SqlParameter("@ReagentName", item.ReagentName ?? (object)DBNull.Value),
                        new SqlParameter("@ReagentBatchNumber", item.ReagentBatchNumber ?? (object)DBNull.Value),
                        new SqlParameter("@ReagentVolume", item.ReagentVolume ?? (object)DBNull.Value),
                        new SqlParameter("@TubeLoadingOperator", item.TubeLoadingOperator ?? (object)DBNull.Value),
                        new SqlParameter("@TubeLoadingTime", item.TubeLoadingTime ?? (object)DBNull.Value),
                        new SqlParameter("@MicroplateID", item.MicroplateID ?? (object)DBNull.Value),
                        new SqlParameter("@LoadTime", item.LoadTime ?? (object)DBNull.Value),
                        new SqlParameter("@OD_Value", item.OD_Value ?? (object)DBNull.Value),
                        new SqlParameter("@Test_Operator", item.Test_Operator ?? (object)DBNull.Value),
                        new SqlParameter("@Concentration_Test_Time", item.Concentration_Test_Time ?? (object)DBNull.Value),
                        new SqlParameter("@PoolingLogic", item.PoolingLogic ?? (object)DBNull.Value),
                        new SqlParameter("@Target", item.Target ?? (object)DBNull.Value),
                        new SqlParameter("@Final_Protein_Mass", item.Final_Protein_Mass ?? (object)DBNull.Value),
                        new SqlParameter("@Final_Volume", item.Final_Volume ?? (object)DBNull.Value),
                        new SqlParameter("@Pooling_Operator", item.Pooling_Operator ?? (object)DBNull.Value),
                        new SqlParameter("@PoolingTime", item.PoolingTime ?? (object)DBNull.Value),
                        new SqlParameter("@NeutralizationBuffer", item.NeutralizationBuffer ?? (object)DBNull.Value),
                        new SqlParameter("@NeutralizationBufferBatchNumber", item.NeutralizationBufferBatchNumber ?? (object)DBNull.Value),
                        new SqlParameter("@NeutralizationBufferVolume", item.NeutralizationBufferVolume ?? (object)DBNull.Value),
                        new SqlParameter("@Neutralization_Operator", item.Neutralization_Operator ?? (object)DBNull.Value),
                        new SqlParameter("@Neutralization_Time", item.Neutralization_Time ?? (object)DBNull.Value),
                        new SqlParameter("@DesaltingColumn", item.DesaltingColumn ?? (object)DBNull.Value),
                        new SqlParameter("@DesaltingColumnBatchNumber", item.DesaltingColumnBatchNumber ?? (object)DBNull.Value),
                        new SqlParameter("@Desalting_Operator", item.Desalting_Operator ?? (object)DBNull.Value),
                        new SqlParameter("@Desalting_Time", item.Desalting_Time ?? (object)DBNull.Value),
                        new SqlParameter("@CreateTime", item.CreateTime ?? (object)DBNull.Value)
                        };

                        string mergeSql = @"
                                    MERGE INTO ProductionRecordsT AS Target
                                    USING (SELECT @RackID AS Rack_ID, @TubeID AS Tube_ID) AS Source
                                    ON Target.Rack_ID = Source.Rack_ID AND Target.Tube_ID = Source.Tube_ID
                                    WHEN MATCHED THEN
                                        UPDATE SET 
                                            ID = @ID,
                                            Location = @Location,
                                            [Order ID] = @OrderID,
                                            [Order Repeat ID] = @OrderRepeatID,
                                            [Batch ID] = @BatchID,
                                            [Expression Scale(ml)] = @ExpressionScale,
                                            [Cal.M.W.(KDa)] = @Cal_M_W,
                                            [Cedex yield] = @CedexYield,
                                            [Extinction Coefficient] = @ExtinctionCoefficient,
                                            [Reagent types] = @ReagentTypes,
                                            [Reagent Name] = @ReagentName,
                                            [Reagent Batch Number] = @ReagentBatchNumber,
                                            [Reagent Volume(ml)] = @ReagentVolume,
                                            Tube_Loading_Operator = @TubeLoadingOperator,
                                            Tube_Loading_Time = @TubeLoadingTime,
                                            [Microplate ID] = @MicroplateID,
                                            [Load Time] = @LoadTime,
                                            OD_Value = @OD_Value,
                                            Test_Operator = @Test_Operator,
                                            Concentration_Test_Time = @Concentration_Test_Time,
                                            [Pooling Logic] = @PoolingLogic,
                                            Target = @Target,
                                            Final_Protein_Mass = @Final_Protein_Mass,
                                            [Final_Volume(ml)] = @Final_Volume,
                                            Pooling_Operator = @Pooling_Operator,
                                            [pooling time] = @PoolingTime,
                                            [Neutralization Buffer] = @NeutralizationBuffer,
                                            [Neutralization Buffer batch number] = @NeutralizationBufferBatchNumber,
                                            [Neutralization Buffer Volume(ml)] = @NeutralizationBufferVolume,
                                            Neutralization_Operator = @Neutralization_Operator,
                                            Neutralization_Time = @Neutralization_Time,
                                            [Desalting Column] = @DesaltingColumn,
                                            [Desalting Column batch number] = @DesaltingColumnBatchNumber,
                                            Desalting_Operator = @Desalting_Operator,
                                            Desalting_Time = @Desalting_Time,
                                            [Create Time] = @CreateTime
                                    WHEN NOT MATCHED THEN
                                        INSERT (ID, Rack_ID, Tube_ID, Location, [Order ID], [Order Repeat ID], [Batch ID], 
                                            [Expression Scale(ml)], [Cal.M.W.(KDa)], [Cedex yield], [Extinction Coefficient], 
                                            [Reagent types], [Reagent Name], [Reagent Batch Number], [Reagent Volume(ml)], 
                                            Tube_Loading_Operator, Tube_Loading_Time, [Microplate ID], [Load Time], 
                                            OD_Value, Test_Operator, Concentration_Test_Time, [Pooling Logic], Target, 
                                            Final_Protein_Mass, [Final_Volume(ml)], Pooling_Operator, [pooling time], 
                                            [Neutralization Buffer], [Neutralization Buffer batch number], [Neutralization Buffer Volume(ml)], 
                                            Neutralization_Operator, Neutralization_Time, [Desalting Column], 
                                            [Desalting Column batch number], Desalting_Operator, Desalting_Time, [Create Time])
                                        VALUES (@ID, @RackID, @TubeID, @Location, @OrderID, @OrderRepeatID, @BatchID, 
                                            @ExpressionScale, @Cal_M_W, @CedexYield, @ExtinctionCoefficient, 
                                            @ReagentTypes, @ReagentName, @ReagentBatchNumber, @ReagentVolume, 
                                            @TubeLoadingOperator, @TubeLoadingTime, @MicroplateID, @LoadTime, 
                                            @OD_Value, @Test_Operator, @Concentration_Test_Time, @PoolingLogic, @Target, 
                                            @Final_Protein_Mass, @Final_Volume, @Pooling_Operator, @PoolingTime, 
                                            @NeutralizationBuffer, @NeutralizationBufferBatchNumber, @NeutralizationBufferVolume, 
                                            @Neutralization_Operator, @Neutralization_Time, @DesaltingColumn, 
                                            @DesaltingColumnBatchNumber, @Desalting_Operator, @Desalting_Time, @CreateTime);
                                    ";
                        using (SqlCommand cmd = new SqlCommand(mergeSql, conn, trans))
                        {
                            cmd.Parameters.AddRange(pms);
                            cmd.CommandTimeout = 10;
                            cmd.ExecuteNonQuery();
                        }
                    }
                });
            }
           // CommonParam.OnMapHoleInfosChanged?.Invoke(); 
        }

        /// <summary>
        /// 从 ConcentrationRecordsT 根据RackID查询，返回List<MapHoleInfo>
        /// </summary>
        public static List<MapHoleInfo> QueryMapHoleInfoByBatch(string rackID)
        {
            string sql = @"SELECT 
                            ID,
                            Rack_ID,
                            Tube_ID,
                            Location,
                            [Order ID],
                            [Order Repeat ID],
                            [Batch ID],
                            [Expression Scale(ml)],
                            [Cal.M.W.(KDa)],
                            [Cedex yield],
                            [Extinction Coefficient],
                            [Reagent types],
                            [Reagent Name],
                            [Reagent Batch Number],
                            [Reagent Volume(ml)],
                            Tube_Loading_Operator,
                            Tube_Loading_Time,
                            [Microplate ID],
                            [Load Time],
                            OD_Value,
                            Test_Operator,
                            Concentration_Test_Time,
                            [Pooling Logic],
                            Target,
                            Final_Protein_Mass,
                            [Final_Volume(ml)],
                            Pooling_Operator,
                            [pooling time],
                            [Neutralization Buffer],
                            [Neutralization Buffer batch number],
                            [Neutralization Buffer Volume(ml)],
                            Neutralization_Operator,
                            Neutralization_Time,
                            [Desalting Column],
                            [Desalting Column batch number],
                            Desalting_Operator,
                            Desalting_Time,
                            [Create Time]
                        FROM ProductionRecordsT 
                        WHERE [Rack_ID] = @Rack_ID ";
            SqlParameter[] pms = {new SqlParameter("@Rack_ID", rackID ?? "")};

            List<MapHoleInfo> list = new List<MapHoleInfo>();
            lock (_mapHoleSaveLocker)
            {
                using (SqlConnection conn = new SqlConnection(SqlHelper.connStr))
                {
                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddRange(pms);
                    conn.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            MapHoleInfo item = new MapHoleInfo
                            {
                                ID = dr["ID"] == DBNull.Value ? "" : dr["ID"].ToString(),
                                RackID = dr["Rack_ID"] == DBNull.Value ? "" : dr["Rack_ID"].ToString(),
                                TubeID = dr["Tube_ID"] == DBNull.Value ? "" : dr["Tube_ID"].ToString(),
                                Location = dr["Location"] == DBNull.Value ? "" : dr["Location"].ToString(),
                                OrderID = dr["Order ID"] == DBNull.Value ? "" : dr["Order ID"].ToString(),
                                OrderRepeatID = dr["Order Repeat ID"] == DBNull.Value ? "" : dr["Order Repeat ID"].ToString(),
                                BatchID = dr["Batch ID"] == DBNull.Value ? "" : dr["Batch ID"].ToString(),

                                ExpressionScale = dr["Expression Scale(ml)"] == DBNull.Value ? "" : dr["Expression Scale(ml)"].ToString(),
                                Cal_M_W = dr["Cal.M.W.(KDa)"] == DBNull.Value ? "" : dr["Cal.M.W.(KDa)"].ToString(),
                                CedexYield = dr["Cedex yield"] == DBNull.Value ? "" : dr["Cedex yield"].ToString(),
                                ExtinctionCoefficient = dr["Extinction Coefficient"] == DBNull.Value ? "" : dr["Extinction Coefficient"].ToString(),

                                ReagentTypes = dr["Reagent types"] == DBNull.Value ? "" : dr["Reagent types"].ToString(),
                                ReagentName = dr["Reagent Name"] == DBNull.Value ? "" : dr["Reagent Name"].ToString(),
                                ReagentBatchNumber = dr["Reagent Batch Number"] == DBNull.Value ? "" : dr["Reagent Batch Number"].ToString(),
                                ReagentVolume = dr["Reagent Volume(ml)"] == DBNull.Value ? "" : dr["Reagent Volume(ml)"].ToString(),

                                TubeLoadingOperator = dr["Tube_Loading_Operator"] == DBNull.Value ? "" : dr["Tube_Loading_Operator"].ToString(),
                                TubeLoadingTime = dr["Tube_Loading_Time"] == DBNull.Value ? "" : dr["Tube_Loading_Time"].ToString(),
                                MicroplateID = dr["Microplate ID"] == DBNull.Value ? "" : dr["Microplate ID"].ToString(),
                                LoadTime = dr["Load Time"] == DBNull.Value ? "" : dr["Load Time"].ToString(),

                                OD_Value = dr["OD_Value"] == DBNull.Value ? "" : dr["OD_Value"].ToString(),
                                Test_Operator = dr["Test_Operator"] == DBNull.Value ? "" : dr["Test_Operator"].ToString(),
                                Concentration_Test_Time = dr["Concentration_Test_Time"] == DBNull.Value ? "" : dr["Concentration_Test_Time"].ToString(),
                                PoolingLogic = dr["Pooling Logic"] == DBNull.Value ? "" : dr["Pooling Logic"].ToString(),

                                Target = dr["Target"] == DBNull.Value ? "" : dr["Target"].ToString(),
                                Final_Protein_Mass = dr["Final_Protein_Mass"] == DBNull.Value ? "" : dr["Final_Protein_Mass"].ToString(),
                                Final_Volume = dr["Final_Volume(ml)"] == DBNull.Value ? "" : dr["Final_Volume(ml)"].ToString(),
                                Pooling_Operator = dr["Pooling_Operator"] == DBNull.Value ? "" : dr["Pooling_Operator"].ToString(),
                                PoolingTime = dr["pooling time"] == DBNull.Value ? "" : dr["pooling time"].ToString(),

                                NeutralizationBuffer = dr["Neutralization Buffer"] == DBNull.Value ? "" : dr["Neutralization Buffer"].ToString(),
                                NeutralizationBufferBatchNumber = dr["Neutralization Buffer batch number"] == DBNull.Value ? "" : dr["Neutralization Buffer batch number"].ToString(),
                                NeutralizationBufferVolume = dr["Neutralization Buffer Volume(ml)"] == DBNull.Value ? "" : dr["Neutralization Buffer Volume(ml)"].ToString(),
                                Neutralization_Operator = dr["Neutralization_Operator"] == DBNull.Value ? "" : dr["Neutralization_Operator"].ToString(),
                                Neutralization_Time = dr["Neutralization_Time"] == DBNull.Value ? "" : dr["Neutralization_Time"].ToString(),

                                DesaltingColumn = dr["Desalting Column"] == DBNull.Value ? "" : dr["Desalting Column"].ToString(),
                                DesaltingColumnBatchNumber = dr["Desalting Column batch number"] == DBNull.Value ? "" : dr["Desalting Column batch number"].ToString(),
                                Desalting_Operator = dr["Desalting_Operator"] == DBNull.Value ? "" : dr["Desalting_Operator"].ToString(),
                                Desalting_Time = dr["Desalting_Time"] == DBNull.Value ? "" : dr["Desalting_Time"].ToString(),
                                CreateTime = dr["Create Time"] == DBNull.Value ? "" : dr["Create Time"].ToString(),

                                State = MapHoleInfoState.Running,
                                Progress = ""
                            };
                            list.Add(item);
                        }
                    }
                }
            }
                
            return list;
        }

    }
    /// <summary>
    /// 工单
    /// </summary>
    public abstract class WorkOrder
    {
        public string ID { get; set; }
        /// <summary>
        /// 托盘ID
        /// </summary>
        public string RackID { get; set; }
        /// <summary>
        /// 试管ID
        /// </summary>
        public string TubeID { get; set; }
        /// <summary>
        /// 试管位置
        /// </summary>
        public string Location { get; set; }
        /// <summary>
        /// 订单ID
        /// </summary>
        public string OrderID { get; set; }
        /// <summary>
        /// 重复订单ID
        /// </summary>
        public string OrderRepeatID { get; set; }
        /// <summary>
        /// 批次ID
        /// </summary>
        public string BatchID { get; set; }
        /// <summary>
        /// 表达量表
        /// </summary>
        public string ExpressionScale { get; set; }
        /// <summary>
        /// 分子量
        /// </summary>
        public string Cal_M_W { get; set; }
        public string CedexYield { get; set; }
        /// <summary>
        /// 消光系数
        /// </summary>
        public string ExtinctionCoefficient { get; set; }
        /// <summary>
        /// 试剂类型
        /// </summary>
        public string ReagentTypes { get; set; }
        /// <summary>
        /// 试剂名称
        /// </summary>
        public string ReagentName { get; set; }
        /// <summary>
        /// 试剂批次号
        /// </summary>
        public string ReagentBatchNumber { get; set; }
        /// <summary>
        /// 试剂体积
        /// </summary>
        public string ReagentVolume { get; set; }
        /// <summary>
        /// 上料操作人
        /// </summary>
        public string TubeLoadingOperator { get; set; }
        /// <summary>
        /// 上料时间
        /// </summary>
        public string TubeLoadingTime { get; set; }
        /// <summary>
        /// 酶标板ID
        /// </summary>
        public string MicroplateID { get; set; }
        /// <summary>
        /// 点样时间
        /// </summary>
        public string LoadTime { get; set; }
        /// <summary>
        /// 校正A260值
        /// </summary>
        public string OD_Value { get; set; }
        /// <summary>
        /// 测浓操作人
        /// </summary>
        public string Test_Operator { get; set; }
        /// <summary>
        /// 测浓时间
        /// </summary>
        public string Concentration_Test_Time { get; set; }
        /// <summary>
        /// 合并逻辑
        /// </summary>
        public string PoolingLogic { get; set; }
        /// <summary>
        /// 指标
        /// </summary>
        public string Target { get; set; }
        /// <summary>
        /// 最终蛋白质质量
        /// </summary>
        public string Final_Protein_Mass { get; set; }
        /// <summary>
        /// 最终体积
        /// </summary>
        public string Final_Volume { get; set; }
        /// <summary>
        /// 合并操作人
        /// </summary>
        public string Pooling_Operator { get; set; }
        /// <summary>
        /// 合并时间
        /// </summary>
        public string PoolingTime { get; set; }
        /// <summary>
        /// 中和缓冲液
        /// </summary>
        public string NeutralizationBuffer { get; set; }
        /// <summary>
        /// 中和缓冲液批次号
        /// </summary>
        public string NeutralizationBufferBatchNumber { get; set; }
        /// <summary>
        /// 中和缓冲液体积
        /// </summary>
        public string NeutralizationBufferVolume { get; set; }
        /// <summary>
        /// 中和操作人
        /// </summary>
        public string Neutralization_Operator { get; set; }
        /// <summary>
        /// 中和时间
        /// </summary>
        public string Neutralization_Time { get; set; }
        public string DesaltingColumn { get; set; }

        public string DesaltingColumnBatchNumber { get; set; }
        public string Desalting_Operator { get; set; }
        public string Desalting_Time { get; set; }
        /// <summary>
        /// 建单时间
        /// </summary>
        public string CreateTime { get; set; }

    }
    /// <summary>
    /// 耗材数量
    /// </summary>
    public class Consumableitems
    {
        /// <summary>
        /// buffer1试管批号
        /// </summary>
        public string Buffer1_BatchNumber;
        /// <summary>
        /// buffer1试管位置码
        /// </summary>
        public string Buffer1_PositionalCode;
        /// <summary>
        /// buffer1试管名称
        public string Buffer1_Name;
        /// <summary>
        /// buffer1试管体积
        /// </summary>
        public string Buffer1_Volume;
        /// <summary>
        /// buffer2试管批号
        /// </summary>
        public string Buffer2_BatchNumber;
        /// <summary>
        /// buffer2试管位置码
        /// </summary>
        public string Buffer2_PositionalCode;
        /// <summary>
        /// buffer2试管名称
        public string Buffer2_Name;
        /// <summary>
        /// buffer2试管体积
        /// </summary>
        public string Buffer2_Volume;
        /// <summary>
        /// 中和液1批次码
        /// </summary>
        public string Neutralize1_BatchNumber;
        /// <summary>
        /// 中和液1位置码
        /// </summary>
        public string Neutralize1_PositionalCode;
        /// <summary>
        ///中和液1名称
        /// </summary>
        public string Neutralize1_Name;
        /// <summary>
        /// 中和液2批次码
        /// </summary>
        public string Neutralize2_BatchNumber;
        /// <summary>
        /// 中和液2位置码
        /// </summary>
        public string Neutralize2_PositionalCode;
        /// <summary>
        ///中和液2名称
        /// </summary>
        public string Neutralize2_Name;

        /// <summary>
        /// 酶标板数量
        /// </summary>
        public int ElisaPlateCount;
        /// <summary>
        ///50ultip-1数量
        /// </summary>
        public int Tip1Count_50ul;
        /// <summary>
        ///50ultip-2数量
        /// </summary>
        public int Tip2Count_50ul;
        /// <summary>
        ///50ultip-3数量
        /// </summary>
        public int Tip3Count_50ul;
        /// <summary>
        ///50ultip-4数量
        /// </summary>
        public int Tip4Count_50ul;
        /// <summary>
        ///50ultip-5数量
        /// </summary>
        public int Tip5Count_50ul;
        /// <summary>
        ///10ultip-1数量
        /// </summary>
        public int Tip1Count_10ul;
        /// <summary>
        ///10ultip-2数量
        /// </summary>
        public int Tip2Count_10ul;
        /// <summary>
        ///10ultip-3数量
        /// </summary>
        public int Tip3Count_10ul;
        /// <summary>
        ///10ultip-4数量
        /// </summary>
        public int Tip4Count_10ul;
        /// <summary>
        ///10ultip-5数量
        /// </summary>
        public int Tip5Count_10ul;
        /// <summary>
        ///10ultip-6数量
        /// </summary>
        public int Tip6Count_10ul;
        /// <summary>
        ///10ultip-7数量
        /// </summary>
        public int Tip7Count_10ul;
        /// <summary>
        ///10ultip-8数量
        /// </summary>
        public int Tip8Count_10ul;
    }
    /// <summary>
    /// Map孔位信息
    /// 继承自WorkOrder类，包含了工单信息和Map孔位自定义需要的数据中转，
    /// </summary>
    public class MapHoleInfo: WorkOrder
    {
        /// <summary>
        /// 状态
        /// </summary>
        public MapHoleInfoState State { get; set; } = MapHoleInfoState.Pending;
        public string Progress = "";

        public MapHoleInfo Clone()
        {
            return new MapHoleInfo()
            {
                ID = this.ID,
                RackID = this.RackID,
                TubeID = this.TubeID,
                Location = this.Location,
                OrderID = this.OrderID,
                OrderRepeatID = this.OrderRepeatID,
                BatchID = this.BatchID,
                ExpressionScale = this.ExpressionScale,
                Cal_M_W = this.Cal_M_W,
                CedexYield = this.CedexYield,
                ExtinctionCoefficient = this.ExtinctionCoefficient,
                ReagentTypes = this.ReagentTypes,
                ReagentName = this.ReagentName,
                ReagentBatchNumber = this.ReagentBatchNumber,
                ReagentVolume = this.ReagentVolume,
                TubeLoadingOperator = this.TubeLoadingOperator,
                TubeLoadingTime = this.TubeLoadingTime,
                MicroplateID = this.MicroplateID,
                LoadTime = this.LoadTime,
                OD_Value = this.OD_Value,
                Test_Operator = this.Test_Operator,
                Concentration_Test_Time = this.Concentration_Test_Time,
                PoolingLogic = this.PoolingLogic,
                Target = this.Target,
                Final_Protein_Mass = this.Final_Protein_Mass,
                Final_Volume = this.Final_Volume,
                Pooling_Operator = this.Pooling_Operator,
                PoolingTime = this.PoolingTime,
                NeutralizationBuffer = this.NeutralizationBuffer,
                NeutralizationBufferBatchNumber = this.NeutralizationBufferBatchNumber,
                NeutralizationBufferVolume = this.NeutralizationBufferVolume,
                Neutralization_Operator = this.Neutralization_Operator,
                Neutralization_Time = this.Neutralization_Time,
                DesaltingColumn = this.DesaltingColumn,
                DesaltingColumnBatchNumber = this.DesaltingColumnBatchNumber,
                Desalting_Operator = this.Desalting_Operator,
                Desalting_Time = this.Desalting_Time,
                CreateTime = this.CreateTime,
                State = this.State,
                Progress = this.Progress
            };
        }

    }
    public enum MapHoleInfoState
    {
        /// <summary>
        /// 待处理
        /// </summary>
        Pending = 0,
        /// <summary>
        /// 进行中
        /// </summary>
        Running = 1,
        /// <summary>
        /// 已完成
        /// </summary>
        Completed = 2,
        /// <summary>
        /// 异常
        /// </summary>
        Abnormal = 3,
    }

}
