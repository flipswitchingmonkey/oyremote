using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using Microsoft.Win32;
using System.Security.Permissions;
using System.Threading;
using System.Text.RegularExpressions;

namespace OyRemote
{
    public partial class Form1 : Form
    {
        public const string APPVERSION = "0.11";
        
        Form1 thisform;
        AboutBox1 aboutBox = new AboutBox1(APPVERSION);
        LiveStreamForm liveView = new LiveStreamForm();

        //global variables
        //public const bool DEBUG = true;
        RegistryKey regConnectionDetails = Registry.CurrentUser.OpenSubKey("Software\\OyRemote\\ConnectionDetails",true);
        RegistryKey regBatchCommands = Registry.CurrentUser.OpenSubKey("Software\\OyRemote\\BatchCommands", true);
        RegistryKey regLMDpresets = Registry.CurrentUser.OpenSubKey("Software\\OyRemote\\LMDPresets", true);
        string address = "192.168.0.101";
        int onkyoIPPort = 60128;
        //RadioButton previousRadioButton;
        bool showPresets = false;
        int batchDelay = 500;
        int numberOfLMDPresets = 15;
        public string lastFoundListeningMode;
        LMDPresetItem[] lmdPreset;
//        Thread volumeThread;
        private object lockObject = new object();
        delegate void SetVolLabelCallback(Decimal dec);
        delegate void SetSleepModeLabelCallback(Decimal dec);
        delegate void SetVolTrackBarCallback(int vol);
        delegate void SetSleepModeTrackBarCallback(int vol);
        delegate void SetRadioButtonrCallback();
        delegate void NTMChangeCallback(string time);

        OnkyoConnection onkyoConnection;
        List<InputDevice> inputDev = new List<InputDevice>();
        RadioButton tmpRadio;
        //bool mousepressed;
        Thread netScrub;

        public Form1(string[] args)
        {
            InitializeComponent();

            Debug.WriteLine(args);

            liveView.Show();
            liveView.Hide();
            thisform = this;
            thisform.Width = thisform.Width - 300;
            this.Text = this.Text + " v" + APPVERSION.ToString();
            lmdPreset = new LMDPresetItem[numberOfLMDPresets];
            this.Closing += new System.ComponentModel.CancelEventHandler(this.Form1_Closing);
            this.trackBarVolume.MouseUp += new MouseEventHandler(trackBarVolume_MouseUp);
            this.trackBarVolume.KeyUp += new KeyEventHandler(trackBarVolume_KeyUp);
            this.trackBarVolume.MouseDown += new MouseEventHandler(trackBarVolume_MouseDown);
            this.trackBarSleepMode.KeyUp += new KeyEventHandler(trackBarSleepMode_KeyUp);
            this.trackBarSleepMode.MouseUp += new MouseEventHandler(trackBarSleepMode_MouseUp);
            this.trackBarSubwooferLevel.KeyUp += new KeyEventHandler(trackBarSubwooferLevel_KeyUp);
            this.trackBarSubwooferLevel.MouseUp += new MouseEventHandler(trackBarSubwooferLevel_MouseUp);
            this.lockLMDpresets.MouseClick += new MouseEventHandler(lockLMDpresets_MouseClick);
            this.radioCD.MouseClick += new MouseEventHandler(radioCD_MouseClick);
            this.radioDVD.MouseClick += new MouseEventHandler(radioDVD_MouseClick);
            this.radioVideo1.MouseClick += new MouseEventHandler(radioVideo1_MouseClick);
            this.radioVideo2.MouseClick += new MouseEventHandler(radioVideo2_MouseClick);
            this.radioVideo3.MouseClick += new MouseEventHandler(radioVideo3_MouseClick);
            this.radioVideo4.MouseClick += new MouseEventHandler(radioVideo4_MouseClick);
            this.radioVideo5.MouseClick += new MouseEventHandler(radioVideo5_MouseClick);
            this.radioTape1.MouseClick += new MouseEventHandler(radioTape1_MouseClick);
            this.radioPhono.MouseClick += new MouseEventHandler(radioPhono_MouseClick);
            this.radioFM.MouseClick += new MouseEventHandler(radioFM_MouseClick);
            this.radioAM.MouseClick += new MouseEventHandler(radioAM_MouseClick);
            this.radioButtonSirius.MouseClick += new MouseEventHandler(radioButtonSirius_MouseClick);
            this.radioButtonXM.MouseClick += new MouseEventHandler(radioButtonXM_MouseClick);
            this.radioButtonMultiChannel.MouseClick += new MouseEventHandler(radioButtonMultiChannel_MouseClick);
            this.radioButtonUSB.MouseClick += new MouseEventHandler(radioButtonUSB_MouseClick);
            this.radioButtonMusicServer.MouseClick += new MouseEventHandler(radioButtonMusicServer_MouseClick);
            this.radioButtonInternetRadio.MouseClick += new MouseEventHandler(radioButtonInternetRadio_MouseClick);
            this.btNetFF.MouseDown += new MouseEventHandler(btNetFF_MouseDown);
            this.btNetREW.MouseDown += new MouseEventHandler(btNetREW_MouseDown);
            this.btNetFF.MouseUp += new MouseEventHandler(btNetFF_MouseUp);
            this.btNetREW.MouseUp += new MouseEventHandler(btNetREW_MouseUp);
            thisform.KeyDown += new KeyEventHandler(thisform_KeyDown);

            groupBoxNetworkControls.Enabled = false;

            Monitor.Enter(lockObject);
            //volumeThread = new Thread(new ThreadStart(getCurrentVolumeLoop));
            //volumeThread.Start();

            addLMDButtons(numberOfLMDPresets);
            getConfigurationData();
            showButtons(false);
            showLMDStoreButtons(false);
            txtIP.Text = address;
            txtPort.Text = onkyoIPPort.ToString();

            fillDeviceList();
            onkyoConnection = new OnkyoConnection(thisform, liveView);
            //openConnection();
            if (checkBoxAutoConnect.Checked) btConnect_Click(this, new EventArgs());
        }

