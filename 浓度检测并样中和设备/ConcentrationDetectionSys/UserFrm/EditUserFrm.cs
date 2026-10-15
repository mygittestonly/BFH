using ConcentrationDetectionSys.Common;
using Sunny.UI.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ConcentrationDetectionSys
{
    public partial class EditUserFrm : Form
    {
        public Common.User _user = new Common.User();
        public EditUserFrm()
        {
            InitializeComponent();
        }
        public EditUserFrm(Common.User user)
        {
            InitializeComponent();
            tb_UserPassWord.PasswordChar = '*';
            tb_UserPassWord2.PasswordChar = '*';
            this._user = user;
            tb_UserName.Text = _user.UserName;
            cb_DeviceRunMode.SelectedItem = _user.Class;
            uiSwitch1.Active = _user.State;
            tb_UserPassWord.Text = user.PassWord;
            tb_UserPassWord2.Text = user.PassWord;
        }

        private void btn_User_Cancel_Button_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
        private void tb_UserPassWord_TextChanged(object sender, EventArgs e)
        {
            _user.PassWord = tb_UserPassWord.Text.Trim();
        }
        private void cb_DeviceRunMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            _user.Class = cb_DeviceRunMode.SelectedItem.ToString();
        }

        private void uiSwitch1_ValueChanged(object sender, bool value)
        {
            _user.State = value;
        }
        private void btn_User_Ack_button_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_user.PassWord))
            {
                MessageBox.Show("密码不能为空");
                return;
            }
            if (_user.PassWord.Length > 20)
            {
                MessageBox.Show("密码过长不能超过20个字符");
                return;
            }
            if (tb_UserPassWord2.Text.Trim() != _user.PassWord)
            {
                MessageBox.Show("两次密码输入不一致");
                return;
            }
            if (string.IsNullOrEmpty(_user.Class))
            {
                MessageBox.Show("请选择权限等级");
                return;
            }
            this.DialogResult = DialogResult.OK;
        }
    }
}
