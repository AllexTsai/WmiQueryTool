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
        private readonly List<string> history = new();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void QuerySelector_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (QuerySelector.SelectedItem is ComboBoxItem item && item.Tag is string className)
            {
                QueryBox.Text = $"SELECT * FROM {className}";
            }
        }

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

                if (results.Count > 0)
                {
                    ResultGrid.ItemsSource = results;
                    StatusMessage.Text = $"查詢完成，共 {objectIndex - 1} 個物件，{results.Count} 筆屬性。";

                    // 更新歷史紀錄
                    AddToHistory(query);
                }
                else
                {
                    ResultGrid.ItemsSource = null;
                    StatusMessage.Text = "查詢沒有返回任何結果。";
                }
            }
            catch (Exception ex)
            {
                StatusMessage.Text = $"查詢失敗: {ex.Message}";
                MessageBox.Show($"Error: {ex.Message}", "Query Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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

        private void HistoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HistoryList.SelectedItem is string selectedQuery)
            {
                QueryBox.Text = selectedQuery;
            }
        }

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
    }
}