using System;
using System.Management;
using System.Windows;

namespace WmiQueryTool
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void RunQuery_Click(object sender, RoutedEventArgs e)
        {
            ResultBox.Items.Clear();
            try
            {
                string query = QueryBox.Text;
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);

                foreach (ManagementObject obj in searcher.Get())
                {
                    foreach (var prop in obj.Properties)
                    {
                        ResultBox.Items.Add($"{prop.Name}: {prop.Value}");
                    }
                    ResultBox.Items.Add("----");
                }
            }
            catch (Exception ex)
            {
                ResultBox.Items.Add($"Error: {ex.Message}");
            }
        }
    }
}