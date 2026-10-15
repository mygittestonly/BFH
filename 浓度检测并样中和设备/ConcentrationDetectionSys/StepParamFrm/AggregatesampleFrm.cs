using ConcentrationDetectionSys.Bll;
using ConcentrationDetectionSys.Common;
using Sunny.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static ConcentrationDetectionSys.Bll.Aggregatesample;

namespace ConcentrationDetectionSys.StepParamFrm
{
    public partial class AggregatesampleFrm : Form
    {
        public event Action<AggregatesampleParam> PassingValues;
        public AggregatesampleParam aggregatesampleParam;
        public AggregatesampleFrm()
        {
            InitializeComponent();
        }
        public AggregatesampleFrm(AggregatesampleParam param)
        {
            InitializeComponent();
            aggregatesampleParam = new AggregatesampleParam();
            aggregatesampleParam = param;
            //No.1   K3
            No1_Operator.SelectedItem = param.No1_Operator;
            No1_ComparisonValue.Value = param.No1_ComparisonValue;
            //No.2   Mt
            No2_Operator.SelectedItem = param.No2_Operator;
            No2_ComparisonValue.Value = param.No2_ComparisonValue;
            //No.3   (M1+M2)/Mt
            No3_Operator.SelectedItem = param.No3_Operator;
            No3_ComparisonValue.Value = param.No3_ComparisonValue;
            //No.4   (M1+M2)/Mt
            No4_Operator.SelectedItem = param.No4_Operator;
            No4_ComparisonValue.Value = param.No4_ComparisonValue;
            //No.5   Mn/(M1+M2)
            No5_Operator.SelectedItem = param.No5_Operator;
            No5_ComparisonValue.Value = param.No5_ComparisonValue;
            //No.6   Mn/(M1+M2)
            No6_Operator.SelectedItem = param.No6_Operator;
            No6_ComparisonValue.Value = param.No6_ComparisonValue;
            //No.7   V1+V2
            No7_Operator.SelectedItem = param.No7_Operator;
            No7_ComparisonValue.Value = param.No7_ComparisonValue;
        }

