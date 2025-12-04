using IPCSoftware.App.Services;
using IPCSoftware.App.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace IPCSoftware.App.Views
{
    public partial class PLCIOView : UserControl
    {
        public PLCIOView()
        {
            InitializeComponent();
            DataContext = new PLCIOViewModel(App.TcpClient);

            // Test: get one packet from PLC
            // TestPLCData();

        }

        //private async void TestPLCData()
        //{
        //    var client = new CoreClient(App.TcpClient);
        //    var data = await client.GetIoValuesAsync();

        //    // Print to Output Window
        //    foreach (var kv in data)
        //    {
        //        System.Diagnostics.Debug.WriteLine($"TagId[{kv.Key}] = {kv.Value}");
        //    }
        //}
        //private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        //{
        //    await TestRequest();
        //}

        //private async Task TestRequest()
        //{
        //    try
        //    {
        //        System.Diagnostics.Debug.WriteLine(">> Sending RequestId=5");

        //        var client = new CoreClient(App.TcpClient);
        //        var result = await client.GetIoValuesAsync();

        //        System.Diagnostics.Debug.WriteLine(">> Response received");

        //        foreach (var kv in result)
        //        {
        //            System.Diagnostics.Debug.WriteLine($"TagId[{kv.Key}] = {kv.Value}");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Diagnostics.Debug.WriteLine(">> ERROR: " + ex.Message);
        //    }
        //}

    }
}
