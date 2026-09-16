using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace WmiQueryTool
{
    public record WmiProperty(string ObjectId, string Name, string Value);

    public partial class MainWindow : Window
    {
        private List<WmiProperty> currentResults = new();
        private readonly List<string> history = new();
        private int currentPage = 1;
        private int pageSize = 10; // 10 entries per page

        public MainWindow()
        {
            InitializeComponent();
        }

        // Commonly used query selections
        private void QuerySelector_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (QuerySelector.SelectedItem is ComboBoxItem item && item.Tag is string className)
            {
                QueryBox.Text = $"SELECT * FROM {className}";
            }
        }

        // Template search selection
        private void TemplateSelector_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (TemplateSelector.SelectedItem is ComboBoxItem item && item.Tag is string queryTemplate)
            {
                QueryBox.Text = queryTemplate;
            }
        }

        // Run query
        private void RunQuery_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string query = QueryBox.Text;
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);

                var results = new List<WmiProperty>();
                int objectIndex = 1;

                foreach (ManagementObject obj in searcher.Get())
                {
                    foreach (var prop in obj.Properties)
                    {
                        results.Add(new WmiProperty(
                            $"Object {objectIndex}",
                            prop.Name,
                            prop.Value?.ToString() ?? ""
                        ));
                    }
                    objectIndex++;
                }

                currentResults = results;
                currentPage = 1;
                ShowPage();

                StatusMessage.Text = results.Count > 0
                    ? $"查詢完成，共 {objectIndex - 1} 個物件，{results.Count} 筆屬性。"
                    : "查詢沒有返回任何結果。";

                if (results.Count > 0) AddToHistory(query);
            }
            catch (Exception ex)
            {
                StatusMessage.Text = $"查詢失敗: {ex.Message}";
                MessageBox.Show($"Error: {ex.Message}", "Query Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Show page
        private void ShowPage()
        {
            int total = currentResults.Count;
            if (total == 0)
            {
                ResultGrid.ItemsSource = null;
                PageInfo.Text = "沒有資料";
                return;
            }

            int totalPages = (int)Math.Ceiling((double)total / pageSize);
            if (currentPage > totalPages) currentPage = totalPages;

            int startIndex = (currentPage - 1) * pageSize;
            int count = Math.Min(pageSize, total - startIndex);

            var pageData = currentResults.GetRange(startIndex, count);

            ResultGrid.ItemsSource = pageData;
            PageInfo.Text = $"第 {currentPage}/{totalPages} 頁，共 {total} 筆屬性";
        }

        // Previous page
        private void PrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage > 1)
            {
                currentPage--;
                ShowPage();
            }
            else
            {
                StatusMessage.Text = "已在第一頁";
            }
        }

        // Next page
        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            int totalPages = (int)Math.Ceiling((double)currentResults.Count / pageSize);
            if (currentPage < totalPages)
            {
                currentPage++;
                ShowPage();
            }
            else
            {
                StatusMessage.Text = "已在最後一頁";
            }
        }

        // Update query history
        private void AddToHistory(string query)
        {
            if (!history.Contains(query))
            {
                history.Insert(0, query);
                if (history.Count > 10) history.RemoveAt(history.Count - 1);

                HistoryList.ItemsSource = null;
                HistoryList.ItemsSource = history;
            }
        }

        // Select historical query
        private void HistoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HistoryList.SelectedItem is string selectedQuery)
            {
                QueryBox.Text = selectedQuery;
            }
        }

        // Export CSV
        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (currentResults.Count > 0)
                {
                    var dialog = new SaveFileDialog
                    {
                        Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                        FileName = "results.csv"
                    };

                    if (dialog.ShowDialog() == true)
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine("ObjectId,Property,Value");

                        foreach (var item in currentResults)
                        {
                            string objectId = item.ObjectId.Replace("\"", "\"\"");
                            string name = item.Name.Replace("\"", "\"\"");
                            string value = item.Value.Replace("\"", "\"\"");
                            sb.AppendLine($"\"{objectId}\",\"{name}\",\"{value}\"");
                        }

                        File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);

                        StatusMessage.Text = $"CSV 已匯出至 {dialog.FileName}";
                        MessageBox.Show($"CSV 已匯出至 {dialog.FileName}", "Export Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    StatusMessage.Text = "目前沒有資料可匯出。";
                    MessageBox.Show("目前沒有資料可匯出。", "No Data", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                StatusMessage.Text = $"匯出失敗: {ex.Message}";
                MessageBox.Show($"匯出失敗: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Apply filter
        private void ApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            string keyword = FilterBox.Text.Trim();
            if (string.IsNullOrEmpty(keyword))
            {
                StatusMessage.Text = "請輸入篩選關鍵字。";
                return;
            }

            var filtered = currentResults.FindAll(prop =>
                prop.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                prop.Value.Contains(keyword, StringComparison.OrdinalIgnoreCase));

            currentResults = filtered;
            currentPage = 1;
            ShowPage();

            StatusMessage.Text = $"已套用篩選：{keyword}，共 {filtered.Count} 筆結果。";
        }

        // Clear filter
        private void ClearFilter_Click(object sender, RoutedEventArgs e)
        {
            RunQuery_Click(sender, e); // 重新執行查詢以恢復完整結果
            FilterBox.Text = "";
            StatusMessage.Text = "已清除篩選。";
        }

        // Override the Windows lifecycle and intercept AI commands before the window is actually rendered.
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Retrieve command line startup parameters
            string[] args = Environment.GetCommandLineArgs();

            // args[0] is the path to the executable file itself, and args[1] is the wmiCategory (such as CPU, GPU) that we passed over.
            if (args.Length > 1)
            {
                string category = args[1].ToUpper();
                Console.WriteLine($"[OnSourceInitialized] Matched category: {category}");

                // Align the original UI ComboBox's SQL template
                string wmiSql = category switch
                {
                    "OS" => "SELECT Caption, Version, BuildNumber FROM Win32_OperatingSystem",
                    "CPU" => "SELECT Name, NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor",
                    "GPU" => "SELECT Name, DriverVersion, AdapterRAM FROM Win32_VideoController",
                    "MEMORY" => "SELECT Manufacturer, Capacity, Speed FROM Win32_PhysicalMemory",
                    "DISK" => "SELECT Model, Size, InterfaceType FROM Win32_DiskDrive",
                    _ => "SELECT Caption, Version, BuildNumber FROM Win32_OperatingSystem"
                };

                // Execution occurs silently in the background: Simply enter QueryBox and it will call pre-written core engine.
                QueryBox.Text = wmiSql;
                
                // In essence, it calls original query function (passing in null is sufficient, since the code doesn't use sender/e).
                RunQueryForAi();

                // Process-level standard output: Returns the results to LocalAIApp as a text stream.
                if (currentResults != null && currentResults.Count > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var item in currentResults)
                    {
                        sb.AppendLine($"[{item.ObjectId}] {item.Name} = {item.Value}");
                    }
                    Console.WriteLine(sb.ToString());
                }
                else
                {
                    Console.WriteLine("未查詢到任何硬體數據。");
                }

                // Task complete, process exits cleanly without popping up WPF UI to disturb the user.
                Application.Current.Shutdown();
            }
        }

        /// <summary>
        /// A static query plugin specifically designed for AI calls (copies and simplifies the original RunQuery_Click, removes MessageBox to prevent background freezing).
        /// </summary>
        private void RunQueryForAi()
        {
            try
            {
                string query = QueryBox.Text;
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
                var results = new List<WmiProperty>();
                int objectIndex = 1;

                foreach (ManagementObject obj in searcher.Get())
                {
                    foreach (var prop in obj.Properties)
                    {
                        results.Add(new WmiProperty(
                            $"Object {objectIndex}",
                            prop.Name,
                            prop.Value?.ToString() ?? ""
                        ));
                    }
                    objectIndex++;
                }
                // Fill your original paging buffer for the Console above to read.
                currentResults = results; 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WMI底層錯誤] {ex.Message}");
            }
        }

    }
}