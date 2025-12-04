using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Automation;

namespace CodeTableExtractor
{
    /// <summary>
    /// 编码条目类
    /// </summary>
    public class CodeItem
    {
        public string Category { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int Index { get; set; }
        public string Sort { get; set; } = string.Empty;
        public string Word { get; set; } = string.Empty;
    }

    /// <summary>
    /// 使用 GridPattern 读取 ListView 数据的读取器
    /// </summary>
    public class GridPatternReader
    {
        private GridPattern? _gridPattern;

        public string GetCellData(int rowIndex, int columnIndex)
        {
            if (_gridPattern == null)
                throw new InvalidOperationException("读取器未初始化");

            try
            {
                AutomationElement cell = _gridPattern.GetItem(rowIndex, columnIndex);
                if (cell != null)
                {
                    object valuePattern;
                    if (cell.TryGetCurrentPattern(ValuePattern.Pattern, out valuePattern))
                    {
                        return ((ValuePattern)valuePattern).Current.Value ?? string.Empty;
                    }
                    return cell.Current.Name ?? string.Empty;
                }
            }
            catch (ArgumentOutOfRangeException) { }
            catch (ElementNotAvailableException) { }

            return string.Empty;
        }

        public int GetColumnCount()
        {
            if (_gridPattern == null)
                throw new InvalidOperationException("读取器未初始化");
            return _gridPattern.Current.ColumnCount;
        }

        public int GetRowCount()
        {
            if (_gridPattern == null)
                throw new InvalidOperationException("读取器未初始化");
            return _gridPattern.Current.RowCount;
        }

        public string[] GetRowData(int rowIndex)
        {
            if (_gridPattern == null)
                throw new InvalidOperationException("读取器未初始化");

            int colCount = _gridPattern.Current.ColumnCount;
            string[] rowData = new string[colCount];

            for (int col = 0; col < colCount; col++)
            {
                rowData[col] = GetCellData(rowIndex, col);
            }

            return rowData;
        }

        public void Initialize(AutomationElement listViewElement)
        {
            if (listViewElement == null)
                throw new ArgumentNullException(nameof(listViewElement));

            object pattern;
            if (listViewElement.TryGetCurrentPattern(GridPattern.Pattern, out pattern))
            {
                _gridPattern = (GridPattern)pattern;
            }
            else
            {
                throw new InvalidOperationException("控件不支持 GridPattern");
            }
        }

        public bool IsSupported(AutomationElement listViewElement)
        {
            if (listViewElement == null)
                return false;

            object pattern;
            return listViewElement.TryGetCurrentPattern(GridPattern.Pattern, out pattern);
        }
    }

    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private AutomationElement? listControl;
        private string saveFilePath = string.Empty;
        private System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        private AutomationElement? targetWindow;
        private System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();

        public MainWindow()
        {
            InitializeComponent();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
        }

        /// <summary>
        /// 清空日志按钮点击事件
        /// </summary>
        private void BtnClearLog_Click(object sender, RoutedEventArgs e)
        {
            txtResult.Clear();
        }

        /// <summary>
        /// 快速提取按钮点击事件
        /// </summary>
        private async void BtnFastExtract_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (targetWindow == null || listControl == null)
                {
                    System.Windows.MessageBox.Show("请先查找窗口和表格控件");
                    return;
                }

                var reader = new GridPatternReader();
                if (!reader.IsSupported(listControl))
                {
                    txtResult.AppendText("❌ 该控件不支持 GridPattern，无法使用快速提取\n");
                    return;
                }

                reader.Initialize(listControl);
                int rowCount = reader.GetRowCount();
                int colCount = reader.GetColumnCount();

                txtResult.AppendText($"\n✅ GridPattern 支持！\n");
                txtResult.AppendText($"总行数: {rowCount}, 总列数: {colCount}\n");

                // 显示前3行数据预览
                txtResult.AppendText("\n前3行数据预览:\n");
                for (int debugRow = 0; debugRow < Math.Min(3, rowCount); debugRow++)
                {
                    string[] debugData = reader.GetRowData(debugRow);
                    txtResult.AppendText($"  行{debugRow}: ");
                    for (int col = 0; col < debugData.Length; col++)
                    {
                        txtResult.AppendText($"[{col}]={debugData[col]} | ");
                    }
                    txtResult.AppendText("\n");
                }

                // 让用户选择保存路径
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV文件|*.csv",
                    Title = "保存快速提取结果",
                    FileName = "快速提取_编码表数据.csv"
                };
                if (saveFileDialog.ShowDialog() != true)
                {
                    txtResult.AppendText("操作已取消。\r\n");
                    return;
                }
                saveFilePath = saveFileDialog.FileName;

                // 写入CSV文件头
                using (StreamWriter sw = new StreamWriter(saveFilePath, false, Encoding.UTF8))
                {
                    sw.WriteLine("序号,编码,词条,分类,候选排序");
                }

