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
using ConcentrationDetectionSys.DAL;
using System.Data;
using System.Data.SqlClient;

namespace ConcentrationDetectionSys
{
    public partial class LoginFrm : Form
    {
        public User user = new User();
        public LoginFrm()
        {
            InitializeComponent();
        }

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
            //校验
            try
            {
                // 查询该用户
                string sql = "SELECT PassWord,Class,State,DateCreated FROM UserT WHERE UserName=@UserName";
                SqlParameter[] paras =
                {
                    new SqlParameter("@UserName", user.UserName)
                };
                DataTable dt = SqlHelper.ExecuteDataTable(sql, paras);

                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("用户名不存在！", "登录失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                //取出数据库密码
                string PassWord = dt.Rows[0]["PassWord"].ToString();
                string Class = dt.Rows[0]["Class"].ToString();
                bool State = dt.Rows[0]["State"].ToString() == "1" ? true : false;
                DateTime DateCreated = dt.Rows[0]["DateCreated"] != DBNull.Value ? Convert.ToDateTime(dt.Rows[0]["DateCreated"]) : DateTime.MinValue;
                if (PassWord == user.PassWord)
                {
                    CommonParam.s_CurrentUser.UserName = user.UserName;
                    CommonParam.s_CurrentUser.PassWord = PassWord;
                    CommonParam.s_CurrentUser.Class = Class;
                    CommonParam.s_CurrentUser.State = State;
                    CommonParam.s_CurrentUser.DateCreated = DateCreated;
                    MessageBox.Show("登录成功！");
                    this.DialogResult = DialogResult.OK;
                }
                else
                {
                    MessageBox.Show("密码错误！", "登录失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("数据库连接失败：" + ex.Message);
            }
        }

        private void LoginFrm_Load(object sender, EventArgs e)
        {
            tb_UserPassWord.PasswordChar = '*';

        }
    }
}
