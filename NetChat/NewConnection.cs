using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NetChat
{
    public partial class NewConnection : Form
    {
        Main mainWindow;

        public NewConnection(Main mainWindow)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
        }

        bool IsValidInput(out ushort port)
        {
            if (!ushort.TryParse(tb_Port.Text, out port))
            {
                MessageBox.Show("Invalid port", "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return false;
            }

            if (string.IsNullOrWhiteSpace(tb_IP.Text) || string.IsNullOrWhiteSpace(tb_Name.Text) || string.IsNullOrWhiteSpace(tb_Secret.Text))
            {
                MessageBox.Show("Missing input", "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return false;
            }
            return true;
        }

        private void btn_Connect_Click(object sender, EventArgs e)
        {
            ushort port;
            if (!IsValidInput(out port))
                return;

            mainWindow.Connect(tb_IP.Text, port, tb_Name.Text, tb_Secret.Text, false);
            Close();
        }

        private void btn_Register_Click(object sender, EventArgs e)
        {
            ushort port;
            if (!IsValidInput(out port))
                return;

            mainWindow.Connect(tb_IP.Text, port, tb_Name.Text, tb_Secret.Text, true);
            Close();
        }
    }
}
