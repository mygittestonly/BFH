using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ConcentrationDetectionSys
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            bool createdNew;
            using (Mutex mutex = new Mutex(true, "ConcentrationDetectionSys_1.1.2.28", out createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show("软件已经打开，不能重复启动！");
                    return;
                }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                //登录
                LoginFrm loginForm = new LoginFrm();
                if (loginForm.ShowDialog() == DialogResult.OK)
                {
                    loginForm.Close();
                    Application.Run(new MainForm());
                }
            }
        }
    }
}
