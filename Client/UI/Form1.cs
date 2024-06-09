using PlcClient;

namespace UI
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        BrDevice? plc = null;

        private async void connectBtn_Click(object sender, EventArgs e)
        {
            plc = new BrDevice();
            await plc.Connect();

            flagCheckBox.Checked = plc.Flag;
            counter.Value = plc.Counter;
            plc.CounterChanged += (v) => counter.BeginInvoke((Action)(() => counter.Value = v));
        }

        private void flagCheckBox_CheckedChanged(object sender, EventArgs e) => plc.Flag = flagCheckBox.Checked;
    }
}
