using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConcentrationDetectionSys.Tools
{
    /// <summary>
    /// 日志帮助类。每天 5 个文件（DEBUG/INFO/WARN/ERROR/FATAL），路径为
    /// <c>Log\年\年月\日期_级别.txt</c>。
    /// </summary>
    public static class LogHelper
    {
        private static readonly object SyncRoot = new object();
        private static string _rootDirectory;

        /// <summary>最低输出级别，低于此级别的日志不写入文件。</summary>
        public static LogLevel MinimumLevel { get; set; } = LogLevel.DEBUG;

        /// <summary>日志根目录，默认为 <c>{运行目录}\Log</c>。</summary>
        public static string RootDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(_rootDirectory))
                    _rootDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
                return _rootDirectory;
            }
            set => _rootDirectory = value ?? string.Empty;
        }

        /// <summary>写入文件后触发，供界面日志列表订阅。</summary>
        public static event Action<DateTime, LogLevel, Type, string> LogRecorded;
        public static event Action<DateTime, LogLevel, string> LogRecorded2;

        public static void WriteLog(Type sourceType, string message, LogLevel level = LogLevel.DEBUG)
        {
            WriteInternal(level, sourceType, message, null);
        }

        public static void WriteLog(Type sourceType, Exception exception, LogLevel level = LogLevel.DEBUG)
        {
            if (exception == null)
                return;
            WriteInternal(level, sourceType, exception.Message, exception);
        }

        public static void WriteLog<T>(string message, LogLevel level = LogLevel.DEBUG)
        {
            WriteInternal(level, typeof(T), message, null);
        }

        public static void Debug(Type sourceType, string message) =>
            WriteInternal(LogLevel.DEBUG, sourceType, message, null);

        public static void Info(Type sourceType, string message, bool isWrite = false)
        {
            WriteInternal(LogLevel.INFO, sourceType, message, null, isWrite);
        }
        public static void Info(string message)
        {
            WriteInternal(LogLevel.INFO, message, null);
        }

        public static void Warn(Type sourceType, string message) =>
            WriteInternal(LogLevel.WARN, sourceType, message, null);
        public static void Warn(string message) =>
            WriteInternal(LogLevel.WARN,message, null);

        public static void Error(Type sourceType, string message) =>
            WriteInternal(LogLevel.ERROR, sourceType, message, null);
        public static void Error(string message) =>
           WriteInternal(LogLevel.ERROR,message, null);

        public static void Error(Type sourceType, string message, Exception exception) =>
            WriteInternal(LogLevel.ERROR, sourceType, message, exception);

        public static void Fatal(Type sourceType, string message) =>
            WriteInternal(LogLevel.FATAL, sourceType, message, null);

        public static void Fatal(Type sourceType, string message, Exception exception) =>
            WriteInternal(LogLevel.FATAL, sourceType, message, exception);

        /// <summary>根据级别返回当日日志文件完整路径（如 <c>20260501_DEBUG.txt</c>）。</summary>
        public static string GetLogFilePath(LogLevel level, DateTime? time = null)
        {
            var now = time ?? DateTime.Now;
            string fileName = now.ToString("yyyyMMdd") + "_" + level + ".txt";
            return Path.Combine(
                RootDirectory,
                now.Year.ToString(),
                now.ToString("yyyyMM"),
                fileName);
        }

        private static bool PassesLevelFilter(LogLevel level) => level >= MinimumLevel;

        private static void WriteInternal(LogLevel level, Type sourceType, string message, Exception exception)
        {
            if (!PassesLevelFilter(level))
                return;

            var time = DateTime.Now;
            var line = FormatLine(time, level, sourceType, message, exception);

            AppendLineSafe(level, line);
            RaiseLogRecorded(time, level, sourceType, message, exception);
        }
        private static void WriteInternal(LogLevel level, string message, Exception exception)
        {
            if (!PassesLevelFilter(level))
                return;

            var time = DateTime.Now;
            var line = FormatLine(time, level, message, exception);

            AppendLineSafe(level, line);
            RaiseLogRecorded(time, level, message, exception);
        }
        private static void WriteInternal(LogLevel level, Type sourceType, string message, Exception exception, bool iswrite)
        {
            if (!PassesLevelFilter(level))
                return;
            if (!iswrite)
                return;
            var time = DateTime.Now;
            var line = FormatLine(time, level, sourceType, message, exception);

            AppendLineSafe(level, line);
            RaiseLogRecorded(time, level, sourceType, message, exception);
        }

        private static void RaiseLogRecorded(
            DateTime time,
            LogLevel level,
            Type sourceType,
            string message,
            Exception exception)
        {
            var displayMessage = exception == null ? message : message + Environment.NewLine + exception;
            var handler = LogRecorded;
            if (handler == null)
                return;
            try
            {
                handler.Invoke(time, level, sourceType, displayMessage);
            }
            catch
            {
            }
        }
        private static void RaiseLogRecorded(
           DateTime time,
           LogLevel level,
           string message,
           Exception exception)
        {
            var displayMessage = exception == null ? message : message + Environment.NewLine + exception;
            var handler = LogRecorded2;
            if (handler == null)
                return;
            try
            {
                handler.Invoke(time,level, displayMessage);
            }
            catch
            {
            }
        }
        private static string FormatLine(DateTime time, LogLevel level, Type sourceType, string message, Exception ex)
        {
            var sb = new StringBuilder();
            sb.Append(time.ToString("yyyy-MM-dd HH:mm:ss.fff")).Append('\t');
            sb.Append(level).Append('\t');
            if (sourceType != null)
                sb.Append(sourceType.FullName).Append('\t');
            sb.Append(message);
            if (ex != null)
                sb.AppendLine().Append(ex);
            return sb.ToString();
        }
        private static string FormatLine(DateTime time, LogLevel level,string message, Exception ex)
        {
            var sb = new StringBuilder();
            sb.Append(time.ToString("yyyy-MM-dd HH:mm:ss.fff")).Append('\t');
            sb.Append(level).Append('\t');
            sb.Append(message);
            if (ex != null)
                sb.AppendLine().Append(ex);
            return sb.ToString();
        }

        private static void AppendLineSafe(LogLevel level, string line)
        {
            lock (SyncRoot)
            {
                try
                {
                    string path = GetLogFilePath(level);
                    string dir = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(dir))
                        Directory.CreateDirectory(dir);
                    File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
                }
                catch
                {
                }
            }
        }
        /// <summary>
        /// 删除3个月之前的日志年月文件夹
        /// </summary>
        public static void CleanOldLogs(int keepMonthCount = 3)
        {
            lock (SyncRoot)
            {
                try
                {
                    DateTime cutoffTime = DateTime.Now.AddMonths(-keepMonthCount);

                    string logRoot = RootDirectory;
                    if (!Directory.Exists(logRoot))
                        return;

                    // 遍历所有【年份文件夹】Log\2024、Log\2025、Log\2026
                    foreach (var yearDir in Directory.GetDirectories(logRoot))
                    {
                        if (!int.TryParse(Path.GetFileName(yearDir), out int year))
                            continue;

                        // 遍历年份下所有【年月文件夹】202601、202602
                        foreach (var ymDir in Directory.GetDirectories(yearDir))
                        {
                            string dirName = Path.GetFileName(ymDir);
                            if (dirName.Length != 6) continue;
                            if (!int.TryParse(dirName, out int ymNum)) continue;

                            int dirYear = ymNum / 100;
                            int dirMonth = ymNum % 100;

                            // 解析文件夹代表的年月
                            if (dirMonth < 1 || dirMonth > 12)
                                continue;

                            DateTime folderDate = new DateTime(dirYear, dirMonth, 1);
                            // 这个文件夹早于截止时间 → 删除整个文件夹
                            if (folderDate < cutoffTime)
                            {
                                Directory.Delete(ymDir, recursive: true);
                            }
                        }

                        // 可选：年份文件夹空了之后，把空的年文件夹也删掉
                        if (!Directory.EnumerateDirectories(yearDir).Any())
                        {
                            Directory.Delete(yearDir);
                        }
                    }
                }
                catch
                {
                    // 异常静默捕获，不影响主程序，也可以打日志
                }
            }
        }
    }

    /// <summary>日志级别（数值越大越严重）。</summary>
    public enum LogLevel
    {
        DEBUG = 0,
        INFO = 10,
        WARN = 20,
        ERROR = 30,
        FATAL = 40,
    }
}
