using System;
using System.Collections.Generic;
using System.Management;
using System.Windows;
using System.Windows.Controls;

namespace WmiQueryTool
{
    // 用 record 定義，不會有 NRT 警告
    public record WmiProperty(string Name, string Value);

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

                foreach (ManagementObject obj in searcher.Get())
                {
                    foreach (var prop in obj.Properties)
                    {
                        results.Add(new WmiProperty(
                            prop.Name,
                            prop.Value?.ToString() ?? ""
                        ));
                    }
                }

                ResultGrid.ItemsSource = results;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Query Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}