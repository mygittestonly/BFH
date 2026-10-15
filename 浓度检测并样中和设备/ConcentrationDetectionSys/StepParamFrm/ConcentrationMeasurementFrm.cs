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

namespace ConcentrationDetectionSys
{
    public partial class ConcentrationMeasurementFrm : Form
    {
        public event Action<ConcentrationMeasurementParam> PassingValues;
        public ConcentrationMeasurementParam concentrationMeasurementParam;
        public ConcentrationMeasurementFrm()
        {
            InitializeComponent();
        }
        public ConcentrationMeasurementFrm(ConcentrationMeasurementParam param)
        {
            InitializeComponent();
            concentrationMeasurementParam = new ConcentrationMeasurementParam();
            //下限值
            concentrationMeasurementParam.RatioLowerLimit = param.RatioLowerLimit;
            DoubleUpDown_LowerLimitingValue.Value = concentrationMeasurementParam.RatioLowerLimit;
            //下限运算符
            concentrationMeasurementParam.LowerOperator = param.LowerOperator;
            cb_LowerOperator.SelectedItem = concentrationMeasurementParam.LowerOperator;
            //上限值
            concentrationMeasurementParam.RatioUpperLimit = param.RatioUpperLimit;
            DoubleUpDown_UpperLimitValue.Value = concentrationMeasurementParam.RatioUpperLimit;
            //上限运算符
            concentrationMeasurementParam.UpperOperator = param.UpperOperator;
            cb_UpperOperator.SelectedItem = concentrationMeasurementParam.UpperOperator;
            //光路
            concentrationMeasurementParam.OpticalDistance = param.OpticalDistance;
            DoubleUpDown_LightPathValue.Value = concentrationMeasurementParam.OpticalDistance;
            //人工干预
            concentrationMeasurementParam.HumanIntervention = param.HumanIntervention;
            cb_HumanIntervention.Checked = param.HumanIntervention;
        }

        private void btn_SaveParam_Click(object sender, EventArgs e)
        {
            string str = "";
            if (DoubleUpDown_LightPathValue.Value != concentrationMeasurementParam.OpticalDistance)
            {
                str = $"光程旧值={concentrationMeasurementParam.OpticalDistance}修改为{DoubleUpDown_LightPathValue.Value};";
            }
            if (cb_LowerOperator.Text != concentrationMeasurementParam.LowerOperator)
            {
                str = str + $"下限阈值运算符={concentrationMeasurementParam.LowerOperator}修改为{cb_LowerOperator.Text};";
            }
            if (DoubleUpDown_LowerLimitingValue.Value != concentrationMeasurementParam.RatioLowerLimit)
            {
                str = str + $"下限阈值={concentrationMeasurementParam.RatioLowerLimit}修改为{DoubleUpDown_LowerLimitingValue.Value};";
            }
            if (DoubleUpDown_UpperLimitValue.Value != concentrationMeasurementParam.RatioUpperLimit)
            {
                str = str + $"上限阈值={concentrationMeasurementParam.RatioUpperLimit}修改为{DoubleUpDown_UpperLimitValue.Value};";
            }
            if (cb_UpperOperator.Text != concentrationMeasurementParam.UpperOperator)
            {
                str = str + $"上限阈值运算符={concentrationMeasurementParam.UpperOperator}修改为{cb_UpperOperator.Text};";
            }
            if (cb_HumanIntervention.Checked != concentrationMeasurementParam.HumanIntervention)
            {
                str = str + $"人工干预{concentrationMeasurementParam.HumanIntervention}修改为{cb_HumanIntervention.Checked};";
            }
            if (str != "")
            {
                DialogResult dr = MessageBox.Show("确认更改参数？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dr == DialogResult.Yes)
                {
                    if (DoubleUpDown_UpperLimitValue.Value < DoubleUpDown_LowerLimitingValue.Value)
                    {
                        MessageBox.Show("上限阈值<上限阈值,参数错误");
                        return;
                    }
                    LogEventManager.LogEvent(31, str + $" 操作人：{CommonParam.s_CurrentUser.UserName}");
                    concentrationMeasurementParam.OpticalDistance = DoubleUpDown_LightPathValue.Value;
                    concentrationMeasurementParam.LowerOperator = cb_LowerOperator.Text;
                    concentrationMeasurementParam.RatioLowerLimit = DoubleUpDown_LowerLimitingValue.Value;
                    concentrationMeasurementParam.RatioUpperLimit = DoubleUpDown_UpperLimitValue.Value;
                    concentrationMeasurementParam.UpperOperator = cb_UpperOperator.Text;
                    concentrationMeasurementParam.HumanIntervention = cb_HumanIntervention.Checked;
                    PassingValues?.Invoke(concentrationMeasurementParam);
                }
                else
                {
                    //下限值
                    DoubleUpDown_LowerLimitingValue.Value = concentrationMeasurementParam.RatioLowerLimit;
                    //下限运算符
                    cb_LowerOperator.SelectedItem = concentrationMeasurementParam.LowerOperator;
                    //上限值
                    DoubleUpDown_UpperLimitValue.Value = concentrationMeasurementParam.RatioUpperLimit;
                    //上限运算符
                    cb_UpperOperator.SelectedItem = concentrationMeasurementParam.UpperOperator;
                    //光路
                    DoubleUpDown_LightPathValue.Value = concentrationMeasurementParam.OpticalDistance;

                    cb_HumanIntervention.Checked = concentrationMeasurementParam.HumanIntervention;
                }
            }
        }
    }
}