        private void btn_SaveParam_Click(object sender, EventArgs e)
        {
            string str = "";
            if (No1_Operator.Text != aggregatesampleParam.No1_Operator)
            {
                str = $"No.1运算符旧值={aggregatesampleParam.No1_Operator}修改为{No1_Operator.Text};";
            }
            if (No1_ComparisonValue.Value != aggregatesampleParam.No1_ComparisonValue)
            {
                str = str + $"No.1比较值旧值={aggregatesampleParam.No1_ComparisonValue}修改为{No1_ComparisonValue.Value};";
            }
            if (No2_Operator.Text != aggregatesampleParam.No2_Operator)
            {
                str = str + $"No.2运算符旧值={aggregatesampleParam.No2_Operator}修改为{No2_Operator.Text};";
            }
            if (No2_ComparisonValue.Value != aggregatesampleParam.No2_ComparisonValue)
            {
                str = str + $"No.2比较值旧值={aggregatesampleParam.No2_ComparisonValue}修改为{No2_ComparisonValue.Value};";
            }
            if (No3_Operator.Text != aggregatesampleParam.No3_Operator)
            {
                str = str + $"No.3运算符旧值={aggregatesampleParam.No3_Operator}修改为{No3_Operator.Text};";
            }
            if (No3_ComparisonValue.Value != aggregatesampleParam.No3_ComparisonValue)
            {
                str = str + $"No.3比较值旧值={aggregatesampleParam.No3_ComparisonValue}修改为{No3_ComparisonValue.Value};";
            }
            if (No4_Operator.Text != aggregatesampleParam.No4_Operator)
            {
                str = str + $"No.4运算符旧值={aggregatesampleParam.No4_Operator}修改为{No4_Operator.Text};";
            }
            if (No4_ComparisonValue.Value != aggregatesampleParam.No4_ComparisonValue)
            {
                str = str + $"No.4比较值旧值={aggregatesampleParam.No4_ComparisonValue}修改为{No4_ComparisonValue.Value};";
            }
            if (No5_Operator.Text != aggregatesampleParam.No5_Operator)
            {
                str = str + $"No.5运算符旧值={aggregatesampleParam.No5_Operator}修改为{No5_Operator.Text};";
            }
            if (No5_ComparisonValue.Value != aggregatesampleParam.No5_ComparisonValue)
            {
                str = str + $"No.5比较值旧值={aggregatesampleParam.No5_ComparisonValue}修改为{No5_ComparisonValue.Value};";
            }
            if (No6_Operator.Text != aggregatesampleParam.No6_Operator)
            {
                str = str + $"No.6运算符旧值={aggregatesampleParam.No6_Operator}修改为{No6_Operator.Text};";
            }
            if (No6_ComparisonValue.Value != aggregatesampleParam.No6_ComparisonValue)
            {
                str = str + $"No.6比较值旧值={aggregatesampleParam.No6_ComparisonValue}修改为{No6_ComparisonValue.Value};";
            }
            if (No7_Operator.Text != aggregatesampleParam.No7_Operator)
            {
                str = str + $"No.7运算符旧值={aggregatesampleParam.No7_Operator}修改为{No7_Operator.Text};";
            }
            if (No7_ComparisonValue.Value != aggregatesampleParam.No7_ComparisonValue)
            {
                str = str + $"No.7比较值旧值={aggregatesampleParam.No7_ComparisonValue}修改为{No7_ComparisonValue.Value};";
            }
            if (str != "")
            {
                DialogResult dr = MessageBox.Show("确认更改参数？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dr == DialogResult.Yes)
                {
                    LogEventManager.LogEvent(31, str + $" 操作人：{CommonParam.s_CurrentUser.UserName}");
                    //No.1   K3
                    aggregatesampleParam.No1_Operator = No1_Operator.Text;
                    aggregatesampleParam.No1_ComparisonValue = No1_ComparisonValue.Value;
                    //No.2   Mt
                    aggregatesampleParam.No2_Operator = No2_Operator.Text;
                    aggregatesampleParam.No2_ComparisonValue = No2_ComparisonValue.Value;
                    //No.3   (M1+M2)/Mt
                    aggregatesampleParam.No3_Operator = No3_Operator.Text;
                    aggregatesampleParam.No3_ComparisonValue = No3_ComparisonValue.Value;
                    //No.4   (M1+M2)/Mt
                    aggregatesampleParam.No4_Operator = No4_Operator.Text;
                    aggregatesampleParam.No4_ComparisonValue = No4_ComparisonValue.Value;
                    //No.5   Mn/(M1+M2)
                    aggregatesampleParam.No5_Operator = No5_Operator.Text;
                    aggregatesampleParam.No5_ComparisonValue = No5_ComparisonValue.Value;
                    //No.6   Mn/(M1+M2)
                    aggregatesampleParam.No6_Operator = No6_Operator.Text;
                    aggregatesampleParam.No6_ComparisonValue = No6_ComparisonValue.Value;
                    //No.7   V1+V2
                    aggregatesampleParam.No7_Operator = No7_Operator.Text;
                    aggregatesampleParam.No7_ComparisonValue = No7_ComparisonValue.Value;
                    PassingValues?.Invoke(aggregatesampleParam);
                }
                else
                {
                    //No.1   K3
                    No1_Operator.SelectedItem = aggregatesampleParam.No1_Operator;
                    No1_ComparisonValue.Value = aggregatesampleParam.No1_ComparisonValue;
                    //No.2   Mt
                    No2_Operator.SelectedItem = aggregatesampleParam.No2_Operator;
                    No2_ComparisonValue.Value = aggregatesampleParam.No2_ComparisonValue;
                    //No.3   (M1+M2)/Mt
                    No3_Operator.SelectedItem = aggregatesampleParam.No3_Operator;
                    No3_ComparisonValue.Value = aggregatesampleParam.No3_ComparisonValue;
                    //No.4   (M1+M2)/Mt
                    No4_Operator.SelectedItem = aggregatesampleParam.No4_Operator;
                    No4_ComparisonValue.Value = aggregatesampleParam.No4_ComparisonValue;
                    //No.5   Mn/(M1+M2)
                    No5_Operator.SelectedItem = aggregatesampleParam.No5_Operator;
                    No5_ComparisonValue.Value = aggregatesampleParam.No5_ComparisonValue;
                    //No.6   Mn/(M1+M2)
                    No6_Operator.SelectedItem = aggregatesampleParam.No6_Operator;
                    No6_ComparisonValue.Value = aggregatesampleParam.No6_ComparisonValue;
                    //No.7   V1+V2
                    No7_Operator.SelectedItem = aggregatesampleParam.No7_Operator;
                    No7_ComparisonValue.Value = aggregatesampleParam.No7_ComparisonValue;
                }
            }
        }
    }
}
