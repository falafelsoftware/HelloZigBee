using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace HelloZigBee
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //using Windows.Devices.SerialCommunication; 
        private SerialDevice _zbCoordinator = null;
        //using Windows.Storage.Streams;
        private DataWriter _dataWriter = null;
        private DataReader _dataReader = null;

        public MainPage()
        {
            this.InitializeComponent();
            btnConnect.IsEnabled = true;
            btnSend.IsEnabled = false;
            btnRead.IsEnabled = false;
        }

        private async void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            string deviceQuery = SerialDevice.GetDeviceSelector();
            
            //using Windows.Devices.Enumeration
            var deviceInfo = await DeviceInformation.FindAllAsync(deviceQuery);

            if (deviceInfo != null && deviceInfo.Count > 0)
            {
                //your board name may differ, introspect the return value of deviceInfo to 
                //determine the serial hardware you have your modem attached to
                var serialBoardName = "CP2102 USB to UART Bridge Controller";
                var zbInfo = deviceInfo.Where(x => x.Name.Equals(serialBoardName)).First();
                _zbCoordinator = await SerialDevice.FromIdAsync(zbInfo.Id);

                // Configure serial settings
                _zbCoordinator.WriteTimeout = TimeSpan.FromMilliseconds(1000);
                _zbCoordinator.ReadTimeout = TimeSpan.FromMilliseconds(1000);
                _zbCoordinator.BaudRate = 9600;
                _zbCoordinator.Parity = SerialParity.None;
                _zbCoordinator.StopBits = SerialStopBitCount.One;
                _zbCoordinator.DataBits = 8;

                btnConnect.IsEnabled = false;
                btnSend.IsEnabled = true;
                btnRead.IsEnabled = true;
            }
            else
            {
                this.txtHeader.Text = "Something went wrong :( -- Device not found";
            }

        }

        private async void btnSend_Click(object sender, RoutedEventArgs e)
        {
            var writeResult = await Write();
            txtHeader.Text = writeResult.TextResult;
        }

        private async void btnRead_Click(object sender, RoutedEventArgs e)
        {
            var readResult = await Read();
            txtHeader.Text = readResult.TextResult;
        }

        //using System.Threading.Tasks
        private async Task<ZigBeeCommResult> Read()
        {
            var retvalue = new ZigBeeCommResult();
            try
            {
                _dataReader = new DataReader(_zbCoordinator.InputStream);
                var numBytesRecvd = await _dataReader.LoadAsync(1024);
                retvalue.IsSuccessful = true;
                if (numBytesRecvd > 0)
                {
                    retvalue.TextResult = _dataReader.ReadString(numBytesRecvd).Trim();
                }
            }
            catch (Exception ex)
            {
                retvalue.IsSuccessful = false;
                retvalue.TextResult = ex.Message;
            }
            finally
            {
                if (_dataReader != null)
                {
                    _dataReader.DetachStream();
                    _dataReader = null;
                }
            }
            return retvalue;
        }

        private async Task<ZigBeeCommResult> Write()
        {
            ZigBeeCommResult retvalue = new ZigBeeCommResult();
            try
            {
                _dataWriter = new DataWriter(_zbCoordinator.OutputStream);
                //send the message
                var numBytesWritten = _dataWriter.WriteString(txtToSend.Text);
                await _dataWriter.StoreAsync();
                retvalue.IsSuccessful = true;
                retvalue.TextResult = "Text has been successfully sent";
            }
            catch (Exception ex)
            {
                retvalue.IsSuccessful = false;
                retvalue.TextResult = ex.Message;
            }
            finally
            {
                if (_dataWriter != null)
                {
                    _dataWriter.DetachStream();
                    _dataWriter = null;
                }
            }
            return retvalue;
        }

        internal class ZigBeeCommResult
        {
            public bool IsSuccessful { get; set; }
            public string TextResult { get; set; }

            public ZigBeeCommResult()
            {
                this.IsSuccessful = false;
                this.TextResult = "";
            }
        }
    }
}
