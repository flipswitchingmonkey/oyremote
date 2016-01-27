using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace OyRemote
{
    class OyEventArgs : EventArgs
    {
        public string eventString = "";
        public OyEventArgs(string eventString)
        {
            this.eventString = eventString;
        }
    }


    class OnkyoConnection
    {
        private string ipAddress = "192.168.0.101";
        private int ipPort = 60128;
        private Socket sock;
        private EndPoint onkyoIP;
        private IPAddress onkyoIPAddress;
        private int socketTimout = 1000;
        private object Token = new object();
        Thread listenerThread;
        Form1 parentForm;
        LiveStreamForm logForm;

        //
        // EVENT DEFINITONS
        //

        public delegate void VolumeChangedHandler(object myObject, OyEventArgs myEvent);
        public delegate void SleepModeChangedHandler(object myObject, OyEventArgs myEvent);
        public delegate void InputDeviceChangedHandler(object myObject, OyEventArgs myEvent);
        public delegate void NTMChangedHandler(object myObject, OyEventArgs myEvent);

        public VolumeChangedHandler VolumeChanged;
        public SleepModeChangedHandler SleepModeChanged;
        public InputDeviceChangedHandler InputDeviceChanged;
        public NTMChangedHandler NTMChanged;

        protected virtual void onVolumeChanged(OyEventArgs e)
        {if (VolumeChanged != null) VolumeChanged(this, e);}

        protected virtual void onSleepModeChanged(OyEventArgs e)
        {if (SleepModeChanged != null) SleepModeChanged(this, e);}

        protected virtual void onInputDeviceChanged(OyEventArgs e)
        { if (InputDeviceChanged != null) InputDeviceChanged(this, e); }

        protected virtual void onNTMChanged(OyEventArgs e)
        { if (NTMChanged != null) NTMChanged(this, e); }

        //
        //
        //
        public OnkyoConnection(Form1 pF, LiveStreamForm lF)
        {
            parentForm = pF;
            logForm = lF;
        }

        public bool isConnected()
        {
            try
            {
                return this.sock.Connected;
            }
            catch (Exception e)
            {
                return false;
                Debug.WriteLine(e.Message);
                throw;
            }
        }

        public void setSocketTimeout(int i)
        {
            this.socketTimout = i;
        }
        
        public void setIPAddress(string s)
        {
            this.ipAddress = s;
        }

        public void setIPPort(int i)
        {
            this.ipPort = i;
        }

        public void createConnection()
        {
            try
            {
                bool validIP = IPAddress.TryParse(ipAddress, out onkyoIPAddress);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                throw;
            }
            onkyoIP = new IPEndPoint(onkyoIPAddress, ipPort);
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sock.ReceiveTimeout = socketTimout;
        }

        public void connect()
        {
            try
            {
                sock.Connect(onkyoIP);
                listenerThread = new Thread(new ThreadStart(socketListener));
                listenerThread.Start();
            }
            catch (Exception e)
            {
                MessageBox.Show("Connection failed, please check IP and Port.\nThe error was:\n" + e.Message, "Connection failed");
            }

        }

        public void close()
        {
            try
            {
                sock.Close();
                listenerThread.Abort();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        private Byte[] combineBytes(Byte[] a, Byte[] b)
        {
            Byte[] c = new byte[a.Length + b.Length];
            System.Buffer.BlockCopy(a, 0, c, 0, a.Length);
            System.Buffer.BlockCopy(b, 0, c, a.Length, b.Length);
            return c;
        }

        private Byte[] buildCommandBytes(string cmd)
        {
            Debug.WriteLine(((cmd.Length + 1).ToString("0:X2")));
            ASCIIEncoding encoding = new ASCIIEncoding();
            Byte[] ISCPbytes = new byte[] { 0x49, 0x53, 0x43, 0x50 }; // "ISCP"
            Byte[] intro1bytes = new byte[] { 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00 }; // header size (16 = 0x10)
            Byte[] sizebytes = { Byte.Parse( String.Format( "{0:X2}", (cmd.Length + 1).ToString() ), System.Globalization.NumberStyles.HexNumber) }; // size of command string in characters
            Byte[] intro2bytes = new byte[] { 0x01, 0x00, 0x00, 0x00 }; // header version + 3 reserved bytes
            Byte[] cmdbytes = encoding.GetBytes(cmd); // encoded command
            Byte[] endbytes = new byte[] { 0x0D }; // finish message with [CR]

            // combine all parts to one bytestream
            Byte[] combinedCommand;
            combinedCommand = this.combineBytes(ISCPbytes, intro1bytes);
            combinedCommand = this.combineBytes(combinedCommand, sizebytes);
            combinedCommand = this.combineBytes(combinedCommand, intro2bytes);
            combinedCommand = this.combineBytes(combinedCommand, cmdbytes);
            combinedCommand = this.combineBytes(combinedCommand, endbytes);
            return combinedCommand;
        }

        public string sendCommand(string cmd)
        {
            string sendResult = "";
            // FORMAT: string ISCP\x00\x00\x00\x10\x00\x00\x00LengthOfCmd\x01\x00\x00\x00cmd\x0D
            
            Byte[] fullCommand = buildCommandBytes(cmd);

            try
            {
                // send message to Onkyo receiver
                try
                {
                    Monitor.Enter(sock);
                    sock.Send(fullCommand, fullCommand.Length, 0);
                    sendResult = receiveFromSocket();
                    logForm.logMessageOutgoing(cmd);
                    Monitor.Exit(sock);
                }
                catch (Exception e)
                {
                    //MessageBox.Show("Send failed (not connected to amplifier?)", "Send failed");
                    Debug.WriteLine(e.Message);
                }
            } 
             catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            Debug.WriteLine("onkyoconnector.sendResult: " + sendResult);
            return sendResult;
        }

        private string receiveFromSocket()
        {
            try
            {
                if (sock.Available > 0)
                {
                    Byte[] RecvBytes = new Byte[256];
                    Int32 bytes = sock.Receive(RecvBytes, RecvBytes.Length, 0);
                    //                        if (!cmd.Contains("!1MVL"))     
                    //                        {
                    //string incoming = encoding.GetString(RecvBytes, 0, RecvBytes.Length);
                    StringBuilder sb = new StringBuilder();
                    int intValue;
                    for (int i = 16; RecvBytes[i] != 0x1A; i++) 
                    //for (int i = 16; i < (Int32.Parse((RecvBytes[11].ToString()), System.Globalization.NumberStyles.HexNumber) + 16); i++) // starting at 16 because everything before is command header
                    //for (int i = 16; i < 256; i++) // starting at 16 because everything before is command header
                    {
                        //sb.Append(string.Format("{0:x2}", RecvBytes[i]));
                        if (RecvBytes[i] != 0x1A)
                        {
                            intValue = Convert.ToInt32(string.Format("{0:x2}", RecvBytes[i]), 16);
                            sb.Append(Char.ConvertFromUtf32(intValue));
                        }
                    }
                    string listenResult = sb.ToString();

                    switch (listenResult.Substring(2,3))
                    {
                        case "MVL": //VOLUME
                        {
                            Debug.WriteLine("MVL found " + listenResult.Substring(5,2));
                            onVolumeChanged(new OyEventArgs(listenResult.Substring(5,2)));
                            break;
                        }

                        case "SLP": //SLEEP MODE
                        {
                            Debug.WriteLine("SLP found " + listenResult.Substring(5, 2));
                            onSleepModeChanged(new OyEventArgs(listenResult.Substring(5, 2)));
                            break;
                        }

                        case "SLI": //INPUT DEVICE
                        {
                            Debug.WriteLine("SLI found " + listenResult.Substring(5, 2));
                            onInputDeviceChanged(new OyEventArgs(listenResult.Substring(5, 2)));
                            break;
                        }

                        case "NTM": //NETWORK PLAYTIME
                        {
                            Debug.WriteLine("NTM found " + listenResult.Substring(5));
                            onNTMChanged(new OyEventArgs(listenResult.Substring(5)));
                            break;
                        }

                        case "LMD": //LISTENING MODE
                        {
                            Debug.WriteLine("LMD found " + listenResult.Substring(5,2));
                            parentForm.lastFoundListeningMode = listenResult;
                            break;
                        }

                        default: break;
                    }

                    Debug.WriteLine(listenResult);
                    parentForm.setResult(listenResult);
                    logForm.logMessageIncoming(listenResult);
                    return listenResult;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
            return "";
        }

        private void socketListener()
        {
            while (true)
            {
                try
                {
                    Monitor.Enter(sock);
                    if (sock.Available > 0) receiveFromSocket();
                    Monitor.Exit(sock);
                    Thread.Sleep(50);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    throw;
                }
            }
        }
    }
}
