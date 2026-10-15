using ConcentrationDetectionSys.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace ConcentrationDetectionSys
{
    public partial class AddUserFrm : Form
    {
        public User user = new User();
        public AddUserFrm()
        {
            InitializeComponent();
        }
        /// <summary>
        /// 取消按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_User_Cancel_Button_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void tb_UserName_TextChanged(object sender, EventArgs e)
        {
            user.UserName = tb_UserName.Text.Trim();
        }

        private void tb_UserPassWord_TextChanged(object sender, EventArgs e)
        {
            user.PassWord = tb_UserPassWord.Text.Trim();
        }

        private void tb_UserPassWord2_TextChanged(object sender, EventArgs e)
        {
            
        }

        private void cb_DeviceRunMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            user.Class = cb_DeviceRunMode.SelectedItem.ToString();
        }

        private void uiSwitch1_ValueChanged(object sender, bool value)
        {
            user.State = value;
        }

        private void btn_User_Ack_button_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(user.UserName))
            {
                MessageBox.Show("用户名不能为空");
                return;
            }
            if (user.UserName.Length > 20)
            {
                MessageBox.Show("用户名不能超过20个字符");
                return;
            }
            if (string.IsNullOrEmpty(user.PassWord))
            {
                MessageBox.Show("密码不能为空");
                return;
            }
            if (user.PassWord.Length > 20)
            {
                MessageBox.Show("密码过长不能超过20个字符");
                return;
            }
            if (string.IsNullOrEmpty(user.PassWord))
            {
                MessageBox.Show("请先输入密码");
                return;
            }
            if (tb_UserPassWord2.Text.Trim() != user.PassWord)
            {
                MessageBox.Show("两次密码输入不一致");
                return;
            }
            if (string.IsNullOrEmpty(user.Class))
            {
                MessageBox.Show("请选择权限等级");
                return;
            }
            this.DialogResult = DialogResult.OK; 
        }

        private void AddUserFrm_Load(object sender, EventArgs e)
        {
            tb_UserPassWord.PasswordChar = '*';
            tb_UserPassWord2.PasswordChar = '*';
          
        }
    }
}
