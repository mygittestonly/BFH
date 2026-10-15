using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using static System.Net.Mime.MediaTypeNames;

namespace ConcentrationDetectionSys.Bll
{
    public class ProcessScheme
    {
        /// <summary>配方名称（方案名称）</summary>
        public string SchemeName { get; set; }
        /// <summary>创建时间</summary>
        public DateTime CreateTime { get; set; }
        /// <summary>整套工艺流程步骤</summary>
        public List<BaseStep> Steps { get; set; } = new List<BaseStep>();
    }
  
    public static class ProjectManagement
    {
        // 单文件路径：所有配方全部放在这一个json里面
        public static readonly string AllSchemeFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemes", "schemes.json");

        // 文件锁，防止多线程读写损坏文件
        private static readonly object _fileLock = new object();

        // Json配置：开启TypeNameHandling，解决多态工序子类丢失问题
        private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.All,
            Formatting = Newtonsoft.Json.Formatting.Indented //美化json，方便查看
        };

        /// <summary>加载全部配方列表</summary>
        public static List<ProcessScheme> LoadAllSchemes()
        {
            lock (_fileLock)
            {
                try
                {
                    if (!File.Exists(AllSchemeFile))
                        return new List<ProcessScheme>();

                    string jsonText = File.ReadAllText(AllSchemeFile);
                    var list = JsonConvert.DeserializeObject<List<ProcessScheme>>(jsonText, _jsonSettings);
                    return list ?? new List<ProcessScheme>();
                }
                catch (Exception ex)
                {
                    // 生产环境这里写入log4net日志 ex.Message
                    return new List<ProcessScheme>();
                }
            }
        }

        /// <summary>保存完整配方列表，覆盖写入单文件</summary>
        public static void SaveAllSchemes(List<ProcessScheme> schemeList)
        {
            lock (_fileLock)
            {
                // 先创建文件夹
                string dir = Path.GetDirectoryName(AllSchemeFile);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string json = JsonConvert.SerializeObject(schemeList, _jsonSettings);
                File.WriteAllText(AllSchemeFile, json);
            }
        }

        /// <summary>新增一个配方，名称不能重复</summary>
        /// <returns>成功返回true；重名返回false</returns>
        public static bool AddNewScheme(ProcessScheme newScheme)
        {
            lock (_fileLock)
            {
                List<ProcessScheme> all = LoadAllSchemes();

                // 判断配方名称重复（忽略大小写）
                bool exists = all.Any(s => string.Equals(s.SchemeName, newScheme.SchemeName, StringComparison.OrdinalIgnoreCase));
                if (exists)
                    return false;

                newScheme.CreateTime = DateTime.Now;
                all.Add(newScheme);
                SaveAllSchemes(all);
                return true;
            }
        }

        /// <summary>修改保存一个已存在的配方（覆盖旧配方）</summary>
        public static bool UpdateScheme(ProcessScheme editScheme)
        {
            lock (_fileLock)
            {
                List<ProcessScheme> all = LoadAllSchemes();
                int index = all.FindIndex(s => string.Equals(s.SchemeName, editScheme.SchemeName, StringComparison.OrdinalIgnoreCase));
                if (index < 0)
                    return false;

                editScheme.CreateTime = DateTime.Now;
                all[index] = editScheme;
                SaveAllSchemes(all);
                return true;
            }
        }

        /// <summary>按名称删除配方</summary>
        public static void DeleteSchemeByName(string schemeName)
        {
            lock (_fileLock)
            {
                List<ProcessScheme> all = LoadAllSchemes();
                all.RemoveAll(s => s.SchemeName == schemeName);
                SaveAllSchemes(all);
            }
        }

        /// <summary>根据配方名字读取单个配方</summary>
        public static ProcessScheme GetSingleScheme(string schemeName)
        {
            var all = LoadAllSchemes();
            return all.FirstOrDefault(s => s.SchemeName == schemeName);
        }
    }

}
