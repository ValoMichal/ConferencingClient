using Microsoft.VisualBasic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using AForge;
using AForge.Imaging;
using AForge.Imaging.Formats;
using AForge.Math;
using AForge.Video;
using AForge.Video.DirectShow;
using Accord;
using Accord.Video;
using System.Collections;
using System.Drawing.Imaging;
using System.IO.Ports;
using System.Globalization;
using Accord.Collections;
using System.Runtime.CompilerServices;
using System.Printing.IndexedProperties;
using System.Windows.Interop;
using NAudio.Wave;
using System.Diagnostics.Tracing;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using NAudio.Wave.SampleProviders;
using Concentus;
using Concentus.Structs;
using Concentus.Enums;
using Microsoft.VisualBasic.ApplicationServices;

namespace ConferencingClient
{
    public partial class MainWindow : Window
    {
        private int camNum = 0;
        private int micNum = 0;
        private static bool camMute = false;
        private static bool micMute = false;
        private static WaveInEvent waveIn;
        private static WaveOutEvent waveOut;
        private static BufferedWaveProvider waveProvider;
        private static OpusEncoder opusEncoder;
        private static OpusDecoder opusDecoder;
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;
        private static int chunkSize = 65000;
        private int id = 0;
        private String host = new String("");
        private static IPEndPoint server = new IPEndPoint(IPAddress.Any, 0);
        UdpClient comms = new UdpClient(5000);
        UdpClient portChatUp = new UdpClient(5001);
        UdpClient portAudioUp = new UdpClient(5002);
        UdpClient portVideoUp = new UdpClient(5003);
        UdpClient portChatDown = new UdpClient(5004);
        UdpClient portAudioDown = new UdpClient(5005);
        UdpClient portVideoDown = new UdpClient(5006);
        public MainWindow()
        {
            InitializeComponent();
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            try
            {
                InitializeCamera();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private void HideChat(object sender, EventArgs e)
        {
            if (ChatPanel.Visibility == Visibility.Visible)
            {
                ChatPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                ChatPanel.Visibility = Visibility.Visible;
            }
        }
        private void HideSettings(object sender, EventArgs e)
        {
            if (Settings.Visibility == Visibility.Visible)
            {
                Settings.Visibility = Visibility.Collapsed;
            }
            else
            {
                Settings.Visibility = Visibility.Visible;
            }
        }
        private void SwitchCam(object sender, EventArgs e)
        {
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if (camNum < videoDevices.Count - 1)
            {
                camNum++;
            }
            else
            {
                camNum = 0;
            }
            ((Button)sender).ToolTip = videoDevices[camNum].Name;
            CamOff(null, null);
            InitializeCamera();
        }
        private void SwitchMic(object sender, EventArgs e)
        {
            if (micNum < WaveInEvent.DeviceCount - 1)
            {
                micNum++;
            }
            else
            {
                micNum = 0;
            }
            ((Button)sender).ToolTip = WaveInEvent.GetCapabilities(micNum).ProductName;
            AudioUp(null, null);
        }
        private void MuteCam(object sender, EventArgs e)
        {
            camMute = !camMute;
            if (camMute)
            {
                Cam.Background = System.Windows.Media.Brushes.Red;
            }
            else
            {
                Cam.Background = System.Windows.Media.Brushes.LightGray;
            }
        }
        private void MuteMic(object sender, EventArgs e)
        {
            micMute = !micMute;
            if (micMute)
            {
                Mic.Background = System.Windows.Media.Brushes.Red;
            }
            else
            {
                Mic.Background = System.Windows.Media.Brushes.LightGray;
            }
        }
        private void InitializeCamera()
        {
            try
            {
                if (videoDevices.Count > 0)
                {
                    videoSource = new VideoCaptureDevice(videoDevices[camNum].MonikerString);
                    videoSource.NewFrame += VideoSource_NewFrame;
                    videoSource.Start();
                }
                else
                {
                    MessageBox.Show("No camera devices found.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Please check your camera and try again.");
            }
        }
        private void VideoSource_NewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {
            //
            if (host.Equals(""))
            {
                if (camMute)
                {
                    Bitmap newFrame = new Bitmap(1920, 1080);
                    using (Graphics graph = Graphics.FromImage(newFrame))
                    {
                        System.Drawing.Rectangle ImageSize = new System.Drawing.Rectangle(0, 0, 1920, 1080);
                        graph.FillRectangle(System.Drawing.Brushes.Black, ImageSize);
                    }
                    this.Dispatcher.Invoke(() =>
                    {
                        Display.Source = Utils.BitmapToImageSource(newFrame);
                    });
                }
                else
                {
                    Bitmap newFrame = (Bitmap)eventArgs.Frame.Clone();
                    try
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            Display.Source = Utils.BitmapToImageSource(newFrame);
                        });
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                        Environment.Exit(Environment.ExitCode);
                    }
                }
            }
            else
            {
                VideoUp(sender, eventArgs);
                AudioUp(null, null);
            }
            //
            /*
            if (camMute)
            {
                Bitmap newFrame = new Bitmap(1920, 1080);
                using (Graphics graph = Graphics.FromImage(newFrame))
                {
                    Rectangle ImageSize = new Rectangle(0, 0, 1920, 1080);
                    graph.FillRectangle(System.Drawing.Brushes.Black, ImageSize);
                }
                this.Dispatcher.Invoke(() =>
                {
                    Display.Source = Utils.BitmapToImageSource(newFrame);
                });
            }
            else
            {
                Bitmap newFrame = (Bitmap)eventArgs.Frame.Clone();
                this.Dispatcher.Invoke(() =>
                {
                    Display.Source = Utils.BitmapToImageSource(newFrame);
                });
            }
            if (users.Count != 0)
            {
                VideoUp(null, null);
                AudioUp(null, null);
            }
            */
        }
        internal static class Utils
        {
            public static ImageSource BitmapToImageSource(Bitmap bitmap)
            {
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                var memoryStream = new MemoryStream();

                bitmap.Save(memoryStream, ImageFormat.Bmp);
                memoryStream.Seek(0, SeekOrigin.Begin);
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }
        private void CamOff(object sender, EventArgs e)
        {
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.NewFrame -= VideoSource_NewFrame;
                //Dispatcher.InvokeShutdown();
                videoSource.SignalToStop();
                //videoSource.WaitForStop();
            }
        }
        private void Connect(object sender, RoutedEventArgs e)
        {
            Settings.Visibility = Visibility.Collapsed;
            host = ip.Text;
            Task.Run(() =>
            {
                try
                {
                    List<String> output = new List<String>();
                    output.Add("connect");
                    output.Add(Username.Text);
                    string sendText = string.Join(Environment.NewLine, output);
                    byte[] sendBytes = Encoding.ASCII.GetBytes(sendText);
                    comms.Send(sendBytes, sendBytes.Length, host, 5000);
                    output.Clear();
                    //comms.Client.ReceiveTimeout = 2500;
                    byte[] recvBytes = comms.Receive(ref server);
                    string recvText = Encoding.ASCII.GetString(recvBytes);
                    List<string> input = new List<string>(recvText.Split(new[] { Environment.NewLine }, StringSplitOptions.None));
                    id = Int32.Parse(input[0]);
                    //server.Port.ToString()
                    this.Dispatcher.Invoke(() =>
                    {
                        ChatAdd(sender, e, "Connected.");
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    //Debug.WriteLine("Attempting to reconnect in 2,5s");
                    //Thread.Sleep(2500);
                }
                Task.Run(() => VideoDown(null, null));
                Task.Run(() => AudioDown(null, null));
                Task.Run(() => ChatDown(null, null));
            });
        }
        private void ChatUpEnter(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter || e.Key == System.Windows.Input.Key.Return)
            {
                ChatUp(null, null);
                e.Handled = true;
            }
        }
        private void ChangeNameEnter(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter || e.Key == System.Windows.Input.Key.Return)
            {
                ChangeName(null, null);
                e.Handled = true;
            }
        }
        private void ConnectEnter(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter || e.Key == System.Windows.Input.Key.Return)
            {
                Connect(null, null);
                e.Handled = true;
            }
        }
        private void ChangeName(object sender, RoutedEventArgs e)
        {
            List<String> output = new List<String>();
            output.Add("rename");
            output.Add(Username.Text);
            string sendText = string.Join(Environment.NewLine, output);
            byte[] sendBytes = Encoding.ASCII.GetBytes(sendText);
            comms.Send(sendBytes, sendBytes.Length, host, 5000);
            output.Clear();
            comms.Receive(ref server);
        }
        private void ChatAdd(object sender, RoutedEventArgs e, String msg)
        {
            TextBlock newTextBlock = new TextBlock();
            newTextBlock.Text = msg;
            newTextBlock.TextWrapping = TextWrapping.Wrap;
            Chat.Children.Insert(Chat.Children.Count, newTextBlock);
            if (Chat.Children.Count > 25)
            {
                Chat.Children.RemoveAt(0);
            }
            scrollViewer.ScrollToEnd();
        }
        private void ChatUp(object sender, RoutedEventArgs e)
        {
            try
            {
                string sendText = Msg.Text;
                Msg.Text = "";
                byte[] sendBytes = Encoding.ASCII.GetBytes(sendText);
                portChatUp.Send(sendBytes, sendBytes.Length, host, 5001 + 6 * id);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private void AudioUp(object sender, RoutedEventArgs e)
        {
            if (waveIn != null)
            {
                waveIn.Dispose();
            }
            waveIn = new WaveInEvent
            {
                DeviceNumber = micNum,
                WaveFormat = new WaveFormat(44100, 16, 1)
            };
            waveIn.DataAvailable += (sender, e) => WaveIn_DataAvailable(sender, e, portAudioUp);
            waveIn.StartRecording();

        }
        private void WaveIn_DataAvailable(object sender, WaveInEventArgs e, UdpClient port)
        {
            if (!micMute&&!host.Equals(""))
            {
                port.Send(e.Buffer, e.BytesRecorded, host, 5002 + 6 * id);
            }
        }
        private void VideoUp(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                if (camMute)
                {
                    Bitmap bmp = new Bitmap(1920, 1080);
                    using (Graphics graph = Graphics.FromImage(bmp))
                    {
                        System.Drawing.Rectangle ImageSize = new System.Drawing.Rectangle(0, 0, 1920, 1080);
                        graph.FillRectangle(System.Drawing.Brushes.Black, ImageSize);
                    }
                    bmp.Save(ms, ImageFormat.Jpeg);
                }
                else
                {
                    eventArgs.Frame.Save(ms, ImageFormat.Jpeg);
                }
                byte[] buffer = ms.ToArray();
                int totalChunks = (buffer.Length + chunkSize - 1) / chunkSize;
                for (int i = 0; i < totalChunks; i++)
                {
                    int currentChunkSize = Math.Min(chunkSize, buffer.Length - i * chunkSize);
                    byte[] chunk = new byte[currentChunkSize + 4];
                    BitConverter.GetBytes(i).CopyTo(chunk, 0);
                    Array.Copy(buffer, i * chunkSize, chunk, 4, currentChunkSize);
                    try
                    {
                        portVideoUp.Send(chunk, chunk.Length, host, 5003 + 6 * id);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                }
            }
        }
        private void ChatDown(object sender, RoutedEventArgs e)
        {
            while (true)
            {
                try
                {
                    byte[] recvBytes = portChatDown.Receive(ref server);
                    string recvText = Encoding.ASCII.GetString(recvBytes);
                    this.Dispatcher.Invoke(() =>
                    {
                        ChatAdd(sender, e, recvText);
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
        private void AudioDown(object sender, RoutedEventArgs e)
        {
            waveProvider = new BufferedWaveProvider(new WaveFormat(44100, 16, 1));
            waveOut = new WaveOutEvent();
            waveOut.Init(waveProvider);
            waveOut.Play();
            StartReceiving(portAudioDown);
        }
        private static void StartReceiving(UdpClient port)
        {
            port.BeginReceive((ar) => OnDataReceived(ar, port), null);
        }
        private static void OnDataReceived(IAsyncResult ar, UdpClient port)
        {
            byte[] receivedBytes = port.EndReceive(ar, ref server);
            waveProvider.AddSamples(receivedBytes, 0, receivedBytes.Length);
            StartReceiving(port);
        }
        private void VideoDown(object sender, RoutedEventArgs e)
        {
            Dictionary<int, byte[]> receivedChunks = new Dictionary<int, byte[]>();
            try
            {
                byte[] data = portVideoDown.Receive(ref server);
                int chunkIndex = BitConverter.ToInt32(data, 0);
                byte[] chunkData = new byte[data.Length - 4];
                Array.Copy(data, 4, chunkData, 0, chunkData.Length);
                receivedChunks[chunkIndex] = chunkData;
                //Debug.WriteLine($"received chunk {chunkIndex}");
                while (true)
                {
                    data = portVideoDown.Receive(ref server);
                    chunkIndex = BitConverter.ToInt32(data, 0);
                    if (chunkIndex == 0)
                    {
                        int pictureSize = 0;
                        int lengthSum = 0;
                        foreach (var chunk in receivedChunks.OrderBy(kv => kv.Key))
                        {
                            pictureSize += chunk.Value.Length;
                        }
                        byte[] picture = new byte[receivedChunks.Count * chunkSize];
                        foreach (var chunk in receivedChunks.OrderBy(kv => kv.Key))
                        {
                            Array.Copy(chunk.Value, 0, picture, lengthSum, chunk.Value.Length);
                            lengthSum += chunk.Value.Length;
                        }
                        Dispatcher.Invoke(() =>
                        {
                            BitmapImage bitmapImage = ConvertToBitmapImage(picture);
                            Display.Source = bitmapImage;
                        });
                    }
                    chunkData = new byte[data.Length - 4];
                    Array.Copy(data, 4, chunkData, 0, chunkData.Length);
                    receivedChunks[chunkIndex] = chunkData;
                    //Debug.WriteLine($"received chunk {chunkIndex}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
        private BitmapImage ConvertToBitmapImage(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = ms;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }
    }
}
