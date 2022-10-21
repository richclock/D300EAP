using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;



namespace TCP
{
    public class OnTCPConnectionArgs : EventArgs {
        public string Msg { get; set; }
        public TcpClient TcpClient { get; set; }
    }
    public class Server
    {
        public event EventHandler<OnTCPConnectionArgs> OnTCPConnection;
        public void DoUpdateConnection(OnTCPConnectionArgs OnTCPConnectionArgs)
        {
            EventHandler<OnTCPConnectionArgs> _connection
                = new EventHandler<OnTCPConnectionArgs>(OnTCPConnection);

            if (_connection != null) {
                OnTCPConnectionArgs e = new OnTCPConnectionArgs();
                e.Msg = OnTCPConnectionArgs.Msg;

                if (_connection != null)
                    _connection(this, e);//fire event

            }
        }
        public TcpClient _tmpTcpClient = null;
        TcpListener _tcpListener = null;
        public int Port { get; set; }
        public Server(int Port)
        {
            this.Port = Port;
        }
        
        public async Task StartAsync()
        {
            
            //取得本機IP
            IPAddress ip = GetLocalIP();

            //建立本機端的IPEndPoint物件
            IPEndPoint ipe = new IPEndPoint(ip, this.Port);

            //建立TcpListener物件
            _tcpListener = new TcpListener(ipe);
            
            
            //開始監聽port
            _tcpListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            _tcpListener.Start(5000);

            await Task.Run(() => {
                //TcpClient tmpTcpClient;
                int numberOfClients = 0;
                while (true) {
                    try {
                        //建立與客戶端的連線
                        _tmpTcpClient = _tcpListener.AcceptTcpClient();
                        
                        if (_tmpTcpClient.Connected) {

                            object obj = (object)_tmpTcpClient;
                            Thread myThread = new Thread(new ParameterizedThreadStart(Communicate));
                            numberOfClients += 1;
                            myThread.IsBackground = true;
                            myThread.Start(obj);
                            myThread.Name = _tmpTcpClient.Client.RemoteEndPoint.ToString();
                        }
                    }
                    catch (Exception ex) {
                        string aa = ex.ToString();
                    }
                }
            });
            
        } 
        public void Stop()
        {


            try {
                _tcpListener.Stop();
                _tcpListener = null;
                //_tmpTcpClient.Close();
                //_tmpTcpClient = null;
                //await Task.Run(() => {

                //});
            }
            catch (Exception ex) {
                string aa = ex.ToString();
            }
            GC.Collect();
            
        }
        private IPAddress GetLocalIP()
        {
            List<IPAddress> ipList = new List<IPAddress>();
            IPAddress ip = null;
            string strHostName = Dns.GetHostName();
            // 取得本機的IpHostEntry類別實體，用這個會提示已過時
            //IPHostEntry iphostentry = Dns.GetHostByName(strHostName);

            // 取得本機的IpHostEntry類別實體，MSDN建議新的用法
            IPHostEntry iphostentry = Dns.GetHostEntry(strHostName);

            // 取得所有 IP 位址
            foreach (IPAddress ipaddress in iphostentry.AddressList) {
                // 只取得IP V4的Address
                if (ipaddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) {
                    ipList.Add(ipaddress);
                }
            }
            if (ipList.Count > 1) {
                foreach (IPAddress ipaddress in ipList) {
                    if (!ipaddress.ToString().Contains("192.168")) {
                        ip = ipaddress;
                    }
                }
            }
            else {
                ip = ipList[0];
            }

            return ip;
        }
        private void SendMsg(string msg, TcpClient tmpTcpClient)
        {
            
            try {
                NetworkStream ns = tmpTcpClient.GetStream();
                if (ns.CanWrite) {
                    byte[] msgByte = Encoding.Default.GetBytes(msg);
                    ns.Write(msgByte, 0, msgByte.Length);
                }
            }
            catch (Exception ex) { }
        }
        private string ReceiveMsg(TcpClient tmpTcpClient)
        {
            string receiveMsg = string.Empty;
            byte[] receiveBytes = new byte[tmpTcpClient.ReceiveBufferSize];
            int numberOfBytesRead = 0;
            

            try {
                NetworkStream ns = tmpTcpClient.GetStream();
                if (ns.CanRead) {
                    do {
                        numberOfBytesRead = ns.Read(receiveBytes, 0, tmpTcpClient.ReceiveBufferSize);
                        receiveMsg = Encoding.Default.GetString(receiveBytes, 0, numberOfBytesRead);
                    }
                    while (ns.DataAvailable);
                }
            }
            catch (Exception ex) {
                receiveMsg = "";
            }
            return receiveMsg;
        }
        private void Communicate(object TcpClient)
        {
            string[] ss = null;
            TcpClient tcpClient = (TcpClient)TcpClient;
            OnTCPConnectionArgs e = new OnTCPConnectionArgs();
            try {
                string msg = ReceiveMsg(tcpClient);
                SendMsg("response", tcpClient);
                #region 設定OnUpdateConnectionArgs
                e.Msg = msg;
                DoUpdateConnection(e);
                
                #endregion
            }
            catch {
                e = null;
                DoUpdateConnection(e);
                tcpClient.Close();
            }
            tcpClient.Close();
        }
        
    }
    public class Client {
        public event EventHandler<OnTCPConnectionArgs> OnTCPConnection;
        public void DoUpdateConnection(OnTCPConnectionArgs OnTCPConnectionArgs) {
            EventHandler<OnTCPConnectionArgs> _connection
                = new EventHandler<OnTCPConnectionArgs>(OnTCPConnection);

            if (_connection != null) {
                OnTCPConnectionArgs e = new OnTCPConnectionArgs {
                    Msg = OnTCPConnectionArgs.Msg
                };

                if (_connection != null)
                    _connection(this, e);//fire event

            }
        }
        public TcpClient _tcpClient = null;
        public string HostIP { get; set; }
        public int Port { get; set; }
        public bool Connected { get; private set; }
        public Client(string ip, int port) {
            _tcpClient = new TcpClient();
            this.HostIP = ip;
            this.Port = port;
            Connect();
        }
        public Client(TcpClient tcpClient) {
            _tcpClient = tcpClient;
            IPEndPoint ipEndPoint = (IPEndPoint)_tcpClient.Client.RemoteEndPoint;
            this.HostIP = ipEndPoint.Address.ToString();
            this.Port = ipEndPoint.Port;
            this.Connected = true;
        }
        public void Connect() {
            try {
                string hostIP = this.HostIP;
                IPAddress ipa = IPAddress.Parse(hostIP);
                IPEndPoint ipe = new IPEndPoint(ipa, this.Port);
                if (!_tcpClient.Connected) {
                    _tcpClient.Connect(ipe);
                    this.Connected = true;
                }
            } catch (Exception ex) { }
        }
        public void Close() {
            try {
                _tcpClient.Close();
                _tcpClient.Dispose();
                //_tcpClient = null;
                this.Connected = false;
            } catch (Exception ex) { }
        }

