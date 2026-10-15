using ConcentrationDetectionSys.Bll;
using ConcentrationDetectionSys.Common;
using ConcentrationDetectionSys.DAL;
using ConcentrationDetectionSys.Hardware;
using ConcentrationDetectionSys.StepParamFrm;
using ConcentrationDetectionSys.Tools;
using Sunny.UI;
using Sunny.UI.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace ConcentrationDetectionSys
{
    public partial class MainForm : Form
    {
        private bool _isConnectedPLC
        {
            get
            {
                if (CommonParam.s_PLC == null)
                {
                    return false;
                }
                else
                {
                    return CommonParam.s_PLC.connected;
                }

            }
        }
        private static ProcessScheme _runningScheme = new ProcessScheme();
        private ExecuteBll _executeBll = new ExecuteBll();
        public MainForm()
        {
            InitializeComponent();
            Console.WriteLine(BitConverter.IsLittleEndian); // Windows PC输出 true（小端）
            dgv_ActiveAlarm.AutoGenerateColumns = false;
            //订阅日志事件
            LogEventManager.LogTriggered += AddLog;
            _scanPLCAlarmTimer = new System.Windows.Forms.Timer();
            _scanPLCAlarmTimer.Interval = 1000;
            _scanPLCAlarmTimer.Tick += ScanPLCAlarmTimer_Tick;
            _mapHoleInfosTimer = new System.Windows.Forms.Timer();
            _mapHoleInfosTimer.Interval = 1000;
            _mapHoleInfosTimer.Tick += MapHoleInfosTimer_Tick;
            ConcentrationMeasurement.OnHumanConfirm += ShowHumanConfirmDialog;
            //订阅报警变化事件
            AlarmManager.Instance.OnAlarmChanged += RefreshLiveAlarmDgvAndTb;
           // CommonParam.OnMapHoleInfosChanged += ProductionRecordsLoadDataToDgv;
            ConcentrationMeasurement.OnDataRestChanged += (dataA260Str, dataA280Str, dataA340Str) => { UpDataResultToDGv(dataA260Str, dataA280Str, dataA340Str); };
        }
        #region 窗体最小化最大化关闭移动事件
        private void btn_minimizeWindows_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }
        private void btn_maximizeWindow_Click(object sender, EventArgs e)
        {
            WindowState = WindowState == FormWindowState.Maximized
               ? FormWindowState.Normal
               : FormWindowState.Maximized;
        }
        private void btn_closeWindow_Click(object sender, EventArgs e)
        {
            Close();
        }
        private const int WM_NCLBUTTONDOWN = 0x00A1;
        private const int HTCAPTION = 0x2;

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
        private void tLP_topLocation_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;
            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
        }
        #endregion
        private async void MainForm_Load(object sender, EventArgs e)
        {
            MainTabControl.TabVisible = false;
            lb_CurrentUserName.Text = CommonParam.s_CurrentUser.UserName + "(" + CommonParam.s_CurrentUser.Class + ")";
            //加载方案list+初始化UI控件
            RefreshSchemeList();
            Updatas_ConsumableitemsUIInfoFromSql();
            DateTime today = DateTime.Now.Date;
            dtp_AlarmRecordStartingTimeScreen.MaxDate = today;
            dtp_AlarmRecordStartingTimeScreen.MinDate = today.AddMonths(-3);
            dtp_AlarmRecordStartingTimeScreen.Value = today.AddMonths(-3);
            dtp_AlarmRecordEndTimeScreen.MaxDate = today;
            dtp_AlarmRecordEndTimeScreen.MinDate = today.AddMonths(-3);
            dtp_AlarmRecordEndTimeScreen.Value = today;
            //初始化UV灯使用时间计时器
            //_UVLightStartTime_Left = DateTime.Now;
            //_UVLightUsageTimer_Left.Interval = 1000;
            //_UVLightUsageTimer_Left.Tick += _UVLightUsageTimer_Left_Tick;
            //_UVLightStartTime_Right = DateTime.Now;
            //_UVLightUsageTimer_Right.Interval = 1000;
            //_UVLightUsageTimer_Right.Tick += _UVLightUsageTimer_Right_Tick;
            //string input = SqlHelper.GetString("UVLightUsageTime_Left");
            //if (input != "")
            //{
            //    string[] parts = input.Split(':');
            //    int hours = int.Parse(parts[0]);   // 28
            //    int minutes = int.Parse(parts[1]);   // 30
            //    int seconds = int.Parse(parts[2]);   // 0

            //    // 用 FromHours 等组合，可以支持超过 23 的小时
            //    TimeSpan ts = TimeSpan.FromHours(hours)
            //                + TimeSpan.FromMinutes(minutes)
            //                + TimeSpan.FromSeconds(seconds);
            //}
            //uiLedStopwatch1.Text = SqlHelper.GetString("UVLightUsageTime_Left");
            //SqlHelper.GetString("UVLightUsageTime_Right");
            //显示运行方案
            cb_SelectScheme.SelectedItem = SqlHelper.GetString("RunningSchemeName");
            UpDataRunningTime();
            //删除3月前的报警记录
            SqlHelper.DeleteBeforeSQLData("AlarmRecordT", "[Triggering Time]");
            //删除3月前的日志记录
            LogHelper.CleanOldLogs();
            LoadAlarmInfo_PLC();
            //连接PLC
            string ip = SqlHelper.GetString("PLCIP");
            int port = SqlHelper.GetInt("PLCPort");
            ConnectPLC(ip, port);
            //连接Magellan
            //CommonParam.s_Magellan.Disconnect();
            string MagellanDeviceName = SqlHelper.GetString("MagellanDeviceName");
            MagellanConnect(MagellanDeviceName);
            MagellanArrivalRunStatus();
            //初始化状态
            CommonParam.s_UpDataWorkOrderTableFinish = false;
        }
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 取消订阅全局日志事件
            LogEventManager.LogTriggered -= AddLog;
            ConcentrationMeasurement.OnDataRestChanged -= (dataA260Str, dataA280Str, dataA340Str) => { UpDataResultToDGv(dataA260Str, dataA280Str, dataA340Str); };
            AlarmManager.Instance.OnAlarmChanged -= RefreshLiveAlarmDgvAndTb;
            ConcentrationMeasurement.OnHumanConfirm -= ShowHumanConfirmDialog;
            // CommonParam.OnMapHoleInfosChanged -= ProductionRecordsLoadDataToDgv;
            //释放AlarmManager资源
            AlarmManager.Instance.Stop();
            //释放定时器
            _scanPLCAlarmTimer?.Stop();
            _scanPLCAlarmTimer?.Dispose();
            _mapHoleInfosTimer?.Stop();
            _mapHoleInfosTimer?.Dispose();
            //断开酶标仪
            //CommonParam.s_Magellan.Disconnect();
            //耗材更新数据库
            Updatas_ConsumableitemsInfoToSql();
            //更新UV灯使用时间
            SqlHelper.SetStringValue("UVLightUsageTime_Left", uiLedStopwatch1.Text);
            SqlHelper.SetStringValue("UVLightUsageTime_Right", uiLedStopwatch2.Text);
        }
        #region UI事件
        private void uiNavMenu1_MenuItemClick(TreeNode node, Sunny.UI.NavMenuItem item, int pageIndex)
        {
            switch (node.Name)
            {
                case "TN_PlanExecution":
                    MainTabControl.SelectTab(tP_PlanExecution);
                    break;
                case "TN_PlanRun":
                    if (CommonParam.s_CurrentUser.Class == "操作员")
                    {
                        MessageBox.Show("权限不足，无法访问【方案编辑】");
                        return;
                    }
                    MainTabControl.SelectTab(tP_PlanRedact);
                    break;
                case "TN_Log":
                    MainTabControl.SelectTab(tP_Log);
                    break;
                case "TN_ProductionRecords":
                    MainTabControl.SelectTab(tP_ProductionRecords);
                    break;
                case "TN_AlarmRecords":
                    MainTabControl.SelectTab(tP_AlarmRecords);
                    break;
                case "TN_DeviceParameter":
                    if (CommonParam.s_CurrentUser.Class == "操作员")
                    {
                        MessageBox.Show("权限不足，无法访问【设备参数】");
                        return;
                    }
                    MainTabControl.SelectTab(tP_DeviceParameter);
                    break;
                case "TN_AlarmConfiguration":
                    if (CommonParam.s_CurrentUser.Class == "操作员")
                    {
                        MessageBox.Show("权限不足，无法访问【报警配置】");
                        return;
                    }
                    MainTabControl.SelectTab(tP_AlarmConfiguration);
                    break;
                case "TN_MaintainConfiguration":
                    // MainTabControl.SelectTab(tP_MaintainConfiguration);
                    break;
                case "TN_UserManagement":
                    if (CommonParam.s_CurrentUser.Class == "操作员" || CommonParam.s_CurrentUser.Class == "管理员")
                    {
                        MessageBox.Show("权限不足，无法访问【用户管理】");
                        return;
                    }
                    MainTabControl.SelectTab(tP_UserManagement);
                    break;
                case "TN_AuthorityManagement":
                    //MainTabControl.SelectTab(tP_AuthorityManagement);
                    break;
                default:
                    break;
            }

        }
        private void cb_MES_Mode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cb_MES_Mode.SelectedIndex == 0)
            {
                CommonParam.s_MES_Mode = MES_Mode.On_Line;
            }
            else
            {
                CommonParam.s_MES_Mode = MES_Mode.Off_Line;
            }
        }
        private void cb_DeviceRunMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cb_DeviceRunMode.SelectedIndex == 0)
            {
                CommonParam.s_RunMode = RunMode.Auto;
            }
            else if (cb_DeviceRunMode.SelectedIndex == 1)
            {
                CommonParam.s_RunMode = RunMode.Manual;
            }
            else
            {
                CommonParam.s_RunMode = RunMode.Maintain;
            }
            ModeSwitchUIEnable(); 
        }
        /// <summary>
        /// 模式切换UI使能变化
        /// </summary>
        private void ModeSwitchUIEnable()
        {
            //酶标仪
            if (CommonParam.s_RunMode == RunMode.Auto)
            {
                btn_MagellanPlateOut.Enabled = false;
                btn_MagellanPlateIn.Enabled = false;
                btn_MagellanWorkspaceSetEmpty.Enabled = false;
                btn_MagellanStartRun.Enabled = false;
                btn_MagellanStopRun.Enabled = false;
                cb_MagellanDeviceName.Enabled = false;
                cb_MagellanMethods.Enabled = false;
                btn_MagellanConnect.Enabled = false;
            }
            else
            {
                if (CommonParam.s_CurrentUser.Class == "工程师")
                {
                    btn_MagellanPlateOut.Enabled = true;
                    btn_MagellanPlateIn.Enabled = true;
                    btn_MagellanWorkspaceSetEmpty.Enabled = true;
                    btn_MagellanStartRun.Enabled = true;
                    btn_MagellanStopRun.Enabled = true;
                    cb_MagellanDeviceName.Enabled = true;
                    cb_MagellanMethods.Enabled = true;
                    btn_MagellanConnect.Enabled = true;
                }
            }
            //PLC
            if (CommonParam.s_RunMode == RunMode.Auto)
            {
                tb_PLC_IP.Enabled = false;
                intUD_PLC_Port.Enabled = false;
                btn_ConnectPLC.Enabled = false;
            }
            else
            {
                if (CommonParam.s_CurrentUser.Class == "工程师")
                {
                    tb_PLC_IP.Enabled = true;
                    intUD_PLC_Port.Enabled = true;
                    btn_ConnectPLC.Enabled = true;
                }
            }
        }
        private void btn_Run_Click(object sender, EventArgs e)
        {
            if (CommonParam.s_DeviceState == DeviceState.Alarm)
            {
                MessageBox.Show("报警状态，请先处理报警再启动");
                return;
            }
            if (CommonParam.s_RunMode != RunMode.Auto)
            {
                MessageBox.Show("请启用自动模式再启动");
                return;
            }
            
            if (CommonParam.s_DeviceState == DeviceState.Stop && CommonParam.s_RunMode == RunMode.Auto)
            {
                #region 防呆
                if (CommonParam.s_MES_Mode == MES_Mode.Off_Line)
                {
                    if(!CommonParam.s_UpDataWorkOrderTableFinish)
                    {
                        MessageBox.Show("请人工上传工单");
                        return;
                    }
                }
                if (_runningScheme == null)
                {
                    MessageBox.Show("请选择方案进行运行");
                    return;
                }
                else
                {
                    _runningScheme = ProjectManagement.GetSingleScheme(cb_SelectScheme.SelectedItem.ToString());
                    SqlHelper.SetStringValue("RunningSchemeName", _runningScheme.SchemeName);
                    LogEventManager.LogEvent(31, $"开始自动运行，方案：{_runningScheme.SchemeName}");
                }
                #endregion
                if (!_executeBll.isRunning)
                {
                    _executeBll.Execute(_runningScheme);
                }
                btn_Run.Text = "运行中";
                CommonParam.s_DeviceState = DeviceState.Running;
                lb_RunTime.Text = DateTime.Now.ToString("HH:mm:ss");
                lb_FinishTime.Text = "--";
                btn_Stop.Text = "暂停";
            }

        }

        private void btn_Stop_Click(object sender, EventArgs e)
        {
            if (CommonParam.s_DeviceState == DeviceState.Running)
            {
                CommonParam.s_DeviceState = DeviceState.Stop;
                LogEventManager.LogEvent(31, $"暂停自动运行，方案：{_runningScheme.SchemeName}");
                btn_Stop.Text = "暂停中";
            }
        }
        private void btn_Suspend_Click(object sender, EventArgs e)
        {
            _executeBll.cts?.Cancel();
            _executeBll.cts?.Dispose();
            _executeBll.cts = null;
            _executeBll.isRunning = false;
            CommonParam.s_DeviceState = DeviceState.Stop;
            lb_FinishTime.Text = DateTime.Now.ToString("HH:mm:ss");
            LogEventManager.LogEvent(31, $"结束自动运行，方案：{_runningScheme.SchemeName}");
            btn_Run.Text = "启动";
        }
        private void btn_ManualScanningSignal_Click(object sender, EventArgs e)
        {
            bool[] scancCodesFinish = new bool[8];
            bool askScaning = false;
            if (CommonParam.s_DeviceState != DeviceState.Running && btn_ManualScanningSignal.Text == "请求上传人工扫码")
            {
                btn_ManualScanningSignal.Text = "请求中...";
                askScaning = true;
                Stopwatch stopwatch = Stopwatch.StartNew();
                Task.Run(() =>
                {
                    //请求buffer试剂1扫批次码信号
                    CommonParam.s_PLC.Write_Register_Value(1, 2100, "1", "short");
                    //请求buffer试剂1扫位置码
                    CommonParam.s_PLC.Write_Register_Value(1, 2101, "1", "short");
                    //请求buffer试剂2扫批次码信号
                    CommonParam.s_PLC.Write_Register_Value(1, 2102, "1", "short");
                    //请求buffer试剂2扫位置码
                    CommonParam.s_PLC.Write_Register_Value(1, 2103, "1", "short");
                    //请求中和试剂1扫批次码信号
                    CommonParam.s_PLC.Write_Register_Value(1, 3000, "1", "short");
                    //请求中和试剂1扫位置码
                    CommonParam.s_PLC.Write_Register_Value(1, 3001, "1", "short");
                    //请求中和试剂2扫批次码信号
                    CommonParam.s_PLC.Write_Register_Value(1, 3002, "1", "short");
                    //请求中和试剂2扫位置码
                    CommonParam.s_PLC.Write_Register_Value(1, 3003, "1", "short");
                    stopwatch.Restart();
                    while (askScaning)
                    {
                        if (!scancCodesFinish[0])
                        {
                            //buffer试剂1扫批次码
                            string code1 = CommonParam.s_PLC.Read_Register_State_ACSII(1, 1000, 20);
                            if (code1 != "")
                            {
                                scancCodesFinish[0] = true;
                                Invoke(new Action(() =>
                                {
                                    tb_Buffer1_BatchNumber.Text = code1;
                                }));
                                CommonParam.s_Consumableitems.Buffer1_BatchNumber = code1;
                                LogEventManager.LogEvent(31, $"buffer试剂1扫批次码请求完成,code:{code1}");
                            }
                        }
                        if (!scancCodesFinish[1])
                        {
                            //buffer试剂1扫位置码
                            string code2 = CommonParam.s_PLC.Read_Register_State_ACSII(1, 1020, 20);
                            if (code2 != "")
                            {
                                scancCodesFinish[1] = true;
                                Invoke(new Action(() =>
                                {
                                    tb_Buffer1_PositionalCode.Text = code2;
                                }));
                                CommonParam.s_Consumableitems.Buffer1_PositionalCode = code2;
                                LogEventManager.LogEvent(31, $"buffer试剂1扫位置码请求完成,code:{code2}");
                            }
                        }
                        if (!scancCodesFinish[2])
                        {
                            //buffer试剂2扫批次码
                            string code3 = CommonParam.s_PLC.Read_Register_State_ACSII(1, 1040, 20);
                            if (code3 != "")
                            {
                                scancCodesFinish[2] = true;
                                Invoke(new Action(() =>
                                {
                                    tb_Buffer2_BatchNumber.Text = code3;
                                }));
                                CommonParam.s_Consumableitems.Buffer2_BatchNumber = code3;
                                LogEventManager.LogEvent(31, $"buffer试剂2扫批次码请求完成,code:{code3}");
                            }
                        }
                        if (!scancCodesFinish[3])
                        {
                            //buffer试剂2扫位置码
                            string code4 = CommonParam.s_PLC.Read_Register_State_ACSII(1, 1060, 20);
                            if (code4 != "")
                            {
                                scancCodesFinish[3] = true;
                                Invoke(new Action(() =>
                                {
                                    tb_Buffer2_PositionalCode.Text = code4;
                                }));
                                CommonParam.s_Consumableitems.Buffer2_PositionalCode = code4;
                                LogEventManager.LogEvent(31, $"buffer试剂2扫位置码请求完成,code:{code4}");
                            }
                        }
                        if (!scancCodesFinish[4])
                        {
                            //中和试剂1扫批次码
                            string code5 = CommonParam.s_PLC.Read_Register_State_ACSII(1, 2000, 20);
                            if (code5 != "")
                            {
                                scancCodesFinish[4] = true;
                                Invoke(new Action(() =>
                                {
                                    tb_Neutralize1_BatchNumber.Text = code5;
                                }));
                                CommonParam.s_Consumableitems.Neutralize1_BatchNumber = code5;
                                LogEventManager.LogEvent(31, $"中和试剂1扫批次码请求完成,code:{code5}");
                            }
                        }
                        if (!scancCodesFinish[5])
                        {
                            //中和试剂1扫位置码
                            string code6 = CommonParam.s_PLC.Read_Register_State_ACSII(1, 2020, 20);
                            if (code6 != "")
                            {
                                scancCodesFinish[5] = true;
                                Invoke(new Action(() =>
                                {
                                    tb_Neutralize1_PositionalCode.Text = code6;
                                }));
                                CommonParam.s_Consumableitems.Neutralize1_PositionalCode = code6;
                                LogEventManager.LogEvent(31, $"中和试剂1扫位置码请求完成,code:{code6}");
                            }
                        }
                        if (!scancCodesFinish[6])
                        {
                            //中和试剂2扫批次码
                            string code7 = CommonParam.s_PLC.Read_Register_State_ACSII(1, 2040, 20);
                            if (code7 != "")
                            {
                                scancCodesFinish[6] = true;
                                Invoke(new Action(() =>
                                {
                                    tb_Neutralize2_BatchNumber.Text = code7;
                                }));
                                CommonParam.s_Consumableitems.Neutralize1_BatchNumber = code7;
                                LogEventManager.LogEvent(31, $"中和试剂2扫批次码请求完成,code:{code7}");
                            }
                        }
                        if (!scancCodesFinish[7])
                        {
                            //中和试剂2扫位置码
                            string code8 = CommonParam.s_PLC.Read_Register_State_ACSII(1, 2060, 20);
                            if (code8 != "")
                            {
                                scancCodesFinish[7] = true;
                                Invoke(new Action(() =>
                                {
                                    tb_Neutralize2_PositionalCode.Text = code8;
                                }));
                                CommonParam.s_Consumableitems.Neutralize1_PositionalCode = code8;
                                LogEventManager.LogEvent(31, $"中和试剂2扫位置码请求完成,code:{code8}");
                            }
                        }
                        if (scancCodesFinish[0] && scancCodesFinish[1] && scancCodesFinish[2] && scancCodesFinish[3]
                            && scancCodesFinish[4] && scancCodesFinish[5] && scancCodesFinish[6] && scancCodesFinish[7])
                        {
                            askScaning = false;
                            Invoke(new Action(() =>
                            {
                                btn_ManualScanningSignal.Text = "请求上传人工扫码";
                            }));
                            break;
                        }
                        if (stopwatch.ElapsedMilliseconds > 5000)
                        {
                            askScaning = false;
                            Invoke(new Action(() =>
                            {
                                btn_ManualScanningSignal.Text = "请求上传人工扫码";
                            }));
                            LogEventManager.LogEvent(32, $"请求人工扫码超时");
                            break;
                        }
                        Thread.Sleep(100);
                    }
                });

            }

        }
        private void btn_UpTableData_Click(object sender, EventArgs e)
        {
            try
            {
                if (CommonParam.s_MES_Mode == MES_Mode.Off_Line)
                {
                    // 1. 创建 OpenFileDialog 实例
                    using (OpenFileDialog openFileDialog = new OpenFileDialog())
                    {
                        // 2. 配置对话框属性
                        openFileDialog.InitialDirectory = "c:\\"; // 设置初始目录
                        openFileDialog.Filter = "Excel 文件|*.xls;*.xlsx|所有文件|*.*";
                        openFileDialog.FilterIndex = 1; // 默认选中第一个筛选器
                        openFileDialog.RestoreDirectory = false; // 关闭对话框后恢复初始目录

                        // 3. 显示对话框并判断结果
                        if (openFileDialog.ShowDialog() == DialogResult.OK)
                        {
                            // 4. 获取文件路径并处理
                            string filePath = openFileDialog.FileName;
                            tb_TableDataFileName.Text = filePath;
                            // 读取指定Sheet
                            CommonParam.s_WorkOrderTable = ExcelHelper.ReadExcelToDataTable(filePath, "Sheet1");
                            CommonParam.s_WorkOrderMapHoleInfoList.Clear();
                            //解析工单
                            //遍历每一行工单
                            foreach (DataRow row in CommonParam.s_WorkOrderTable.Rows)
                            {
                                //映射一行数据到MapHoleInfo实体
                                MapHoleInfo order = new MapHoleInfo()
                                {
                                    ID = row["ID"]?.ToString(),
                                    RackID = row["Rack_ID"]?.ToString(),
                                    TubeID = row["Tube_ID"]?.ToString(),
                                    Location = row["Location"]?.ToString(),
                                    OrderID = row["Order ID"]?.ToString(),
                                    OrderRepeatID = row["Order Repeat ID"]?.ToString(),
                                    BatchID = row["Batch ID"]?.ToString(),
                                    ExpressionScale = row["Expression Scale(ml)"]?.ToString(),
                                    Cal_M_W = row["Cal.M.W.(KDa)"]?.ToString(),
                                    CedexYield = row["Cedex yield"]?.ToString(),
                                    ExtinctionCoefficient = row["Extinction Coefficient"]?.ToString(),
                                    ReagentTypes = row["Reagent types"]?.ToString(),
                                    ReagentName = row["Reagent Name"]?.ToString(),
                                    ReagentBatchNumber = row["Reagent Batch Number"]?.ToString(),
                                    ReagentVolume = row["Reagent Volume(ml)"]?.ToString(),
                                    TubeLoadingOperator = row["Tube_Loading_Operator"]?.ToString(),
                                    TubeLoadingTime = row["Tube_Loading_Time"]?.ToString(),
                                    MicroplateID = row["Microplate ID"]?.ToString(),
                                    LoadTime = row["Load Time"]?.ToString(),
                                    OD_Value = row["OD_Value"]?.ToString(),
                                    Test_Operator = row["Test_Operator"]?.ToString(),
                                    Concentration_Test_Time = row["Concentration_Test_Time"]?.ToString(),
                                    PoolingLogic = row["Pooling Logic"]?.ToString(),
                                    Target = row["Target"]?.ToString(),
                                    Final_Protein_Mass = row["Final_Protein_Mass"]?.ToString(),
                                    Final_Volume = row["Final_Volume(ml)"]?.ToString(),
                                    Pooling_Operator = row["Pooling_Operator"]?.ToString(),
                                    PoolingTime = row["pooling time"]?.ToString(),
                                    NeutralizationBuffer = row["Neutralization Buffer"]?.ToString(),
                                    NeutralizationBufferBatchNumber = row["Neutralization Buffer batch number"]?.ToString(),
                                    NeutralizationBufferVolume = row["Neutralization Buffer Volume(ml)"]?.ToString(),
                                    Neutralization_Operator = row["Neutralization_Operator"]?.ToString(),
                                    Neutralization_Time = row["Neutralization_Time"]?.ToString(),
                                    DesaltingColumn = row["Desalting Column"]?.ToString(),
                                    DesaltingColumnBatchNumber = row["Desalting Column batch number"]?.ToString(),
                                    Desalting_Operator = row["Desalting_Operator"]?.ToString(),
                                    Desalting_Time = row["Desalting_Time"]?.ToString(),
                                    CreateTime = row["Create Time"]?.ToString(),
                                };
                                ////工单添加到对应rackId的列表
                                CommonParam.s_WorkOrderMapHoleInfoList.Add(order);
                            }
                            LogEventManager.LogEvent(31, "读取Excel文件成功");
                            CommonParam.s_UpDataWorkOrderTableFinish = true;
                            WorkOrdersLoadDataToDgv();
                        }
                    }
                }
                else
                {
                    MessageBox.Show("请切换到离线模式后再上传表格数据");
                }
            }
            catch (Exception ex)
            {
                tb_TableDataFileName.Text = "";
                LogEventManager.LogEvent(32, "读取Excel文件失败");
                MessageBox.Show(ex.Message);
            }
        }
        /// <summary>
        /// 将DataTable整张插入数据库表
        /// </summary>
        /// <param name="dt">内存数据表</param>
        public void BulkInsertDataTable(DataTable dt)
        {
            using (SqlConnection conn = new SqlConnection(SqlHelper.connStr))
            {
                conn.Open();
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn))
                {
                    //目标数据库表名
                    bulkCopy.DestinationTableName = "WorkOrderT";
                    bulkCopy.WriteToServer(dt);
                }
            }
        }

        private void UpDataRunningTime()
        {
            string LastRunningTime = SqlHelper.GetString("LastRunningTime");
            string CurrentRunningTime = DateTime.Today.ToString("yyyy/MM/dd");
            if (CurrentRunningTime != LastRunningTime)
            {
                //更新数据库中的数据
                SqlHelper.SetStringValue("LastRunningTime", CurrentRunningTime);
                SqlHelper.SetStringValue("DoneNum_CN", "0");
                SqlHelper.SetStringValue("DoneOKNum_CN", "0");
                SqlHelper.SetStringValue("AbnormalNum_CN", "0");
                SqlHelper.SetStringValue("DoneNum_BY", "0");
                SqlHelper.SetStringValue("DoneNum_ZH", "0");
                CommonParam.s_Capacity.DoneOKNum_CN = 0;
                CommonParam.s_Capacity.DoneNGNum_CN = 0;
                CommonParam.s_Capacity.DoneNum_BY = 0;
                CommonParam.s_Capacity.DoneNum_ZH = 0;
            }
            else
            {
                CommonParam.s_Capacity.DoneOKNum_CN = SqlHelper.GetInt("DoneOKNum_CN");
                CommonParam.s_Capacity.DoneNGNum_CN = SqlHelper.GetInt("AbnormalNum_CN");
                CommonParam.s_Capacity.DoneNum_BY = SqlHelper.GetInt("DoneNum_BY");
                CommonParam.s_Capacity.DoneNum_ZH = SqlHelper.GetInt("DoneNum_ZH");
            }
            UpDataCapacityToUI();
        }
        private void UpDataCapacityToUI()
        {
            lb_DoneNum_CN.Text = CommonParam.s_Capacity.DoneNum_CN + "";
            lb_DoneOKNum_CN.Text = CommonParam.s_Capacity.DoneOKNum_CN + "";
            lb_DoneNGNum_CN.Text = CommonParam.s_Capacity.DoneNGNum_CN + "";
            lb_DoneNum_BY.Text = CommonParam.s_Capacity.DoneNum_BY + "";
            lb_DoneNum_ZH.Text = CommonParam.s_Capacity.DoneNum_ZH + "";
            lb_Yield.Text = CommonParam.s_Capacity.Yield.ToString("F2") + "%";
            lb_AbnormalityRate.Text = CommonParam.s_Capacity.AbnormalityRate.ToString("F2") + "%";
        }
        private void UI_Timer_Tick(object sender, EventArgs e)
        {
            UpDataCapacityToUI();
        }
        #endregion
        #region 用户管理
        private bool _isLoginFormShowing = false;
        private void uiAvatar1_Click(object sender, EventArgs e)
        {
            if (_isLoginFormShowing)
                return;
            _isLoginFormShowing = true;
            LoginFrm loginForm = new LoginFrm();
            if (loginForm.ShowDialog() == DialogResult.OK)
            {
                loginForm.Close();
                MessageBox.Show($"切换账号成功，当前用户：{CommonParam.s_CurrentUser.UserName}，权限：{CommonParam.s_CurrentUser.Class}");
                lb_CurrentUserName.Text = CommonParam.s_CurrentUser.UserName + "(" + CommonParam.s_CurrentUser.Class + ")";
            }
            _isLoginFormShowing = false;
        }
        private void CurrentUserJurisdiction()
        {
            int CurrentUserClass = 0;
            if (CommonParam.s_CurrentUser.Class == "操作员")
                CurrentUserClass = 0;
            if (CommonParam.s_CurrentUser.Class == "管理员")
                CurrentUserClass = 1;
            if (CommonParam.s_CurrentUser.Class == "工程师")
                CurrentUserClass = 2;
            tb_PLC_IP.Enabled = true;
            intUD_PLC_Port.Enabled = true;
            cb_MagellanDeviceName.Enabled = true;
            btn_MagellanWorkspaceSetEmpty.Enabled = true;
            cb_MagellanMethods.Enabled = true;
            if (CurrentUserClass < 2)
            {
                tb_PLC_IP.Enabled = false;
                intUD_PLC_Port.Enabled = false;
                cb_MagellanDeviceName.Enabled = false;
                btn_MagellanWorkspaceSetEmpty.Enabled = false;
                cb_MagellanMethods.Enabled = false;
            }
            if (CurrentUserClass < 1)
            {

            }
        }
        #endregion

        /// <summary>
        /// 日志 30-报警 31-普通 32-警告
        /// </summary>
        /// <param name="index"></param>
        /// <param name="log"></param>
        public void AddLog(int index, string log)
        {
            if (lv_Log.InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    ListViewItem lv = new ListViewItem(DateTime.Now.ToString("HH:mm:ss"), index);
                    lv.SubItems.Add(log);
                    lv_Log.Items.Insert(0, lv);
                }));
            }
            else
            {
                ListViewItem lv = new ListViewItem(DateTime.Now.ToString("HH:mm:ss"), index);
                lv.SubItems.Add(log);
                lv_Log.Items.Insert(0, lv);
            }
            if (index == 30)
            {
                LogHelper.Error(log);
            }
            else if (index == 31)
            {
                LogHelper.Info(log);
            }
            else
            {
                LogHelper.Warn(log);
            }
        }
        #region 方案编辑

        /// <summary>
        /// 方案列表
        /// </summary>
        private List<ProcessScheme> _schemeList;
        /// <summary>当前编辑的方案</summary>
        private ProcessScheme _currentScheme;
        /// <summary>
        /// 正在添加新方案标志位
        /// </summary>
        private bool _isAddNewScheming = false;
        private List<string> _scheming_StrList = new List<string>();
        /// <summary>刷新左侧所有配方列表</summary>
        private void RefreshSchemeList()
        {
            _scheming_StrList.Clear();
            FLP_ProjectList.Clear();
            _schemeList = ProjectManagement.LoadAllSchemes();
            foreach (var scheme in _schemeList)
            {
                AddProjectCard(scheme.SchemeName);
                _scheming_StrList.Add(scheme.SchemeName);
            }
            UpdateSelectSchemeComboBox();
        }
        /// <summary>
        /// 更新方案选择下拉框
        /// </summary>
        private void UpdateSelectSchemeComboBox()
        {
            cb_SelectScheme.Items.Clear();
            cb_SelectScheme.Items.AddRange(_scheming_StrList.ToArray());
        }
        private void cb_SelectScheme_SelectedIndexChanged(object sender, EventArgs e)
        {
            _runningScheme = ProjectManagement.GetSingleScheme(cb_SelectScheme.SelectedItem.ToString());
        }
        /// <summary>
        /// 新建方案按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_NewProject_Click(object sender, EventArgs e)
        {
            if (_isAddNewScheming)
            {
                MessageBox.Show($"方案名称{_currentScheme.SchemeName}未保存");
                return;
            }
            //弹窗输入配方名称
            string name = "";
            UIInputDialog.ShowInputStringDialog(ref name, UIStyle.Custom, true, "请输入方案名称：");
            if (string.IsNullOrWhiteSpace(name)) return;
            bool exists = _schemeList.Any(s => string.Equals(s.SchemeName, name, StringComparison.OrdinalIgnoreCase));
            if (exists)
            {
                MessageBox.Show("方案名称已存在！");
                return;
            }
            _isAddNewScheming = true;
            _currentScheme = new ProcessScheme();
            _currentScheme.SchemeName = name;
            _currentScheme.CreateTime = DateTime.Now;
            AddProjectCard(_currentScheme.SchemeName);
        }
        /// <summary>
        /// 保存方案
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_SaveProject_Click(object sender, EventArgs e)
        {
            if (_currentScheme == null)
                return;
            bool exists = _schemeList.Any(s => string.Equals(s.SchemeName, _currentScheme.SchemeName, StringComparison.OrdinalIgnoreCase));
            if (!exists)
            {
                if (ProjectManagement.AddNewScheme(_currentScheme))
                {
                    _isAddNewScheming = false;
                    _scheming_StrList.Add(_currentScheme.SchemeName);
                    UpdateSelectSchemeComboBox();
                    MessageBox.Show($"保存方案{_currentScheme.SchemeName}成功");
                    LogEventManager.LogEvent(31, $"保存方案{_currentScheme.SchemeName}成功");
                }
                else
                {
                    MessageBox.Show($"保存方案{_currentScheme.SchemeName}失败");
                    LogEventManager.LogEvent(30, $"保存方案{_currentScheme.SchemeName}失败");
                }
            }
            else
            {
                if (ProjectManagement.UpdateScheme(_currentScheme))
                {
                    MessageBox.Show($"保存方案{_currentScheme.SchemeName}成功");
                    LogEventManager.LogEvent(31, $"保存方案{_currentScheme.SchemeName}成功");
                }
                else
                {
                    MessageBox.Show($"保存方案{_currentScheme.SchemeName}失败");
                    LogEventManager.LogEvent(30, $"保存方案{_currentScheme.SchemeName}失败");
                }
            }

        }
        /// <summary>添加一个方案卡片到方案列表</summary>
        private void AddProjectCard(string schemeName)
        {
            UIButton btnProject = new UIButton();
            btnProject.Text = schemeName;
            btnProject.Height = 50;
            btnProject.Width = 180;
            btnProject.Location = new Point(0, 50 * (FLP_ProjectList.ControlCount + 1));
            btnProject.Margin = new Padding(2, 2, 2, 2); //卡片之间间距
            btnProject.ForeColor = Color.White;
            btnProject.FillHoverColor = System.Drawing.Color.DarkCyan;
            btnProject.FillColor = System.Drawing.Color.LightSeaGreen;
            btnProject.RectColor = System.Drawing.Color.LightSeaGreen;
            //当前方案卡片高亮
            ChangeCardColor(FLP_ProjectList, btnProject);
            //添加点击事件，点击卡片，选中这个流程步骤
            btnProject.Click += ProjectButton_Click;
            //添加删除事件
            btnProject.ContextMenuStrip = ContextMenuStrip_Scheme;
            //清空步骤列表中的button按钮
            ClearFLP_StepList();
            //清空参数界面
            ClearPanel_Param();
            FLP_ProjectList.Controls.Add(btnProject);
        }
        /// <summary>
        /// 删除方案卡片
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tsmiDelete_Click(object sender, EventArgs e)
        {
            UIButton clickBtn = ContextMenuStrip_Scheme.SourceControl as UIButton;
            if (clickBtn == null)
                return;
            //删除方案list
            //_schemeList.RemoveAll(item => item.SchemeName == clickBtn.Text);
            ProjectManagement.DeleteSchemeByName(clickBtn.Text);
            //保存更新以及ui
            RefreshSchemeList();
        }
        private void ProjectButton_Click(object sender, EventArgs e)
        {
            if (_isAddNewScheming)
            {
                MessageBox.Show("当前正在添加新方案，无法选中已有方案,请先保存新方案");
                return;
            }
            var btnProject = sender as Sunny.UI.UIButton;
            //获取方案对象
            ProcessScheme isExistsScheme = ProjectManagement.GetSingleScheme(btnProject.Text);
            if (isExistsScheme != null)
            {
                _currentScheme = isExistsScheme;
            }
            else
                return;
            //当前方案卡片高亮
            ChangeCardColor(FLP_ProjectList, btnProject);
            //刷新步骤列表
            ClearFLP_StepList();
            foreach (var step in _currentScheme.Steps)
            {
                AddStepCard(step, step.StepTypeName);
            }
            //清空参数界面
            ClearPanel_Param();
        }
        private void ClearPanel_Param()
        {
            foreach (Control c in panel_Param.Controls)
            {
                if (c is Form frm)
                {
                    frm.Close();
                }
            }
            panel_Param.Controls.Clear();
        }
        /// <summary>
        /// 清空步骤列表中的button按钮
        /// </summary>
        private void ClearFLP_StepList()
        {
            foreach (Control _FLP in FLP_StepList.Controls)
            {
                if (_FLP is System.Windows.Forms.FlowLayoutPanel)
                {
                    _FLP.Controls.Clear();
                }
            }
        }
        /// <summary>添加一个步骤卡片到步骤列表</summary>
        private void btn_Step_Click(object sender, EventArgs e)
        {
            var btnStep = sender as Sunny.UI.UIButton;
            string schemeName = "";
            if (btnStep != null)
            {
                switch (btnStep.Text)
                {
                    case "称重":
                        schemeName = (FLP_StepList.ControlCount + 1).ToString() + "称重";
                        _currentScheme.Steps.Add(new WeightStep());
                        break;
                    case "随样测浓":
                        schemeName = (FLP_StepList.ControlCount + 1).ToString() + "随样测浓";
                        _currentScheme.Steps.Add(new ConcentrationMeasurement());
                        break;
                    case "并样":
                        schemeName = (FLP_StepList.ControlCount + 1).ToString() + "并样";
                        _currentScheme.Steps.Add(new Aggregatesample());
                        break;
                    case "中和":
                        schemeName = (FLP_StepList.ControlCount + 1).ToString() + "中和";
                        _currentScheme.Steps.Add(new Neutralize());
                        break;
                    case "振荡":
                        schemeName = (FLP_StepList.ControlCount + 1).ToString() + "振荡";
                        _currentScheme.Steps.Add(new Shock());
                        break;
                    default:
                        break;
                }
                //判断当前步骤列表中是否已经存在该类型的步骤，如果存在，则不允许添加
                if (_currentScheme.Steps.Count > 1)
                {
                    var lastStep = _currentScheme.Steps[_currentScheme.Steps.Count - 2];
                    if (lastStep.StepTypeName == _currentScheme.Steps[_currentScheme.Steps.Count - 1].StepTypeName)
                    {
                        MessageBox.Show("当前步骤列表中已经存在该类型的步骤，请勿重复添加！");
                        _currentScheme.Steps.RemoveAt(_currentScheme.Steps.Count - 1);
                        return;
                    }
                }
                //判断步骤列表顺序
                if (!CheckStepOrder())
                {
                    return;
                }
                //往步骤列表添加卡片，绑定步骤对象
                AddStepCard(_currentScheme.Steps[_currentScheme.Steps.Count - 1], schemeName);


            }


        }
        /// <summary>
        /// 检查新添步骤的顺序是否正确
        /// </summary>
        /// <returns></returns>
        private bool CheckStepOrder()
        {
            //定义一个步骤顺序的字典
            Dictionary<string, int> stepOrder = new Dictionary<string, int>()
            {
                { "称重", 1 },
                { "随样测浓", 2 },
                { "并样", 3 },
                { "中和", 4 },
                { "振荡", 5 }
            };
            if (_currentScheme.Steps.Count > 1)
            {
                var lastStep = _currentScheme.Steps[_currentScheme.Steps.Count - 2];
                int lastStepOrder = stepOrder[lastStep.StepTypeName];
                int currentStepOrder = stepOrder[_currentScheme.Steps[_currentScheme.Steps.Count - 1].StepTypeName];
                if (currentStepOrder < lastStepOrder)
                {
                    MessageBox.Show("在当前步骤列表中此步骤顺序错误，请勿添加！");
                    _currentScheme.Steps.RemoveAt(_currentScheme.Steps.Count - 1);
                    return false;
                }
            }
            return true;
        }
        /// <summary>往步骤列表添加卡片，绑定步骤对象</summary>
        private void AddStepCard(BaseStep step, string cardText)
        {
            UIButton btnStep = new UIButton();
            btnStep.Text = cardText;
            btnStep.Height = 60;
            btnStep.Width = 260;
            btnStep.Location = new Point(0, 50 * (FLP_StepList.ControlCount + 1));
            btnStep.Margin = new Padding(2, 2, 2, 2); //卡片之间间距
            //btnStep.FillColor = System.Drawing.Color.DarkCyan;
            //btnStep.RectColor = System.Drawing.Color.DarkCyan;
            btnStep.ForeColor = Color.White;
            btnStep.FillHoverColor = System.Drawing.Color.DarkCyan;
            btnStep.Tag = step; //把步骤对象存入Tag，点击后给到属性面板
            //新增步骤卡片颜色高亮
            ChangeCardColor(FLP_StepList, btnStep);
            //添加点击事件，点击卡片，选中这个流程步骤
            btnStep.Click += StepButton_Click;
            //添加删除事件
            btnStep.ContextMenuStrip = ContextMenuStrip_Step;

            FLP_StepList.Controls.Add(btnStep);
        }
        /// <summary>
        /// 删除步骤卡片
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tsmiDeleteStep_Click(object sender, EventArgs e)
        {
            UIButton clickBtn = ContextMenuStrip_Step.SourceControl as UIButton;
            if (clickBtn == null)
                return;
            //删除当前方案step
            _currentScheme.Steps.Remove(clickBtn.Tag as BaseStep);
            //刷新步骤列表
            ClearFLP_StepList();
            foreach (var step in _currentScheme.Steps)
            {
                AddStepCard(step, step.StepTypeName);
            }
            //清空属性面板
            ClearPanel_Param();
        }
        private void StepButton_Click(object sender, EventArgs e)
        {
            UIButton btnStep = sender as UIButton;
            ChangeCardColor(FLP_StepList, btnStep);
            //右侧属性面板加载当前步骤参数
            //propertyGrid1.SelectedObject = btnStep.Tag as BaseStep;
            BaseStep stepObj = btnStep.Tag as BaseStep;
            LoadChildFormIntoPanel(stepObj.StepTypeName, stepObj);
        }
        private void LoadChildFormIntoPanel(string stepName, BaseStep stepObj)
        {
            ClearPanel_Param();
            switch (stepName)
            {
                case "称重":

                    break;
                case "随样测浓":
                    ConcentrationMeasurement concentrationMeasurement = stepObj as ConcentrationMeasurement;
                    ConcentrationMeasurementFrm frm = new ConcentrationMeasurementFrm(concentrationMeasurement.concentrationMeasurementParam);
                    frm.PassingValues += (res) =>
                    {
                        concentrationMeasurement.concentrationMeasurementParam = res;
                        btn_SaveProject_Click(null, EventArgs.Empty);
                    };
                    frm.TopLevel = false;
                    frm.Parent = panel_Param;
                    frm.Show();
                    break;
                case "并样":
                    Aggregatesample aggregatesample = stepObj as Aggregatesample;
                    AggregatesampleFrm frm1 = new AggregatesampleFrm(aggregatesample.aggregatesampleParam);
                    frm1.PassingValues += (res) =>
                    {
                        aggregatesample.aggregatesampleParam = res;
                        btn_SaveProject_Click(null, EventArgs.Empty);
                    };
                    frm1.TopLevel = false;
                    frm1.Parent = panel_Param;
                    frm1.Show();
                    break;
                case "中和":
                    Neutralize neutralize = stepObj as Neutralize;
                    NeutralizeFrm frm2 = new NeutralizeFrm(neutralize.neutralizeParam);
                    frm2.PassingValues += (res) =>
                    {
                        neutralize.neutralizeParam = res;
                        btn_SaveProject_Click(null, EventArgs.Empty);
                    };
                    frm2.TopLevel = false;
                    frm2.Parent = panel_Param;
                    frm2.Show();
                    break;
                case "振荡":

                    break;
                default:
                    break;
            }

        }
        /// <summary>
        /// 改变卡片颜色
        /// </summary>
        /// <param name="btnProject"></param>
        private void ChangeCardColor(UIFlowLayoutPanel flowLayoutPanel, UIButton btnProject)
        {
            //把所有卡片取消选中，当前卡片高亮
            foreach (Control _FLP in flowLayoutPanel.Controls)
            {
                if (_FLP is System.Windows.Forms.FlowLayoutPanel)
                {
                    foreach (Control b in _FLP.Controls)
                    {
                        if (b is Sunny.UI.UIButton btn)
                        {
                            btn.FillColor = System.Drawing.Color.LightSeaGreen;
                            btn.RectColor = System.Drawing.Color.LightSeaGreen;
                        }
                    }
                }
            }
            if (btnProject != null)
            {
                btnProject.FillColor = System.Drawing.Color.DarkCyan;
                btnProject.RectColor = System.Drawing.Color.DarkCyan;
            }
        }
        #endregion

        private void TabControl_DeviceParameter_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (TabControl_DeviceParameter.SelectedTab == tP_magellan)
            {
                cb_MagellanMethods.Items.Clear();
                cb_MagellanMethods.Items.Add("");
                AvailableMethods();
               
            }
            if (TabControl_DeviceParameter.SelectedTab == tP_plc)
            {
                
            }
        }
        #region Magellan
        private void AvailableMethods()
        {
            string[] sMethodsArray = CommonParam.s_Magellan.AvailableMethods();
            if (sMethodsArray != null)
            {
                foreach (string s in sMethodsArray)
                {
                    cb_MagellanMethods.Items.Add(s);
                }
                if (_currentMagellanMethod != "")
                {
                    cb_MagellanMethods.SelectedItem = _currentMagellanMethod;
                }
            }
        }
        private void btn_MagellanConnect_Click(object sender, EventArgs e)
        {

            MagellanConnect(cb_MagellanDeviceName.Text);
        }
        private void MagellanConnect(string magellanDeviceName)
        {
            if (btn_MagellanConnect.Text == "连 接")
            {
                CommonParam.s_Magellan._DeviceName = magellanDeviceName;
                if (CommonParam.s_Magellan.Connect())
                {
                    LogEventManager.LogEvent(31, $"连接Magellan设备成功，设备名称：{CommonParam.s_Magellan._DeviceName}");
                    SqlHelper.SetStringValue("MagellanDeviceName", CommonParam.s_Magellan._DeviceName);
                    btn_MagellanConnect.Text = "断 开";
                    Light_MagellanCommunicationStatus.State = UILightState.On;
                    cb_MagellanDeviceName.Text = magellanDeviceName;
                    tb_RunStatus.Text = $"连接Magellan设备成功，设备名称：{CommonParam.s_Magellan._DeviceName}";
                }
                else
                {
                    LogEventManager.LogEvent(30, $"连接Magellan设备失败，设备名称：{CommonParam.s_Magellan._DeviceName}");
                    Light_MagellanCommunicationStatus.State = UILightState.Off;
                    tb_RunStatus.Text = $"连接Magellan设备失败，设备名称：{CommonParam.s_Magellan._DeviceName}";
                }
            }
            else
            {
                CommonParam.s_Magellan.Disconnect();
                LogEventManager.LogEvent(32, $"断开Magellan设备连接，设备名称：{CommonParam.s_Magellan._DeviceName}");
                btn_MagellanConnect.Text = "连 接";
                Light_MagellanCommunicationStatus.State = UILightState.Off;
                tb_RunStatus.Text = $"断开Magellan设备连接，设备名称：{CommonParam.s_Magellan._DeviceName}";
            }
        }
        private string _currentMagellanMethod = "";
        private void MagellanArrivalRunStatus()
        {
            CommonParam.s_Magellan.WorkSpaceSetEmpty();
            _currentMagellanMethod = SqlHelper.GetString("MagellanMethod");
            CommonParam.s_Magellan.InsertMethod(_currentMagellanMethod);
        }
        private void btn_MagellanPlateOut_Click(object sender, EventArgs e)
        {
            if (CommonParam.s_Magellan._IsConnected)
            {
                tb_RunStatus.Text = "打开舱门中";
                LogEventManager.LogEvent(31, "Magellan-打开舱门中");
                CommonParam.s_Magellan.PlateOut();
                tb_RunStatus.Text = "舱门已打开";
                LogEventManager.LogEvent(31, "Magellan-舱门已打开");
            }
        }

        private void btn_MagellanPlateIn_Click(object sender, EventArgs e)
        {
            if (CommonParam.s_Magellan._IsConnected)
            {
                tb_RunStatus.Text = "关闭舱门中";
                LogEventManager.LogEvent(31, "Magellan-关闭舱门中");
                CommonParam.s_Magellan.PlateIn();
                tb_RunStatus.Text = "舱门已关闭";
                LogEventManager.LogEvent(31, "Magellan-舱门已关闭");
            }
        }
        private void btn_MagellanWorkspaceSetEmpty_Click(object sender, EventArgs e)
        {
            if (CommonParam.s_Magellan._IsConnected)
            {
                CommonParam.s_Magellan.WorkSpaceSetEmpty();
                cb_MagellanMethods.SelectedIndex = 0;
                tb_RunStatus.Text = "WorkSpace已清空";
                LogEventManager.LogEvent(31, "Magellan-WorkSpace已清空");
            }
        }
        private void cb_MagellanMethods_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cb_MagellanMethods.SelectedIndex != 0)
            {
                bool isok = CommonParam.s_Magellan.InsertMethod(cb_MagellanMethods.SelectedItem.ToString());
                if (isok)
                {
                    SqlHelper.SetStringValue("MagellanMethod", cb_MagellanMethods.SelectedItem.ToString());
                    _currentMagellanMethod = cb_MagellanMethods.SelectedItem.ToString();
                    tb_RunStatus.Text = $"已添加方法{cb_MagellanMethods.SelectedText}到WorkSpace";
                    LogEventManager.LogEvent(31, $"Magellan-已添加方法{cb_MagellanMethods.SelectedText}到WorkSpace");
                }
            }
        }
        private bool isMagellanRunning = false;
        private void btn_MagellanStartRun_Click(object sender, EventArgs e)
        {
            if (isMagellanRunning)
                return;
            isMagellanRunning = true;
            CommonParam.s_Magellan.StartRun();
            MagellanRunStatusToUI();
        }
        private void btn_MagellanStopRun_Click(object sender, EventArgs e)
        {
            CommonParam.s_Magellan.StopRun();
            isMagellanRunning = false;
        }
        private void MagellanRunStatusToUI()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    string runStatusText = CommonParam.s_Magellan.GetRunStatusAsText();
                    tb_RunStatus.Invoke(new Action(() =>
                    {
                        tb_RunStatus.Text = runStatusText;
                    }));
                    if (runStatusText != "测量进行中")
                    {
                        LogEventManager.LogEvent(31, $"Magellan-手动测量完成，结果：{runStatusText}");
                        isMagellanRunning = false;
                        if (tb_RunStatus.Text == "测量成功结束")
                        {
                            string dataA260Str = CommonParam.s_Magellan.DataAsTable("A260");
                            string dataA280Str = CommonParam.s_Magellan.DataAsTable("A280");
                            string dataA340Str = CommonParam.s_Magellan.DataAsTable("A340");
                            UpDataResultToDGv(dataA260Str, dataA280Str, dataA340Str);
                            LogEventManager.LogEvent(31, $"Magellan-结果更新完成");
                        }
                        break;
                    }
                    Thread.Sleep(100);
                }
            });
        }
        private void UpDataResultToDGv(string dataA260Str, string dataA280Str, string dataA340Str)
        {
            if (dataA260Str == null || dataA280Str == null || dataA340Str == null)
                return;
            DataTable dt1 = ConcentrationMeasurement.ParseGridToDataTable(dataA260Str);
            DataTable dt2 = ConcentrationMeasurement.ParseGridToDataTable(dataA280Str);
            DataTable dt3 = ConcentrationMeasurement.ParseGridToDataTable(dataA340Str);
            if (dgv_A260OD.IsDisposed || dgv_A280OD.IsDisposed || dgv_A340OD.IsDisposed) return;

            if (dgv_A260OD.InvokeRequired)
            {
                dgv_A260OD.Invoke(new Action(() =>
                {
                    dgv_A260OD.DataSource = dt1;
                }));
            }
            else
                dgv_A260OD.DataSource = dt1;
            if (dgv_A280OD.InvokeRequired)
            {
                dgv_A280OD.Invoke(new Action(() =>
                {
                    dgv_A280OD.DataSource = dt1;
                }));
            }
            else
                dgv_A280OD.DataSource = dt1;
            if (dgv_A340OD.InvokeRequired)
            {
                dgv_A340OD.Invoke(new Action(() =>
                {
                    dgv_A340OD.DataSource = dt1;
                }));
            }
            else
                dgv_A340OD.DataSource = dt1;
        }

        private void btn_ExportMagellanOD_Click(object sender, EventArgs e)
        {
            try
            {
                DataTable dt1 = dgv_A260OD.DataSource as DataTable;
                DataTable dt2 = dgv_A280OD.DataSource as DataTable;
                DataTable dt3 = dgv_A340OD.DataSource as DataTable;
                if (dt1 == null || dt1.Rows.Count == 0)
                {
                    MessageBox.Show("无数据");
                    return;
                }
                Dictionary<string, DataTable> sheets = new Dictionary<string, DataTable>(){{ "A260原始OD", dt1 },{ "A280原始OD", dt2 },{ "A340原始OD", dt3 }};
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string fileName = $"酶标仪检测原始OD_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                string savePath = Path.Combine(desktopPath, fileName);
                ExcelHelper.ExportMultiSheetToExcel(sheets, savePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
        #endregion

        #region PLC
        private void btn_ConnectPLC_Click(object sender, EventArgs e)
        {
            //if (btn_ConnectPLC.Text == "连 接")
            //{
            string ip = tb_PLC_IP.Text;
            int port = Convert.ToInt32(intUD_PLC_Port.Text);
            //    btn_ConnectPLC.Text = "连接中";
            //    await ConnectPLC(ip , port);
            //    if (!_isConnectedPLC)
            //    {
            //        CommonParam.s_PLC.Close();
            //        CommonParam.s_PLC = null;
            //        LogEventManager.LogEvent(30, "PLC连接失败，请检查IP地址和端口号是否正确，PLC是否在线");
            //        btn_ConnectPLC.Text = "连 接";
            //    }
            //    else
            //    {
            //        SqlHelper.SetValue("PLCIP", ip);
            //        SqlHelper.SetValue("PLCPort", port);
            //        LogEventManager.LogEvent(31, "PLC连接成功");
            //        Light_PLCCommunicationStatus.State = UILightState.On;
            //        btn_ConnectPLC.Text = "断 开";
            //    }
            //}
            //else if (_isConnectedPLC && btn_ConnectPLC.Text == "断 开")
            //{
            //    CommonParam.s_PLC.Close();
            //    CommonParam.s_PLC = null;
            //    LogEventManager.LogEvent(32, "PLC连接断开");
            //    btn_ConnectPLC.Text = "连 接";
            //}
            ConnectPLC(ip, port);
        }
        private async void ConnectPLC(string ip, int port)
        {
            if (CommonParam.s_PLC == null)
            {
                CommonParam.s_PLC = new ModbusTcp(ip, port);
            }
            //bool connected = await Task.Run(() => CommonParam.s_PLC.Connect());
            //return connected;

            if (btn_ConnectPLC.Text == "连 接")
            {
                btn_ConnectPLC.Text = "连接中";
                await Task.Run(() => CommonParam.s_PLC.Connect());
                if (!_isConnectedPLC)
                {
                    CommonParam.s_PLC.Close();
                    CommonParam.s_PLC = null;
                    LogEventManager.LogEvent(30, "PLC连接失败，请检查IP地址和端口号是否正确，PLC是否在线");
                    btn_ConnectPLC.Text = "连 接";
                }
                else
                {
                    SqlHelper.SetStringValue("PLCIP", ip);
                    SqlHelper.SetStringValue("PLCPort", port.ToString());
                    LogEventManager.LogEvent(31, "PLC连接成功");
                    Light_PLCCommunicationStatus.State = UILightState.On;
                    btn_ConnectPLC.Text = "断 开";
                }
            }
            else if (btn_ConnectPLC.Text == "断 开")
            {
                CommonParam.s_PLC.Close();
                CommonParam.s_PLC = null;
                LogEventManager.LogEvent(32, "PLC连接断开");
                btn_ConnectPLC.Text = "连 接";
            }
        }
        private void PLC_Timer_Tick(object sender, EventArgs e)
        {
            lb_ShowTime.Text = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

            lb_ShowTime.Text = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            if (!_isConnectedPLC)
            {
                Light_PLCCommunicationStatus.State = UILightState.Off;
                return;
            }
            Light_PLCCommunicationStatus.State = UILightState.On;
           
            //PLC心跳
            short[] registerValues_800 = CommonParam.s_PLC.Read_Register_State_Short(1, 800, 1);
            if (registerValues_800 != null && registerValues_800[0] == 0)
            {
                registerValues_800[0] = 1;
                CommonParam.s_PLC.Write_Register_Value(1, 800, registerValues_800[0].ToString(), "short");
            }
            //模式请求
            short[] registerValues_802 = CommonParam.s_PLC.Read_Register_State_Short(1, 802, 1);
            if (registerValues_802 != null && registerValues_802[0] == 1 && CommonParam.s_DeviceState == DeviceState.Running)
            {
                CommonParam.s_PLC.Write_Register_Value(1, 802, "0", "short");
                bool[] modeBools = new bool[32];
                foreach (BaseStep step in _runningScheme.Steps)
                {
                    switch (step.StepTypeName)
                    {
                        case "称重":
                            modeBools[1] = true;
                            break;
                        case "随样测浓":
                            modeBools[2] = true;
                            break;
                        case "并样":
                            modeBools[3] = true;
                            break;
                        case "中和":
                            modeBools[4] = true;
                            break;
                        case "振荡":
                            modeBools[5] = true;
                            break;
                    }
                }
                if (modeBools[1] || modeBools[2] || modeBools[3] || modeBools[4] || modeBools[5])
                {
                    modeBools[0] = true;
                }
                ushort registerValues_803 = 0, registerValues_804 = 0;
                CommonParam.s_PLC.Bool32ToTwoWord(modeBools, out registerValues_803, out registerValues_804);
                CommonParam.s_PLC.Write_Register_Value(1, 803, registerValues_803.ToString(), "short");
                LogEventManager.LogEvent(31, $"给PLC请求结果：总开关{modeBools[0]}, 称重{modeBools[1]}, " +
                $"随样测浓{modeBools[2]}, 并样{modeBools[3]}, 中和{modeBools[4]}, 振荡{modeBools[5]}");

            }

            //读取紫外灯状态
            short[] registerValues_3101 = CommonParam.s_PLC.Read_Register_State_Short(1, 3101, 1);
            if (registerValues_3101 != null && registerValues_3101[0] == 0)
                uiLedStopwatch1.Stop();
            else
                uiLedStopwatch1.Start();
            short[] registerValues_3103 = CommonParam.s_PLC.Read_Register_State_Short(1, 3103, 1);
            if (registerValues_3103 != null && registerValues_3103[0] == 0)
                uiLedStopwatch2.Stop();
            else
                uiLedStopwatch2.Start();
            //检测报警
            short[] registerValues_801 = CommonParam.s_PLC.Read_Register_State_Short(1, 801, 1);
            if (registerValues_801 != null && registerValues_801[0] == 1)
            {
                //有报警
                _scanPLCAlarmTimer.Start();
            }
            else
            {
                _scanPLCAlarmTimer.Stop();
            }
            
        }
        #endregion
        private void button2_Click(object sender, EventArgs e)
        {

            string sql;

            //新增一条生产记录
            sql = @"INSERT INTO ProductionRecordsT
                        ([Production Batch],[Tube ID],[Rack ID],Location,[Order ID],[Order Repeat ID],[Operator])
                        VALUES(@ProductionBatch,@TubeID,@RackID,@Location,@OrderID,@OrderRepeatID,@Operator)";

            SqlParameter[] paras =
            {
                new SqlParameter("@ProductionBatch", "P20260828‑001"),
                new SqlParameter("@TubeID", "T‑1001"),
                new SqlParameter("@RackID", "RK‑05"),
                new SqlParameter("@Location", "A3"),
                new SqlParameter("@OrderID", "ORD‑2026002"),
                new SqlParameter("@OrderRepeatID", "ORD‑2026001-2"),
                new SqlParameter("@Operator", "Zhang")
            };
            int i = SqlHelper.ExecuteNonQuery(sql, paras);

            ////删除一条生产记录
            //sql = "DELETE FROM ProductionRecordsT WHERE [Order ID]=@OrderID";
            //SqlParameter[] paras = { new SqlParameter("@OrderID", "ORD‑2026001") };
            //SqlHelper.ExecuteNonQuery(sql, paras);
        }
        #region 孔版监控
        private readonly System.Windows.Forms.Timer _mapHoleInfosTimer;
        private UILight _selectlight = null;
        private void IniMapHoleInfoUI_Light()
        {
            Light_A1.BackColor = Color.White;
            Light_A2.BackColor = Color.White;
            Light_A3.BackColor = Color.White;
            Light_A4.BackColor = Color.White;
            Light_A5.BackColor = Color.White;
            Light_A6.BackColor = Color.White;
            Light_B1.BackColor = Color.White;
            Light_B2.BackColor = Color.White;
            Light_B3.BackColor = Color.White;
            Light_B4.BackColor = Color.White;
            Light_B5.BackColor = Color.White;
            Light_B6.BackColor = Color.White;
            Light_C1.BackColor = Color.White;
            Light_C2.BackColor = Color.White;
            Light_C3.BackColor = Color.White;
            Light_C4.BackColor = Color.White;
            Light_C5.BackColor = Color.White;
            Light_C6.BackColor = Color.White;
            Light_D1.BackColor = Color.White;
            Light_D2.BackColor = Color.White;
            Light_D3.BackColor = Color.White;
            Light_D4.BackColor = Color.White;
            Light_D5.BackColor = Color.White;
            Light_D6.BackColor = Color.White;

        }
        private void LightColorChange(UILight light, MapHoleInfo mapHoleInfos)
        {
            if (mapHoleInfos.State == MapHoleInfoState.Running)
                light.OnColor = Color.Goldenrod;
            if (mapHoleInfos.State == MapHoleInfoState.Completed)
                light.OnColor = Color.DarkCyan;
            if (mapHoleInfos.State == MapHoleInfoState.Pending)
                light.OnColor = Color.Gray;
            if (mapHoleInfos.State == MapHoleInfoState.Abnormal)
                light.OnColor = Color.Red;
        }
        private void ChangeMapHoleInfo_Click(object sender, EventArgs e)
        {
            IniMapHoleInfoUI_Light();
            _selectlight = sender as UILight;
            _selectlight.BackColor = Color.LightSeaGreen;

        }
        private void MapHoleInfosTimer_Tick(object sender, EventArgs e)
        {
            if (cb_MAP_HoleSite.SelectedIndex < 0) return;
            List<MapHoleInfo> mapHoleInfos = null;
            switch (cb_MAP_HoleSite.SelectedItem)
            {
                case "测浓孔板1": mapHoleInfos = CommonParam.s_MapHoleInfoListS1_1; break;
                case "测浓孔板2": mapHoleInfos = CommonParam.s_MapHoleInfoListS1_2; break;
                case "测浓孔板3": mapHoleInfos = CommonParam.s_MapHoleInfoListS1_3; break;
                case "并样中和孔板1": mapHoleInfos = CommonParam.s_MapHoleInfoListS2_1; break;
                case "并样中和孔板2": mapHoleInfos = CommonParam.s_MapHoleInfoListS2_2; break;
                case "并样中和孔板3": mapHoleInfos = CommonParam.s_MapHoleInfoListS2_3; break;
                default: return;
            }
            if (mapHoleInfos == null || mapHoleInfos.Count == 0)
            {
                IniMapHoleInfoUI_Light();
                lb_Location.Text = "";
                lb_OrderID.Text = "";
                lb_OrderRepeatID.Text = "";
                lb_BatchID.Text = "";
                lb_ReagentTypes.Text = "";
                lb_ReagentName.Text = "";
                lb_ReagentBatchNumber.Text = "";
                lb_ReagentVolume.Text = "";
                lb_State.Text = "";
                return;
            }
            lb_TrayCode.Text = mapHoleInfos.First().RackID;
            foreach (var item in mapHoleInfos)
            {
                switch (item.Location)
                {
                    case "A1":
                        LightColorChange(Light_A1, item);
                        break;
                    case "A2":
                        LightColorChange(Light_A2, item);
                        break;
                    case "A3":
                        LightColorChange(Light_A3, item);
                        break;
                    case "A4":
                        LightColorChange(Light_A4, item);
                        break;
                    case "A5":
                        LightColorChange(Light_A5, item);
                        break;
                    case "A6":
                        LightColorChange(Light_A6, item);
                        break;
                    case "B1":
                        LightColorChange(Light_B1, item);
                        break;
                    case "B2":
                        LightColorChange(Light_B2, item);
                        break;
                    case "B3":
                        LightColorChange(Light_B3, item);
                        break;
                    case "B4":
                        LightColorChange(Light_B4, item);
                        break;
                    case "B5":
                        LightColorChange(Light_B5, item);
                        break;
                    case "B6":
                        LightColorChange(Light_B6, item);
                        break;
                    case "C1":
                        LightColorChange(Light_C1, item);
                        break;
                    case "C2":
                        LightColorChange(Light_C2, item);
                        break;
                    case "C3":
                        LightColorChange(Light_C3, item);
                        break;
                    case "C4":
                        LightColorChange(Light_C4, item);
                        break;
                    case "C5":
                        LightColorChange(Light_C5, item);
                        break;
                    case "C6":
                        LightColorChange(Light_C6, item);
                        break;
                    case "D1":
                        LightColorChange(Light_D1, item);
                        break;
                    case "D2":
                        LightColorChange(Light_D2, item);
                        break;
                    case "D3":
                        LightColorChange(Light_D3, item);
                        break;
                    case "D4":
                        LightColorChange(Light_D4, item);
                        break;
                    case "D5":
                        LightColorChange(Light_D5, item);
                        break;
                    case "D6":
                        LightColorChange(Light_D6, item);
                        break;
                }
            }
            if (_selectlight == null) return;
            MapHoleInfo mapHoleInfo = mapHoleInfos.FirstOrDefault(info => info != null && info.Location == _selectlight.Text);
            if (mapHoleInfo == null)
            {
                lb_Location.Text = "";
                lb_OrderID.Text = "";
                lb_OrderRepeatID.Text = "";
                lb_BatchID.Text = "";
                lb_ReagentTypes.Text = "";
                lb_ReagentName.Text = "";
                lb_ReagentBatchNumber.Text = "";
                lb_ReagentVolume.Text = "";
                lb_State.Text = "";
                return;
            }
            lb_Location.Text = _selectlight.Text;
            lb_OrderID.Text = mapHoleInfo.OrderID;
            lb_OrderRepeatID.Text = mapHoleInfo.OrderRepeatID;
            lb_BatchID.Text = mapHoleInfo.BatchID;
            lb_ReagentTypes.Text = mapHoleInfo.ReagentTypes;
            lb_ReagentName.Text = mapHoleInfo.ReagentName;
            lb_ReagentBatchNumber.Text = mapHoleInfo.ReagentBatchNumber;
            lb_ReagentVolume.Text = mapHoleInfo.ReagentVolume;
            lb_State.Text = mapHoleInfo.Progress;
        }
        #endregion

        private bool ShowHumanConfirmDialog(List<MapHoleInfo> s1,List<MapHoleInfo> s2,List<MapHoleInfo> s3)
        {
            // 已经在 UI 线程就直接执行；否则 Invoke 封送
            if (InvokeRequired)
            {
                return (bool)Invoke(
                    new Func<List<MapHoleInfo>, List<MapHoleInfo>, List<MapHoleInfo>, bool>(
                        ShowHumanConfirmDialog),
                    s1, s2, s3);
            }

            using (var form = new HumanInterventionFrm())
            {
                form.LoadData(s1, s2, s3);
                DialogResult dr = form.ShowDialog(this);   // 阻塞 UI 线程
                return dr == DialogResult.OK;              // 返回给业务线程
            }
        }

        /// <summary>
        /// 自动运行方案TabControl切换事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RunFrmTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (RunFrmTabControl.SelectedTab == tp_MapHoleInfoState)
                _mapHoleInfosTimer.Start();
            else
                _mapHoleInfosTimer.Stop();
        }
        private void WorkOrdersLoadDataToDgv()
        {
            if (CommonParam.s_WorkOrderTable == null && CommonParam.s_UpDataWorkOrderTableFinish)
                return;
            dgv_WorkOrder.DataSource = null;
            dgv_WorkOrder.AutoGenerateColumns = true;
            dgv_WorkOrder.DataSource = CommonParam.s_WorkOrderTable;
            //禁止编辑
            dgv_WorkOrder.ReadOnly = true;
            dgv_WorkOrder.AllowUserToAddRows = false;
            dgv_WorkOrder.AllowUserToDeleteRows = false;

        }
        #region 主菜单TabControl切换事件
        /// <summary>
        /// 主菜单TabControl切换事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (MainTabControl.SelectedTab == tP_ProductionRecords)
            {
                dtp_ProductionRecordEndTimeScreen.Value = DateTime.Now;
                ProductionRecordsLoadDataToDgv("SELECT * FROM ProductionRecordsT",null);
            }
            if (MainTabControl.SelectedTab == tP_UserManagement)
            {
                UserLoadDataToDgv();
            }
        }
        /// <summary>
        /// 报警TabControl切换事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TabControl_Alarm_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (TabControl_Alarm.SelectedTab == tbP_HistoricalAlarms)
            {
                AlarmRecordsLoadDataToDgv("SELECT * FROM AlarmRecordT",null);
            }
        }
        /// <summary>
        /// 生产记录绑定数据库表
        /// </summary>
        private void ProductionRecordsLoadDataToDgv(string sql, List<SqlParameter> paramList)
        {
            //string sql = "SELECT * FROM ProductionRecordsT";
            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(SqlHelper.connStr))
            {
                SqlDataAdapter sda = new SqlDataAdapter(sql, conn);
                //把参数添加到adapter
                if (paramList != null && paramList.Count > 0)
                {
                    sda.SelectCommand.Parameters.AddRange(paramList.ToArray());
                }
                sda.Fill(dt);
            }
            dgv_ProductionRecords.DataSource = dt;
            //禁止编辑
            dgv_ProductionRecords.ReadOnly = true;
            dgv_ProductionRecords.AllowUserToAddRows = false;
            dgv_ProductionRecords.AllowUserToDeleteRows = false;
        }
        /// <summary>
        /// 报警记录绑定数据库表
        /// </summary>
        private void AlarmRecordsLoadDataToDgv(string sql,List<SqlParameter> paramList)
        {
            //string sql = "SELECT * FROM AlarmRecordT";
            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(SqlHelper.connStr))
            {
                SqlDataAdapter sda = new SqlDataAdapter(sql, conn);
                //把参数添加到adapter
                if (paramList != null && paramList.Count > 0)
                {
                    sda.SelectCommand.Parameters.AddRange(paramList.ToArray());
                }
                sda.Fill(dt);
            }
            dgv_AlarmRecords.DataSource = dt;
            // 修改每一列的表头文本
            dgv_AlarmRecords.Columns["Alarm Number"].HeaderText = "报警编号";
            dgv_AlarmRecords.Columns["Device Number"].HeaderText = "设备编号";
            dgv_AlarmRecords.Columns["Device Name"].HeaderText = "设备名称";
            dgv_AlarmRecords.Columns["Alarm Code"].HeaderText = "报警代码";
            dgv_AlarmRecords.Columns["Alarm Category"].HeaderText = "报警类别";
            dgv_AlarmRecords.Columns["Alarm Description"].HeaderText = "报警描述";
            dgv_AlarmRecords.Columns["Alarm Level"].HeaderText = "报警级别";
            dgv_AlarmRecords.Columns["Operator"].HeaderText = "操作人";
            dgv_AlarmRecords.Columns["Treatment Measure"].HeaderText = "处理措施";
            dgv_AlarmRecords.Columns["Status"].HeaderText = "Status";
            dgv_AlarmRecords.Columns["Triggering Time"].HeaderText = "触发时间";
            dgv_AlarmRecords.Columns["Recovery Time"].HeaderText = "恢复时间";
            dgv_AlarmRecords.Columns["Remark"].HeaderText = "备注";

            //禁止编辑
            dgv_AlarmRecords.ReadOnly = true;
            dgv_AlarmRecords.AllowUserToAddRows = false;
            dgv_AlarmRecords.AllowUserToDeleteRows = false;
        }
        private void UserLoadDataToDgv()
        {
            string sql = "SELECT * FROM UserT";
            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(SqlHelper.connStr))
            {
                SqlDataAdapter sda = new SqlDataAdapter(sql, conn);
                sda.Fill(dt);
            }
            dgv_User.DataSource = dt;
            dgv_User.Columns["PassWord"].Visible = false;
            // 修改每一列的表头文本
            dgv_User.Columns["UserName"].HeaderText = "用户名";
            dgv_User.Columns["Class"].HeaderText = "权限等级";
            dgv_User.Columns["State"].HeaderText = "启用状态";
            dgv_User.Columns["DateCreated"].HeaderText = "创建时间";
            dgv_User.ReadOnly = true;
            dgv_AlarmRecords.AllowUserToAddRows = false;
            dgv_AlarmRecords.AllowUserToDeleteRows = false;
        }

        #endregion
        private void button3_Click(object sender, EventArgs e)
        {
            //byte[] bts = CommonParam.s_PLC.Fun03(1,1201,20);
            //StringBuilder sbText = new StringBuilder();
            ////从i=3开始读取数据，步长2，一个寄存器2字节
            //for (int i = 3; i < bts.Length; i = i + 2)
            //{
            //    char ch1 = (char)bts[i];      //高字节 第一个字符
            //    char ch2 = (char)bts[i + 1];  //低字节 第二个字符
            //    sbText.Append(ch1);
            //    sbText.Append(ch2);
            //}
            ////最终ASCII字符串
            //string asciiResult = sbText.ToString().Replace("\0", ""); 
            //tb_Buffer1_PositionalCode.Text = asciiResult;


            //tb_Buffer1_PositionalCode.Text = CommonParam.s_PLC.Read_Register_State_ACSII(1, 1201, 20);


            bool[] locationInfo = { false, false ,true, false, true, false, true, false,
                                    true, false ,false, false, true, false, true, false,
                                    true, true ,true, false, true, false, true, true,
                                    false, false ,false, false, false, false, false, false};
            ushort registerValues_2301 = 0, registerValues_2302 = 0;
            CommonParam.s_PLC.Bool32ToTwoWord(locationInfo, out registerValues_2301, out registerValues_2302);
            CommonParam.s_PLC.Write_Register_Value(1, 2301, registerValues_2301.ToString(), "short");
            CommonParam.s_PLC.Write_Register_Value(1, 2302, registerValues_2302.ToString(), "short");
            CommonParam.s_PLC.Write_Register_Value(1, 2300, "1", "short");

        }

        #region 用户管理操作
        private void btn_AddUser_Click(object sender, EventArgs e)
        {
            AddUserFrm addUserFrm = new AddUserFrm();
            if (addUserFrm.ShowDialog() == DialogResult.OK)
            {
                //弹窗确认成功
                //新增
                try
                {
                    string sql = @"INSERT INTO UserT
                        (UserName,PassWord,Class,State,DateCreated)
                        VALUES(@UserName,@PassWord,@Class,@State,@DateCreated)";

                    SqlParameter[] paras =
                    {
                        new SqlParameter("@UserName", addUserFrm.user.UserName),
                        new SqlParameter("@PassWord", addUserFrm.user.PassWord),
                        new SqlParameter("@Class", addUserFrm.user.Class),
                        new SqlParameter("@State", addUserFrm.user.State),
                        new SqlParameter("@DateCreated",DateTime.Today.ToString("yyyy/MM/dd"))
                    };
                    SqlHelper.ExecuteNonQuery(sql, paras);
                }
                catch (SqlException ex)
                {
                    //UserName唯一重复捕获
                    if (ex.Number == 2627)
                    {
                        MessageBox.Show("该用户名已经存在");
                    }
                    else
                    {
                        MessageBox.Show("新增失败：" + ex.Message);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("异常：" + ex.Message);
                }

                addUserFrm.Close();
                UserLoadDataToDgv();
            }
        }
        /// <summary>
        /// 修改用户信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_RevampUser_Click(object sender, EventArgs e)
        {
            //判断是否选中一行
            if (dgv_User.SelectedRows.Count == 0)
            {
                MessageBox.Show("请选中一行用户数据");
                return;
            }
            //拿到选中行
            DataGridViewRow row = dgv_User.SelectedRows[0];
            Common.User editUser = new Common.User();
            //读取数据库到用户对象
            //SQL按主键UserName查询全部字段
            string sql = @"SELECT UserName,PassWord,Class,State,DateCreated FROM UserT WHERE UserName=@UserName";
            SqlParameter[] paras =
            {
                new SqlParameter("@UserName", SqlDbType.NVarChar) { Value = row.Cells["UserName"].Value.ToString() }
            };
            //调用EntireRow方法
            object[] rowArr = SqlHelper.EntireRow(sql, paras);
            if (rowArr != null)
            {
                editUser.UserName = rowArr[0].ToString();
                editUser.PassWord = rowArr[1].ToString();
                editUser.Class = rowArr[2].ToString();
                object objState = rowArr[3];
                if (objState is System.Data.SqlTypes.SqlBoolean sqlBool)
                {
                    editUser.State = sqlBool.Value;
                }
                else
                {
                    editUser.State = Convert.ToBoolean(objState);
                }
                editUser.DateCreated = Convert.ToDateTime(rowArr[4].ToString());
            }
            else
            {
                MessageBox.Show("该用户不存在");
            }
            //实例化修改窗体，把选中的用户对象传进去
            EditUserFrm editFrm = new EditUserFrm(editUser);
            if (editFrm.ShowDialog() == DialogResult.OK)
            {
                // 更新：权限、状态、密码
                sql = @"UPDATE UserT 
                SET Class=@Class, State=@State, PassWord=@PassWord
                WHERE UserName=@UserName";

                paras = new SqlParameter[]
                {
                    new SqlParameter("@UserName", SqlDbType.NVarChar) {Value = editFrm._user.UserName},
                    new SqlParameter("@Class", SqlDbType.NVarChar) {Value = editFrm._user.Class},
                    new SqlParameter("@State", SqlDbType.Bit) {Value = editFrm._user.State},
                    new SqlParameter("@PassWord", SqlDbType.NVarChar) {Value = editFrm._user.PassWord}
                };
                SqlHelper.ExecuteNonQuery(sql, paras);
                editFrm.Close();
                UserLoadDataToDgv();
            }
        }
        private void btn_UserDelete_Click(object sender, EventArgs e)
        {
            //判断是否选中一行
            if (dgv_User.SelectedRows.Count == 0)
            {
                MessageBox.Show("请选中一行用户数据");
                return;
            }
            //拿到选中行
            DataGridViewRow row = dgv_User.SelectedRows[0];
            if (MessageBox.Show($"确定要删除用户[{row.Cells["UserName"].Value.ToString()}]吗？", "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                string sql = @"DELETE FROM UserT WHERE UserName=@UserName";
                SqlParameter[] pms =
                {
                    new SqlParameter("@UserName", SqlDbType.NVarChar){ Value = row.Cells["UserName"].Value.ToString()}
                };
                int rows = SqlHelper.ExecuteNonQuery(sql, pms);
                if (rows > 0)
                {
                    MessageBox.Show("删除成功");
                    UserLoadDataToDgv();
                }
                else
                {
                    MessageBox.Show("删除失败，未找到该用户");
                }
            }

        }
        #endregion

        #region 报警
        private bool LoadAlarmInfo_PLC()
        {
            try
            {
                string filePath = Path.GetDirectoryName(Application.ExecutablePath);
                filePath = filePath + "\\AlarmInfo\\AlarmInfo_PLC.xls";
                DataTable dataTable = ExcelHelper.ReadExcelToDataTable(filePath, "SevereAlarmInfo");
                AlarmManager.SevereAlarmInfoDic_PLC.Clear();
                AlarmManager.MildAlarmInfoDic_PLC.Clear();
                int index = 0;
                List<string> AlarmTypeList = new List<string>();
                AlarmTypeList.Add("报警类型");
                //遍历每一行工单
                foreach (DataRow row in dataTable.Rows)
                {
                    AlarmManager.SevereAlarmInfoDic_PLC.Add(index, new AlarmInfo());
                    AlarmInfo alarmInfo = new AlarmInfo()
                    {
                        AlarmNumber = row["AlarmNumber"]?.ToString(),
                        DeviceNumber = row["DeviceNumber"]?.ToString(),
                        DeviceName = row["DeviceName"]?.ToString(),
                        AlarmCode = row["AlarmCode"]?.ToString(),
                        AlarmCategory = row["AlarmCategory"]?.ToString(),
                        AlarmDescription = row["AlarmDescription"]?.ToString(),
                        AlarmLevel = row["AlarmLevel"]?.ToString(),
                        Operator = row["Operator"]?.ToString(),
                        TreatmentMeasure = row["TreatmentMeasure"]?.ToString(),
                        Status = row["Status"]?.ToString(),
                        TriggeringTime = row["TriggeringTime"]?.ToString(),
                        RecoveryTime = row["RecoveryTime"]?.ToString(),
                        Remark = row["Remark"]?.ToString(),

                    };
                    AlarmManager.SevereAlarmInfoDic_PLC[index] = alarmInfo;
                    index++;
                    string alarmCategory = alarmInfo.AlarmCategory;
                    if (!string.IsNullOrWhiteSpace(alarmCategory) && !AlarmTypeList.Contains(alarmCategory))
                    {
                        AlarmTypeList.Add(alarmCategory);
                    }
                }
                DataTable dataTable2 = ExcelHelper.ReadExcelToDataTable(filePath, "MildAlarmInfo");
                index = 0;
                //遍历每一行工单
                foreach (DataRow row in dataTable2.Rows)
                {
                    AlarmManager.MildAlarmInfoDic_PLC.Add(index, new AlarmInfo());
                    AlarmInfo alarmInfo = new AlarmInfo()
                    {
                        AlarmNumber = row["AlarmNumber"]?.ToString(),
                        DeviceNumber = row["DeviceNumber"]?.ToString(),
                        DeviceName = row["DeviceName"]?.ToString(),
                        AlarmCode = row["AlarmCode"]?.ToString(),
                        AlarmCategory = row["AlarmCategory"]?.ToString(),
                        AlarmDescription = row["AlarmDescription"]?.ToString(),
                        AlarmLevel = row["AlarmLevel"]?.ToString(),
                        Operator = row["Operator"]?.ToString(),
                        TreatmentMeasure = row["TreatmentMeasure"]?.ToString(),
                        Status = row["Status"]?.ToString(),
                        TriggeringTime = row["TriggeringTime"]?.ToString(),
                        RecoveryTime = row["RecoveryTime"]?.ToString(),
                        Remark = row["Remark"]?.ToString(),

                    };
                    AlarmManager.MildAlarmInfoDic_PLC[index] = alarmInfo;
                    index++;
                    string alarmCategory = alarmInfo.AlarmCategory;
                    if (!string.IsNullOrWhiteSpace(alarmCategory) && !AlarmTypeList.Contains(alarmCategory))
                    {
                        AlarmTypeList.Add(alarmCategory);
                    }
                }
                cb_AlarmRecordTypeScreen.Items.Clear();
                cb_AlarmRecordTypeScreen.Items.AddRange(AlarmTypeList.ToArray());
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
        private System.Windows.Forms.Timer _scanPLCAlarmTimer;
        private List<AlarmInfo> _activeAlarmList = new List<AlarmInfo>();
        private void ScanPLCAlarmTimer_Tick(object sender, EventArgs e)
        {
            _activeAlarmList.Clear();
            if (!_isConnectedPLC)
                return;
            //严重报警
            short[] registerValues_200_219 = CommonParam.s_PLC.Read_Register_State_Short(1, 200, 20);
            if (registerValues_200_219 != null)
            {
                for (int wordIndex = 0; wordIndex < registerValues_200_219.Length; wordIndex++)
                {
                    short wordVal = registerValues_200_219[wordIndex];
                    for (int bitIndex = 0; bitIndex < 16; bitIndex++)
                    {
                        if ((wordVal & (1 << bitIndex)) != 0)
                        {
                            int alarmIndex = bitIndex + wordIndex * 16;
                            if (AlarmManager.SevereAlarmInfoDic_PLC.ContainsKey(alarmIndex))
                            {
                                _activeAlarmList.Add(AlarmManager.SevereAlarmInfoDic_PLC[alarmIndex]);
                            }
                        }
                    }
                }
            }
            
            //轻度报警
            short[] registerValues_250_269 = CommonParam.s_PLC.Read_Register_State_Short(1, 250, 20);
            if (registerValues_250_269 != null)
            {
                for (int wordIndex = 0; wordIndex < registerValues_250_269.Length; wordIndex++)
                {
                    short wordVal = registerValues_250_269[wordIndex];
                    for (int bitIndex = 0; bitIndex < 16; bitIndex++)
                    {
                        if ((wordVal & (1 << bitIndex)) != 0)
                        {
                            int alarmIndex = bitIndex + wordIndex * 16;
                            if (AlarmManager.MildAlarmInfoDic_PLC.ContainsKey(alarmIndex))
                            {
                                _activeAlarmList.Add(AlarmManager.MildAlarmInfoDic_PLC[alarmIndex]);
                            }
                        }
                    }
                }
            }   
            AlarmManager.Instance.AlarmTrigger(_activeAlarmList);
        }
        /// <summary>刷新实时报警DGV</summary>
        private void RefreshLiveAlarmDgvAndTb()
        {
            if (dgv_ActiveAlarm.IsDisposed || tb_AlarmDisplay.IsDisposed)
                return;
            // 拿到未复位报警列表
            List<AlarmInfo> list = AlarmManager.Instance.GetAllLiveAlarms();

            // 先置null解除绑定，再重新赋值，界面才会更新
            dgv_ActiveAlarm.DataSource = null;
            dgv_ActiveAlarm.DataSource = new List<AlarmInfo>(list);
            if (list != null && list.Count > 0)
            {
                //报警信息
                string allAlarmText = string.Join(Environment.NewLine, list.Select(x => $"{x.AlarmCode}：{x.AlarmDescription}"));
                UpdateAlarmTextBox(allAlarmText, true);
            }
            else
            {
                UpdateAlarmTextBox("无报警", false);
            }

        }
        //查询报警记录
        private void btn_AlarmRecordInquire_Click(object sender, EventArgs e)
        {
            DateTime startTime = dtp_AlarmRecordStartingTimeScreen.Value;
            DateTime endTime = dtp_AlarmRecordEndTimeScreen.Value;

            if (startTime > endTime)
            {
                MessageBox.Show("开始时间大于结束时间，筛选错误");
                return;
            }
            string level = cb_AlarmRecordClassScreen.Text;
            string category = cb_AlarmRecordTypeScreen.Text;
            StringBuilder sqlSb = new StringBuilder();
            sqlSb.Append("SELECT * FROM AlarmRecordT WHERE 1=1 ");
            List<SqlParameter> paramList = new List<SqlParameter>();
            // 条件1：时间范围
            sqlSb.Append(" AND TRY_CONVERT(datetime,[Triggering Time]) BETWEEN @StartTime AND @EndTime ");
            paramList.Add(new SqlParameter("@StartTime", startTime));
            paramList.Add(new SqlParameter("@EndTime", endTime));
            //条件2：报警等级
            if (level != "报警等级")
            {
                sqlSb.Append(" AND [Alarm Level] = @AlarmLevel ");
                paramList.Add(new SqlParameter("@AlarmLevel", level));
            }
            //条件3：报警类型
            if (category != "报警类型")
            {
                sqlSb.Append(" AND [Alarm Category] = @AlarmCategory ");
                paramList.Add(new SqlParameter("@AlarmCategory", category));
            }
            //加排序，最新的放第一行
            sqlSb.Append(" ORDER BY TRY_CONVERT(datetime,[Triggering Time]) DESC ");
            AlarmRecordsLoadDataToDgv(sqlSb.ToString(), paramList);
        }
        private void btn_ExportAlarmTable_Click(object sender, EventArgs e)
        {
            try
            {
                DataTable dt = (DataTable)dgv_AlarmRecords.DataSource;
                // 桌面路径
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string fileName = $"报警记录_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                string savePath = Path.Combine(desktopPath, fileName);
                ExcelHelper.ExportDataTableToExcel(dt, savePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        /// <summary>
        /// 更新报警文本框，跨线程安全
        /// </summary>
        private void UpdateAlarmTextBox(string alarmMsg, bool hasAlarm)
        {
            if (tb_AlarmDisplay.IsDisposed) return;

            if (tb_AlarmDisplay.InvokeRequired)
            {
                tb_AlarmDisplay.Invoke(new Action(() =>
                {
                    UpdateAlarmTextBox(alarmMsg, hasAlarm);
                }));
                return;
            }

            tb_AlarmDisplay.Text = hasAlarm ? alarmMsg : "无报警";
        }
        #endregion

        #region 生产记录
        /// <summary>
        /// 生产记录筛选
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_ProductionRecordInquire_Click(object sender, EventArgs e)
        {
            DateTime startTime = dtp_ProductionRecordStartingTimeScreen.Value;
            DateTime endTime = dtp_ProductionRecordEndTimeScreen.Value;

            if (startTime > endTime)
            {
                MessageBox.Show("开始时间大于结束时间，筛选错误");
                return;
            }
            string rackID = tb_ProductionRecordRackID.Text;
            string tubelID = tb_ProductionRecordTubelID.Text;
            StringBuilder sqlSb = new StringBuilder();
            sqlSb.Append("SELECT * FROM ProductionRecordsT WHERE 1=1 ");
            List<SqlParameter> paramList = new List<SqlParameter>();
            // 条件1：时间范围
            sqlSb.Append(" AND TRY_CONVERT(datetime,[Tube_Loading_Time]) BETWEEN @StartTime AND @EndTime ");
            paramList.Add(new SqlParameter("@StartTime", startTime));
            paramList.Add(new SqlParameter("@EndTime", endTime));
            //条件2
            if (rackID != "")
            {
                sqlSb.Append(" AND [Rack_ID] = @Rack_ID ");
                paramList.Add(new SqlParameter("@Rack_ID", rackID));
            }
            //条件3
            if (tubelID != "")
            {
                sqlSb.Append(" AND [Tube_ID] = @Tube_ID ");
                paramList.Add(new SqlParameter("@Tube_ID", tubelID));
            }
            //加排序，最新的放第一行
            sqlSb.Append(" ORDER BY TRY_CONVERT(datetime,[Tube_Loading_Time]) DESC ");
            ProductionRecordsLoadDataToDgv(sqlSb.ToString(), paramList);
        }
        /// <summary>
        /// 导出生产记录
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_ExportProductionTable_Click(object sender, EventArgs e)
        {
            try
            {
                DataTable dt = (DataTable)dgv_ProductionRecords.DataSource;
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string fileName = $"生产记录_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                string savePath = Path.Combine(desktopPath, fileName);
                ExcelHelper.ExportDataTableToExcel(dt, savePath);
                MessageBox.Show("生产记录导出成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        #endregion

        #region 耗材堆栈
        /// <summary>
        /// 更新耗材数量
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_UpdateConsumableQuantity_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show($"人工确认界面的耗材数量与实际数量是否一致", "确认更新", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                CommonParam.s_Consumableitems.ElisaPlateCount = IntegerUpDown_ElisaPlateCount.Value;
                CommonParam.s_Consumableitems.Tip1Count_50ul = IntegerUpDown_tip1Count_50ul.Value;
                CommonParam.s_Consumableitems.Tip2Count_50ul = IntegerUpDown_tip2Count_50ul.Value;
                CommonParam.s_Consumableitems.Tip3Count_50ul = IntegerUpDown_tip3Count_50ul.Value;
                CommonParam.s_Consumableitems.Tip4Count_50ul = IntegerUpDown_tip4Count_50ul.Value;
                CommonParam.s_Consumableitems.Tip5Count_50ul = IntegerUpDown_tip5Count_50ul.Value;
                CommonParam.s_Consumableitems.Tip1Count_10ul = IntegerUpDown_tip1Count_10ul.Value;
                CommonParam.s_Consumableitems.Tip2Count_10ul = IntegerUpDown_tip2Count_10ul.Value;
                CommonParam.s_Consumableitems.Tip3Count_10ul = IntegerUpDown_tip3Count_10ul.Value;
                CommonParam.s_Consumableitems.Tip4Count_10ul = IntegerUpDown_tip4Count_10ul.Value;
                CommonParam.s_Consumableitems.Tip5Count_10ul = IntegerUpDown_tip5Count_10ul.Value;
                CommonParam.s_Consumableitems.Tip6Count_10ul = IntegerUpDown_tip6Count_10ul.Value;
                CommonParam.s_Consumableitems.Tip7Count_10ul = IntegerUpDown_tip7Count_10ul.Value;
                CommonParam.s_Consumableitems.Tip8Count_10ul = IntegerUpDown_tip8Count_10ul.Value;
            }
            else
            {
                IntegerUpDown_ElisaPlateCount.Value = CommonParam.s_Consumableitems.ElisaPlateCount;
                IntegerUpDown_tip1Count_50ul.Value = CommonParam.s_Consumableitems.Tip1Count_50ul;
                IntegerUpDown_tip2Count_50ul.Value = CommonParam.s_Consumableitems.Tip2Count_50ul;
                IntegerUpDown_tip3Count_50ul.Value = CommonParam.s_Consumableitems.Tip3Count_50ul;
                IntegerUpDown_tip4Count_50ul.Value = CommonParam.s_Consumableitems.Tip4Count_50ul;
                IntegerUpDown_tip5Count_50ul.Value = CommonParam.s_Consumableitems.Tip5Count_50ul;
                IntegerUpDown_tip1Count_10ul.Value = CommonParam.s_Consumableitems.Tip1Count_10ul;
                IntegerUpDown_tip2Count_10ul.Value = CommonParam.s_Consumableitems.Tip2Count_10ul;
                IntegerUpDown_tip3Count_10ul.Value = CommonParam.s_Consumableitems.Tip3Count_10ul;
                IntegerUpDown_tip4Count_10ul.Value = CommonParam.s_Consumableitems.Tip4Count_10ul;
                IntegerUpDown_tip5Count_10ul.Value = CommonParam.s_Consumableitems.Tip5Count_10ul;
                IntegerUpDown_tip6Count_10ul.Value = CommonParam.s_Consumableitems.Tip6Count_10ul;
                IntegerUpDown_tip7Count_10ul.Value = CommonParam.s_Consumableitems.Tip7Count_10ul;
                IntegerUpDown_tip8Count_10ul.Value = CommonParam.s_Consumableitems.Tip8Count_10ul;
            }
        }
        private void Updatas_ConsumableitemsInfoToSql()
        {
            SqlHelper.SetIntValue("ElisaPlateCount", CommonParam.s_Consumableitems.ElisaPlateCount);
            SqlHelper.SetIntValue("Tip1Count_50ul", CommonParam.s_Consumableitems.Tip1Count_50ul);
            SqlHelper.SetIntValue("Tip2Count_50ul", CommonParam.s_Consumableitems.Tip2Count_50ul);
            SqlHelper.SetIntValue("Tip3Count_50ul", CommonParam.s_Consumableitems.Tip3Count_50ul);
            SqlHelper.SetIntValue("Tip4Count_50ul", CommonParam.s_Consumableitems.Tip4Count_50ul);
            SqlHelper.SetIntValue("Tip5Count_50ul", CommonParam.s_Consumableitems.Tip5Count_50ul);
            SqlHelper.SetIntValue("Tip1Count_10ul", CommonParam.s_Consumableitems.Tip1Count_10ul);
            SqlHelper.SetIntValue("Tip2Count_10ul", CommonParam.s_Consumableitems.Tip2Count_10ul);
            SqlHelper.SetIntValue("Tip3Count_10ul", CommonParam.s_Consumableitems.Tip3Count_10ul);
            SqlHelper.SetIntValue("Tip4Count_10ul", CommonParam.s_Consumableitems.Tip4Count_10ul);
            SqlHelper.SetIntValue("Tip5Count_10ul", CommonParam.s_Consumableitems.Tip5Count_10ul);
            SqlHelper.SetIntValue("Tip6Count_10ul", CommonParam.s_Consumableitems.Tip6Count_10ul);
            SqlHelper.SetIntValue("Tip7Count_10ul", CommonParam.s_Consumableitems.Tip7Count_10ul);
            SqlHelper.SetIntValue("Tip8Count_10ul", CommonParam.s_Consumableitems.Tip8Count_10ul);
            SqlHelper.SetStringValue("Buffer1_BatchNumber", CommonParam.s_Consumableitems.Buffer1_BatchNumber);
            SqlHelper.SetStringValue("Buffer1_PositionalCode", CommonParam.s_Consumableitems.Buffer1_PositionalCode);
            SqlHelper.SetStringValue("Buffer1_Name", CommonParam.s_Consumableitems.Buffer1_Name);
            SqlHelper.SetStringValue("Buffer1_Volume", CommonParam.s_Consumableitems.Buffer1_Volume);
            SqlHelper.SetStringValue("Buffer2_BatchNumber", CommonParam.s_Consumableitems.Buffer2_BatchNumber);
            SqlHelper.SetStringValue("Buffer2_PositionalCode", CommonParam.s_Consumableitems.Buffer2_PositionalCode);
            SqlHelper.SetStringValue("Buffer2_Name", CommonParam.s_Consumableitems.Buffer2_Name);
            SqlHelper.SetStringValue("Buffer2_Volume", CommonParam.s_Consumableitems.Buffer2_Volume);
            SqlHelper.SetStringValue("Neutralize1_BatchNumber", CommonParam.s_Consumableitems.Neutralize1_BatchNumber);
            SqlHelper.SetStringValue("Neutralize1_PositionalCode", CommonParam.s_Consumableitems.Neutralize1_PositionalCode);
            SqlHelper.SetStringValue("Neutralize1_Name", CommonParam.s_Consumableitems.Neutralize1_Name);
            SqlHelper.SetStringValue("Neutralize2_BatchNumber", CommonParam.s_Consumableitems.Neutralize2_BatchNumber);
            SqlHelper.SetStringValue("Neutralize2_PositionalCode", CommonParam.s_Consumableitems.Neutralize2_PositionalCode);
            SqlHelper.SetStringValue("Neutralize2_Name", CommonParam.s_Consumableitems.Neutralize2_Name);
        }
        private void Updatas_ConsumableitemsUIInfoFromSql()
        {
            CommonParam.s_Consumableitems.ElisaPlateCount = SqlHelper.GetInt("ElisaPlateCount");
            CommonParam.s_Consumableitems.Tip1Count_50ul = SqlHelper.GetInt("Tip1Count_50ul");
            CommonParam.s_Consumableitems.Tip2Count_50ul = SqlHelper.GetInt("Tip2Count_50ul");
            CommonParam.s_Consumableitems.Tip3Count_50ul = SqlHelper.GetInt("Tip3Count_50ul");
            CommonParam.s_Consumableitems.Tip4Count_50ul = SqlHelper.GetInt("Tip4Count_50ul");
            CommonParam.s_Consumableitems.Tip5Count_50ul = SqlHelper.GetInt("Tip5Count_50ul");
            CommonParam.s_Consumableitems.Tip1Count_10ul = SqlHelper.GetInt("Tip1Count_10ul");
            CommonParam.s_Consumableitems.Tip2Count_10ul = SqlHelper.GetInt("Tip2Count_10ul");
            CommonParam.s_Consumableitems.Tip3Count_10ul = SqlHelper.GetInt("Tip3Count_10ul");
            CommonParam.s_Consumableitems.Tip4Count_10ul = SqlHelper.GetInt("Tip4Count_10ul");
            CommonParam.s_Consumableitems.Tip5Count_10ul = SqlHelper.GetInt("Tip5Count_10ul");
            CommonParam.s_Consumableitems.Tip6Count_10ul = SqlHelper.GetInt("Tip6Count_10ul");
            CommonParam.s_Consumableitems.Tip7Count_10ul = SqlHelper.GetInt("Tip7Count_10ul");
            CommonParam.s_Consumableitems.Tip8Count_10ul = SqlHelper.GetInt("Tip8Count_10ul");
            IntegerUpDown_ElisaPlateCount.Value = CommonParam.s_Consumableitems.ElisaPlateCount;
            IntegerUpDown_tip1Count_50ul.Value = CommonParam.s_Consumableitems.Tip1Count_50ul;
            IntegerUpDown_tip2Count_50ul.Value = CommonParam.s_Consumableitems.Tip2Count_50ul;
            IntegerUpDown_tip3Count_50ul.Value = CommonParam.s_Consumableitems.Tip3Count_50ul;
            IntegerUpDown_tip4Count_50ul.Value = CommonParam.s_Consumableitems.Tip4Count_50ul;
            IntegerUpDown_tip5Count_50ul.Value = CommonParam.s_Consumableitems.Tip5Count_50ul;
            IntegerUpDown_tip1Count_10ul.Value = CommonParam.s_Consumableitems.Tip1Count_10ul;
            IntegerUpDown_tip2Count_10ul.Value = CommonParam.s_Consumableitems.Tip2Count_10ul;
            IntegerUpDown_tip3Count_10ul.Value = CommonParam.s_Consumableitems.Tip3Count_10ul;
            IntegerUpDown_tip4Count_10ul.Value = CommonParam.s_Consumableitems.Tip4Count_10ul;
            IntegerUpDown_tip5Count_10ul.Value = CommonParam.s_Consumableitems.Tip5Count_10ul;
            IntegerUpDown_tip6Count_10ul.Value = CommonParam.s_Consumableitems.Tip6Count_10ul;
            IntegerUpDown_tip7Count_10ul.Value = CommonParam.s_Consumableitems.Tip7Count_10ul;
            IntegerUpDown_tip8Count_10ul.Value = CommonParam.s_Consumableitems.Tip8Count_10ul;
            CommonParam.s_Consumableitems.Buffer1_BatchNumber = SqlHelper.GetString("Buffer1_BatchNumber");
            CommonParam.s_Consumableitems.Buffer1_PositionalCode = SqlHelper.GetString("Buffer1_PositionalCode");
            CommonParam.s_Consumableitems.Buffer1_Name = SqlHelper.GetString("Buffer1_Name");
            CommonParam.s_Consumableitems.Buffer1_Volume = SqlHelper.GetString("Buffer1_Volume");
            CommonParam.s_Consumableitems.Buffer2_BatchNumber = SqlHelper.GetString("Buffer2_BatchNumber");
            CommonParam.s_Consumableitems.Buffer2_PositionalCode = SqlHelper.GetString("Buffer2_PositionalCode");
            CommonParam.s_Consumableitems.Buffer2_Name = SqlHelper.GetString("Buffer2_Name");
            CommonParam.s_Consumableitems.Buffer2_Volume = SqlHelper.GetString("Buffer2_Volume");
            CommonParam.s_Consumableitems.Neutralize1_BatchNumber = SqlHelper.GetString("Neutralize1_BatchNumber");
            CommonParam.s_Consumableitems.Neutralize1_PositionalCode = SqlHelper.GetString("Neutralize1_PositionalCode");
            CommonParam.s_Consumableitems.Neutralize1_Name = SqlHelper.GetString("Neutralize1_Name");
            CommonParam.s_Consumableitems.Neutralize2_BatchNumber = SqlHelper.GetString("Neutralize2_BatchNumber");
            CommonParam.s_Consumableitems.Neutralize2_PositionalCode = SqlHelper.GetString("Neutralize2_PositionalCode");
            CommonParam.s_Consumableitems.Neutralize2_Name = SqlHelper.GetString("Neutralize2_Name");
            tb_Buffer1_BatchNumber.Text = CommonParam.s_Consumableitems.Buffer1_BatchNumber;
            tb_Buffer1_PositionalCode.Text = CommonParam.s_Consumableitems.Buffer1_PositionalCode;
            tb_Buffer1_Name.Text = CommonParam.s_Consumableitems.Buffer1_Name;
            tb_Buffer1_Volume.Text = CommonParam.s_Consumableitems.Buffer1_Volume;
            tb_Buffer2_BatchNumber.Text = CommonParam.s_Consumableitems.Buffer2_BatchNumber;
            tb_Buffer2_PositionalCode.Text = CommonParam.s_Consumableitems.Buffer2_PositionalCode;
            tb_Buffer2_Name.Text = CommonParam.s_Consumableitems.Buffer2_Name;
            tb_Buffer2_Volume.Text = CommonParam.s_Consumableitems.Buffer2_Volume;
            tb_Neutralize1_BatchNumber.Text = CommonParam.s_Consumableitems.Neutralize1_BatchNumber;
            tb_Neutralize1_PositionalCode.Text = CommonParam.s_Consumableitems.Neutralize1_PositionalCode;
            tb_Neutralize1_Name.Text = CommonParam.s_Consumableitems.Neutralize1_Name;
            tb_Neutralize2_BatchNumber.Text = CommonParam.s_Consumableitems.Neutralize2_BatchNumber;
            tb_Neutralize2_PositionalCode.Text = CommonParam.s_Consumableitems.Neutralize2_PositionalCode;
            tb_Neutralize2_Name.Text = CommonParam.s_Consumableitems.Neutralize2_Name;
        }


        #endregion

        private void dtp_AlarmRecordStartingTimeScreen_ValueChanged(object sender, DateTime value)
        {

        }

      
        /// <summary>
        /// 左紫外灯打开/关闭
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="value">判断结果返回</param>
        private void uiSwitch1_ValueChanged(object sender, bool value)
        {
            if (!_isConnectedPLC)
            {
                MessageBox.Show("PLC未连接，无法操作");
                uiSwitch1.Active = !value;
                return;
            }
            if (uiSwitch1.Active)
            {
                CommonParam.s_PLC.Write_Register_Value(1, 3100, "1", "short");
                // 启动计时
                _UVLightUsageTimer_Left.Start();
            }
            else
            {
                CommonParam.s_PLC.Write_Register_Value(1, 3100, "0", "short");
                // 暂停计时
                _UVLightUsageTimer_Left.Stop();
            }
        }

        /// <summary>
        /// 右紫外灯打开/关闭
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="value"></param>
        private void uiSwitch2_ValueChanged(object sender, bool value)
        {
            if (!_isConnectedPLC)
            {
                MessageBox.Show("PLC未连接，无法操作");
                uiSwitch2.Active = !value;
                return;
            }
            if (uiSwitch2.Active)
            {
                CommonParam.s_PLC.Write_Register_Value(1, 3102, "1", "short");
                // 启动计时
                _UVLightUsageTimer_Right.Start();
            }
            else
            {
                CommonParam.s_PLC.Write_Register_Value(1, 3102, "0", "short");
                // 暂停计时
                _UVLightUsageTimer_Right.Stop();
            }
        }
        private System.Windows.Forms.Timer _UVLightUsageTimer_Left = new System.Windows.Forms.Timer();
        private System.Windows.Forms.Timer _UVLightUsageTimer_Right = new System.Windows.Forms.Timer();
        private DateTime _UVLightStartTime_Left;
        private DateTime _UVLightStartTime_Right;
        private TimeSpan _startOffsetLeft;
        private TimeSpan _startOffsetRight;
        private void _UVLightUsageTimer_Left_Tick(object sender, EventArgs e)
        {
            // 计算从启动到现在的真实流逝时间
            TimeSpan elapsed = DateTime.Now - _UVLightStartTime_Left;
            // 计算显示时间 = 起始偏移量 + 真实流逝时间
            TimeSpan displayTime = _startOffsetLeft + elapsed;
            // 将计算出的时间格式化后，赋值给控件的 Text 属性
            uiLedStopwatch1.Text = displayTime.ToString(@"hh\:mm\:ss");
        }

        private void _UVLightUsageTimer_Right_Tick(object sender, EventArgs e)
        {
            TimeSpan elapsed = DateTime.Now - _UVLightStartTime_Right;
            TimeSpan displayTime = _startOffsetRight + elapsed;
            uiLedStopwatch2.Text = displayTime.ToString(@"hh\:mm\:ss");
        }

       
    }
}
