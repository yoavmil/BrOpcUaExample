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
            components = new System.ComponentModel.Container();
            connectBtn = new Button();
            counter = new NumericUpDown();
            brDeviceBindingSource = new BindingSource(components);
            flagCheckBox = new CheckBox();
            ((System.ComponentModel.ISupportInitialize)counter).BeginInit();
            ((System.ComponentModel.ISupportInitialize)brDeviceBindingSource).BeginInit();
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
            // counter
            // 
            counter.DataBindings.Add(new Binding("Value", brDeviceBindingSource, "Counter", true));
            counter.Location = new Point(38, 72);
            counter.Maximum = new decimal(new int[] { 256, 0, 0, 0 });
            counter.Name = "counter";
            counter.ReadOnly = true;
            counter.Size = new Size(120, 23);
            counter.TabIndex = 1;
            // 
            // brDeviceBindingSource
            // 
            brDeviceBindingSource.DataSource = typeof(PlcClient.OpcDevice);
            // 
            // flagCheckBox
            // 
            flagCheckBox.AutoSize = true;
            flagCheckBox.Location = new Point(38, 111);
            flagCheckBox.Name = "flagCheckBox";
            flagCheckBox.Size = new Size(46, 19);
            flagCheckBox.TabIndex = 2;
            flagCheckBox.Text = "flag";
            flagCheckBox.UseVisualStyleBackColor = true;
            flagCheckBox.CheckedChanged += flagCheckBox_CheckedChanged;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(328, 229);
            Controls.Add(flagCheckBox);
            Controls.Add(counter);
            Controls.Add(connectBtn);
            Name = "Form1";
            Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)counter).EndInit();
            ((System.ComponentModel.ISupportInitialize)brDeviceBindingSource).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button connectBtn;
        private NumericUpDown counter;
        private BindingSource brDeviceBindingSource;
        private CheckBox flagCheckBox;
    }
}