        public string SendData(string msg) {
            string response = "";
            //Connect();
            try {
                if (_tcpClient.Connected) {
                    SendMsg(msg, _tcpClient);
                }
            } catch (Exception ex) {
                _tcpClient.Close();
            }
            //TcpClient.Close();
            return response;
        }
        public async void ReceiveDataAsync() {
            string msg = "";

            await Task.Run(() => {
                msg = ReceiveMsg();
            });

        }

        private void SendMsg(string msg, TcpClient tmpTcpClient) {
            try {
                NetworkStream ns = tmpTcpClient.GetStream();
                if (ns.CanWrite) {
                    byte[] msgByte = Encoding.UTF8.GetBytes(msg);
                    ns.Write(msgByte, 0, msgByte.Length);
                }
            } catch (Exception ex) {
            }

        }
        private string ReceiveMsg() {
            string receiveMsg = string.Empty;
            byte[] receiveBytes = new byte[_tcpClient.ReceiveBufferSize];
            int numberOfBytesRead = 0;
            string[] ss = null;
            while (_tcpClient.Connected) {
                try {
                    NetworkStream ns = _tcpClient.GetStream();
                    if (ns.CanRead) {
                        do {
                            numberOfBytesRead = ns.Read(receiveBytes, 0, _tcpClient.ReceiveBufferSize);
                            if (numberOfBytesRead != 0) {
                                receiveMsg = Encoding.UTF8.GetString(receiveBytes, 0, numberOfBytesRead);
                                #region 設定OnUpdateConnectionArgs
                                OnTCPConnectionArgs e = new OnTCPConnectionArgs {
                                    TcpClient = _tcpClient,
                                    Msg = receiveMsg
                                };
                                DoUpdateConnection(e);
                                #endregion
                            } else {
                                Close();
                                this.Connected = false;
                                return "";
                            }
                        }
                        while (ns.DataAvailable);
                    }
                } catch (Exception ex) {
                    receiveMsg = "";
                }
            }

            return receiveMsg;
        }
        private IPAddress GetLocalIP() {
            List<IPAddress> ipList = new List<IPAddress>();
            IPAddress ip = null;
            string strHostName = Dns.GetHostName();
            // 取得本機的IpHostEntry類別實體，用這個會提示已過時
            //IPHostEntry iphostentry = Dns.GetHostByName(strHostName);

            // 取得本機的IpHostEntry類別實體，MSDN建議新的用法
            IPHostEntry iphostentry = Dns.GetHostEntry(strHostName);

            // 取得所有 IP 位址
            foreach (IPAddress ipaddress in iphostentry.AddressList) {
                // 只取得IP V4的Address
                if (ipaddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) {
                    ipList.Add(ipaddress);
                }
            }
            if (ipList.Count > 1) {
                foreach (IPAddress ipaddress in ipList) {
                    if (!ipaddress.ToString().Contains("192.168")) {
                        ip = ipaddress;
                    }
                }
            } else {
                ip = ipList[0];
            }

            return ip;
        }
    }
}
