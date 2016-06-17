using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace LANSupervisorClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private readonly TcpClient client = new TcpClient();
        private NetworkStream mainStream;
        Thread FastConnectionThread;
        private readonly UdpClient BroadcastClient = new UdpClient();
        Thread BroadcastThread;
        bool Broadcastbool = false;
        public int PortToConnect = 0;

        public System.Windows.Forms.Timer timer1; 

        public static Socket Client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
       
        public static IPEndPoint PClientIEP;
        DispatcherTimer BroadcastConnectionTimer = new DispatcherTimer();
        DispatcherTimer SlowConnectionTimer = new DispatcherTimer();
        DispatcherTimer FastConnectionTimer = new DispatcherTimer();
        public static string SERVIP;
        public MainWindow()
        {
            InitializeComponent();

            string myIP = GetMyComputer_LanIP();

            string toset = "";

            string[] words = myIP.Split('.');

            for (int i = 0; i < 3; i++)
            {
                toset += words[i] + ".";
            }

            HostIP_TB.Text = toset;

            IPAddress ClientIP = IPAddress.Parse(GetMyComputer_LanIP());
            IPEndPoint ClientIEP = new IPEndPoint(ClientIP, 12414);
            PClientIEP = ClientIEP;
            Client.Bind(PClientIEP);

            SlowConnectionTimer.Interval = TimeSpan.FromMilliseconds(1000);
            SlowConnectionTimer.Tick += SlowConnectionTimerTick;

            FastConnectionTimer.Interval = TimeSpan.FromMilliseconds(100);
            FastConnectionTimer.Tick += FastConnectionTimerTick;

            BroadcastConnectionTimer.Interval = TimeSpan.FromMilliseconds(3000);
            BroadcastConnectionTimer.Tick += BroadcastConnectionTimerTick;
            BroadcastConnectionTimer.Start();

            //BroadcastThread = new Thread(()=>Broadcast());
            //BroadcastThread.Start();
            InitTimer();

          

            FastConnectionThread = new Thread(()=>CheckFastConnection());
            FastConnectionThread.Start();
        }

        private void BroadcastConnectionTimerTick(object sender, EventArgs e)
        {
            if (Broadcastbool != true)
            {
                IPEndPoint BroadcastIP = new IPEndPoint(IPAddress.Broadcast, 415);
                byte[] Info = GetBytes("WhoIsMaster");
                BroadcastClient.Send(Info, Info.Length, BroadcastIP);
            }
            else if (Broadcastbool == true)
            {
                BroadcastConnectionTimer.Stop();
            }
        }

        private void FastConnectionTimerTick(object sender, EventArgs e)
        {
            SendDesktopImage();
        }

        private void SlowConnectionTimerTick(object sender, EventArgs e)
        {
            SendDesktopImage();
        }

        private void Broadcast()
        {
            while (true)
            {
                if (Broadcastbool != true)
                {
                    IPEndPoint BroadcastIP = new IPEndPoint(IPAddress.Broadcast, 415);
                    byte[] Info = GetBytes("WhoIsMaster");
                    BroadcastClient.Send(Info, Info.Length, BroadcastIP);  

                    Thread.Sleep(3000);
                }
                else if (Broadcastbool == true)
                {
                }
            }

        }

        public void InitTimer()
        {
            timer1 = new System.Windows.Forms.Timer();
            timer1.Tick += new EventHandler(timer1_Tick);
            timer1.Interval = 1000;
            timer1.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (Broadcastbool == false)
            {
                ConnectionAfterBroadcast();
            }
            else
            {

            }
        }

        private void CheckFastConnection()
        {
            while (true)
            {
           
            IPEndPoint ServerIEP = new IPEndPoint(IPAddress.Any, 12414);
            EndPoint ServerEP = (EndPoint)(ServerIEP);
            byte[] Response = new byte[200];
            
                Client.ReceiveFrom(Response,ref ServerEP);
                string rgvtvyv = GetString(Response);
                string[] messageReceived = SplitString2(rgvtvyv);
                if (messageReceived[0].Contains("STARTFASTCONNECTION"))
                {
                    SlowConnectionTimer.Stop();
                    FastConnectionTimer.Start();
                }
                else if (messageReceived[0].Contains("STOPFASTCONNECTION"))
                {
                    FastConnectionTimer.Stop();
                    SlowConnectionTimer.Start();
                }
                else if (messageReceived[0].Contains("SHOWPROCESESS"))
                {
                    Process[] process = Process.GetProcesses();
                    string processes = "PROCESESS;";
                    foreach (Process prs in process)
                    {
                        double size = prs.PrivateMemorySize64 / 1024;
                        processes += prs.ProcessName + ";" + size+ ";";
                    }
                    
                    IPEndPoint ServerIEPPROC = new IPEndPoint(IPAddress.Parse(SERVIP), 415);
                    EndPoint ServerEPPRC = (EndPoint)(ServerIEPPROC);
                    byte[] pro = GetBytes(processes);
                    Client.SendTo(pro, ServerEPPRC);
                }
            }
        }


        private void ConnectionAfterBroadcast()
        {
            IPEndPoint ServerIEP = new IPEndPoint(IPAddress.Any, 415);
            
            
            EndPoint ServerEP = (EndPoint)(ServerIEP);
            byte[] Response = new byte[200];
            Response = BroadcastClient.Receive(ref ServerIEP);
            string rgvtvyv = GetString(Response);
            string[] messageReceived = SplitString2(rgvtvyv);
                if (messageReceived[0] == "CONNECTIONPORT" && messageReceived[1] != "")
                 {
                        PortToConnect = Int32.Parse(messageReceived[1]);
                        
                        HostIP_TB.Text = messageReceived[2];
                        SERVIP = HostIP_TB.Text;
                        Broadcastbool = true;
                        ConfirmButton.IsEnabled = true;
                 }
                



                
                

            


        }

        private void SendDesktopImage()
        {
            try
            {
                BinaryFormatter binFormatter = new BinaryFormatter();
                mainStream = client.GetStream();
              //  binFormatter.Serialize(mainStream, GrabDesktop());
                binFormatter.Serialize(mainStream, CaptureScreen(true));
            }
            catch
            {
                SlowConnectionTimer.Stop();
                FastConnectionTimer.Stop();
                //powiadom o przerwaniu poł
            }
        }


        private string GetMyComputer_LanIP()
        {
            string strHostName = System.Net.Dns.GetHostName();

            IPHostEntry ipEntry = System.Net.Dns.GetHostEntry(strHostName);

            foreach (IPAddress ipAddress in ipEntry.AddressList)
            {
                if (ipAddress.AddressFamily.ToString() == "InterNetwork")
                {
                    return ipAddress.ToString();
                }
            }

            return "-";
        }

        private void RegisterInStartup(bool isChecked)
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey
                    ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (isChecked)
            {
                registryKey.SetValue("LANSupervisorClient", System.Reflection.Assembly.GetExecutingAssembly().Location);
            }
            else
            {
                registryKey.DeleteValue("LANSupervisorClient");
            }
        }

        private static System.Drawing.Image GrabDesktop()
        {
            System.Drawing.Rectangle bounds = Screen.PrimaryScreen.Bounds;
            Bitmap screenshot = new Bitmap(bounds.Width, bounds.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Graphics graphic = Graphics.FromImage(screenshot);
            graphic.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
            return screenshot;
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            string HostIP = HostIP_TB.Text.Trim();
            int PortNumber = 12300;
            string[] words = HostIP.Split('.');

            if (words.Length != 4 || words[3] == "")
            {
                System.Windows.MessageBox.Show("Wpisz poprawny adres IP");
            }
            else
            {
                try
                {
                    IPAddress IpServer = IPAddress.Parse(HostIP);
                    IPEndPoint ServerIEP = new IPEndPoint(IpServer, 415);
                    EndPoint ServerEP = (EndPoint)(ServerIEP);

                    //string OnlineMessage = "STARTCONNECTION"; ;
                    //byte[] TempForOnline = GetBytes(OnlineMessage);
                    //Client.SendTo(TempForOnline, TempForOnline.Length, SocketFlags.None, ServerIEP);

                    //byte[] Response = new byte[200];

                    //Client.ReceiveFrom(Response, ref ServerEP);

                    //string rgvtvyv = GetString(Response);
                    //string[] messageReceived = SplitString2(rgvtvyv);

                    //if (messageReceived[0] == "CONNECTIONPORT" && messageReceived[1] != "")
                    //{
                        
                        client.Connect(HostIP, PortToConnect);
                        SlowConnectionTimer.Start();
                        ConnectedTB.Text += " " + HostIP;
                        ConnectedTB.Visibility = Visibility.Visible;
                        //System.Windows.MessageBox.Show("Connected!");
                        Broadcastbool = true;
                    //}

                }

                catch (Exception ee)
                {
                    System.Windows.MessageBox.Show("Nie można połączyć z nadzorcą. Sprawdź poprawność adresu IP oraz czy aplikacja nadzorcy jest włączona." + ee.Message);
                }

            }
        }



        public static string GetString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }

        public static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public string[] SplitString2(string InfoToSplit)
        {
            string[] SplittedInfo;
            SplittedInfo = InfoToSplit.Split('_');
            SplittedInfo[SplittedInfo.Length - 1].Replace("\0", string.Empty);
            SplittedInfo[SplittedInfo.Length - 1].Trim();
            return SplittedInfo;
        }



        [StructLayout(LayoutKind.Sequential)]
        struct CURSORINFO
        {
            public Int32 cbSize;
            public Int32 flags;
            public IntPtr hCursor;
            public POINTAPI ptScreenPos;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct POINTAPI
        {
            public int x;
            public int y;
        }

        [DllImport("user32.dll")]
        static extern bool GetCursorInfo(out CURSORINFO pci);

        [DllImport("user32.dll")]
        static extern bool DrawIcon(IntPtr hDC, int X, int Y, IntPtr hIcon);

        const Int32 CURSOR_SHOWING = 0x00000001;

        public static Bitmap CaptureScreen(bool CaptureMouse)
        {
            Bitmap result = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            try
            {
                using (Graphics g = Graphics.FromImage(result))
                {
                    g.CopyFromScreen(0, 0, 0, 0, Screen.PrimaryScreen.Bounds.Size, CopyPixelOperation.SourceCopy);

                    if (CaptureMouse)
                    {
                        CURSORINFO pci;
                        pci.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(CURSORINFO));

                        if (GetCursorInfo(out pci))
                        {
                            if (pci.flags == CURSOR_SHOWING)
                            {
                                DrawIcon(g.GetHdc(), pci.ptScreenPos.x, pci.ptScreenPos.y, pci.hCursor);
                                g.ReleaseHdc();
                            }
                        }
                    }
                }
            }
            catch
            {
                result = null;
            }

            return result;
        }


    }
}
