using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConcentrationDetectionSys.Common
{
    public static class LogEventManager
    {
        public static event Action<int, string> LogTriggered;
        /// <summary>
        /// 30-报警 31-普通 32-警告
        /// 日志事件触发器，调用此方法会触发 LogTriggered 事件，
        /// 并传递日志等级和日志信息。
        /// 订阅此事件的组件可以根据需要更新界面或执行其他操作。
        /// </summary>
        /// <param name="index">日志等级</param>
        /// <param name="log">日志信息</param>
        public static void LogEvent(int index, string log)
        {
            LogTriggered?.Invoke(index, log);
        }
    }
}