        private void getConfigurationData()
        {
            if (regConnectionDetails == null)
                regConnectionDetails = Registry.CurrentUser.CreateSubKey("Software\\OyRemote\\ConnectionDetails");

            if (regBatchCommands == null)
                regBatchCommands = Registry.CurrentUser.CreateSubKey("Software\\OyRemote\\BatchCommands");

            if (regLMDpresets == null)
                regLMDpresets = Registry.CurrentUser.CreateSubKey("Software\\OyRemote\\LMDPresets");

            if (regConnectionDetails.GetValue("AmplifierIP") != null) address = (string)regConnectionDetails.GetValue("AmplifierIP");
            if (regConnectionDetails.GetValue("AmplifierPort") != null) Int32.TryParse((string)regConnectionDetails.GetValue("AmplifierPort"), out onkyoIPPort);
            if (regConnectionDetails.GetValue("BatchDelay") != null) batchDelay = (int)regConnectionDetails.GetValue("BatchDelay");
            if (regConnectionDetails.GetValue("Autoconnect") != null) checkBoxAutoConnect.Checked = Boolean.Parse((string)regConnectionDetails.GetValue("Autoconnect"));
            if (regConnectionDetails.GetValue("CheckVolume") != null) checkBoxVolumeContinuous.Checked = Boolean.Parse((string)regConnectionDetails.GetValue("CheckVolume"));

            if (regBatchCommands.GetValue("Batch1Name") != null) textBoxBatchName1.Text = (string)regBatchCommands.GetValue("Batch1Name");
            if (regBatchCommands.GetValue("Batch1Command") != null) textBoxBatchCommand1.Text = (string)regBatchCommands.GetValue("Batch1Command");
            if (regBatchCommands.GetValue("Batch1Autorun") != null) checkBoxAutorunBatch1.Checked = Boolean.Parse((string)regBatchCommands.GetValue("Batch1Autorun"));
            if (regBatchCommands.GetValue("Batch2Name") != null) textBoxBatchName2.Text = (string)regBatchCommands.GetValue("Batch2Name");
            if (regBatchCommands.GetValue("Batch2Command") != null) textBoxBatchCommand2.Text = (string)regBatchCommands.GetValue("Batch2Command");
            if (regBatchCommands.GetValue("Batch2Autorun") != null) checkBoxAutorunBatch2.Checked = Boolean.Parse((string)regBatchCommands.GetValue("Batch2Autorun"));
            if (regBatchCommands.GetValue("Batch3Name") != null) textBoxBatchName3.Text = (string)regBatchCommands.GetValue("Batch3Name");
            if (regBatchCommands.GetValue("Batch3Command") != null) textBoxBatchCommand3.Text = (string)regBatchCommands.GetValue("Batch3Command");
            if (regBatchCommands.GetValue("Batch3Autorun") != null) checkBoxAutorunBatch3.Checked = Boolean.Parse((string)regBatchCommands.GetValue("Batch3Autorun"));
            if (regBatchCommands.GetValue("Batch4Name") != null) textBoxBatchName4.Text = (string)regBatchCommands.GetValue("Batch4Name");
            if (regBatchCommands.GetValue("Batch4Command") != null) textBoxBatchCommand4.Text = (string)regBatchCommands.GetValue("Batch4Command");
            if (regBatchCommands.GetValue("Batch4Autorun") != null) checkBoxAutorunBatch4.Checked = Boolean.Parse((string)regBatchCommands.GetValue("Batch4Autorun"));
            if (regBatchCommands.GetValue("Batch5Name") != null) textBoxBatchName5.Text = (string)regBatchCommands.GetValue("Batch5Name");
            if (regBatchCommands.GetValue("Batch5Command") != null) textBoxBatchCommand5.Text = (string)regBatchCommands.GetValue("Batch5Command");
            if (regBatchCommands.GetValue("Batch5Autorun") != null) checkBoxAutorunBatch5.Checked = Boolean.Parse((string)regBatchCommands.GetValue("Batch5Autorun"));
            if (regBatchCommands.GetValue("Batch6Name") != null) textBoxBatchName6.Text = (string)regBatchCommands.GetValue("Batch6Name");
            if (regBatchCommands.GetValue("Batch6Command") != null) textBoxBatchCommand6.Text = (string)regBatchCommands.GetValue("Batch6Command");
            if (regBatchCommands.GetValue("Batch6Autorun") != null) checkBoxAutorunBatch6.Checked = Boolean.Parse((string)regBatchCommands.GetValue("Batch6Autorun"));

            for (int i = 0; i < numberOfLMDPresets; i++)
            {
                if (regLMDpresets.GetValue("LMD" + i + "Label") != null) lmdPreset[i].setLabel((string)regLMDpresets.GetValue("LMD" + i + "Label"));
                if (regLMDpresets.GetValue("LMD" + i + "Preset") != null) lmdPreset[i].setCommand((string)regLMDpresets.GetValue("LMD" + i + "Preset"));
            }

            regConnectionDetails.Flush();
            regConnectionDetails.Close();
        }

