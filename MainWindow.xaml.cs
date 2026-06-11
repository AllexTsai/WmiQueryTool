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
    // 用 record 定義，避免 NRT 警告
    public record WmiProperty(string ObjectId, string Name, string Value);

    public partial class MainWindow : Window
    {
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

                ResultGrid.ItemsSource = results;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Query Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ResultGrid.ItemsSource is IEnumerable<WmiProperty> results)
                {
                    // 讓使用者選擇存檔位置
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

                        MessageBox.Show($"CSV 已匯出至 {dialog.FileName}", "Export Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show("目前沒有資料可匯出。", "No Data", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"匯出失敗: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
