using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ConcentrationDetectionSys.Tools
{
    
        
    /// <summary>
    /// Excel操作帮助类（基于NPOI）
    /// 支持 .xls (Excel 97-2003) 和 .xlsx (Excel 2007+)
    /// </summary>
    public static class ExcelHelper
    {
        #region 读取Excel → DataTable

        /// <summary>
        /// 读取Excel文件，返回DataTable（默认读取第一个Sheet，第一行为表头）
        /// </summary>
        /// <param name="filePath">Excel文件路径</param>
        /// <param name="sheetIndex">工作表索引，默认0</param>
        /// <param name="firstRowAsHeader">第一行是否作为列名，默认true</param>
        /// <returns>DataTable</returns>
        public static DataTable ReadExcelToDataTable(string filePath, int sheetIndex = 0, bool firstRowAsHeader = true)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"文件不存在：{filePath}");

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                IWorkbook workbook = CreateWorkbook(fs, filePath);
                ISheet sheet = workbook.GetSheetAt(sheetIndex);
                if (sheet == null)
                    throw new Exception($"未找到索引为 {sheetIndex} 的工作表");

                return SheetToDataTable(sheet, firstRowAsHeader);
            }
        }

        /// <summary>
        /// 读取Excel文件，返回DataTable（指定Sheet名称）
        /// </summary>
        public static DataTable ReadExcelToDataTable(string filePath, string sheetName, bool firstRowAsHeader = true)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"文件不存在：{filePath}");
            
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                IWorkbook workbook = CreateWorkbook(fs, filePath);
                ISheet sheet = workbook.GetSheet(sheetName);
                if (sheet == null)
                {
                    // 如果未找到指定Sheet，尝试获取第一个
                    sheet = workbook.GetSheetAt(0);
                }

                return SheetToDataTable(sheet, firstRowAsHeader);
            }
        }

        /// <summary>
        /// 读取Excel所有Sheet，返回DataSet
        /// </summary>
        public static DataSet ReadExcelToDataSet(string filePath, bool firstRowAsHeader = true)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"文件不存在：{filePath}");

            DataSet ds = new DataSet();

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                IWorkbook workbook = CreateWorkbook(fs, filePath);

                for (int i = 0; i < workbook.NumberOfSheets; i++)
                {
                    ISheet sheet = workbook.GetSheetAt(i);
                    DataTable dt = SheetToDataTable(sheet, firstRowAsHeader);
                    dt.TableName = sheet.SheetName;
                    ds.Tables.Add(dt);
                }
            }

            return ds;
        }

        /// <summary>
        /// 将Sheet转换为DataTable
        /// </summary>
        private static DataTable SheetToDataTable(ISheet sheet, bool firstRowAsHeader)
        {
            DataTable dt = new DataTable();

            // 获取第一行（用于列名或判断列数）
            IRow headerRow = sheet.GetRow(0);
            if (headerRow == null)
                return dt;

            int columnCount = headerRow.LastCellNum;

            // 设置列名
            int startRow = 0;
            if (firstRowAsHeader)
            {
                for (int col = 0; col < columnCount; col++)
                {
                    ICell cell = headerRow.GetCell(col);
                    string columnName = cell?.ToString() ?? $"Column{col + 1}";
                    // 处理重复列名
                    if (dt.Columns.Contains(columnName))
                        columnName = $"{columnName}_{col}";
                    dt.Columns.Add(columnName);
                }
                startRow = 1;
            }
            else
            {
                for (int col = 0; col < columnCount; col++)
                {
                    dt.Columns.Add($"Column{col + 1}");
                }
            }

            // 读取数据行
            for (int rowIdx = startRow; rowIdx <= sheet.LastRowNum; rowIdx++)
            {
                IRow row = sheet.GetRow(rowIdx);
                if (row == null) continue;

                // 判断是否为空行（所有单元格为空）
                bool isEmptyRow = true;
                for (int col = 0; col < columnCount; col++)
                {
                    ICell cell = row.GetCell(col);
                    if (cell != null && !string.IsNullOrEmpty(cell.ToString()))
                    {
                        isEmptyRow = false;
                        break;
                    }
                }
                if (isEmptyRow) continue;

                DataRow dr = dt.NewRow();
                for (int col = 0; col < columnCount; col++)
                {
                    ICell cell = row.GetCell(col);
                    dr[col] = GetCellValue(cell);
                }
                dt.Rows.Add(dr);
            }

            return dt;
        }

        /// <summary>
        /// 获取单元格值（自动处理日期、数字等类型）
        /// </summary>
        private static object GetCellValue(ICell cell)
        {
            if (cell == null)
                return DBNull.Value;

            switch (cell.CellType)
            {
                case CellType.Numeric:
                    // 判断是否为日期类型
                    if (DateUtil.IsCellDateFormatted(cell))
                    {
                        return cell.DateCellValue;
                    }
                    return cell.NumericCellValue;

                case CellType.String:
                    return cell.StringCellValue;

                case CellType.Boolean:
                    return cell.BooleanCellValue;

                case CellType.Formula:
                    try
                    {
                        // 尝试获取公式计算结果
                        switch (cell.CachedFormulaResultType)
                        {
                            case CellType.Numeric:
                                if (DateUtil.IsCellDateFormatted(cell))
                                    return cell.DateCellValue;
                                return cell.NumericCellValue;
                            case CellType.String:
                                return cell.StringCellValue;
                            case CellType.Boolean:
                                return cell.BooleanCellValue;
                            default:
                                return cell.ToString();
                        }
                    }
                    catch
                    {
                        return cell.ToString();
                    }

                case CellType.Blank:
                    return DBNull.Value;

                default:
                    return cell.ToString();
            }
        }

        #endregion

        #region 导出 DataTable → Excel

        /// <summary>
        /// 将DataTable导出到Excel文件
        /// </summary>
        /// <param name="dt">数据源</param>
        /// <param name="filePath">目标文件路径</param>
        /// <param name="sheetName">工作表名称，默认Sheet1</param>
        /// <param name="includeHeader">是否包含表头，默认true</param>
        /// <param name="autoFitColumn">是否自动调整列宽，默认true</param>
        /// <returns>导出的行数</returns>
        public static int ExportDataTableToExcel(DataTable dt, string filePath, string sheetName = "Sheet1",
            bool includeHeader = true, bool autoFitColumn = true)
        {
            if (dt == null || dt.Rows.Count == 0)
                throw new ArgumentException("数据源为空，无法导出");

            IWorkbook workbook = CreateWorkbookByExtension(filePath);
            ISheet sheet = workbook.CreateSheet(sheetName);

            int rowIndex = 0;

            // 写入表头
            if (includeHeader)
            {
                IRow headerRow = sheet.CreateRow(rowIndex++);
                for (int col = 0; col < dt.Columns.Count; col++)
                {
                    ICell cell = headerRow.CreateCell(col);
                    cell.SetCellValue(dt.Columns[col].ColumnName);
                    // 表头样式
                    cell.CellStyle = CreateHeaderStyle(workbook);
                }
            }

            // 写入数据
            for (int row = 0; row < dt.Rows.Count; row++)
            {
                IRow dataRow = sheet.CreateRow(rowIndex++);
                for (int col = 0; col < dt.Columns.Count; col++)
                {
                    object value = dt.Rows[row][col];
                    ICell cell = dataRow.CreateCell(col);
                    SetCellValue(cell, value);
                }
            }

            // 自动调整列宽
            if (autoFitColumn)
            {
                for (int col = 0; col < dt.Columns.Count; col++)
                {
                    sheet.AutoSizeColumn(col);
                    // 中文字符自适应：在AutoSize基础上增加宽度
                    int currentWidth = (int)sheet.GetColumnWidth(col);
                    sheet.SetColumnWidth(col, currentWidth + 512);
                }
            }

            // 保存文件
            SaveWorkbook(workbook, filePath);

            return dt.Rows.Count;
        }

        /// <summary>
        /// 将DataTable导出到Excel（带自定义样式）
        /// </summary>
        public static int ExportDataTableToExcel(DataTable dt, string filePath, ExcelExportOptions options)
        {
            if (dt == null || dt.Rows.Count == 0)
                throw new ArgumentException("数据源为空，无法导出");

            IWorkbook workbook = CreateWorkbookByExtension(filePath);
            ISheet sheet = workbook.CreateSheet(options.SheetName ?? "Sheet1");

            int rowIndex = 0;

            // 写入表头
            if (options.IncludeHeader)
            {
                IRow headerRow = sheet.CreateRow(rowIndex++);
                for (int col = 0; col < dt.Columns.Count; col++)
                {
                    ICell cell = headerRow.CreateCell(col);
                    string headerText = options.HeaderMapping != null && options.HeaderMapping.ContainsKey(col)
                        ? options.HeaderMapping[col]
                        : dt.Columns[col].ColumnName;
                    cell.SetCellValue(headerText);
                    cell.CellStyle = CreateHeaderStyle(workbook);
                }
            }

            // 写入数据
            for (int row = 0; row < dt.Rows.Count; row++)
            {
                IRow dataRow = sheet.CreateRow(rowIndex++);
                for (int col = 0; col < dt.Columns.Count; col++)
                {
                    object value = dt.Rows[row][col];
                    ICell cell = dataRow.CreateCell(col);
                    SetCellValue(cell, value);
                    cell.CellStyle = CreateTextStyle(workbook);
                }
            }

            // 自动调整列宽
            if (options.AutoFitColumn)
            {
                for (int col = 0; col < dt.Columns.Count; col++)
                {
                    sheet.AutoSizeColumn(col);
                    int currentWidth = (int)sheet.GetColumnWidth(col);
                    sheet.SetColumnWidth(col, currentWidth + 512);
                }
            }

            SaveWorkbook(workbook, filePath);
            return dt.Rows.Count;
        }

        #endregion

        #region 导出 泛型List<T> → Excel

        /// <summary>
        /// 将泛型集合导出到Excel（使用反射自动生成列）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="list">数据集合</param>
        /// <param name="filePath">目标文件路径</param>
        /// <param name="sheetName">工作表名称</param>
        /// <param name="propertyNames">指定要导出的属性名称列表（为空则导出所有公共属性）</param>
        /// <param name="headerMapping">列名映射（属性名 → 显示名）</param>
        /// <returns>导出的行数</returns>
        public static int ExportListToExcel<T>(IEnumerable<T> list, string filePath, string sheetName = "Sheet1",
            string[] propertyNames = null, Dictionary<string, string> headerMapping = null) where T : class
        {
            if (list == null || !list.Any())
                throw new ArgumentException("数据源为空，无法导出");

            IWorkbook workbook = CreateWorkbookByExtension(filePath);
            ISheet sheet = workbook.CreateSheet(sheetName);

            // 获取属性信息
            Type type = typeof(T);
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // 筛选要导出的属性
            if (propertyNames != null && propertyNames.Length > 0)
            {
                properties = properties.Where(p => propertyNames.Contains(p.Name)).ToArray();
            }

            if (properties.Length == 0)
                throw new Exception("未找到可导出的属性");

            int rowIndex = 0;

            // 写入表头
            IRow headerRow = sheet.CreateRow(rowIndex++);
            for (int i = 0; i < properties.Length; i++)
            {
                ICell cell = headerRow.CreateCell(i);
                string headerText = headerMapping != null && headerMapping.ContainsKey(properties[i].Name)
                    ? headerMapping[properties[i].Name]
                    : properties[i].Name;
                cell.SetCellValue(headerText);
                cell.CellStyle = CreateHeaderStyle(workbook);
            }

            // 写入数据
            foreach (T item in list)
            {
                IRow dataRow = sheet.CreateRow(rowIndex++);
                for (int i = 0; i < properties.Length; i++)
                {
                    object value = properties[i].GetValue(item);
                    ICell cell = dataRow.CreateCell(i);
                    SetCellValue(cell, value);
                    cell.CellStyle = CreateTextStyle(workbook);
                }
            }

            // 自动调整列宽
            for (int i = 0; i < properties.Length; i++)
            {
                sheet.AutoSizeColumn(i);
                int currentWidth = (int)sheet.GetColumnWidth(i);
                sheet.SetColumnWidth(i, currentWidth + 512);
            }

            SaveWorkbook(workbook, filePath);
            return rowIndex - 1;
        }

        /// <summary>
        /// 将泛型集合导出到Excel（使用特性配置，如DisplayName）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        public static int ExportListToExcel<T>(IEnumerable<T> list, string filePath, string sheetName = "Sheet1")
            where T : class
        {
            // 尝试从特性获取列名（如DisplayNameAttribute、DescriptionAttribute等）
            Type type = typeof(T);
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var headerMapping = new Dictionary<string, string>();
            foreach (var prop in properties)
            {
                // 尝试获取 DisplayName 特性
                var displayAttr = prop.GetCustomAttributes()
                    .FirstOrDefault(a => a.GetType().Name == "DisplayNameAttribute");
                if (displayAttr != null)
                {
                    var displayNameProp = displayAttr.GetType().GetProperty("DisplayName");
                    if (displayNameProp != null)
                    {
                        headerMapping[prop.Name] = displayNameProp.GetValue(displayAttr)?.ToString() ?? prop.Name;
                        continue;
                    }
                }

                // 尝试获取 Description 特性
                var descAttr = prop.GetCustomAttributes()
                    .FirstOrDefault(a => a.GetType().Name == "DescriptionAttribute");
                if (descAttr != null)
                {
                    var descProp = descAttr.GetType().GetProperty("Description");
                    if (descProp != null)
                    {
                        headerMapping[prop.Name] = descProp.GetValue(descAttr)?.ToString() ?? prop.Name;
                        continue;
                    }
                }

                headerMapping[prop.Name] = prop.Name;
            }

            return ExportListToExcel(list, filePath, sheetName, null, headerMapping);
        }

        #endregion

        #region 导出 DataGridView → Excel（WinForm专用）

        /// <summary>
        /// 将DataGridView数据导出到Excel
        /// </summary>
        //public static int ExportDataGridViewToExcel(System.Windows.Forms.DataGridView dgv, string filePath,
        //    string sheetName = "Sheet1", bool includeHeader = true)
        //{
        //    if (dgv == null || dgv.Rows.Count == 0)
        //        throw new ArgumentException("DataGridView无数据，无法导出");

        //    IWorkbook workbook = CreateWorkbookByExtension(filePath);
        //    ISheet sheet = workbook.CreateSheet(sheetName);

        //    int rowIndex = 0;

        //    // 写入表头
        //    if (includeHeader)
        //    {
        //        IRow headerRow = sheet.CreateRow(rowIndex++);
        //        for (int col = 0; col < dgv.Columns.Count; col++)
        //        {
        //            // 跳过隐藏列
        //            if (!dgv.Columns[col].Visible) continue;

        //            ICell cell = headerRow.CreateCell(GetVisibleColumnIndex(dgv, col));
        //            cell.SetCellValue(dgv.Columns[col].HeaderText);
        //            cell.CellStyle = CreateHeaderStyle(workbook);
        //        }
        //    }

        //    // 写入数据
        //    foreach (System.Windows.Forms.DataGridViewRow row in dgv.Rows)
        //    {
        //        if (row.IsNewRow) continue;

        //        IRow dataRow = sheet.CreateRow(rowIndex++);
        //        int visibleColIndex = 0;
        //        for (int col = 0; col < dgv.Columns.Count; col++)
        //        {
        //            if (!dgv.Columns[col].Visible) continue;

        //            object value = row.Cells[col].Value;
        //            ICell cell = dataRow.CreateCell(visibleColIndex++);
        //            SetCellValue(cell, value);
        //            cell.CellStyle = CreateTextStyle(workbook);
        //        }
        //    }

        //    // 自动调整列宽
        //    int visibleCount = 0;
        //    for (int col = 0; col < dgv.Columns.Count; col++)
        //    {
        //        if (!dgv.Columns[col].Visible) continue;
        //        sheet.AutoSizeColumn(visibleCount);
        //        int currentWidth = sheet.GetColumnWidth(visibleCount);
        //        sheet.SetColumnWidth(visibleCount, currentWidth + 512);
        //        visibleCount++;
        //    }

        //    SaveWorkbook(workbook, filePath);
        //    return rowIndex - 1;
        //}

        //private static int GetVisibleColumnIndex(System.Windows.Forms.DataGridView dgv, int colIndex)
        //{
        //    int index = 0;
        //    for (int i = 0; i < colIndex; i++)
        //    {
        //        if (dgv.Columns[i].Visible)
        //            index++;
        //    }
        //    return index;
        //}

        #endregion

        #region 多Sheet操作

        /// <summary>
        /// 将多个DataTable导出到同一个Excel文件的不同Sheet
        /// </summary>
        public static void ExportMultiSheetToExcel(Dictionary<string, DataTable> sheets, string filePath,
            bool includeHeader = true, bool autoFitColumn = true)
        {
            if (sheets == null || sheets.Count == 0)
                throw new ArgumentException("数据源为空");

            IWorkbook workbook = CreateWorkbookByExtension(filePath);

            foreach (var kvp in sheets)
            {
                string sheetName = kvp.Key;
                DataTable dt = kvp.Value;

                if (dt == null || dt.Rows.Count == 0) continue;

                // Sheet名称不能超过31个字符，且不能包含特殊字符
                string safeSheetName = SanitizeSheetName(sheetName);
                ISheet sheet = workbook.CreateSheet(safeSheetName);

                int rowIndex = 0;

                if (includeHeader)
                {
                    IRow headerRow = sheet.CreateRow(rowIndex++);
                    for (int col = 0; col < dt.Columns.Count; col++)
                    {
                        ICell cell = headerRow.CreateCell(col);
                        cell.SetCellValue(dt.Columns[col].ColumnName);
                        cell.CellStyle = CreateHeaderStyle(workbook);
                    }
                }

                for (int row = 0; row < dt.Rows.Count; row++)
                {
                    IRow dataRow = sheet.CreateRow(rowIndex++);
                    for (int col = 0; col < dt.Columns.Count; col++)
                    {
                        ICell cell = dataRow.CreateCell(col);
                        SetCellValue(cell, dt.Rows[row][col]);
                        cell.CellStyle = CreateTextStyle(workbook);
                    }
                }

                if (autoFitColumn)
                {
                    for (int col = 0; col < dt.Columns.Count; col++)
                    {
                        sheet.AutoSizeColumn(col);
                        int currentWidth = (int)sheet.GetColumnWidth(col);
                        sheet.SetColumnWidth(col, currentWidth + 512);
                    }
                }
            }

            SaveWorkbook(workbook, filePath);
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 根据文件扩展名创建对应的IWorkbook
        /// </summary>
        private static IWorkbook CreateWorkbook(Stream stream, string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            if (extension == ".xlsx")
                return new XSSFWorkbook(stream);
            else if (extension == ".xls")
                return new HSSFWorkbook(stream);
            else
                throw new NotSupportedException($"不支持的文件格式：{extension}");
        }

        /// <summary>
        /// 根据文件扩展名创建新的IWorkbook
        /// </summary>
        private static IWorkbook CreateWorkbookByExtension(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            if (extension == ".xlsx")
                return new XSSFWorkbook();
            else if (extension == ".xls")
                return new HSSFWorkbook();
            else
                throw new NotSupportedException($"不支持的文件格式：{extension}，请使用.xls或.xlsx");
        }

        /// <summary>
        /// 保存工作簿到文件
        /// </summary>
        private static void SaveWorkbook(IWorkbook workbook, string filePath)
        {
            // 确保目录存在
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                workbook.Write(fs);
                //fs.Flush();
            }
        }

        /// <summary>
        /// 设置单元格值（自动处理各种类型）
        /// </summary>
        private static void SetCellValue(ICell cell, object value)
        {
            if (value == null || value == DBNull.Value)
            {
                cell.SetCellValue(string.Empty);
                return;
            }

            Type type = value.GetType();

            if (type == typeof(string))
            {
                cell.SetCellValue((string)value);
            }
            else if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte))
            {
                cell.SetCellValue(Convert.ToDouble(value));
            }
            else if (type == typeof(decimal) || type == typeof(float) || type == typeof(double))
            {
                cell.SetCellValue(Convert.ToDouble(value));
            }
            else if (type == typeof(bool))
            {
                cell.SetCellValue((bool)value);
            }
            else if (type == typeof(DateTime))
            {
                cell.SetCellValue((DateTime)value);
            }
            else if (type == typeof(DateTime?))
            {
                DateTime? dt = (DateTime?)value;
                if (dt.HasValue)
                    cell.SetCellValue(dt.Value);
                else
                    cell.SetCellValue(string.Empty);
            }
            else
            {
                cell.SetCellValue(value.ToString());
            }
        }

        /// <summary>
        /// 创建表头样式（加粗、居中、灰色背景、边框）
        /// </summary>
        private static ICellStyle CreateHeaderStyle(IWorkbook workbook)
        {
            ICellStyle style = workbook.CreateCellStyle();

            // 字体：加粗
            IFont font = workbook.CreateFont();
            font.IsBold = true;
            style.SetFont(font);

            // 对齐：居中
            style.Alignment = HorizontalAlignment.Center;
            style.VerticalAlignment = VerticalAlignment.Center;

            // 背景色：浅灰色
            style.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Grey25Percent.Index;
            style.FillPattern = FillPattern.SolidForeground;

            // 边框
            style.BorderBottom = BorderStyle.Thin;
            style.BorderLeft = BorderStyle.Thin;
            style.BorderRight = BorderStyle.Thin;
            style.BorderTop = BorderStyle.Thin;

            return style;
        }

        /// <summary>
        /// 创建文本样式（带边框）
        /// </summary>
        private static ICellStyle CreateTextStyle(IWorkbook workbook)
        {
            ICellStyle style = workbook.CreateCellStyle();

            style.Alignment = HorizontalAlignment.Left;
            style.VerticalAlignment = VerticalAlignment.Center;

            style.BorderBottom = BorderStyle.Thin;
            style.BorderLeft = BorderStyle.Thin;
            style.BorderRight = BorderStyle.Thin;
            style.BorderTop = BorderStyle.Thin;

            return style;
        }

        /// <summary>
        /// 清理Sheet名称（不超过31字符，移除非法字符）
        /// </summary>
        private static string SanitizeSheetName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "Sheet";

            // Excel Sheet名称不能包含: \ / ? * [ ]
            char[] invalidChars = new char[] { '\\', '/', '?', '*', '[', ']' };
            foreach (char c in invalidChars)
            {
                name = name.Replace(c.ToString(), "");
            }

            if (name.Length > 31)
                name = name.Substring(0, 31);

            return name;
        }

        #endregion
    }

    /// <summary>
    /// Excel导出选项配置类
    /// </summary>
    public class ExcelExportOptions
    {
        public string SheetName { get; set; } = "Sheet1";
        public bool IncludeHeader { get; set; } = true;
        public bool AutoFitColumn { get; set; } = true;
        /// <summary>
        /// 列索引 → 自定义表头文本 映射
        /// </summary>
        public Dictionary<int, string> HeaderMapping { get; set; }
    }
}

