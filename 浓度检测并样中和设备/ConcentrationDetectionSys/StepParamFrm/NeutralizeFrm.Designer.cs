namespace ConcentrationDetectionSys.StepParamFrm
{
    partial class NeutralizeFrm
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
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.DoubleUpDown_eValue = new Sunny.UI.UIDoubleUpDown();
            this.btn_SaveParam = new Sunny.UI.UIButton();
            this.SuspendLayout();
            // 
            // textBox1
            // 
            this.textBox1.BackColor = System.Drawing.Color.White;
            this.textBox1.Location = new System.Drawing.Point(5, 5);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.Size = new System.Drawing.Size(635, 75);
            this.textBox1.TabIndex = 9;
            this.textBox1.Text = "中和液Volume(ml)=(Reagent Volume(ml)+磁珠添加量(ml)*0.15*中和系数e\r\n磁珠添加量=(Cedex yield/150)*C" +
    "al.M.W.(KDa)*1.2)/30*Expression Scale(ml)/1000, \r\n(若有小数，向上进位0.5即可，比如计算结果是3.1，那就进" +
    "位到3.5，3.7就进位到4.0;)";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(151, 112);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(104, 21);
            this.label2.TabIndex = 10;
            this.label2.Text = "中和系数 e：";
            // 
            // DoubleUpDown_eValue
            // 
            this.DoubleUpDown_eValue.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.DoubleUpDown_eValue.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.DoubleUpDown_eValue.Location = new System.Drawing.Point(252, 108);
            this.DoubleUpDown_eValue.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.DoubleUpDown_eValue.Maximum = 1D;
            this.DoubleUpDown_eValue.Minimum = 0D;
            this.DoubleUpDown_eValue.MinimumSize = new System.Drawing.Size(1, 16);
            this.DoubleUpDown_eValue.Name = "DoubleUpDown_eValue";
            this.DoubleUpDown_eValue.Padding = new System.Windows.Forms.Padding(5);
            this.DoubleUpDown_eValue.RectColor = System.Drawing.Color.DarkCyan;
            this.DoubleUpDown_eValue.ShowText = false;
            this.DoubleUpDown_eValue.Size = new System.Drawing.Size(110, 29);
            this.DoubleUpDown_eValue.Step = 0.01D;
            this.DoubleUpDown_eValue.TabIndex = 13;
            this.DoubleUpDown_eValue.Text = "1.00";
            this.DoubleUpDown_eValue.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            this.DoubleUpDown_eValue.Value = 1D;
            // 
            // btn_SaveParam
            // 
            this.btn_SaveParam.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_SaveParam.FillColor = System.Drawing.Color.DarkCyan;
            this.btn_SaveParam.FillColor2 = System.Drawing.Color.DarkCyan;
            this.btn_SaveParam.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_SaveParam.Location = new System.Drawing.Point(503, 100);
            this.btn_SaveParam.MinimumSize = new System.Drawing.Size(1, 1);
            this.btn_SaveParam.Name = "btn_SaveParam";
            this.btn_SaveParam.RectColor = System.Drawing.Color.DarkCyan;
            this.btn_SaveParam.Size = new System.Drawing.Size(137, 45);
            this.btn_SaveParam.TabIndex = 14;
            this.btn_SaveParam.Text = "保存参数";
            this.btn_SaveParam.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_SaveParam.Click += new System.EventHandler(this.btn_SaveParam_Click);
            // 
            // NeutralizeFrm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 21F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(641, 147);
            this.Controls.Add(this.btn_SaveParam);
            this.Controls.Add(this.DoubleUpDown_eValue);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textBox1);
            this.Font = new System.Drawing.Font("微软雅黑", 12F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Name = "NeutralizeFrm";
            this.Text = "NeutralizeFrm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label2;
        private Sunny.UI.UIDoubleUpDown DoubleUpDown_eValue;
        private Sunny.UI.UIButton btn_SaveParam;
    }
}