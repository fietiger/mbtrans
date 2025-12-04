using System.IO;

// P/Invoke引用
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

// UI Automation引用
using System.Windows.Automation;
using WinForms = System.Windows.Forms;

namespace CodeTableExtractor
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int LVIF_IMAGE = 0x0002;

        private const int LVIF_STATE = 0x0008;

        private const int LVIF_TEXT = 0x0001;

        private const int LVM_FIRST = 0x1000;

        private const int LVM_GETITEM = 0x1005;

        // ListView消息常量
        private const int LVM_GETITEMCOUNT = 0x1004;

        private const int LVM_GETITEMTEXTA = (LVM_FIRST + 45);

        private const int LVM_GETITEMTEXTW = (LVM_FIRST + 115);

        private const int LVM_GETNEXTITEM = 0x100c;

        // 子窗口列表
        private List<ControlInfo> childControls = new List<ControlInfo>();

        private List<CodeItem> extractedData;

        private AutomationElement listControl;

        // 实时保存文件路径
        private string saveFilePath = string.Empty;

        // 用于记录运行时间
        private System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

        private AutomationElement targetWindow;
        private System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();

        public MainWindow()
        {
            InitializeComponent();
            extractedData = new List<CodeItem>();

            // 初始化计时器
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
        }

        // 委托定义
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        // P/Invoke声明
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SendMessage(IntPtr hWnd, int wMsg, int wParam, ref LVITEM lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SendMessage(IntPtr hWnd, int wMsg, int wParam, IntPtr lParam);

        /// <summary>
        /// 枚举SysListView32的子控件
        /// </summary>
        private void BtnEnumListViewChildren_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (targetWindow == null || listControl == null)
                {
                    System.Windows.MessageBox.Show("请先查找窗口和表格控件");
                    return;
                }

                // 获取SysListView32控件的句柄
                IntPtr listViewHandle = (IntPtr)listControl.Current.NativeWindowHandle;
                txtResult.AppendText($"\nSysListView32控件句柄: {listViewHandle}\n");

                // 清空子控件列表
                childControls.Clear();

                // 枚举子窗口
                txtResult.AppendText("开始枚举子控件...\n");
                EnumChildWindows(listViewHandle, EnumChildProc, IntPtr.Zero);

                // 显示结果
                txtResult.AppendText($"共找到 {childControls.Count} 个子控件:\n");
                foreach (var control in childControls)
                {
                    txtResult.AppendText($"  - 句柄: {control.Handle}, 类名: {control.ClassName}, 文本: {control.WindowText}\n");
                }

                // 获取ListView项目数量
                int itemCount = SendMessage(listViewHandle, LVM_GETITEMCOUNT, 0, IntPtr.Zero);
                txtResult.AppendText($"ListView项目数量: {itemCount}\n");

                // 如果有项目，获取第一行内容
                if (itemCount > 0)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        txtResult.AppendText($"\n获取第一行内容:\n");

                        // 获取第一行第一列（主列）
                        string firstColumnText = GetListViewItemText(listViewHandle, i, 0);
                        txtResult.AppendText($"  第一列: {firstColumnText}\n");

                        // 获取第一行第二列
                        string secondColumnText = GetListViewItemText(listViewHandle, i, 1);
                        txtResult.AppendText($"  第二列: {secondColumnText}\n");

                        // 获取第一行第三列
                        string thirdColumnText = GetListViewItemText(listViewHandle, i, 2);
                        txtResult.AppendText($"  第三列: {thirdColumnText}\n");
                    }
                }
            }
            catch (Exception ex)
            {
                txtResult.AppendText($"枚举子控件出错: {ex.Message}\n");
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

                // 查找窗口
                targetWindow = AutomationElement.RootElement.FindFirst(
                    TreeScope.Children,
                    new PropertyCondition(AutomationElement.NameProperty, windowTitle));

                if (targetWindow != null)
                {
                    txtResult.AppendText($"窗口已找到: {targetWindow.Current.Name}\n");

                    // 查找ListControl (SysListView32)
                    txtResult.AppendText("正在查找表格控件...\n");
                    listControl = targetWindow.FindFirst(
                        TreeScope.Descendants,
                        new PropertyCondition(AutomationElement.ClassNameProperty, "SysListView32"));

                    if (listControl != null)
                    {
                        txtResult.AppendText($"找到表格控件: {listControl.Current.ClassName}\n");
                    }
                    else
                    {
                        txtResult.AppendText("未找到表格控件\n");
                    }
                }
                else
                {
                    txtResult.AppendText("未找到表格控件\n");
                }
            }
            catch (Exception ex)
            {
                txtResult.AppendText($"查找窗口出错: {ex.Message}\n");
            }
        }

        /// <summary>
        /// 保存为CSV按钮点击事件
        /// </summary>
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (extractedData.Count == 0)
            {
                System.Windows.MessageBox.Show("没有提取到数据");
                return;
            }

            try
            {
                // 使用SaveFileDialog保存文件
                Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV文件 (*.csv)|*.csv|所有文件 (*.*)|*.*",
                    DefaultExt = ".csv",
                    FileName = "编码表数据.csv"
                };

                bool? result = saveFileDialog.ShowDialog();
                if (result == true)
                {
                    // 创建CSV内容
                    StringBuilder csvContent = new StringBuilder();
                    csvContent.AppendLine("序号,编码,词条,分类,候选排序");

                    foreach (var item in extractedData)
                    {
                        csvContent.AppendLine($"{item.Index},\"{item.Code}\",\"{item.Word}\",\"{item.Category}\",\"{item.Sort}\"");
                    }

                    // 保存文件
                    File.WriteAllText(saveFileDialog.FileName, csvContent.ToString(), Encoding.UTF8);
                    txtResult.AppendText($"\n数据已保存到: {saveFileDialog.FileName}\n");
                    System.Windows.MessageBox.Show("保存成功");
                }
            }
            catch (Exception ex)
            {
                txtResult.AppendText($"保存文件出错: {ex.Message}\n");
            }
        }

        /// <summary>
        /// 开始提取按钮点击事件
        /// </summary>
        private void BtnStartExtract_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (targetWindow == null || listControl == null)
                {
                    System.Windows.MessageBox.Show("请先查找窗口和表格控件");
                    return;
                }

                txtResult.AppendText("\n开始提取数据...\n");
                extractedData.Clear();

                // 让用户选择保存路径
                Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
                saveFileDialog.Filter = "CSV文件|*.csv";
                saveFileDialog.Title = "保存提取结果";
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
                txtProgress.Text = "进度: 0/0";

                // 启动秒表
                stopwatch.Restart();
                timer.Start();

                // 确保窗口和控件获得焦点
                AutomationElement focusedElement = AutomationElement.FocusedElement;
                if (focusedElement != targetWindow)
                {
                    // 激活窗口
                    ((WindowPattern)targetWindow.GetCurrentPattern(WindowPattern.Pattern)).SetWindowVisualState(WindowVisualState.Normal);
                    ((WindowPattern)targetWindow.GetCurrentPattern(WindowPattern.Pattern)).WaitForInputIdle(1000);
                }

                // 确保ListControl获得焦点
                listControl.SetFocus();
                System.Threading.Thread.Sleep(500);

                // 滚动到顶部
                WinForms.SendKeys.SendWait("{HOME}");
                System.Threading.Thread.Sleep(500);

                // 获取ListView的实际项目数量
                IntPtr listViewHandle = new IntPtr(listControl.Current.NativeWindowHandle);
                int totalItems = SendMessage(listViewHandle, LVM_GETITEMCOUNT, 0, 0);

                txtResult.AppendText($"发现项目数量: {totalItems}\n");

                // 设置进度条最大值
                progressBar.Maximum = totalItems;

                // 提取数据
                int maxAttempts = totalItems > 0 ? totalItems : 147645;  // 使用实际数量或默认值
                int successCount = 0;
                int failedCount = 0;
                int maxFailed = 3;     // 连续失败3次停止

                for (int i = 0; i < maxAttempts; i++)
                {
                    if (failedCount >= maxFailed)
                    {
                        txtResult.AppendText("连续失败3次，停止提取\n");
                        break;
                    }

                    try
                    {
                        txtResult.AppendText($"\n处理第 {i + 1} 行...\n");

                        // 复制当前行
                        WinForms.SendKeys.SendWait("^c");
                        System.Threading.Thread.Sleep(800);

                        // 读取剪贴板数据
                        string clipboardData = System.Windows.Clipboard.GetText();

                        if (!string.IsNullOrEmpty(clipboardData))
                        {
                            txtResult.AppendText($"   剪贴板数据: '{clipboardData}'\n");

                            // 解析数据
                            string[] columns = clipboardData.Trim().Split('\t');
                            if (columns.Length < 2)
                            {
                                // 如果没有制表符分隔，尝试使用空格
                                columns = Regex.Split(clipboardData.Trim(), @"\s+");
                            }

                            if (columns.Length >= 2)
                            {
                                CodeItem item = new CodeItem
                                {
                                    Index = i + 1,
                                    Word = columns[0].Trim(),
                                    Code = columns[1].Trim(),
                                    Category = columns.Length > 2 ? columns[2].Trim() : "",
                                    Sort = columns.Length > 3 ? columns[3].Trim() : ""
                                };

                                if (!string.IsNullOrEmpty(item.Word) && !string.IsNullOrEmpty(item.Code))
                                {
                                    // 检查重复
                                    if (!extractedData.Any(x => x.Word == item.Word && x.Code == item.Code))
                                    {
                                        extractedData.Add(item);
                                        successCount++;

                                        // 实时保存到CSV文件
                                        using (StreamWriter sw = new StreamWriter(saveFilePath, true, Encoding.UTF8))
                                        {
                                            sw.WriteLine($"{item.Index},\"{item.Code}\",\"{item.Word}\",\"{item.Category}\",\"{item.Sort}\"");
                                        }

                                        // 检查并清空日志（如果内容过长）
                                        if (txtResult.Text.Length > 10000)
                                        {
                                            txtResult.Clear();
                                            txtResult.AppendText("--- 日志已自动清空 ---\r\n");
                                        }

                                        // 显示提取成功的信息
                                        txtResult.AppendText($"✅ 提取成功: 编码={item.Code} | 词条={item.Word}\n");
                                        failedCount = 0;  // 重置失败计数

                                        // 更新进度
                                        progressBar.Value = i + 1;
                                        txtProgress.Text = string.Format("进度: {0}/{1}", i + 1, maxAttempts);

                                        // 刷新UI
                                        this.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
                                    }
                                    else
                                    {
                                        txtResult.AppendText($"⚠️  重复数据: 编码={item.Code} | 词条={item.Word}\n");
                                        failedCount++;

                                        // 更新进度
                                        progressBar.Value = i + 1;
                                        txtProgress.Text = string.Format("进度: {0}/{1}", i + 1, maxAttempts);

                                        // 刷新UI
                                        this.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
                                    }
                                }
                                else
                                {
                                    txtResult.AppendText("❌ 数据无效\n");
                                    failedCount++;

                                    // 更新进度
                                    progressBar.Value = i + 1;
                                    txtProgress.Text = string.Format("进度: {0}/{1}", i + 1, maxAttempts);

                                    // 刷新UI
                                    this.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
                                }
                            }
                            else
                            {
                                txtResult.AppendText($"❌ 列数不足: {columns.Length}列\n");
                                failedCount++;

                                // 更新进度
                                progressBar.Value = i + 1;
                                txtProgress.Text = string.Format("进度: {0}/{1}", i + 1, maxAttempts);

                                // 刷新UI
                                this.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
                            }
                        }
                        else
                        {
                            txtResult.AppendText("❌ 剪贴板数据为空\n");
                            failedCount++;

                            // 更新进度
                            progressBar.Value = i + 1;
                            txtProgress.Text = string.Format("进度: {0}/{1}", i + 1, maxAttempts);

                            // 刷新UI
                            this.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
                        }

                        // 移动到下一行
                        if (i < maxAttempts - 1)  // 最后一行不需要移动
                        {
                            WinForms.SendKeys.SendWait("{DOWN}");
                            System.Threading.Thread.Sleep(500);
                        }
                    }
                    catch (Exception ex)
                    {
                        txtResult.AppendText($"❌ 处理出错: {ex.Message}\n");
                        failedCount++;

                        // 更新进度
                        progressBar.Value = i + 1;
                        txtProgress.Text = string.Format("进度: {0}/{1}", i + 1, maxAttempts);

                        // 刷新UI
                        this.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
                    }
                }

                // 停止秒表
                stopwatch.Stop();
                timer.Stop();

                // 更新最终运行时间
                TimeSpan elapsed = stopwatch.Elapsed;
                txtRunTime.Text = string.Format("运行时间: {0:00}:{1:00}:{2:00}",
                    elapsed.Hours, elapsed.Minutes, elapsed.Seconds);

                txtResult.AppendText($"\n数据提取完成，共提取 {extractedData.Count} 行\n");
            }
            catch (Exception ex)
            {
                // 发生异常时也停止计时器
                stopwatch.Stop();
                timer.Stop();
                txtResult.AppendText($"提取数据出错: {ex.Message}\n");
            }
        }

        /// <summary>
        /// 子窗口枚举回调函数
        /// </summary>
        private bool EnumChildProc(IntPtr hWnd, IntPtr lParam)
        {
            // 获取窗口类名
            StringBuilder className = new StringBuilder(256);
            GetClassName(hWnd, className, className.Capacity);

            // 获取窗口文本
            int textLength = GetWindowTextLength(hWnd);
            StringBuilder windowText = new StringBuilder(textLength + 1);
            GetWindowText(hWnd, windowText, windowText.Capacity);

            // 添加到子控件列表
            childControls.Add(new ControlInfo
            {
                Handle = hWnd,
                ClassName = className.ToString(),
                WindowText = windowText.ToString()
            });

            // 继续枚举
            return true;
        }

        /// <summary>
        /// 获取ListView指定行和列的文本内容
        /// </summary>
        /// <param name="listViewHandle">ListView控件句柄</param>
        /// <param name="itemIndex">行索引</param>
        /// <param name="subItemIndex">列索引（0为主列）</param>
        /// <returns>文本内容</returns>
        private string GetListViewItemText(IntPtr listViewHandle, int itemIndex, int subItemIndex)
        {
            try
            {
                const int bufferSize = 256;
                string buffer = new string(' ', bufferSize);

                LVITEM lvItem = new LVITEM();
                lvItem.mask = LVIF_TEXT;
                lvItem.iItem = itemIndex;
                lvItem.iSubItem = subItemIndex;
                lvItem.pszText = buffer;
                lvItem.cchTextMax = bufferSize;

                SendMessage(listViewHandle, LVM_GETITEM, 0, ref lvItem);

                // 去除末尾的空格
                if (!string.IsNullOrEmpty(lvItem.pszText))
                {
                    int nullIndex = lvItem.pszText.IndexOf('\0');
                    if (nullIndex > 0)
                    {
                        return lvItem.pszText.Substring(0, nullIndex);
                    }
                    return lvItem.pszText.Trim();
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                txtResult.AppendText($"获取ListView项目文本出错: {ex.Message}\n");
                return string.Empty;
            }
        }

        /// <summary>
        /// 计时器Tick事件
        /// </summary>
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (stopwatch.IsRunning)
            {
                TimeSpan elapsed = stopwatch.Elapsed;
                txtRunTime.Text = string.Format("运行时间: {0:00}:{1:00}:{2:00}",
                    elapsed.Hours, elapsed.Minutes, elapsed.Seconds);
            }
        }

        // LVITEM结构体定义
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct LVITEM
        {
            public uint mask;
            public int iItem;
            public int iSubItem;
            public uint state;
            public uint stateMask;

            [MarshalAs(UnmanagedType.LPTStr)]
            public string pszText;

            public int cchTextMax;
            public int iImage;
            public IntPtr lParam;
            public int iIndent;
            public int iGroupId;
            public uint cColumns;
            public IntPtr puColumns;
        }

        /// <summary>
        /// 编码条目类
        /// </summary>
        private class CodeItem
        {
            public string Category { get; set; } = string.Empty;
            public string Code { get; set; } = string.Empty;
            public int Index { get; set; }
            public string Sort { get; set; } = string.Empty;
            public string Word { get; set; } = string.Empty;
        }

        /// <summary>
        /// 控件信息类
        /// </summary>
        private class ControlInfo
        {
            public string ClassName { get; set; } = string.Empty;
            public IntPtr Handle { get; set; }
            public string WindowText { get; set; } = string.Empty;
        }
    }
}
