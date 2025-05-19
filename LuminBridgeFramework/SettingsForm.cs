using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LuminBridgeFramework
{
    public partial class SettingsForm : Form
    {
        private List<Monitor> monitors;
        private SerialController serialController;
        public SettingsForm()
        {
            InitializeComponent();
            //chkAutostart.Checked = AutostartHelper.IsEnabled();
        }

        public void LoadSettings(List<Monitor> monitorList, SerialController serialController)
        {
            this.serialController = serialController;
            monitors = monitorList;
            cmbDevices.DataSource = monitors;
            cmbDevices.DisplayMember = "FriendlyName";
            cmbDevices.ValueMember = "Name";
            txtAlias.Text = monitors.FirstOrDefault()?.FriendlyName;

            ColorBtnConnect();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (!serialController.IsConnected)
            {
                serialController.IdentifyAndConnect();
                serialController.SendFullSyncPacket(monitors);
            }
            ColorBtnConnect();
        }

        private void ColorBtnConnect()
        {
            if (serialController.IsConnected)
            {
                btnConnect.BackColor = Color.Green;
                btnConnect.Text = "Connected";
            }
            else
            {
                btnConnect.BackColor = Color.Red;
                btnConnect.Text = "Connect";
            }
        }

        private void btnSaveAlias_Click(object sender, EventArgs e)
        {
            var selected = (Monitor)cmbDevices.SelectedItem;
            if (selected != null)
            {
                selected.FriendlyName = txtAlias.Text;
                selected.SaveConfig();
            }
            RefreshDevices();
        }

        private void cmbDevices_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbDevices.SelectedItem == null) return;
            var selected = (Monitor)cmbDevices.SelectedItem;
            txtAlias.Text = selected.FriendlyName;
        }

        private void RefreshDevices()
        {
            int selectedIndex = cmbDevices.SelectedIndex;
            cmbDevices.DataSource = null;
            cmbDevices.DataSource = monitors;
            cmbDevices.DisplayMember = "FriendlyName";
            cmbDevices.ValueMember = "Name";
            cmbDevices.SelectedIndex = selectedIndex;
        }
    }
}
