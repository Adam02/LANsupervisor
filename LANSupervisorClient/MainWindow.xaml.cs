using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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

        private readonly UdpClient BroadcastClient = new UdpClient();
        Thread BroadcastThread;
        bool Broadcastbool = false;
        public int PortToConnect = 0;

        public System.Windows.Forms.Timer timer1; 

        public static Socket Client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        DispatcherTimer SlowConnectionTimer = new DispatcherTimer();

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
            IPEndPoint ClientIEP = new IPEndPoint(ClientIP, 9200);
            Client.Bind(ClientIEP);

            SlowConnectionTimer.Interval = TimeSpan.FromMilliseconds(1000);
            SlowConnectionTimer.Tick += SlowConnectionTimerTick;
            BroadcastThread = new Thread(()=>Broadcast());
            BroadcastThread.Start();
            InitTimer();
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
                        Broadcastbool = true;
                        ConfirmButton.IsEnabled = true;
                 }


                
                

            


        }

        private void SendDesktopImage()
        {
            BinaryFormatter binFormatter = new BinaryFormatter();
            mainStream = client.GetStream();
            binFormatter.Serialize(mainStream, GrabDesktop());
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
                        System.Windows.MessageBox.Show("Connected!");
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

    }
}