        private void saveConfigurationData()
        {
            try
            {
                regConnectionDetails = Registry.CurrentUser.OpenSubKey("Software\\OyRemote\\ConnectionDetails", true);
                regConnectionDetails.SetValue("AmplifierIP", txtIP.Text);
                regConnectionDetails.SetValue("AmplifierPort", txtPort.Text);
                regConnectionDetails.SetValue("BatchDelay", batchDelay);
                regConnectionDetails.SetValue("Autoconnect", checkBoxAutoConnect.Checked);
                regConnectionDetails.SetValue("CheckVolume", checkBoxVolumeContinuous.Checked);

                regBatchCommands = Registry.CurrentUser.OpenSubKey("Software\\OyRemote\\BatchCommands", true);
                regBatchCommands.SetValue("Batch1Name", textBoxBatchName1.Text);
                regBatchCommands.SetValue("Batch1Command", textBoxBatchCommand1.Text);
                regBatchCommands.SetValue("Batch1Autorun", checkBoxAutorunBatch1.Checked);
                regBatchCommands.SetValue("Batch2Name", textBoxBatchName2.Text);
                regBatchCommands.SetValue("Batch2Command", textBoxBatchCommand2.Text);
                regBatchCommands.SetValue("Batch2Autorun", checkBoxAutorunBatch2.Checked);
                regBatchCommands.SetValue("Batch3Name", textBoxBatchName3.Text);
                regBatchCommands.SetValue("Batch3Command", textBoxBatchCommand3.Text);
                regBatchCommands.SetValue("Batch3Autorun", checkBoxAutorunBatch3.Checked);
                regBatchCommands.SetValue("Batch4Name", textBoxBatchName4.Text);
                regBatchCommands.SetValue("Batch4Command", textBoxBatchCommand4.Text);
                regBatchCommands.SetValue("Batch4Autorun", checkBoxAutorunBatch4.Checked);
                regBatchCommands.SetValue("Batch5Name", textBoxBatchName5.Text);
                regBatchCommands.SetValue("Batch5Command", textBoxBatchCommand5.Text);
                regBatchCommands.SetValue("Batch5Autorun", checkBoxAutorunBatch5.Checked);
                regBatchCommands.SetValue("Batch6Name", textBoxBatchName6.Text);
                regBatchCommands.SetValue("Batch6Command", textBoxBatchCommand6.Text);
                regBatchCommands.SetValue("Batch6Autorun", checkBoxAutorunBatch6.Checked);

                for (int i = 0; i < numberOfLMDPresets; i++)
                {
                    regLMDpresets.SetValue(("LMD" + i + "Label"), lmdPreset[i].getLabel());
                    regLMDpresets.SetValue(("LMD" + i + "Preset"), lmdPreset[i].getCommand());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            regConnectionDetails.Flush();
            regConnectionDetails.Close();
        }

        private void Form1_Closing(Object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                //volumeThread.Abort();
                saveConfigurationData();
                onkyoConnection.close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void onkyoConnection_InputDeviceChange(Object sender, OyEventArgs e)
        {
            Debug.WriteLine("Device ID: " + e.eventString);
            try
            {
                InputDevice result = inputDev.Find(
                    delegate(InputDevice iD)
                    {
                        return (iD.deviceID.CompareTo(e.eventString)) == 0;
                    }
                );

                if (result.radio != null)
                {
                    tmpRadio = result.radio;
                    checkRadioButton();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void onkyoConnection_VolumeChange(Object sender, OyEventArgs e)
        {
            try
            {
                int volume = Convert.ToInt32(e.eventString, 16);
                Decimal volumeNo;
                Decimal.TryParse(volume.ToString(), out volumeNo);
                updateVolumeLabel(volumeNo);
                updateVolTrackBar((int)volumeNo);
                Debug.WriteLine("event: " + e.eventString.ToString() + " Int: " + volume);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void onkyoConnection_SleepModeChange(Object sender, OyEventArgs e)
        {
            try
            {
                int smtime = Convert.ToInt32(e.eventString, 16);
                Decimal smtimeNo;
                Decimal.TryParse(smtime.ToString(), out smtimeNo);
                updateSleepModeLabel(smtimeNo);
                updateSleepModeTrackBar(smtime);
                Debug.WriteLine("event: " + e.eventString.ToString() + " Int: " + smtime);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void setNTMlabel(string time)
        {
            if (this.labelNTMtime.InvokeRequired)
            {
                NTMChangeCallback d = new NTMChangeCallback(setNTMlabel);
                this.Invoke(d, new object[] { time });
            }
            else  labelNTMtime.Text = time;
        }

        private void onkyoConnection_NTMChange(Object sender, OyEventArgs e)
        {
            setNTMlabel(e.eventString);
        }

        private void addLMDButtons(int amount)
        {
            for (int x = 0; x < amount; x++)
            {
                this.lmdPreset[x] = new LMDPresetItem(thisform, thisform.groupBoxListeningModes,x,10+25*x);
                this.lmdPreset[x].show();
            }
        }

        public string ConvertToHex(string asciiString)
        {
            string hex = "";
            foreach (char c in asciiString)
            {
                int tmp = c;
                hex += String.Format("{0:x2}", (uint)System.Convert.ToUInt32(tmp.ToString()));
            }
            return hex;
        }

        private void showButtons(bool show)
        {
            this.groupBoxCustomCommand.Enabled = show;
            this.grpVolume.Enabled = show;
            this.groupBoxPower.Enabled = show;
            this.groupInputSelect.Enabled = show;
            this.groupPresets.Enabled = show;
            this.groupBoxListeningModes.Enabled = show;
            this.groupBoxCursor.Enabled = show;
            this.groupBoxVarious.Enabled = show;
        }

        public void setStatus(string status)
        {
            this.labelStatus.Text = status;
        }

        public string removeNonASCII(string i)
        {
            return Regex.Replace(i, @"[^\u0020-\u007F]", "");
        }

        public void setResult(string result)
        {
            this.labelResult.Text = removeNonASCII(result);
        }

        // run a comma separated list of commands (without the !1), e.g. SLI03,MVL30,PRS02
        private void runBatchCommand(string batchCmd)
        {
            foreach (string s in batchCmd.Split(Char.Parse(",")))
            {
                string recv = sendCommand("!1" + s);
                Debug.WriteLine("Sent: " + s + " Received: " + recv);
                Thread.Sleep(batchDelay);
            }
        }

        private void runAutorunBatchs()
        {
            if (checkBoxAutorunBatch1.Checked)
            {
                runBatchCommand(textBoxBatchCommand1.Text);
                setStatus("Autorun: Batch 1");
            }
            if (checkBoxAutorunBatch2.Checked)
            {
                runBatchCommand(textBoxBatchCommand2.Text);
                setStatus("Autorun: Batch 2");
            }
            if (checkBoxAutorunBatch3.Checked)
            {
                runBatchCommand(textBoxBatchCommand3.Text);
                setStatus("Autorun: Batch 3");
            }
            if (checkBoxAutorunBatch4.Checked)
            {
                runBatchCommand(textBoxBatchCommand4.Text);
                setStatus("Autorun: Batch 4");
            }
        }

        private void checkRadioButton()
        {
            if (tmpRadio.InvokeRequired)
            {
                SetRadioButtonrCallback d = new SetRadioButtonrCallback(checkRadioButton);
                this.Invoke(d);
            }
            else
            {
                if (!tmpRadio.Checked) tmpRadio.Checked = true;
            }
        }

        private void updateVolumeLabel(Decimal dec)
        {
            if (this.labelCurrentVolume.InvokeRequired)
            {
                SetVolLabelCallback d = new SetVolLabelCallback(updateVolumeLabel);
                this.Invoke(d, new object[] { dec });
            }
            else labelCurrentVolume.Text = dec.ToString();
        }

        private void updateVolTrackBar(int vol)
        {
            if (this.trackBarVolume.InvokeRequired)
            {
                SetVolTrackBarCallback d = new SetVolTrackBarCallback(updateVolTrackBar);
                this.Invoke(d, new object[] { vol });
            }
            else trackBarVolume.Value = vol;
        }

        private void updateSleepModeLabel(Decimal dec)
        {
            if (this.labelSleepModeMinutes.InvokeRequired)
            {
                SetSleepModeLabelCallback d = new SetSleepModeLabelCallback(updateSleepModeLabel);
                this.Invoke(d, new object[] { dec });
            }
            else
            {
                if (dec == 0) labelSleepModeMinutes.Text = "OFF";
                else labelSleepModeMinutes.Text = dec.ToString();
            }
        }

        private void updateSleepModeTrackBar(int vol)
        {
            if (this.trackBarSleepMode.InvokeRequired)
            {
                SetSleepModeTrackBarCallback d = new SetSleepModeTrackBarCallback(updateSleepModeTrackBar);
                this.Invoke(d, new object[] { vol });
            }
            else trackBarSleepMode.Value = vol;
        }

        private void getCurrentVolumeLoop()
        {
            while (true)
            {
                Monitor.Enter(lockObject);
                //get current volume and set control value
                string returnVolume = sendCommand("!1MVLQSTN");
                if (returnVolume.Contains("MVL"))
                {
                    try
                    {
                        int volume = Convert.ToInt32(returnVolume.Substring(returnVolume.IndexOf("MVL") + 3, 2), 16);
                        Decimal volumeNo;
                        Decimal.TryParse(volume.ToString(), out volumeNo);
                        updateVolumeLabel(volumeNo);
                        updateVolTrackBar((int)volumeNo);
                        //Debug.WriteLine(volume + " " + volumeNo.ToString());
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
                Monitor.Exit(lockObject);
                Thread.Sleep(1000);
            }
        }

        private void getCurrentVolume()
        {
            //get current volume and set control value
            string returnVolume = sendCommand("!1MVLQSTN");
        }

        private void getCurrentInput()
        {
            // get current Input selection
            string returnCommand = sendCommand("!1SLIQSTN");
        }

        private void btMuteOn_Click(object sender, EventArgs e)
        {
            sendCommand("!1AMT01");
            Debug.WriteLine("Button pressed:" + btMuteOn.Text + "/" + btMuteOn.Name);
            setStatus("Mute ON");
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void btMuteOff_Click(object sender, EventArgs e)
        {
            sendCommand("!1AMT00");
            Debug.WriteLine("Button pressed:" + btMuteOff.Text + "/" + btMuteOff.Name);
            setStatus("Mute OFF");
        }

        private void btConnect_Click(object sender, EventArgs e)
        {
            if (!onkyoConnection.isConnected())
            {
                onkyoConnection = new OnkyoConnection(thisform, liveView);
                onkyoConnection.setIPAddress(address);
                onkyoConnection.setIPPort(onkyoIPPort);
                onkyoConnection.createConnection();
                onkyoConnection.VolumeChanged += new OnkyoConnection.VolumeChangedHandler(onkyoConnection_VolumeChange);
                onkyoConnection.SleepModeChanged += new OnkyoConnection.SleepModeChangedHandler(onkyoConnection_SleepModeChange);
                onkyoConnection.InputDeviceChanged += new OnkyoConnection.InputDeviceChangedHandler(onkyoConnection_InputDeviceChange);
                onkyoConnection.NTMChanged += new OnkyoConnection.NTMChangedHandler(onkyoConnection_NTMChange);
                onkyoConnection.connect();

                if (onkyoConnection.isConnected())
                {
                    btConnect.Text = "Disconnect";
                    btConnect.BackColor = SystemColors.Control;
                    setStatus(address + ":" + onkyoIPPort);
                    getCurrentInput();
                    getCurrentVolume();
                    runAutorunBatchs();
                    showButtons(true); // at the end, because otherwise it enables the first radio button of input select, which triggers a "changed" event and sets the input...
                }
            }
            else
            {
                onkyoConnection.close();
                if (!onkyoConnection.isConnected()) 
                {
                    showButtons(false);
                    setStatus("Disconnected");
                    btConnect.Text = "Connect";
                    btConnect.BackColor = SystemColors.Highlight;
                }
                else
                {
                    MessageBox.Show("Error disconnecting (maybe connection was already disconnected?)");
                }
            }
        }

        public string sendCommand(string sendThisCommand)
        {
            return onkyoConnection.sendCommand(sendThisCommand);
        }

        private void btTest_Click(object sender, EventArgs e)
        {
            setStatus("Test");
            liveView.Show();
        }

        private void txtPort_TextChanged(object sender, EventArgs e)
        {
            int oldonkyoIPPort = onkyoIPPort;
            if (!Int32.TryParse(txtPort.Text, out onkyoIPPort))
            {
                MessageBox.Show("Port needs to be a number between 1 and 65535", "Invalid Port");
                txtPort.Text = oldonkyoIPPort.ToString();
            }
        }

        private void txtIP_TextChanged(object sender, EventArgs e)
        {
            address = txtIP.Text;
        }

        private void btCustomCommand_Click(object sender, EventArgs e)
        {
            foreach (string s in txtCustomCommand.Text.Split(Char.Parse(",")))
            {
                string recv = sendCommand("!1" + s); 
                Debug.WriteLine("Sent: " + s + " Received: " + recv);
            }
            setStatus("Custom " + txtCustomCommand.Text);
        }

        private void btPowerStandby_Click(object sender, EventArgs e)
        {
            DialogResult result;
            result = MessageBox.Show("Do you really want to power off the amp? This will make it unresponsive and close this application.", "Power Off", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                sendCommand("!1PWR00");
                Debug.WriteLine("Button pressed:" + btPowerStandby.Text + "/" + btPowerStandby.Name);
                setStatus("System set to Standby");
                Form1_Closing(new object(), new CancelEventArgs());
                Application.Exit();
            }

        }

        private void fillDeviceList()
        {
            inputDev.Add(new InputDevice(radioDVD,"10"));
            inputDev.Add(new InputDevice(radioCD, "23"));
            inputDev.Add(new InputDevice(radioVideo1,"00"));
            inputDev.Add(new InputDevice(radioVideo2,"01"));
            inputDev.Add(new InputDevice(radioVideo3,"02"));
            inputDev.Add(new InputDevice(radioVideo4,"03"));
            inputDev.Add(new InputDevice(radioVideo5,"04"));
            inputDev.Add(new InputDevice(radioTape1,"20"));
            inputDev.Add(new InputDevice(radioPhono,"22"));
            inputDev.Add(new InputDevice(radioFM,"24"));
            inputDev.Add(new InputDevice(radioButtonSirius,"32"));
            inputDev.Add(new InputDevice(radioButtonXM,"31"));
            inputDev.Add(new InputDevice(radioButtonMultiChannel,"30"));
            inputDev.Add(new InputDevice(radioButtonUSB,"29"));
            inputDev.Add(new InputDevice(radioButtonMusicServer,"27"));
            inputDev.Add(new InputDevice(radioButtonInternetRadio,"28"));
        }

        private void radioVideo1_MouseClick(object sender, EventArgs e)
        {
            if (radioVideo1.Checked)
            {
                string returnCommand = sendCommand("!1SLI00");
                setStatus("VCR/DVR");
                if (returnCommand == "Error")
                {
                   // radioVideo1.Checked = false;
                    //previousRadioButton.Checked = true;
                }
                else
                {
                    //previousRadioButton = radioVideo1;
                }
            }
        }

        private void radioVideo2_MouseClick(object sender, EventArgs e)
        {
            if (radioVideo2.Checked)
            {
                string returnCommand = sendCommand("!1SLI01");
                setStatus("CBL/SAT");
                if (returnCommand == "Error")
                {
                    //radioVideo2.Checked = false;
                    //previousRadioButton.Checked = true;
                }
                else
                {
                    //previousRadioButton = radioVideo2;
                }
            }
        }

        private void radioVideo3_MouseClick(object sender, EventArgs e)
        {
            if (radioVideo3.Checked)
            {
                string returnCommand = sendCommand("!1SLI02");
                setStatus("GAME/TV");
                if (returnCommand == "Error")
                {
                    //radioVideo3.Checked = false;
                    //previousRadioButton.Checked = true;
                }
                else
                {
                    //previousRadioButton = radioVideo3;
                }
            }
        }

        private void radioVideo4_MouseClick(object sender, EventArgs e)
        {
            if (radioVideo4.Checked)
            {
                string returnCommand = sendCommand("!1SLI03");
                setStatus("AUX1");
                if (returnCommand == "Error")
                {
                    //radioVideo4.Checked = false;
                    //previousRadioButton.Checked = true;
                }
                else
                {
                    //previousRadioButton = radioVideo4;
                }
            }
        }

        private void radioVideo5_MouseClick(object sender, EventArgs e)
        {
            if (radioVideo5.Checked)
            {
                string returnCommand = sendCommand("!1SLI04");
                setStatus("AUX2");
                if (returnCommand == "Error")
                {
                    //radioVideo5.Checked = false;
                    //previousRadioButton.Checked = true;
                }
                else
                {
                    //previousRadioButton = radioVideo5;
                }
            }
        }

        private void radioDVD_MouseClick(object sender, EventArgs e)
        {
            if (radioDVD.Checked)
            {
                string returnCommand = sendCommand("!1SLI10");
                setStatus("GAME/TV");
                if (returnCommand == "Error")
                {
                   // radioDVD.Checked = false;
                    //previousRadioButton.Checked = true;
                }
                else
                {
                    //previousRadioButton = radioDVD;
                }
            }
        }


        private void radioTape1_MouseClick(object sender, EventArgs e)
        {
            if (radioTape1.Checked)
            {
                string returnCommand = sendCommand("!1SLI20");
                setStatus("Tape");
                if (returnCommand == "Error")
                {
                    // radioDVD.Checked = false;
                    //previousRadioButton.Checked = true;
                }
                else
                {
                    //previousRadioButton = radioTape1;
                }
            }
        }

        private void radioPhono_MouseClick(object sender, EventArgs e)
        {
            if (radioPhono.Checked)
            {
                string returnCommand = sendCommand("!1SLI22");
                setStatus("Phono");
                if (returnCommand == "Error")
                {
                    // radioDVD.Checked = false;
                    //previousRadioButton.Checked = true;
                }
                else
                {
                    //previousRadioButton = radioPhono;
                }
            }
        }

        private void radioCD_MouseClick(object sender, EventArgs e)
        {
            if (radioCD.Checked)
            {
                string returnCommand = sendCommand("!1SLI23");
                setStatus("CD");
                if (returnCommand == "Error")
                {
                    // radioDVD.Checked = false;
                    //previousRadioButton.Checked = true;
                }
                else
                {
                    //previousRadioButton = radioCD;
                }
            }
        }

        private void radioFM_MouseClick(object sender, EventArgs e)
        {
            if (radioFM.Checked)
            {
                string returnCommand = sendCommand("!1SLI24");
                setStatus("FM");
                if (returnCommand == "Error")
                {
                    // radioDVD.Checked = false;
                    //previousRadioButton.Checked = true;
                }
                else
                {
                    //previousRadioButton = radioFM;
                    //get current radio preset
                    string returnPreset = sendCommand("!1PRSQSTN");
                    if (returnPreset.Contains("PRS"))
                    {
                        int preset = Convert.ToInt32(returnPreset.Substring(returnPreset.IndexOf("PRS") + 3, 2),16);
                        Decimal presetNo;
                        Decimal.TryParse(preset.ToString(), out presetNo);
                        Debug.WriteLine(preset + " " + presetNo);
                        numericUpDown2.Value = presetNo;
                    }
                    numericUpDown2.Enabled = true;
                }
            }
            else
            {
                numericUpDown2.Enabled = false;
            }
        }

        private void radioAM_MouseClick(object sender, EventArgs e)
        {
            if (radioAM.Checked)
            {
                string returnCommand = sendCommand("!1SLI25");
                setStatus("AM");
                if (returnCommand == "Error")
                {
                    // radioDVD.Checked = false;
                    //previousRadioButton.Checked = true;
                }
                else
                {
                    //previousRadioButton = radioAM;
                    numericUpDown2.Enabled = true;
                }
            }
            else
            {
                numericUpDown2.Enabled = false;
            }
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            sendCommand("!1PRS" + ((int)numericUpDown2.Value).ToString("X2"));
            Debug.WriteLine("Button pressed:" + numericUpDown2.Value + "/" + numericUpDown2.Name);
            setStatus("Preset changed to " + numericUpDown2.Value.ToString());
        }

        private void btPresetsShow_Click(object sender, EventArgs e)
        {
            if (!showPresets)
            {
                thisform.Width = thisform.Width + 300;
                showPresets = true;
            }
            else
            {
                thisform.Width = thisform.Width - 300;
                showPresets = false;
            }
        }

        private void btBatchCommand1_Click(object sender, EventArgs e)
        {
            runBatchCommand(textBoxBatchCommand1.Text);
            setStatus("Batch 1 sent.");
        }

        private void btBatchCommand2_Click(object sender, EventArgs e)
        {
            runBatchCommand(textBoxBatchCommand2.Text);
            setStatus("Batch 2 sent.");
        }

        private void btBatchCommand3_Click(object sender, EventArgs e)
        {
            runBatchCommand(textBoxBatchCommand3.Text);
            setStatus("Batch 3 sent.");
        }

        private void btBatchCommand4_Click(object sender, EventArgs e)
        {
            runBatchCommand(textBoxBatchCommand4.Text);
            setStatus("Batch 4 sent.");
        }

        private void btBatchCommand5_Click(object sender, EventArgs e)
        {
            runBatchCommand(textBoxBatchCommand5.Text);
            setStatus("Batch 5 sent.");
        }

        private void btBatchCommand6_Click(object sender, EventArgs e)
        {
            runBatchCommand(textBoxBatchCommand6.Text);
            setStatus("Batch 6 sent.");
        }

        private void btAbout_Click(object sender, EventArgs e)
        {
            aboutBox.Show();
        }

        private void numericUpDownDelay_ValueChanged(object sender, EventArgs e)
        {
            batchDelay = (int)numericUpDownDelay.Value;
        }

        private void trackBarVolume_Scroll(object sender, EventArgs e)
        {
            updateVolumeLabel(trackBarVolume.Value);
        }

        private void trackBarVolume_MouseDown(object sender, EventArgs e)
        {
            Monitor.Enter(lockObject);
        }

        private void trackBarVolume_MouseUp(object sender, EventArgs e)
        {
            String returnCommand = sendCommand("!1MVL" + ((int)trackBarVolume.Value).ToString("X2"));
            Debug.WriteLine("Button pressed:" + trackBarVolume.Value + "/" + returnCommand);
            setStatus("Volume changed to " + trackBarVolume.Value.ToString());
            if (checkBoxVolumeContinuous.Checked) Monitor.Exit(lockObject);
        }

        private void trackBarVolume_KeyUp(object sender, EventArgs e)
        {
            String returnCommand = sendCommand("!1MVL" + ((int)trackBarVolume.Value).ToString("X2"));
            Debug.WriteLine("Button pressed:" + trackBarVolume.Value + "/" + returnCommand);
            setStatus("Volume changed to " + trackBarVolume.Value.ToString());
            if (checkBoxVolumeContinuous.Checked) Monitor.Exit(lockObject);
        }

        private void buttonInputUp_Click(object sender, EventArgs e)
        {
            string returnCommand = sendCommand("!1SLIUP");
            getCurrentInput();
            setStatus("Input Source UP");
        }

        private void buttonInputDown_Click(object sender, EventArgs e)
        {
            string returnCommand = sendCommand("!1SLIDOWN");
            getCurrentInput();
            setStatus("Input Source DOWN");
        }

        private void radioButtonMusicServer_MouseClick(object sender, EventArgs e)
        {
            if (radioButtonMusicServer.Checked)
            {
                string returnCommand = sendCommand("!1SLI27");
                setStatus("Music Server");
                if (returnCommand == "Error")
                {
                    // radioDVD.Checked = false;
                    //previousRadioButton.Checked = true;
                }
                else
                {
                    //previousRadioButton = radioButtonMusicServer;
                }
            }
        }

        private void radioButtonInternetRadio_MouseClick(object sender, EventArgs e)
        {
            if (radioButtonInternetRadio.Checked)
            {
                string returnCommand = sendCommand("!1SLI28");
                setStatus("Internet Radio");
            }
        }

        private void radioButtonUSB_MouseClick(object sender, EventArgs e)
        {
            if (radioButtonUSB.Checked)
            {
                string returnCommand = sendCommand("!1SLI29");
                setStatus("USB");
                if (returnCommand == "Error")
                {
                    // radioDVD.Checked = false;
                    //previousRadioButton.Checked = true;
                }
                else
                {
                    //previousRadioButton = radioButtonUSB;
                }
            }
        }

        private void radioButtonMultiChannel_MouseClick(object sender, EventArgs e)
        {
            if (radioButtonMultiChannel.Checked)
            {
                string returnCommand = sendCommand("!1SLI30");
                setStatus("MultiChannel");
                if (returnCommand == "Error")
                {
                    // radioDVD.Checked = false;
                    //previousRadioButton.Checked = true;
                }
                else
                {
                    //previousRadioButton = radioButtonMultiChannel;
                }
            }
        }

        private void radioButtonXM_MouseClick(object sender, EventArgs e)
        {
            if (radioButtonXM.Checked)
            {
                string returnCommand = sendCommand("!1SLI31");
                setStatus("XM");
                if (returnCommand == "Error")
                {
                    // radioDVD.Checked = false;
                    //previousRadioButton.Checked = true;
                }
                else
                {
                    //previousRadioButton = radioButtonXM;
                }
            }
        }

        private void radioButtonSirius_MouseClick(object sender, EventArgs e)
        {
            if (radioButtonSirius.Checked)
            {
                string returnCommand = sendCommand("!1SLI32");
                setStatus("Sirius");
                if (returnCommand == "Error")
                {
                    // radioDVD.Checked = false;
                    //previousRadioButton.Checked = true;
                }
                else
                {
                    //previousRadioButton = radioButtonSirius;
                }
            }
        }

        private void buttonQSTNinput_Click(object sender, EventArgs e)
        {
            string returnCommand = sendCommand("!1SLIQSTN");
            setResult(returnCommand);
            setStatus("Input Source Query");
        }

        private void buttonCursorUp_Click(object sender, EventArgs e)
        {
            string returnCommand;
            if (radioButtonInternetRadio.Checked || radioButtonUSB.Checked || radioButtonMusicServer.Checked) returnCommand = sendCommand("!1NTCUP");
            else returnCommand = sendCommand("!1OSDUP");
            setResult(returnCommand);
        }

        private void buttonCursorRight_Click(object sender, EventArgs e)
        {
            string returnCommand;
            if (radioButtonInternetRadio.Checked || radioButtonUSB.Checked || radioButtonMusicServer.Checked) returnCommand = sendCommand("!1NTCRIGHT");
            else returnCommand = sendCommand("!1OSDRIGHT");
            setResult(returnCommand);
        }

        private void buttonCursorDown_Click(object sender, EventArgs e)
        {
            string returnCommand;
            if (radioButtonInternetRadio.Checked || radioButtonUSB.Checked || radioButtonMusicServer.Checked) returnCommand = sendCommand("!1NTCDOWN");
            else returnCommand = sendCommand("!1OSDDOWN");
            setResult(returnCommand);
        }

        private void buttonCursorLeft_Click(object sender, EventArgs e)
        {
            string returnCommand;
            if (radioButtonInternetRadio.Checked || radioButtonUSB.Checked || radioButtonMusicServer.Checked) returnCommand = sendCommand("!1NTCLEFT");
            else returnCommand = sendCommand("!1OSDLEFT");
            setResult(returnCommand);
        }

        private void buttonCursorEnter_Click(object sender, EventArgs e)
        {
            string returnCommand;
            if (radioButtonInternetRadio.Checked || radioButtonUSB.Checked || radioButtonMusicServer.Checked) returnCommand = sendCommand("!1NTCSELECT");
            else returnCommand = sendCommand("!1OSDENTER");
            setResult(returnCommand);
        }

        private void buttonCursorReturn_Click(object sender, EventArgs e)
        {
            string returnCommand;
            if (radioButtonInternetRadio.Checked || radioButtonUSB.Checked || radioButtonMusicServer.Checked) returnCommand = sendCommand("!1NTCRETURN");
            else returnCommand = sendCommand("!1OSDEXIT");
            setResult(returnCommand);
        }

        private void buttonCursorSetup_Click(object sender, EventArgs e)
        {
            string returnCommand = sendCommand("!1OSDMENU");
            setResult(returnCommand);
        }

        private void buttonDimDisplay_Click(object sender, EventArgs e)
        {
            string returnCommand = sendCommand("!1DIMDIM");
            setStatus("DIM cycled");
        }

        private void buttonQSTNLMD_Click(object sender, EventArgs e)
        {
            string returnCommand = sendCommand("!1LMDQSTN");
            setResult(returnCommand);
            setStatus("Listening Mode Query");
        }

        private void buttonLMDUP_Click(object sender, EventArgs e)
        {
            string returnCommand = sendCommand("!1LMDUP");
            for (int x = 0; x < numberOfLMDPresets; x++) lmdPreset[x].unCheck();
            setResult(returnCommand);
            setStatus("Listening Mode up");
        }

        private void buttonLMDDOWN_Click(object sender, EventArgs e)
        {
            string returnCommand = sendCommand("!1LMDDOWN");
            for (int x = 0; x < numberOfLMDPresets; x++) lmdPreset[x].unCheck();
            setResult(returnCommand);
            setStatus("Listening Mode down");
        }

        private void lockLMDpresets_MouseClick(object sender, EventArgs e)
        {
            if (lockLMDpresets.Checked)
            {   // LOCK
                showLMDStoreButtons(false);
            }
            else
            {   // UNLOCK
                showLMDStoreButtons(true);
            }
        }

        private void showLMDStoreButtons(bool b)
        {
            for (int x = 0; x < numberOfLMDPresets; x++) lmdPreset[x].enableButton(b);
        }

        private void trackBarSleepMode_Scroll(object sender, EventArgs e)
        {
            if (trackBarSleepMode.Value == 0)
                labelSleepModeMinutes.Text = "OFF";
            else
                labelSleepModeMinutes.Text = trackBarSleepMode.Value.ToString();
            
        }

        private void trackBarSleepMode_MouseUp(object sender, EventArgs e)
        {
            String returnCommand;
            if (trackBarSleepMode.Value == 0)
                returnCommand = sendCommand("!1SLPOFF");
            else
            {
                returnCommand = sendCommand("!1SLP" + ((int)trackBarSleepMode.Value).ToString("X2"));
            }
            Debug.WriteLine("Button pressed:" + trackBarSleepMode.Value + "/" + returnCommand);
            setStatus("Sleep Mode changed to " + labelSleepModeMinutes.Text);
        }

        private void trackBarSleepMode_KeyUp(object sender, EventArgs e)
        {
            String returnCommand;
            if (trackBarSleepMode.Value == 0)
                returnCommand = sendCommand("!1SLPOFF");
            else
            {
                returnCommand = sendCommand("!1SLP" + ((int)trackBarSleepMode.Value).ToString("X2"));
            }
            Debug.WriteLine("Button pressed:" + trackBarSleepMode.Value + "/" + returnCommand);
            setStatus("Sleep Mode changed to " + labelSleepModeMinutes.Text);
        }

        private string calcSubwooferLevel(Decimal dec, bool hex)
        {
            StringBuilder sb = new StringBuilder("00", 2);
            int sw = (int)dec;
            if (sw < 0)
            {
                sb.Replace("0", "-", 0, 1);
                if (hex) sb.Replace("0", (Math.Abs(sw)).ToString("X"), 1, 1); // otherwise we get --1 instead of -1
                else sb.Replace("0", (Math.Abs(sw)).ToString(""), 1, 1);
            }
            if (sw > 0)
            {
                sb.Replace("0", "+", 0, 1);
                if (hex) sb.Replace("0", sw.ToString("X"), 1, 1);
                else sb.Replace("0", sw.ToString(""), 1, 1);
            }
            return sb.ToString();
        }

        private void setSubwooferLevel()
        {
            String returnCommand;
            returnCommand = sendCommand("!1SWL" + calcSubwooferLevel(trackBarSubwooferLevel.Value,true));
            Debug.WriteLine("Value:" + calcSubwooferLevel(trackBarSubwooferLevel.Value,true) + " Return: " + returnCommand);
            setStatus("Subwoofer Level changed to " + labelSubwooferLevel.Text);
        }


        private void trackBarSubwooferLevel_MouseUp(object sender, EventArgs e)
        {
            setSubwooferLevel();
        }

        private void trackBarSubwooferLevel_KeyUp(object sender, EventArgs e)
        {
            setSubwooferLevel();
        }

        private void trackBarSubwooferLevel_Scroll(object sender, EventArgs e)
        {
            labelSubwooferLevel.Text = calcSubwooferLevel(trackBarSubwooferLevel.Value,false);
        }

        private void radioButtonInternetRadio_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonInternetRadio.Checked)
            {
                groupBoxNetworkControls.Enabled = true;
            }
            else
            {
                groupBoxNetworkControls.Enabled = false;
            }
        }

        private void radioButtonUSB_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonUSB.Checked)
            {
                groupBoxNetworkControls.Enabled = true;
            }
            else
            {
                groupBoxNetworkControls.Enabled = false;
            }
        }

        private void radioButtonMusicServer_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonMusicServer.Checked)
            {
                groupBoxNetworkControls.Enabled = true;
            }
            else
            {
                groupBoxNetworkControls.Enabled = false;
            }
        }

        private void btNetPlay_Click(object sender, EventArgs e)
        {
            string returnCommand = sendCommand("!1NTCPLAY");
        }

        private void btNetPause_Click(object sender, EventArgs e)
        {
            string returnCommand = sendCommand("!1NTCPAUSE");
        }

        private void btNetStop_Click(object sender, EventArgs e)
        {
            string returnCommand = sendCommand("!1NTCSTOP");
        }

        private void btNetTrUp_Click(object sender, EventArgs e)
        {
            string returnCommand = sendCommand("!1NTCTRUP");
        }

        private void btNetTrDown_Click(object sender, EventArgs e)
        {
            string returnCommand = sendCommand("!1NTCTRDN");
        }

        private void ntcFastForward()
        {
            while (true)
            {
                sendCommand("!1NTCFF");
                Thread.Sleep(90);
            }
        }
        
        private void ntcRewind()
        {
            while (true)
            {
                sendCommand("!1NTCREW");
                Thread.Sleep(50);
            }
        }

        private void btNetFF_MouseDown(object sender, MouseEventArgs e)
        {
            netScrub = new Thread(new ThreadStart(ntcFastForward));
            netScrub.Start();
        }

        private void btNetFF_MouseUp(object sender, MouseEventArgs e)
        {
            netScrub.Abort();
            Debug.WriteLine("Mouse UP");
        }

        private void btNetREW_MouseDown(object sender, MouseEventArgs e)
        {
            netScrub = new Thread(new ThreadStart(ntcRewind));
            netScrub.Start();
        }

        private void btNetREW_MouseUp(object sender, MouseEventArgs e)
        {
            netScrub.Abort();
            Debug.WriteLine("Mouse UP");
        }

        private void thisform_KeyDown(object sender, KeyEventArgs e)
        {
            Debug.WriteLine(e.KeyValue);
            if (textBoxKeyControl.Focused)
            {
                switch (e.KeyValue)
                {
                    case 8:
                        {
                            buttonCursorReturn_Click(new object(), new EventArgs());
                            Debug.WriteLine("left");
                            break;
                        }
                    case 13:
                        {
                            buttonCursorEnter_Click(new object(), new EventArgs());
                            Debug.WriteLine("left");
                            break;
                        }
                    case 37:
                        {
                            buttonCursorLeft_Click(new object(), new EventArgs());
                            Debug.WriteLine("left");
                            break;
                        }
                    case 38:
                        {
                            buttonCursorUp_Click(new object(), new EventArgs());
                            Debug.WriteLine("up");
                            break;
                        }
                    case 39:
                        {
                            buttonCursorRight_Click(new object(), new EventArgs());
                            Debug.WriteLine("right");
                            break;
                        }
                    case 40:
                        {
                            buttonCursorDown_Click(new object(), new EventArgs());
                            Debug.WriteLine("down");
                            break;
                        }
                    default: break;
                }
            }

        }

        private void btShowLiveData_Click(object sender, EventArgs e)
        {
            if (btShowLiveData.Text == "Live Log")
            {
                liveView.Show();
                btShowLiveData.Text = "Hide Log";
            }
            else if (btShowLiveData.Text == "Hide Log")
            {
                liveView.Hide();
                btShowLiveData.Text = "Live Log";
            }
        }

/*        private void checkBoxVolumeContinuous_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxVolumeContinuous.Checked) Monitor.Exit(lockObject);
            else Monitor.Enter(lockObject);
        }
*/

    }
}
