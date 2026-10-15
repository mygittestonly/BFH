using ConcentrationDetectionSys.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ConcentrationDetectionSys.StepParamFrm
{
    public partial class HumanInterventionFrm : Form
    {
        private List<MapHoleInfo> _backup1, _backup2, _backup3;
        private BindingList<MapHoleInfo> _bindingList;
        public HumanInterventionFrm()
        {
            InitializeComponent();
            //dgv_HumanIntervention.CellFormatting += Dgv_CellFormatting;
            dgv_HumanIntervention.CellPainting += Dgv_CellPainting;
        }
        private void Dgv_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            //只处理State列
            if (dgv_HumanIntervention.Columns[e.ColumnIndex]?.Name != "State")
                return;

            //正在编辑单元格时，跳过格式化，防止下拉框显示异常
            if (dgv_HumanIntervention.IsCurrentCellInEditMode)
                return;

            //判断当前单元格的值是枚举
            if (e.Value is MapHoleInfoState state)
            {
                //把枚举转成文字显示（Pending/Running...）
                //e.Value = state.ToString();

                Color foreColor;
                switch (state)
                {
                    case MapHoleInfoState.Pending:
                        foreColor = Color.Black;
                        break;
                    case MapHoleInfoState.Running:
                        foreColor = Color.Green;
                        break;
                    case MapHoleInfoState.Completed:
                        foreColor = Color.Blue;
                        break;
                    case MapHoleInfoState.Abnormal:
                        foreColor = Color.Red;
                        break;
                    default:
                        foreColor = Color.Black;
                        break;
                }
                e.CellStyle.ForeColor = foreColor;
                e.CellStyle.SelectionForeColor = foreColor; //选中行，文字颜色不变，不会变白
            }
        }

        private void Dgv_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (dgv_HumanIntervention.Columns[e.ColumnIndex].Name != "State") return;

            // 绘制背景
            e.PaintBackground(e.ClipBounds, true);

            if (e.Value is MapHoleInfoState state)
            {
                Color textColor;
                switch (state)
                {
                    case MapHoleInfoState.Pending:
                        textColor = Color.Black;
                        break;
                    case MapHoleInfoState.Running:
                        textColor = Color.Green;
                        break;
                    case MapHoleInfoState.Completed:
                        textColor = Color.Blue;
                        break;
                    case MapHoleInfoState.Abnormal:
                        textColor = Color.Red;
                        break;
                    default:
                        textColor = Color.Black;
                        break;
                }
                string text = state.ToString();
                // 文字绘制区域
                var rect = Rectangle.Inflate(e.CellBounds, -2, -2);
                using (SolidBrush brush = new SolidBrush(textColor))
                {
                    e.Graphics.DrawString(text, e.CellStyle.Font, brush, rect, StringFormat.GenericDefault);
                }
            }
            e.Handled = true;
        }
        public void LoadData(List<MapHoleInfo> s1, List<MapHoleInfo> s2, List<MapHoleInfo> s3)
        {
            // 1. 备份，取消时回滚
            _backup1 = CloneList(s1);
            _backup2 = CloneList(s2);
            _backup3 = CloneList(s3);

            // 2. 合并成一个 BindingList（元素就是原对象引用）
            var all = new List<MapHoleInfo>();
            all.AddRange(s1);
            all.AddRange(s2);
            all.AddRange(s3);
            _bindingList = new BindingList<MapHoleInfo>(all);

            // 3. 建列 + 绑定
            BuildColumns(dgv_HumanIntervention);
            dgv_HumanIntervention.DataSource = _bindingList;
           

        }
        private void BuildColumns(DataGridView dgv)
        {
            dgv.AutoGenerateColumns = false;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.RowHeadersVisible = false;
            dgv.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dgv.EditMode = DataGridViewEditMode.EditOnEnter;

            dgv.Columns.Clear();

            // 只读
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Rack_ID",
                HeaderText = "Rack_ID",
                DataPropertyName = "RackID",
                ReadOnly = true,
                Width = 100
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Tube_ID",
                HeaderText = "Tube_ID",
                DataPropertyName = "TubeID",
                ReadOnly = true,
                Width = 100
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Location",
                HeaderText = "Location",
                DataPropertyName = "Location",
                ReadOnly = true,
                Width = 80
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "OrderID",
                HeaderText = "Order ID",
                DataPropertyName = "OrderID",
                ReadOnly = true,
                Width = 100
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "OrderRepeatID",
                HeaderText = "Order Repeat ID",
                DataPropertyName = "OrderRepeatID",
                ReadOnly = true,
                Width = 120
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Volume",
                HeaderText = "体积",
                DataPropertyName = "ReagentVolume",
                ReadOnly = true,
                Width = 80
            });
            // 可编辑
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Concentration",
                HeaderText = "浓度",
                DataPropertyName = "OD_Value",
                Width = 80
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ProteinMass",
                HeaderText = "蛋白量",
                DataPropertyName = "Final_Protein_Mass",
                Width = 90
            });
            // 新建下拉列
            DataGridViewComboBoxColumn cboCol = new DataGridViewComboBoxColumn
            {
                Name = "State",
                HeaderText = "State",
                DataPropertyName = "State", //绑定实体State枚举属性
                Width = 80,
                // 禁止用户手动输入，只能下拉选择
                // DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing, 
                //DropDownStyle = ComboBoxStyle.DropDownList,
                ReadOnly = false
            };

            // 绑定枚举全部选项
            cboCol.DataSource = Enum.GetValues(typeof(MapHoleInfoState));
            // 显示文字：枚举名称，值：枚举本身
            cboCol.ValueMember = null;
            cboCol.DisplayMember = null;

            dgv.Columns.Add(cboCol);
        }

        private List<MapHoleInfo> CloneList(List<MapHoleInfo> src)
        {
            if (src == null) return null;
            var list = new List<MapHoleInfo>();
            foreach (var item in src)
            {
                list.Add(item.Clone());
            }
            return list;
        }
        //private void Restore(List<MapHoleInfo> backup, List<MapHoleInfo> cur)
        //{
        //    if (backup == null || cur == null) return;
        //    for (int i = 0; i < cur.Count && i < backup.Count; i++)
        //    {
        //        cur[i].ReagentVolume = backup[i].ReagentVolume;
        //        cur[i].OD_Value = backup[i].OD_Value;
        //        cur[i].Final_Protein_Mass = backup[i].Final_Protein_Mass;
        //    }
        //}
        private void Restore(List<MapHoleInfo> backupList, List<MapHoleInfo> targetList)
        {
            if (backupList == null || targetList == null) return;
            foreach (var backupItem in backupList)
            {
                var targetItem = targetList.FirstOrDefault(x => x.RackID == backupItem.RackID && x.TubeID == backupItem.TubeID);
                if (targetItem == null) continue;
                targetItem.ReagentVolume = backupItem.ReagentVolume;
                targetItem.OD_Value = backupItem.OD_Value;
                targetItem.Final_Protein_Mass = backupItem.Final_Protein_Mass;
                targetItem.State = backupItem.State;
            }
        }
        private void btn_OK_Click(object sender, EventArgs e)
        {
            dgv_HumanIntervention.EndEdit(); 
        }
        private void btn_CancelChanges_Click(object sender, EventArgs e)
        {
            // 1. 提交未结束的编辑
            dgv_HumanIntervention.EndEdit();
            // 2. 回滚字段
            Restore(_backup1, CommonParam.s_MapHoleInfoListS1_1);
            Restore(_backup2, CommonParam.s_MapHoleInfoListS1_2);
            Restore(_backup3, CommonParam.s_MapHoleInfoListS1_3);
            // 3. 通知 Grid 刷新
            dgv_HumanIntervention.ResetBindings();
        }

        private void HumanInterventionFrm_FormClosing(object sender, FormClosingEventArgs e)
        {
            //dgv_HumanIntervention.CellFormatting -= Dgv_CellFormatting;
            dgv_HumanIntervention.CellPainting -= Dgv_CellPainting;
        }

        private void btn_RunBll_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btn_StopBll_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.No;
            this.Close();
        }
    }
}
