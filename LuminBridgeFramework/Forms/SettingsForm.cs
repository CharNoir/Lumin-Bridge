using LuminBridgeFramework.Helpers;
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
        private List<BaseDevice> _devices;
        private SerialController _serialController;
        public SettingsForm()
        {
            InitializeComponent();
            //chkAutostart.Checked = AutostartHelper.IsEnabled();
        }

        public void LoadSettings(List<BaseDevice> deviceList, SerialController serialController)
        {
            this._serialController = serialController;
            _devices = deviceList;

            cmbDevices.DataSource = null;
            cmbDevices.DataSource = _devices;
            cmbDevices.DisplayMember = "FriendlyName";
            cmbDevices.ValueMember = "IconId";

            txtAlias.Text = _devices.FirstOrDefault()?.FriendlyName;
            chkIsVisible.Checked = _devices.FirstOrDefault().IsVisible;

            chkAutostart.Checked = AutostartHelper.IsEnabled();

            ColorBtnConnect();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (!_serialController.IsConnected)
            {
                _serialController.ConnectAndSync(_devices);
            }
            ColorBtnConnect();
        }

        private void ColorBtnConnect()
        {
            if (_serialController.IsConnected)
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
            var selected = (BaseDevice)cmbDevices.SelectedItem;
            if (selected != null)
            {
                selected.FriendlyName = txtAlias.Text;
                selected.IsVisible = chkIsVisible.Checked;
                selected.SaveConfig();
            }
            RefreshDevices();
        }

        private void cmbDevices_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbDevices.SelectedItem == null) return;
            var selected = (BaseDevice)cmbDevices.SelectedItem;
            txtAlias.Text = selected.FriendlyName;
            chkIsVisible.Checked = selected.IsVisible;
        }

        private void RefreshDevices()
        {
            int selectedIndex = cmbDevices.SelectedIndex;
            cmbDevices.DataSource = null;
            cmbDevices.DataSource = _devices;
            cmbDevices.DisplayMember = "FriendlyName";
            cmbDevices.ValueMember = "IconId";
            cmbDevices.SelectedIndex = selectedIndex;
        }

        private void chkAutostart_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAutostart.Checked)
                AutostartHelper.Enable();
            else
                AutostartHelper.Disable();
        }
    }
}
