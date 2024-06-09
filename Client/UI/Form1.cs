using PlcClient;

namespace UI
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        BrDevice plc;

        private async void connectBtn_Click(object sender, EventArgs e)
        {
            plc = new BrDevice();
            await plc.Connect();
        }

        private void getDevicesBtn_Click(object sender, EventArgs e)
        {
            //plc.GetDevices();
        }
    }
}
