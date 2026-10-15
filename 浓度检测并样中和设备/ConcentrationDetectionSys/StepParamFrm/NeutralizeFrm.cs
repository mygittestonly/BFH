using ConcentrationDetectionSys.Bll;
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

namespace ConcentrationDetectionSys.StepParamFrm
{
    public partial class NeutralizeFrm : Form
    {
        public event Action<NeutralizeParam> PassingValues;
        public NeutralizeParam NeutralizeParam;
        public NeutralizeFrm(NeutralizeParam param)
        {
            InitializeComponent();
            NeutralizeParam = new NeutralizeParam();
            NeutralizeParam = param;
            DoubleUpDown_eValue.Value = NeutralizeParam.e;
        }

        private void btn_SaveParam_Click(object sender, EventArgs e)
        {
            string str = "";
            if (DoubleUpDown_eValue.Value != NeutralizeParam.e)
            {
                str = $"中和系数 e={NeutralizeParam.e}修改为{DoubleUpDown_eValue.Value};";
            }
            if (str != "")
            {
                DialogResult dr = MessageBox.Show("确认更改参数？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dr == DialogResult.Yes)
                {
                    LogEventManager.LogEvent(31, str + $" 操作人：{CommonParam.s_CurrentUser.UserName}");
                    NeutralizeParam.e = DoubleUpDown_eValue.Value;
                    PassingValues?.Invoke(NeutralizeParam);
                }
                else
                {
                    DoubleUpDown_eValue.Value = NeutralizeParam.e;
                  
                }
            }
        }
    }
}
