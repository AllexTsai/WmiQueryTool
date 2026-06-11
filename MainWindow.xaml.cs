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
    // 資料模型
    public record WmiProperty(string ObjectId, string Name, string Value);

    public partial class MainWindow : Window
    {
        private List<WmiProperty> currentResults = new();
        private readonly List<string> history = new();

        public MainWindow()
        {
            InitializeComponent();
        }

        // 常用查詢選擇
        private void QuerySelector_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (QuerySelector.SelectedItem is ComboBoxItem item && item.Tag is string className)
            {
                QueryBox.Text = $"SELECT * FROM {className}";
            }
        }

        // 範本查詢選擇
        private void TemplateSelector_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (TemplateSelector.SelectedItem is ComboBoxItem item && item.Tag is string queryTemplate)
            {
                QueryBox.Text = queryTemplate;
            }
        }

        // 執行查詢
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
                ResultGrid.ItemsSource = currentResults;

                if (results.Count > 0)
                {
                    StatusMessage.Text = $"查詢完成，共 {objectIndex - 1} 個物件，{results.Count} 筆屬性。";
                    AddToHistory(query);
                }
                else
                {
                    StatusMessage.Text = "查詢沒有返回任何結果。";
                }
            }
            catch (Exception ex)
            {
                StatusMessage.Text = $"查詢失敗: {ex.Message}";
                MessageBox.Show($"Error: {ex.Message}", "Query Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 更新查詢歷史
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

        // 選擇歷史查詢
        private void HistoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HistoryList.SelectedItem is string selectedQuery)
            {
                QueryBox.Text = selectedQuery;
            }
        }

        // 匯出 CSV
        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ResultGrid.ItemsSource is IEnumerable<WmiProperty> results)
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

                        foreach (var item in results)
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

        // 套用篩選 (不使用 ICollectionView)
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

            ResultGrid.ItemsSource = filtered;
            StatusMessage.Text = $"已套用篩選：{keyword}，共 {filtered.Count} 筆結果。";
        }

        // 清除篩選 (不使用 ICollectionView)
        private void ClearFilter_Click(object sender, RoutedEventArgs e)
        {
            ResultGrid.ItemsSource = currentResults;
            FilterBox.Text = "";
            StatusMessage.Text = "已清除篩選。";
        }
    }
}