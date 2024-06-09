namespace UI
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            connectBtn = new Button();
            devicesBtn = new Button();
            SuspendLayout();
            // 
            // connectBtn
            // 
            connectBtn.Location = new Point(38, 33);
            connectBtn.Name = "connectBtn";
            connectBtn.Size = new Size(75, 23);
            connectBtn.TabIndex = 0;
            connectBtn.Text = "Connect";
            connectBtn.UseVisualStyleBackColor = true;
            connectBtn.Click += connectBtn_Click;
            // 
            // devicesBtn
            // 
            devicesBtn.Location = new Point(38, 62);
            devicesBtn.Name = "devicesBtn";
            devicesBtn.Size = new Size(75, 23);
            devicesBtn.TabIndex = 0;
            devicesBtn.Text = "Devices";
            devicesBtn.UseVisualStyleBackColor = true;
            devicesBtn.Click += getDevicesBtn_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(devicesBtn);
            Controls.Add(connectBtn);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
        }

        #endregion

        private Button connectBtn;
        private Button devicesBtn;
    }
}
