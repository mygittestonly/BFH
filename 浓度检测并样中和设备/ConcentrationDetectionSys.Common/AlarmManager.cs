using ConcentrationDetectionSys.DAL;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ConcentrationDetectionSys.Common
{

   /// <summary>
   /// 报警信息
   /// </summary>
    public class AlarmInfo
    {
        /// <summary>
        /// 报警编号
        /// </summary>
        public string AlarmNumber { get; set; }
        /// <summary>
        /// 设备编号
        /// </summary>
        public string DeviceNumber { get; set; } = "OP011";
        /// <summary>
        /// 设备名称
        /// </summary>
        public string DeviceName { get; set; } = "测浓合并平台";
        /// <summary>
        /// 报警代码
        /// </summary>
        public string AlarmCode { get; set; }
        /// <summary>
        /// 报警类别
        /// </summary>
        public string AlarmCategory { get; set; }
        /// <summary>
        /// 报警描述
        /// </summary>
        public string AlarmDescription { get; set; }
        /// <summary>
        /// 报警级别
        /// </summary>
        public string AlarmLevel { get; set; }
        /// <summary>
        /// 操作人
        /// </summary>
        public string Operator { get; set; }
        /// <summary>
        /// 处理措施
        /// </summary>
        public string TreatmentMeasure { get; set; } = "维修操作指引";
        /// <summary>
        /// Status
        /// </summary>
        public string Status { get; set; }
        /// <summary>
        /// 触发时间
        /// </summary>
        public string TriggeringTime { get; set; }
        /// <summary>
        /// 恢复时间
        /// </summary>
        public string RecoveryTime { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }
    }
    public class AlarmManager
    {
        //PLC严重报警字典
        public static readonly Dictionary<int, AlarmInfo> SevereAlarmInfoDic_PLC = new Dictionary<int, AlarmInfo>();
        //PLC轻度报警字典
        public static readonly Dictionary<int, AlarmInfo> MildAlarmInfoDic_PLC = new Dictionary<int, AlarmInfo>();
        // 存储plc当前报警List
        private readonly List<AlarmInfo> _liveAlarmList_plc = new List<AlarmInfo>();
        private readonly List<AlarmInfo> _liveAlarmList_software = new List<AlarmInfo>();
        // 单例
        public static AlarmManager Instance { get; } = new AlarmManager();
        private readonly object _alarmLock = new object();
        // 线程安全队列：缓存报警，批量入库，减少数据库IO
        private readonly ConcurrentQueue<AlarmInfo> _alarmQueue = new ConcurrentQueue<AlarmInfo>();
        private Task _consumeTask;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        public event Action OnAlarmChanged;
        private AlarmManager()
        {
            // 启动后台消费任务
            _consumeTask = Task.Run(ConsumeAlarmQueue, _cts.Token);
        }
        // 报警触发
        public void AlarmTrigger(List<AlarmInfo> activeAlarmList)
        {
            lock (_alarmLock)
            {
                // 提取本轮所有激活报警Code集合
                HashSet<string> activeCodes = new HashSet<string>(activeAlarmList.Select(x => x.AlarmCode));
                var needResetAlarms = _liveAlarmList_plc
                    .Where(item => !activeCodes.Contains(item.AlarmCode))
                    .ToList();
                //内存中存在，但不在本轮激活集合需要复位
                foreach (var alarmItem in needResetAlarms)
                {
                    alarmItem.RecoveryTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                    alarmItem.Status = "已复位";
                    _liveAlarmList_plc.Remove(alarmItem);
                    _alarmQueue.Enqueue(alarmItem);
                }
                // 新增本轮激活报警：内存不存在才添加
                foreach (var activeAlarm in activeAlarmList)
                {
                    bool exist = _liveAlarmList_plc.Any(x => x.AlarmCode == activeAlarm.AlarmCode);
                    if (!exist)
                    {
                        activeAlarm.TriggeringTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                        activeAlarm.Status = "未复位";
                        _liveAlarmList_plc.Add(activeAlarm);
                    }
                }
                OnAlarmChanged?.Invoke();
            }
        }
        //外部添加一条报警
        public void AddAlarm(AlarmInfo alarm)
        {
            if (alarm == null) return;
            alarm.TriggeringTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            alarm.Status = "未复位";
            _liveAlarmList_software.Add(alarm);
            OnAlarmChanged?.Invoke();
        }
        //外部复位一条报警
        public void RestAlarm(AlarmInfo alarm)
        {
            if (alarm == null) return;
            alarm.RecoveryTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            alarm.Status = "已复位";
            _liveAlarmList_software.Remove(alarm);
            _alarmQueue.Enqueue(alarm);
            OnAlarmChanged?.Invoke();
        }

        // 获取当前所有未复位报警
        public List<AlarmInfo> GetAllLiveAlarms()
        {
            return _liveAlarmList_plc.Concat(_liveAlarmList_software).ToList();
        }
        /// <summary>后台循环消费队列，写入数据库</summary>
        private async Task ConsumeAlarmQueue()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    if (_alarmQueue.TryDequeue(out AlarmInfo alarm))
                    {
                        InsertAlarmSingle(alarm);
                    }
                    else
                    {
                        // 队列为空，休眠500ms降低CPU占用
                        await Task.Delay(500, _cts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // 写入失败：这里可以写入本地日志文件兜底
                    System.Diagnostics.Trace.WriteLine($"报警入库异常：{ex.Message}");
                    await Task.Delay(200, _cts.Token);
                }
            }
        }
        /// <summary>单条插入SQL</summary>
        private void InsertAlarmSingle(AlarmInfo alarm)
        {
            //新增一条报警记录
            string sql = @"INSERT INTO AlarmRecordT
                        ([Alarm Number],[Device Number],[Device Name],[Alarm Code],[Alarm Category],[Alarm Description],[Alarm Level],Operator,
                        [Treatment Measure],Status,[Triggering Time],[Recovery Time],Remark)
                        VALUES(@AlarmNumber,@DeviceNumber,@DeviceName,@AlarmCode,@AlarmCategory,@AlarmDescription,@AlarmLevel,@Operator,
                               @TreatmentMeasure,@Status,@TriggeringTime,@RecoveryTime,@Remark)";

            SqlParameter[] paras =
            {
                new SqlParameter("@AlarmNumber", alarm.AlarmNumber),
                new SqlParameter("@DeviceNumber", alarm.DeviceNumber),
                new SqlParameter("@DeviceName", alarm.DeviceName),
                new SqlParameter("@AlarmCode", alarm.AlarmCode),
                new SqlParameter("@AlarmCategory",alarm.AlarmCategory),
                new SqlParameter("@AlarmDescription",alarm.AlarmDescription),
                new SqlParameter("@AlarmLevel", alarm.AlarmLevel),
                new SqlParameter("@Operator", alarm.Operator),
                new SqlParameter("@TreatmentMeasure",alarm.TreatmentMeasure),
                new SqlParameter("@Status", alarm.Status),
                new SqlParameter("@TriggeringTime",alarm.TriggeringTime),
                new SqlParameter("@RecoveryTime",alarm.RecoveryTime),
                new SqlParameter("@Remark", alarm.Remark),
            };
            SqlHelper.ExecuteNonQuery(sql, paras);
        }

        /// <summary>程序退出时释放资源，在FormClosing调用</summary>
        public void Stop()
        {
            _cts.Cancel();
            try
            {
                _consumeTask.Wait(1000);
            }
            catch { }
            _cts.Dispose();
        }
    }
}

