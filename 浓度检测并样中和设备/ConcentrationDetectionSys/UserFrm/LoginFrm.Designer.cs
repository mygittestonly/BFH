namespace ConcentrationDetectionSys
{
    partial class LoginFrm
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
            this.lb_UserOperationName = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.btn_User_Ack_button = new Sunny.UI.UIButton();
            this.btn_User_Cancel_Button = new Sunny.UI.UIButton();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tb_UserPassWord = new Sunny.UI.UITextBox();
            this.tb_UserName = new Sunny.UI.UITextBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.SuspendLayout();
            // 
            // lb_UserOperationName
            // 
            this.lb_UserOperationName.AutoSize = true;
            this.lb_UserOperationName.BackColor = System.Drawing.Color.DarkCyan;
            this.lb_UserOperationName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lb_UserOperationName.Font = new System.Drawing.Font("微软雅黑", 16F);
            this.lb_UserOperationName.ForeColor = System.Drawing.Color.White;
            this.lb_UserOperationName.Location = new System.Drawing.Point(3, 0);
            this.lb_UserOperationName.Name = "lb_UserOperationName";
            this.lb_UserOperationName.Size = new System.Drawing.Size(411, 60);
            this.lb_UserOperationName.TabIndex = 0;
            this.lb_UserOperationName.Text = "登 录";
            this.lb_UserOperationName.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Location = new System.Drawing.Point(4, 1);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(128, 47);
            this.label1.TabIndex = 1;
            this.label1.Text = "用户名：";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label2.Location = new System.Drawing.Point(4, 49);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(128, 47);
            this.label2.TabIndex = 2;
            this.label2.Text = "密码：";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btn_User_Ack_button
            // 
            this.btn_User_Ack_button.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_User_Ack_button.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_User_Ack_button.FillColor = System.Drawing.Color.DarkCyan;
            this.btn_User_Ack_button.FillHoverColor = System.Drawing.Color.LightSeaGreen;
            this.btn_User_Ack_button.Font = new System.Drawing.Font("微软雅黑", 16F);
            this.btn_User_Ack_button.Location = new System.Drawing.Point(3, 3);
            this.btn_User_Ack_button.MinimumSize = new System.Drawing.Size(1, 1);
            this.btn_User_Ack_button.Name = "btn_User_Ack_button";
            this.btn_User_Ack_button.RectColor = System.Drawing.Color.DarkCyan;
            this.btn_User_Ack_button.Size = new System.Drawing.Size(199, 48);
            this.btn_User_Ack_button.TabIndex = 6;
            this.btn_User_Ack_button.Text = "确 认";
            this.btn_User_Ack_button.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_User_Ack_button.Click += new System.EventHandler(this.btn_User_Ack_button_Click);
            // 
            // btn_User_Cancel_Button
            // 
            this.btn_User_Cancel_Button.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_User_Cancel_Button.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_User_Cancel_Button.FillColor = System.Drawing.Color.DarkCyan;
            this.btn_User_Cancel_Button.FillHoverColor = System.Drawing.Color.LightSeaGreen;
            this.btn_User_Cancel_Button.Font = new System.Drawing.Font("微软雅黑", 16F);
            this.btn_User_Cancel_Button.Location = new System.Drawing.Point(208, 3);
            this.btn_User_Cancel_Button.MinimumSize = new System.Drawing.Size(1, 1);
            this.btn_User_Cancel_Button.Name = "btn_User_Cancel_Button";
            this.btn_User_Cancel_Button.RectColor = System.Drawing.Color.DarkCyan;
            this.btn_User_Cancel_Button.Size = new System.Drawing.Size(200, 48);
            this.btn_User_Cancel_Button.TabIndex = 7;
            this.btn_User_Cancel_Button.Text = "取 消";
            this.btn_User_Cancel_Button.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_User_Cancel_Button.Click += new System.EventHandler(this.btn_User_Cancel_Button_Click);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single;
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 32.92683F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 67.07317F));
            this.tableLayoutPanel1.Controls.Add(this.tb_UserPassWord, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.tb_UserName, 1, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(3, 63);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(411, 97);
            this.tableLayoutPanel1.TabIndex = 8;
            // 
            // tb_UserPassWord
            // 
            this.tb_UserPassWord.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.tb_UserPassWord.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tb_UserPassWord.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tb_UserPassWord.Location = new System.Drawing.Point(140, 54);
            this.tb_UserPassWord.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.tb_UserPassWord.MinimumSize = new System.Drawing.Size(1, 16);
            this.tb_UserPassWord.Name = "tb_UserPassWord";
            this.tb_UserPassWord.Padding = new System.Windows.Forms.Padding(5);
            this.tb_UserPassWord.RectColor = System.Drawing.Color.DarkCyan;
            this.tb_UserPassWord.ShowText = false;
            this.tb_UserPassWord.Size = new System.Drawing.Size(266, 37);
            this.tb_UserPassWord.TabIndex = 7;
            this.tb_UserPassWord.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            this.tb_UserPassWord.Watermark = "";
            this.tb_UserPassWord.TextChanged += new System.EventHandler(this.tb_UserPassWord_TextChanged);
            // 
            // tb_UserName
            // 
            this.tb_UserName.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.tb_UserName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tb_UserName.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tb_UserName.Location = new System.Drawing.Point(140, 6);
            this.tb_UserName.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.tb_UserName.MinimumSize = new System.Drawing.Size(1, 16);
            this.tb_UserName.Name = "tb_UserName";
            this.tb_UserName.Padding = new System.Windows.Forms.Padding(5);
            this.tb_UserName.RectColor = System.Drawing.Color.DarkCyan;
            this.tb_UserName.ShowText = false;
            this.tb_UserName.Size = new System.Drawing.Size(266, 37);
            this.tb_UserName.TabIndex = 6;
            this.tb_UserName.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            this.tb_UserName.Watermark = "";
            this.tb_UserName.TextChanged += new System.EventHandler(this.tb_UserName_TextChanged);
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 1;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Controls.Add(this.lb_UserOperationName, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.tableLayoutPanel1, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.tableLayoutPanel3, 0, 2);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 3;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(417, 223);
            this.tableLayoutPanel2.TabIndex = 10;
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 2;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel3.Controls.Add(this.btn_User_Ack_button, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.btn_User_Cancel_Button, 1, 0);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(3, 166);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 1;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(411, 54);
            this.tableLayoutPanel3.TabIndex = 9;
            // 
            // LoginFrm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(11F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(417, 223);
            this.Controls.Add(this.tableLayoutPanel2);
            this.Font = new System.Drawing.Font("微软雅黑", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "LoginFrm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "LoginFrm";
            this.Load += new System.EventHandler(this.LoginFrm_Load);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.tableLayoutPanel3.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label lb_UserOperationName;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private Sunny.UI.UIButton btn_User_Ack_button;
        private Sunny.UI.UIButton btn_User_Cancel_Button;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private Sunny.UI.UITextBox tb_UserPassWord;
        private Sunny.UI.UITextBox tb_UserName;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
    }
}