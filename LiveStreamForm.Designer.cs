namespace OyRemote
{
    partial class LiveStreamForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.txtLiveOutput = new System.Windows.Forms.RichTextBox();
            this.checkBoxPauseLogging = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // txtLiveOutput
            // 
            this.txtLiveOutput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtLiveOutput.Location = new System.Drawing.Point(0, 0);
            this.txtLiveOutput.Name = "txtLiveOutput";
            this.txtLiveOutput.ReadOnly = true;
            this.txtLiveOutput.Size = new System.Drawing.Size(366, 395);
            this.txtLiveOutput.TabIndex = 0;
            this.txtLiveOutput.Text = "";
            // 
            // checkBoxPauseLogging
            // 
            this.checkBoxPauseLogging.AutoSize = true;
            this.checkBoxPauseLogging.BackColor = System.Drawing.Color.DarkGray;
            this.checkBoxPauseLogging.Location = new System.Drawing.Point(0, 2);
            this.checkBoxPauseLogging.Name = "checkBoxPauseLogging";
            this.checkBoxPauseLogging.Size = new System.Drawing.Size(92, 17);
            this.checkBoxPauseLogging.TabIndex = 3;
            this.checkBoxPauseLogging.Text = "pause logging";
            this.checkBoxPauseLogging.UseVisualStyleBackColor = false;
            // 
            // LiveStreamForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(366, 395);
            this.ControlBox = false;
            this.Controls.Add(this.checkBoxPauseLogging);
            this.Controls.Add(this.txtLiveOutput);
            this.Name = "LiveStreamForm";
            this.Text = "Live Data Stream";
            this.Load += new System.EventHandler(this.LiveStreamForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.RichTextBox txtLiveOutput;
        private System.Windows.Forms.CheckBox checkBoxPauseLogging;

    }
}