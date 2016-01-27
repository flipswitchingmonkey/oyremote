using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace OyRemote
{
    public partial class LiveStreamForm : Form
    {
        private string tmpString;
        delegate void logMessageCallback(string msg,bool outgoing);
        
        public LiveStreamForm()
        {
            InitializeComponent();
            this.Font = new Font(FontFamily.GenericMonospace, 10);
            this.FormClosing += new FormClosingEventHandler(LiveStreamForm_FormClosing);
        }

        private void LiveStreamForm_FormClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
        }

        public void logMessage(string msg, bool outgoing)
        {
            if (txtLiveOutput.InvokeRequired)
            {
                logMessageCallback d = new logMessageCallback(logMessage);
                this.Invoke(d, new object[] { msg, outgoing });
            }
            else
            {
                if (outgoing) txtLiveOutput.SelectionColor = Color.DarkGreen;
                else txtLiveOutput.SelectionColor = Color.DarkRed;
                if (!checkBoxPauseLogging.Checked) txtLiveOutput.AppendText(msg);
            }
        }

        public void logMessageOutgoing(string msg)
        {
            tmpString = "";
            tmpString += addTimestamp();
            tmpString += "\t";
            tmpString += msg;
            tmpString += "   >>>\n";
            logMessage(tmpString,true);
        }

        public void logMessageIncoming(string msg)
        {
            tmpString = "";
            tmpString += addTimestamp();
            tmpString += "\t\t\t<<<   ";
            tmpString += msg;
            tmpString += "\n";
            logMessage(tmpString,false);
        }

        private string addTimestamp()
        {
            return DateTime.Now.ToString();
        }

        private void LiveStreamForm_Load(object sender, EventArgs e)
        {

        }
    }
}
