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
    public partial class LoadFrm : Form
    {
        public string LoadStepStr
        {
            set
            {
                if (lb_LoadStep.InvokeRequired)
                {
                    lb_LoadStep.Invoke(new Action(() => lb_LoadStep.Text = value));
                }
                else
                {
                    lb_LoadStep.Text = value;
                }
            }
        }
        public LoadFrm()
        {
            InitializeComponent();
            this.TopMost = true;
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}