                // 重置进度条
                progressBar.Value = 0;
                progressBar.Maximum = rowCount;
                txtProgress.Text = $"进度: 0/{rowCount}";

                // 启动秒表
                stopwatch.Restart();
                timer.Start();

                txtResult.AppendText($"\n开始快速提取数据...\n");

                int successCount = 0;
                int batchSize = 100;

                await System.Threading.Tasks.Task.Run(() =>
                {
                    using (StreamWriter sw = new StreamWriter(saveFilePath, true, Encoding.UTF8))
                    {
                        for (int i = 0; i < rowCount; i++)
                        {
                            try
                            {
                                string[] rowData = reader.GetRowData(i);

                                if (rowData.Length >= 2)
                                {
                                    // [0]=序号, [1]=编码, [2]=词条, [3]=分类, [4]=候选排序
                                    var item = new CodeItem
                                    {
                                        Index = i + 1,
                                        Code = rowData.Length > 1 ? rowData[1] : "",
                                        Word = rowData.Length > 2 ? rowData[2] : "",
                                        Category = rowData.Length > 3 ? rowData[3] : "",
                                        Sort = rowData.Length > 4 ? rowData[4] : ""
                                    };

                                    sw.WriteLine($"{item.Index},\"{item.Code}\",\"{item.Word}\",\"{item.Category}\",\"{item.Sort}\"");
                                    successCount++;

                                    if ((i + 1) % batchSize == 0 || i == rowCount - 1)
                                    {
                                        int currentRow = i + 1;
                                        int currentSuccess = successCount;
                                        this.Dispatcher.Invoke(() =>
                                        {
                                            progressBar.Value = currentRow;
                                            txtProgress.Text = $"进度: {currentRow}/{rowCount}";

                                            if (txtResult.Text.Length > 5000)
                                            {
                                                txtResult.Clear();
                                                txtResult.AppendText("--- 日志已自动清空 ---\r\n");
                                            }
                                            txtResult.AppendText($"已处理 {currentRow} 行，成功 {currentSuccess} 条\n");
                                        });
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                this.Dispatcher.Invoke(() =>
                                {
                                    txtResult.AppendText($"❌ 第 {i + 1} 行出错: {ex.Message}\n");
                                });
                            }
                        }
                    }
                });

                stopwatch.Stop();
                timer.Stop();

                TimeSpan elapsed = stopwatch.Elapsed;
                txtRunTime.Text = $"运行时间: {elapsed.Hours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";

                txtResult.AppendText($"\n✅ 快速提取完成！\n");
                txtResult.AppendText($"总行数: {rowCount}，成功提取: {successCount} 条\n");
                txtResult.AppendText($"数据已保存到: {saveFilePath}\n");
                txtResult.AppendText($"耗时: {elapsed.TotalSeconds:F2} 秒\n");

                System.Windows.MessageBox.Show($"快速提取完成！\n成功提取 {successCount} 条数据\n耗时 {elapsed.TotalSeconds:F2} 秒", "完成");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                timer.Stop();
                txtResult.AppendText($"❌ 快速提取出错: {ex.Message}\n");
                System.Windows.MessageBox.Show($"快速提取出错: {ex.Message}", "错误");
            }
        }

        /// <summary>
        /// 查找窗口按钮点击事件
        /// </summary>
        private void BtnFindWindow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string windowTitle = txtWindowTitle.Text.Trim();
                if (string.IsNullOrEmpty(windowTitle))
                {
                    System.Windows.MessageBox.Show("请输入窗口标题");
                    return;
                }

                txtResult.AppendText($"正在查找窗口: {windowTitle}\n");

                targetWindow = AutomationElement.RootElement.FindFirst(
                    TreeScope.Children,
                    new PropertyCondition(AutomationElement.NameProperty, windowTitle));

                if (targetWindow != null)
                {
                    txtResult.AppendText($"✅ 窗口已找到: {targetWindow.Current.Name}\n");

                    txtResult.AppendText("正在查找表格控件...\n");
                    listControl = targetWindow.FindFirst(
                        TreeScope.Descendants,
                        new PropertyCondition(AutomationElement.ClassNameProperty, "SysListView32"));

                    if (listControl != null)
                    {
                        txtResult.AppendText($"✅ 找到表格控件: {listControl.Current.ClassName}\n");
                    }
                    else
                    {
                        txtResult.AppendText("❌ 未找到表格控件\n");
                    }
                }
                else
                {
                    txtResult.AppendText("❌ 未找到窗口\n");
                }
            }
            catch (Exception ex)
            {
                txtResult.AppendText($"❌ 查找窗口出错: {ex.Message}\n");
            }
        }

        /// <summary>
        /// 计时器Tick事件
        /// </summary>
        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (stopwatch.IsRunning)
            {
                TimeSpan elapsed = stopwatch.Elapsed;
                txtRunTime.Text = $"运行时间: {elapsed.Hours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
            }
        }
    }
}
