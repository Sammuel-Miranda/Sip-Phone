namespace SIPLib
{
    public delegate void Del(string Info, string Caption);
    public delegate bool DelRequest(string str);
    public delegate void DelCloseSession(string Name);
    public delegate void DelStopListener();

    public class Session
    {
        private global::SIPLib.DelCloseSession DelClosesession;
        private string ToIP;
        private string ToUser;
        private string MyName;
        private global::System.Net.IPAddress myIP;
        private int n;
        private int port;
        private int myaudioport;
        private int toaudioport;
        private bool SessionConfirmed = false;
        private string SessionID;
        private string _SDP;
        private global::System.Threading.Thread WaitForAnswer;
        public string _ToUser { get { return this.ToUser; } }
        public bool _SessionConfirmed { get { return this.SessionConfirmed; } }
        public string _SessionID { get { return this.SessionID; } }
        public bool CheckSessionByID(string ID) { if (this.SessionID == ID) { return true; } else { return false; } }
        public void CloseSession() { this.DelClosesession(this.MyName); }

        public bool WatchInfo(string Info)
        {
            this.n++;
            if (Info.Contains("BYE")) { this.BYEDecompile(Info); return true; }
            else if (Info.Contains("ACK")) { this._2XXCompile("00", true, false); return true; }
            else if (Info.Contains("CANCEL")) { return true; }
            else if (Info.Contains("REGISTER")) { return true; }
            else if (Info.Contains("OPTIONS")) { this._2XXCompile("00", true, false); return true; }
            else if (Info.Contains("SIP/2.0 1")) { return true; }
            else if (Info.Contains("SIP/2.0 2")) { return true; }
            else if (Info.Contains("SIP/2.0 3")) { return true; }
            else if (Info.Contains("SIP/2.0 5")) { this._5XXDecompile(Info); return true; }
            else if (Info.Contains("SIP/2.0 6")) { this._6XXDecompile(Info); return true; }
            return false;
        }

        private bool SendInfo(string Info)
        {
            global::System.Net.IPAddress ipAddress;
            global::System.Net.Sockets.UdpClient udpClient = new global::System.Net.Sockets.UdpClient();
            byte[] sendBytes = global::System.Text.Encoding.ASCII.GetBytes(Info);
            if (global::System.Net.IPAddress.TryParse(ToIP, out ipAddress)) { try { udpClient.Send(sendBytes, sendBytes.Length, new global::System.Net.IPEndPoint(ipAddress, port)); } catch { return false; } }
            else
            {
                global::System.Net.IPAddress[] ips = global::System.Net.Dns.GetHostAddresses(ToIP);
                foreach (global::System.Net.IPAddress ip in ips) { try { udpClient.Send(sendBytes, sendBytes.Length, new global::System.Net.IPEndPoint(ip, port)); } catch { return false; } }
                if (ips.Length == 0) { return false; }
            };
            return true;
        }

        private void WaitForAnswerFunc()
        {
            for (int i = 0; i < 300; i++)
            {
                global::System.Threading.Thread.Sleep(100);
                if (this._SessionConfirmed == true) { return; }
            }
            this.CloseSession();
        }

        public void Invite()
        {
            string Request = "INVITE sip: " + this.ToUser + "@" + this.ToIP + " SIP/2.0 " + "\n";
            Request += "Record-Route: <sip:" + this.ToUser + "@" + this.myIP.ToString() + ";lr>" + "\n";
            Request += "From: " + "\"" + this.MyName + "\"" + "<sip: " + this.MyName + "@" + this.myIP.ToString() + "> " + "\n";
            Request += "To: " + "<sip: " + this.ToUser + "@" + this.ToIP + "> " + "\n";
            Request += "Call-ID: " + this.SessionID + "@" + this.myIP + "\n";
            Request += "CSeq: " + (this.n).ToString() + " INVITE" + "\n";
            Request += "Date: " + global::System.DateTime.Now.ToString() + "\n";
            Request += "Allow: INVITE, ACK, CANCEL, BYE" + "\n";
            Request += this._SDP;
            this.SendInfo(Request);
            this.WaitForAnswer = new global::System.Threading.Thread(this.WaitForAnswerFunc);
            this.WaitForAnswer.Start();
        }

        public void BYECompile()
        {
            string Request = "BYE sip: " + this.ToUser + "@" + this.ToIP + " SIP/2.0 " + "\n";
            Request += "Record-Route: <sip:" + this.ToUser + "@" + this.myIP.ToString() + ";lr>" + "\n";
            Request += "From: " + "\"" + this.MyName + "\"" + " <sip: " + this.MyName + "@" + this.myIP.ToString() + "> " + "\n";
            Request += "To: " + "<sip: " + this.ToUser + "@" + this.ToIP + "> " + "\n";
            Request += "Call-ID: " + this.SessionID + "@" + this.myIP + "\n";
            Request += "CSeq:" + (++this.n).ToString() + " BYE" + "\n";
            Request += "Date: " + global::System.DateTime.Now.ToString() + "\n";
            this.SendInfo(Request);
            this.DelClosesession(this.ToUser);
        }

        private string SDP()
        {
            string tmp = "v=0\n";
            tmp += "o=" + this.MyName + n.ToString() + "m" + "a" + this.SessionID + "IN IP4" + this.myIP.ToString() + "\n";
            tmp += "c=IN IP4 " + this.myIP.ToString() + "\n";
            tmp += "m=audio " + this.myaudioport.ToString() + " RTP/AVP 0\n";
            tmp += "a=rtpmap:0 PCMU/8000\n";
            tmp = "Content-Type: application/sdp\nContent-Length: " + tmp.Length + "\n\n" + tmp;
            return tmp;
        }

        private void BYEDecompile(string Info)
        {
            this._2XXCompile("00", false, true);
            this.CloseSession();
        }

        public void _1XXCompile(string _XX)
        {
            string Request = "SIP/2.0 1";
            switch (_XX)
            {
                case "80": Request += _XX + " Ringing\n"; break;
                default: return;
            }
            Request += "From: " + this.MyName + " <sip:" + this.MyName + "@" + this.myIP.ToString() + ">" + "\n";
            Request += "To: <sip: " + this.ToUser + "@" + this.ToIP + ">" + "\n";
            Request += this.SessionID + "\n";
            Request += "Cseq: " + (++this.n).ToString();
            switch (_XX)
            {
                case "80": Request += _XX + " Ringing\n"; break;
                default: return;
            }
            Request += "Date: " + global::System.DateTime.Now.ToString();
            this.SendInfo(Request);
        }

        public void _2XXCompile(string _XX, bool SDPRequired, bool EndSession)
        {
            string Request = "SIP/2.0 2" + _XX + " OK" + "\n";
            Request += "From: " + this.MyName + " <sip:" + this.MyName + "@" + this.myIP.ToString() + ">" + "\n";
            Request += "To: <sip: " + this.ToUser + "@" + this.ToIP + ">" + "\n";
            Request += this.SessionID + "\n";
            Request += "Cseq: " + (++this.n).ToString() + " OK" + "\n";
            Request += "Date: " + global::System.DateTime.Now.ToString() + "\n";
            if (SDPRequired) { Request += this._SDP; }
            this.SendInfo(Request);
            if (EndSession) { this.CloseSession(); }
        }

        public void _3XXCompile(string _XX, bool SDPRequired, bool EndSession)
        {
            string Request = "SIP/2.0 3";
            switch (_XX)
            {
                case "00": Request += _XX + " Multiple Choices\n"; break;
                case "01": Request += _XX + " Moved Permanently\n"; break;
                case "02": Request += _XX + " Moved Temporary\n"; break;
                default: return;
            }
            Request += "From: " + this.MyName + " <sip:" + this.MyName + "@" + this.myIP.ToString() + ">" + "\n";
            Request += "To: <sip: " + this.ToUser + "@" + this.ToIP + ">" + "\n";
            Request += this.SessionID + "\n";
            Request += "Cseq: " + (++this.n).ToString();
            switch (_XX)
            {
                case "00": Request += _XX + " Multiple Choices\n"; break;
                case "01": Request += _XX + " Moved Permanently\n"; break;
                case "02": Request += _XX + " Moved Temporary\n"; break;
                default: return;
            }
            Request += "Date: " + global::System.DateTime.Now.ToString() + "\n";
            if (SDPRequired) { Request += "\n" + this.SDP(); }
            this.SendInfo(Request);
            if (EndSession) { this.CloseSession(); }
        }

        public void _5XXCompile(string _XX, bool SDPRequired, bool EndSession)
        {
            string Request = "SIP/2.0 5";
            switch (_XX)
            {
                case "00": Request += _XX + " Server Internal Error\n"; break;
                case "01": Request += _XX + " Not Implemented\n"; break;
                case "02": Request += _XX + " Bad Gateway\n"; break;
                case "03": Request += _XX + " Service Unavailable\n"; break;
                default: Request += "01 Not Implemented\n"; break;
            }
            Request += "From: " + this.MyName + " <sip:" + this.MyName + "@" + this.myIP.ToString() + ">" + "\n";
            Request += "To: <sip: " + this.ToUser + "@" + this.ToIP + ">" + "\n";
            Request += this.SessionID + "\n";
            Request += "Cseq: " + (++this.n).ToString();
            switch (_XX)
            {
                case "00": Request += _XX + " Server Internal Error\n"; break;
                case "01": Request += _XX + " Not Implemented\n"; break;
                case "02": Request += _XX + " Bad Gateway\n"; break;
                case "03": Request += _XX + " Service Unavailable\n"; break;
                default: Request += "01 Not Implemented\n"; break;
            }
            Request += "Date: " + global::System.DateTime.Now.ToString() + "\n";
            if (SDPRequired) { Request += "\n" + this.SDP(); }
            SendInfo(Request);
            if (EndSession) { this.CloseSession(); }
        }

        public void _5XXDecompile(string Info) { this._2XXCompile("00", false, false); }

        public void _6XXCompile(string _XX, bool SDPRequired, bool EndSession)
        {
            string Request = "SIP/2.0 6";
            switch (_XX)
            {
                case "00": Request += _XX + " Busy Everywhere\n"; break;
                case "03": Request += _XX + " Decline\n"; break;
                case "04": Request += _XX + " Does Not Exist Anywhere\n"; break;
                case "06": Request += _XX + " Not Acceptable\n"; break;
                default: Request += "03 Decline\n"; break;
            }
            Request += "From: " + this.MyName + " <sip:" + this.MyName + "@" + this.myIP.ToString() + ">" + "\n";
            Request += "To: <sip: " + this.ToUser + "@" + this.ToIP + ">" + "\n";
            Request += this.SessionID + "\n";
            Request += "Cseq: " + (++this.n).ToString();
            switch (_XX)
            {
                case "00": Request += _XX + " Busy Everywhere\n"; break;
                case "03": Request += _XX + " Decline\n"; break;
                case "04": Request += _XX + " Does Not Exist Anywhere\n"; break;
                case "06": Request += _XX + " Not Acceptable\n"; break;
                default: Request += "03 Decline\n"; break;
            }
            Request += "Date: " + global::System.DateTime.Now.ToString() + "\n";
            if (SDPRequired) { Request += "\n" + this.SDP(); }
            this.SendInfo(Request);
            if (EndSession) { this.CloseSession(); }
        }

        public void _6XXDecompile(string Info)
        {
            this._2XXCompile("00", false, true);
            this.CloseSession();
        }

        private string SDPcombine(string str)
        {
            string tmp = "v=0\n";
            tmp += "o=" + n.ToString() + "m" + "a" + SessionID.ToString() + "IN IP4" + myIP + "\n";
            tmp += "c=IN IP4 " + myIP + "\n";
            string[] ms = str.Split('\n');
            foreach (string str1 in ms)
            {
                if (str1.Contains("m=audio"))
                {
                    tmp += str1 + "\n";
                    string tmp1 = str1.Remove(0, str1.IndexOf("audio ") + "audio ".Length);
                    tmp1 = tmp1.Remove(tmp1.IndexOf(" RTP"));
                    this.toaudioport = global::System.Convert.ToInt32(tmp1);
                }
                if (str1.Contains("PCMU/8000")) tmp += str1 + "\n";
            }
            tmp = "Content-Type: application/sdp\nContent-Length: " + tmp.Length + "\n\n" + tmp;
            return tmp;
        }

        public Session(global::System.Net.IPAddress myIP, int myPort, string ToIP, string ToUser, string FromUser, global::SIPLib.DelCloseSession d1, string ID, string SDPfunc)
        {
            this.ToIP = ToIP;
            this.ToUser = ToUser;
            this.MyName = FromUser;
            this.myIP = myIP;
            this.port = myPort;
            this.myaudioport = 11010;
            this.SessionID = ID;
            this.DelClosesession = d1;
            this.n++;
            if (SDPfunc.Length != 0) { this._SDP = this.SDPcombine(SDPfunc); } else { this._SDP = this.SDP(); }
        }
    }

    public class Listener
    {
        private static global::SIPLib.DelRequest DelRequest1;
        private static global::SIPLib.DelCloseSession DelClosesession;
        private static global::SIPLib.DelStopListener Delstoplistener;
        private static global::SIPLib.Del DelOutput;
        private string host = global::System.Net.Dns.GetHostName();
        private static object LockListen = new object();
        private global::System.Net.IPAddress myIP;
        private static global::System.Threading.Mutex Mut = new global::System.Threading.Mutex();
        private global::System.Threading.Thread ThreadListen;
        private static int port;
        private static double LastSessionID = 0;
        private static string myName;
        private static bool StopFlag = false;
        public static global::System.Collections.Generic.List<global::SIPLib.Session> Sessions = new global::System.Collections.Generic.List<global::SIPLib.Session>();
        public bool CheckSessionExistance(string str) { foreach (global::SIPLib.Session s in Sessions) { if (str == s._ToUser) return true; } return false; }
        private void CloseSession(string name) { global::SIPLib.Listener.Sessions.Clear(); }
        private static global::SIPLib.Session Last() { return ((global::SIPLib.Listener.Sessions != null && global::SIPLib.Listener.Sessions.Count > 0) ? global::SIPLib.Listener.Sessions[global::SIPLib.Listener.Sessions.Count - 1] : null); }

        public void MakeCall(string ToIP, string ToUser, string FromUser)
        {
            global::SIPLib.Listener.Sessions.Add(new global::SIPLib.Session(this.myIP, global::SIPLib.Listener.port, ToIP, ToUser, FromUser, global::SIPLib.Listener.DelClosesession, (global::SIPLib.Listener.LastSessionID++).ToString(), string.Empty));
            global::SIPLib.Listener.Last().Invite();
        }

        public void EndCall()
        {
            foreach (global::SIPLib.Session s in global::SIPLib.Listener.Sessions)
            {
                s.BYECompile();
                global::System.Threading.Thread.Sleep(100);
                global::SIPLib.Listener.Sessions.Remove(s);
                break;
            }
        }

        public void StopPhone()
        {
            this.EndCall();
            global::SIPLib.Listener.StopFlag = true;
            this.ThreadListen.Suspend();
            this.SendSocket("127.0.0.1", port, "quit");
            if (Sessions.Count > 0) { global::SIPLib.Listener.Last().BYECompile(); }
            global::SIPLib.Listener.Sessions.Clear();
        }

        private static void ListenSockets()
        {
            lock (global::SIPLib.Listener.LockListen)
            {
                global::System.Net.Sockets.UdpClient receivingUdpClient = new global::System.Net.Sockets.UdpClient(global::SIPLib.Listener.port);
                try
                {
                    global::System.Net.IPEndPoint RemoteIpEndPoint = new global::System.Net.IPEndPoint(global::System.Net.IPAddress.Any, 0);
                    while (true)
                    {
                        byte[] receiveBytes = receivingUdpClient.Receive(ref RemoteIpEndPoint);
                        global::SIPLib.Listener.WatchInfo(receiveBytes);
                        if (global::SIPLib.Listener.StopFlag == true) { break; }
                    }
                }
                catch
                {
                    receivingUdpClient.Close();
                    return;
                }
                receivingUdpClient.Close();
            }
        }

        private static bool WatchInfo(byte[] receiveBytes)
        {
            global::SIPLib.Listener.Mut.WaitOne();
            string Info = global::System.Text.Encoding.ASCII.GetString(receiveBytes);
            string From = Info.Substring(Info.IndexOf("From: "), Info.IndexOf('\n', Info.IndexOf("From: ")) - Info.IndexOf("From: "));
            if (From.Length <= 0) { return false; }
            From = From.Remove(0, From.IndexOf("sip: ") + "sip: ".Length);
            From = From.Remove(From.IndexOf('>'));
            string tmp = Info.Remove(0, Info.IndexOf("To"));
            tmp = tmp.Remove(tmp.IndexOf('@'));
            tmp = tmp.Remove(0, tmp.IndexOf("sip: ") + "sip: ".Length);
            global::SIPLib.Listener.DelOutput(Info, "HERE");
            if (tmp == myName)
            {
                if (Info.Contains("BYE "))
                {
                    global::SIPLib.Listener.Delstoplistener();
                    global::SIPLib.Listener.Last().WatchInfo(Info);
                }
                if (Info.Contains("INVITE "))
                {
                    string tmp4 = Info.Remove(0, Info.IndexOf("From:"));
                    tmp4 = tmp4.Remove(tmp4.IndexOf('>'));
                    tmp4 = tmp4.Remove(0, tmp4.IndexOf('@') + 1);
                    string tmp2 = Info.Remove(0, Info.IndexOf("To: <sip: ") + "To: <sip: ".Length);
                    tmp2 = tmp2.Remove(tmp2.IndexOf('>'));
                    tmp2 = tmp2.Remove(tmp2.IndexOf('@'));
                    string tmp3 = Info.Remove(0, Info.IndexOf("Call-ID"));
                    tmp3 = tmp3.Remove(tmp3.IndexOf('\n'));
                    string SDP = Info.Remove(0, Info.IndexOf("Content-Length"));
                    SDP = SDP.Remove(0, SDP.IndexOf("\n\n") + 2);
                    global::SIPLib.Listener.Sessions.Add(new global::SIPLib.Session(global::System.Net.Dns.GetHostEntry(global::System.Net.Dns.GetHostName()).AddressList[0], port, tmp4, From.Remove(From.IndexOf('@')), tmp2, DelClosesession, tmp3, SDP));
                    global::SIPLib.Listener.Last()._1XXCompile("01");
                    if (global::SIPLib.Listener.DelRequest1(From) == true) { global::SIPLib.Listener.Last()._2XXCompile("00", true, false); }
                    else
                    {
                        global::SIPLib.Listener.Sessions.Add(new global::SIPLib.Session(global::System.Net.Dns.GetHostEntry(global::System.Net.Dns.GetHostName()).AddressList[0], port, tmp, tmp2, From.Remove(From.IndexOf('@')), DelClosesession, tmp3, string.Empty));
                        global::SIPLib.Listener.Last()._6XXCompile("03", false, true);
                        global::SIPLib.Listener.Sessions.Remove(global::SIPLib.Listener.Last());
                    }
                }
                else
                {
                    tmp = Info.Remove(0, Info.IndexOf("Call-ID"));
                    tmp = tmp.Remove(tmp.IndexOf('\n'));
                    foreach (global::SIPLib.Session s in Sessions) { if (s.CheckSessionByID(tmp)) { s.WatchInfo(Info); } }
                }
            }
            global::SIPLib.Listener.Mut.ReleaseMutex();
            return true;
        }

        private bool SendSocket(string ToIP, int port, string Info)
        {
            global::System.Net.Sockets.UdpClient udpClient = new global::System.Net.Sockets.UdpClient();
            byte[] sendBytes = global::System.Text.Encoding.ASCII.GetBytes(Info);
            global::System.Net.IPAddress ipAddress = null;
            if (!System.Net.IPAddress.TryParse(ToIP, out ipAddress)) { return false; };
            global::System.Net.IPEndPoint ipEndPoint = new global::System.Net.IPEndPoint(ipAddress, port);
            try { udpClient.Send(sendBytes, sendBytes.Length, ipEndPoint); }
            catch { return false; }
            return true;
        }

        public Listener(int newport, global::SIPLib.DelRequest d1, string name, global::SIPLib.DelCloseSession d2, global::SIPLib.Del OUT, global::SIPLib.DelStopListener DelSL)
        {
            global::SIPLib.Listener.DelRequest1 = d1;
            global::SIPLib.Listener.DelClosesession = d2;
            global::SIPLib.Listener.DelOutput = OUT;
            global::SIPLib.Listener.DelClosesession += CloseSession;
            global::SIPLib.Listener.Delstoplistener = DelSL;
            global::SIPLib.Listener.StopFlag = false;
            global::SIPLib.Listener.myName = name;
            this.myIP = global::System.Net.Dns.GetHostEntry(host).AddressList[0];
            global::SIPLib.Listener.port = newport;
            this.ThreadListen = new global::System.Threading.Thread(global::SIPLib.Listener.ListenSockets);
            this.ThreadListen.Start();
        }
    }

    public class Player
    {
        private global::WaveLib.WaveOutPlayer m_Player;
        private global::WaveLib.WaveInRecorder m_Recorder;
        private global::WaveLib.FifoStream m_Fifo = new global::WaveLib.FifoStream();
        private byte[] m_PlayBuffer;
        private byte[] m_RecBuffer;
        private int _portReceive;
        private string _ToIP;
        private object LockSend = new object();
        private object LockReceive = new object();
        private global::System.Threading.Thread ThreadListen;

        private void Filler(global::System.IntPtr data, int size)
        {
            if (this.m_PlayBuffer == null || this.m_PlayBuffer.Length < size) { this.m_PlayBuffer = new byte[size]; }
            if (this.m_Fifo.Length >= size) { this.m_Fifo.Read(this.m_PlayBuffer, 0, size); } else { for (int i = 0; i < this.m_PlayBuffer.Length; i++) { this.m_PlayBuffer[i] = 0; } }
            global::System.Runtime.InteropServices.Marshal.Copy(this.m_PlayBuffer, 0, data, size);
        }

        private void DataArrived(byte[] data, int size)
        {
            if (this.m_RecBuffer == null || this.m_RecBuffer.Length < size) { this.m_RecBuffer = new byte[size]; }
            data.CopyTo(this.m_RecBuffer, 0);
            this.m_Fifo.Write(this.m_RecBuffer, 0, this.m_RecBuffer.Length);
        }

        private void DataSend(global::System.IntPtr data, int size)
        {
            lock (this.LockSend)
            {
                byte[] tmpBuffer = new byte[size];
                global::System.Runtime.InteropServices.Marshal.Copy(data, tmpBuffer, 0, size);
                this.SendSocket(this._ToIP, this._portReceive, tmpBuffer);
            }
        }

        private void DataReceive()
        {
            lock (this.LockReceive)
            {
                global::System.Net.Sockets.UdpClient receivingUdpClient = new global::System.Net.Sockets.UdpClient(this._portReceive);
                global::System.Net.IPAddress ipAddress;
                global::System.Net.IPAddress.TryParse(this._ToIP, out ipAddress);
                try
                {
                    global::System.Net.IPEndPoint RemoteIpEndPoint = new global::System.Net.IPEndPoint(ipAddress, this._portReceive);
                    while (true)
                    {
                        byte[] receiveBytes = receivingUdpClient.Receive(ref RemoteIpEndPoint);
                        this.DataArrived(receiveBytes, receiveBytes.Length);
                    }
                } catch { /* NOTHING */ }
            }
        }

        public void Stop()
        {
            this.ThreadListen.Suspend();
            if (this.m_Player != null) { try { this.m_Player.Dispose(); } finally { this.m_Player = null; } }
            if (this.m_Recorder != null) { try { this.m_Recorder.Dispose(); } finally { this.m_Recorder = null; } }
            this.m_Fifo.Flush();
        }

        public void Start()
        {
            this.Stop();
            this.ThreadListen.Resume();
            try
            {
                global::WaveLib.WaveFormat fmt = new global::WaveLib.WaveFormat(22050, 16, 2);
                this.m_Player = new global::WaveLib.WaveOutPlayer(-1, fmt, 32000, 5, new global::WaveLib.BufferEventHandler(this.Filler));
                this.m_Recorder = new global::WaveLib.WaveInRecorder(-1, fmt, 32000, 5, new global::WaveLib.BufferEventHandler(this.DataSend));
            }
            catch
            {
                this.Stop();
                throw;
            }
        }

        private bool SendSocket(string ToIP, int port, byte[] sendBytes)
        {
            global::System.Net.IPAddress ipAddress;
            global::System.Net.Sockets.UdpClient udpClient = new global::System.Net.Sockets.UdpClient();
            if (!System.Net.IPAddress.TryParse(ToIP, out ipAddress)) { return false; };
            global::System.Net.IPEndPoint ipEndPoint = new global::System.Net.IPEndPoint(ipAddress, port);
            try { udpClient.Send(sendBytes, sendBytes.Length, ipEndPoint); } catch { return false; }
            return true;
        }

        public void SetOptions(string toip, int pr)
        {
            this._portReceive = pr;
            this._ToIP = toip;
        }

        public Player(string toip, int pr)
        {
            this._portReceive = pr;
            this._ToIP = toip;
            this.ThreadListen = new global::System.Threading.Thread(this.DataReceive);
            this.ThreadListen.Start();
            this.ThreadListen.Suspend();
        }
    }
}
