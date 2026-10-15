namespace ConcentrationDetectionSys
{
    partial class ConcentrationMeasurementFrm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.cb_LowerOperator = new Sunny.UI.UIComboBox();
            this.DoubleUpDown_LowerLimitingValue = new Sunny.UI.UIDoubleUpDown();
            this.DoubleUpDown_UpperLimitValue = new Sunny.UI.UIDoubleUpDown();
            this.cb_UpperOperator = new Sunny.UI.UIComboBox();
            this.DoubleUpDown_LightPathValue = new Sunny.UI.UIDoubleUpDown();
            this.btn_SaveParam = new Sunny.UI.UIButton();
            this.cb_HumanIntervention = new Sunny.UI.UICheckBox();
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(234, 128);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(58, 21);
            this.label2.TabIndex = 1;
            this.label2.Text = "光程：";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(13, 169);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(279, 21);
            this.label3.TabIndex = 2;
            this.label3.Text = "判定 A260/A280 纯度比值合格区间：";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(300, 216);
            this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(74, 21);
            this.label6.TabIndex = 5;
            this.label6.Text = "上限阈值";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(300, 169);
            this.label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(74, 21);
            this.label7.TabIndex = 6;
            this.label7.Text = "下限阈值";
            // 
            // textBox1
            // 
            this.textBox1.BackColor = System.Drawing.Color.White;
            this.textBox1.Location = new System.Drawing.Point(11, 12);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.Size = new System.Drawing.Size(580, 96);
            this.textBox1.TabIndex = 8;
            this.textBox1.Text = "接收 TECAN 酶标仪上传 A260、A280、A340 原始 OD 值，自动执行计算：\r\n· 校正 A260 = A260 - A340\r\n· 蛋白浓度 = " +
    "校正 A260 ÷（消光系数 × 光程）\r\n· 自动判定 A260/A280 纯度比值是否在合格区间";
            // 
            // cb_LowerOperator
            // 
            this.cb_LowerOperator.DataSource = null;
            this.cb_LowerOperator.FillColor = System.Drawing.Color.White;
            this.cb_LowerOperator.Font = new System.Drawing.Font("微软雅黑", 12F);
            this.cb_LowerOperator.ItemHoverColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(200)))), ((int)(((byte)(255)))));
            this.cb_LowerOperator.Items.AddRange(new object[] {
            ">=",
            ">"});
            this.cb_LowerOperator.ItemSelectForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.cb_LowerOperator.Location = new System.Drawing.Point(394, 163);
            this.cb_LowerOperator.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cb_LowerOperator.MinimumSize = new System.Drawing.Size(63, 0);
            this.cb_LowerOperator.Name = "cb_LowerOperator";
            this.cb_LowerOperator.Padding = new System.Windows.Forms.Padding(0, 0, 30, 2);
            this.cb_LowerOperator.RectColor = System.Drawing.Color.DarkCyan;
            this.cb_LowerOperator.Size = new System.Drawing.Size(67, 32);
            this.cb_LowerOperator.SymbolSize = 24;
            this.cb_LowerOperator.TabIndex = 9;
            this.cb_LowerOperator.Text = ">=";
            this.cb_LowerOperator.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            this.cb_LowerOperator.Watermark = "";
            // 
            // DoubleUpDown_LowerLimitingValue
            // 
            this.DoubleUpDown_LowerLimitingValue.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.DoubleUpDown_LowerLimitingValue.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.DoubleUpDown_LowerLimitingValue.Location = new System.Drawing.Point(481, 165);
            this.DoubleUpDown_LowerLimitingValue.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.DoubleUpDown_LowerLimitingValue.Maximum = 1D;
            this.DoubleUpDown_LowerLimitingValue.Minimum = 0D;
            this.DoubleUpDown_LowerLimitingValue.MinimumSize = new System.Drawing.Size(1, 16);
            this.DoubleUpDown_LowerLimitingValue.Name = "DoubleUpDown_LowerLimitingValue";
            this.DoubleUpDown_LowerLimitingValue.Padding = new System.Windows.Forms.Padding(5);
            this.DoubleUpDown_LowerLimitingValue.RectColor = System.Drawing.Color.DarkCyan;
            this.DoubleUpDown_LowerLimitingValue.ShowText = false;
            this.DoubleUpDown_LowerLimitingValue.Size = new System.Drawing.Size(110, 29);
            this.DoubleUpDown_LowerLimitingValue.Step = 0.01D;
            this.DoubleUpDown_LowerLimitingValue.TabIndex = 11;
            this.DoubleUpDown_LowerLimitingValue.Text = "0.00";
            this.DoubleUpDown_LowerLimitingValue.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            this.DoubleUpDown_LowerLimitingValue.Value = 0D;
            // 
            // DoubleUpDown_UpperLimitValue
            // 
            this.DoubleUpDown_UpperLimitValue.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.DoubleUpDown_UpperLimitValue.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.DoubleUpDown_UpperLimitValue.Location = new System.Drawing.Point(481, 212);
            this.DoubleUpDown_UpperLimitValue.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.DoubleUpDown_UpperLimitValue.Maximum = 1D;
            this.DoubleUpDown_UpperLimitValue.Minimum = 0D;
            this.DoubleUpDown_UpperLimitValue.MinimumSize = new System.Drawing.Size(1, 16);
            this.DoubleUpDown_UpperLimitValue.Name = "DoubleUpDown_UpperLimitValue";
            this.DoubleUpDown_UpperLimitValue.Padding = new System.Windows.Forms.Padding(5);
            this.DoubleUpDown_UpperLimitValue.RectColor = System.Drawing.Color.DarkCyan;
            this.DoubleUpDown_UpperLimitValue.ShowText = false;
            this.DoubleUpDown_UpperLimitValue.Size = new System.Drawing.Size(110, 29);
            this.DoubleUpDown_UpperLimitValue.Step = 0.01D;
            this.DoubleUpDown_UpperLimitValue.TabIndex = 12;
            this.DoubleUpDown_UpperLimitValue.Text = "1.00";
            this.DoubleUpDown_UpperLimitValue.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            this.DoubleUpDown_UpperLimitValue.Value = 1D;
            // 
            // cb_UpperOperator
            // 
            this.cb_UpperOperator.DataSource = null;
            this.cb_UpperOperator.FillColor = System.Drawing.Color.White;
            this.cb_UpperOperator.Font = new System.Drawing.Font("微软雅黑", 12F);
            this.cb_UpperOperator.ItemHoverColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(200)))), ((int)(((byte)(255)))));
            this.cb_UpperOperator.Items.AddRange(new object[] {
            "<",
            "<="});
            this.cb_UpperOperator.ItemSelectForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.cb_UpperOperator.Location = new System.Drawing.Point(394, 210);
            this.cb_UpperOperator.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cb_UpperOperator.MinimumSize = new System.Drawing.Size(63, 0);
            this.cb_UpperOperator.Name = "cb_UpperOperator";
            this.cb_UpperOperator.Padding = new System.Windows.Forms.Padding(0, 0, 30, 2);
            this.cb_UpperOperator.RectColor = System.Drawing.Color.DarkCyan;
            this.cb_UpperOperator.Size = new System.Drawing.Size(67, 32);
            this.cb_UpperOperator.SymbolSize = 24;
            this.cb_UpperOperator.TabIndex = 10;
            this.cb_UpperOperator.Text = "<";
            this.cb_UpperOperator.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            this.cb_UpperOperator.Watermark = "";
            // 
            // DoubleUpDown_LightPathValue
            // 
            this.DoubleUpDown_LightPathValue.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.DoubleUpDown_LightPathValue.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.DoubleUpDown_LightPathValue.Location = new System.Drawing.Point(304, 124);
            this.DoubleUpDown_LightPathValue.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.DoubleUpDown_LightPathValue.Maximum = 20D;
            this.DoubleUpDown_LightPathValue.Minimum = 0D;
            this.DoubleUpDown_LightPathValue.MinimumSize = new System.Drawing.Size(1, 16);
            this.DoubleUpDown_LightPathValue.Name = "DoubleUpDown_LightPathValue";
            this.DoubleUpDown_LightPathValue.Padding = new System.Windows.Forms.Padding(5);
            this.DoubleUpDown_LightPathValue.RectColor = System.Drawing.Color.DarkCyan;
            this.DoubleUpDown_LightPathValue.ShowText = false;
            this.DoubleUpDown_LightPathValue.Size = new System.Drawing.Size(110, 29);
            this.DoubleUpDown_LightPathValue.Step = 1D;
            this.DoubleUpDown_LightPathValue.TabIndex = 12;
            this.DoubleUpDown_LightPathValue.Text = "0.00";
            this.DoubleUpDown_LightPathValue.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            this.DoubleUpDown_LightPathValue.Value = 0D;
            // 
            // btn_SaveParam
            // 
            this.btn_SaveParam.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_SaveParam.FillColor = System.Drawing.Color.DarkCyan;
            this.btn_SaveParam.FillColor2 = System.Drawing.Color.DarkCyan;
            this.btn_SaveParam.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_SaveParam.Location = new System.Drawing.Point(455, 258);
            this.btn_SaveParam.MinimumSize = new System.Drawing.Size(1, 1);
            this.btn_SaveParam.Name = "btn_SaveParam";
            this.btn_SaveParam.RectColor = System.Drawing.Color.DarkCyan;
            this.btn_SaveParam.Size = new System.Drawing.Size(137, 45);
            this.btn_SaveParam.TabIndex = 13;
            this.btn_SaveParam.Text = "保存参数";
            this.btn_SaveParam.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_SaveParam.Click += new System.EventHandler(this.btn_SaveParam_Click);
            // 
            // cb_HumanIntervention
            // 
            this.cb_HumanIntervention.CheckBoxColor = System.Drawing.Color.DarkCyan;
            this.cb_HumanIntervention.Cursor = System.Windows.Forms.Cursors.Hand;
            this.cb_HumanIntervention.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.cb_HumanIntervention.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.cb_HumanIntervention.Location = new System.Drawing.Point(32, 258);
            this.cb_HumanIntervention.MinimumSize = new System.Drawing.Size(1, 1);
            this.cb_HumanIntervention.Name = "cb_HumanIntervention";
            this.cb_HumanIntervention.Size = new System.Drawing.Size(150, 29);
            this.cb_HumanIntervention.TabIndex = 14;
            this.cb_HumanIntervention.Text = "人工是否干预";
            // 
            // ConcentrationMeasurementFrm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 21F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(612, 552);
            this.Controls.Add(this.cb_HumanIntervention);
            this.Controls.Add(this.btn_SaveParam);
            this.Controls.Add(this.DoubleUpDown_LightPathValue);
            this.Controls.Add(this.cb_UpperOperator);
            this.Controls.Add(this.DoubleUpDown_UpperLimitValue);
            this.Controls.Add(this.DoubleUpDown_LowerLimitingValue);
            this.Controls.Add(this.cb_LowerOperator);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Font = new System.Drawing.Font("微软雅黑", 12F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.Name = "ConcentrationMeasurementFrm";
            this.Text = "ConcentrationMeasurementFrm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox textBox1;
        private Sunny.UI.UIComboBox cb_LowerOperator;
        private Sunny.UI.UIDoubleUpDown DoubleUpDown_LowerLimitingValue;
        private Sunny.UI.UIDoubleUpDown DoubleUpDown_UpperLimitValue;
        private Sunny.UI.UIComboBox cb_UpperOperator;
        private Sunny.UI.UIDoubleUpDown DoubleUpDown_LightPathValue;
        private Sunny.UI.UIButton btn_SaveParam;
        private Sunny.UI.UICheckBox cb_HumanIntervention;
    }
}