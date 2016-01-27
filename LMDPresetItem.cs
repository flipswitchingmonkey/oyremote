using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace OyRemote
{
    class LMDPresetItem
    {
        System.Windows.Forms.RadioButton radio = new RadioButton();
        System.Windows.Forms.TextBox label = new TextBox();
        System.Windows.Forms.Button btStore = new Button();
        string commandString = "";
        public int externalID;
        Form1 parent;

        public LMDPresetItem(Form1 parentForm, GroupBox parentBox, int externalIDx, int vOffset)
        {
            parent = parentForm;
            externalID = externalIDx;
            int vOrigin = 15;
            int hOrigin = 9;
            //
            // CHECKBOX
            //
            parentBox.Controls.Add(this.radio);
            this.radio.AutoSize = true;
            this.radio.Location = new System.Drawing.Point(hOrigin, vOrigin + vOffset);
            this.radio.Name = "lmd1radio";
            this.radio.Size = new System.Drawing.Size(14, 13);
            this.radio.TabIndex = 41;
            this.radio.TabStop = true;
            this.radio.UseVisualStyleBackColor = true;
            this.radio.CheckedChanged += new System.EventHandler(this.radio_CheckedChanged);
            //
            // LABEL
            //
            parentBox.Controls.Add(this.label);
            this.label.AutoCompleteCustomSource.AddRange(new string[] {
            "STEREO",
            "DIRECT ",
            "SURROUND",
            "FILM",
            "THX",
            "ACTION",
            "MUSICAL",
            "MONO MOVIE",
            "ORCHESTRA",
            "UNPLUGGED",
            "STUDIO-MIX",
            "TV LOGIC",
            "ALL CH STEREO",
            "THEATER-DIMENSIONAL ",
            "ENHANCED 7/ENHANCE",
            "MONO ",
            "PURE AUDIO ",
            "MULTIPLEX",
            "FULL MONO",
            "DOLBY VIRTUAL",
            "5.1ch Surround",
            "Straight Decode*1",
            "Dolby EX/DTS ES",
            "Dolby EX*2",
            "THX Cinema",
            "THX Surround EX",
            "THX Music",
            "THX Games",
            "U2/S2 Cinema/Cinema2",
            "MusicMode,U2/S2 Music",
            "Games Mode,U2/S2 Games",
            "PLII/PLIIx Movie",
            "PLII/PLIIx Music",
            "Neo:6 Cinema",
            "Neo:6 Music",
            "PLII/PLIIx THX Cinema",
            "Neo:6 THX Cinema",
            "PLII/PLIIx Game",
            "Neural Surr*3",
            "Neural THX",
            "PLII/PLIIx THX Games",
            "Neo:6 THX Games",
            "PLII/PLIIx THX Music",
            "Neo:6 THX Music",
            "Neural THX Cinema",
            "Neural THX Music",
            "Neural THX Games"});
            this.label.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.label.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
            this.label.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.label.Location = new System.Drawing.Point(hOrigin + 20, vOrigin + vOffset);
            this.label.Name = "label";
            this.label.ReadOnly = true;
            this.label.Size = new System.Drawing.Size(100, 13);
            this.label.TabIndex = 42;
            this.label.Text = "-----------";
            //
            // STORE PRESET BUTTON
            //
            parentBox.Controls.Add(this.btStore);
            this.btStore.Font = new System.Drawing.Font("Microsoft Sans Serif", 6F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btStore.Location = new System.Drawing.Point(hOrigin-5, vOrigin - 4 + vOffset);
            this.btStore.Name = "btStore";
            this.btStore.Size = new System.Drawing.Size(20, 20);
            this.btStore.Text = "";
            this.btStore.BackgroundImage = global::OyRemote.Properties.Resources.redDot16;
            this.btStore.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.btStore.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.btStore.UseVisualStyleBackColor = true;
            this.btStore.Click += new System.EventHandler(this.btStore_Click);
            this.btStore.BringToFront();
        }

        private void radio_CheckedChanged(object sender, EventArgs e)
        {
            if ((this.radio.Checked) && (this.getCommand() != ""))
            {
                string returnCommand = parent.sendCommand(this.getCommand());
                parent.setStatus("Listening Mode: " + this.getLabel());
            }
        }

        private void btStore_Click(object sender, EventArgs e)
        {
            string returnMessage = parent.sendCommand("!1LMDQSTN");
            Debug.WriteLine(parent.lastFoundListeningMode);
            this.setCommand(parent.lastFoundListeningMode);
            parent.setStatus("LMD preset " + externalID.ToString() + " set");
        }

        public bool isChecked()
        {
            return this.radio.Checked;
        }

        public string getLabel()
        {
            return this.label.Text;
        }
        
        public void setLabel(string s)
        {
            this.label.Text = s;
        }

        public string getCommand()
        {
            return this.commandString;
        }

        public void setCommand(string s)
        {
            this.commandString = s;
        }

        public void show()
        {
            this.radio.Show();
            this.label.Show();
        }

        public void enableButton(bool b)
        {
            this.btStore.Enabled = b;
            this.btStore.Visible = b;
            this.label.ReadOnly = !b;
        }

        public void unCheck()
        {
            this.radio.Checked = false;
        }
    }
}
