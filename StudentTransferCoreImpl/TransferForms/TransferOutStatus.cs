using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FISCA.Presentation.Controls;
using FISCA.Authentication;
using FISCA.UDT;
using DevComponents.DotNetBar;

namespace StudentTransferCoreImpl
{
    internal partial class TransferOutStatus : BaseForm
    {
        private StatusForm.TransferOutItem Item { get; set; }

        private StatusUIController StatusUI { get; set; }

        public TransferOutStatus(StatusForm.TransferOutItem item)
        {
            InitializeComponent();
            Item = item;

            StatusUI = new StatusUIController(new PanelEx[] { state1, state2, state3, state4 });
        }

        private void TransferOutStatus_Load(object sender, EventArgs e)
        {
            txtName.Text = string.Format("{0} {1}", Item.ClassName, Item.Name);
            txtTransferCode.Text = string.Format("{0}@{1}", Item.TransferToken, DSAServices.AccessPoint);
            txtTransferTarget.Text = Item.TransferTarget;

            if (Item.Status <= 1)
                btnConfirm.Enabled = false;

            if (Item.Status >= 3)
                ButtonToConfirmedStatus();

            RefreshGridData();
        }

        private void RefreshGridData()
        {
            foreach (string status in new string[] { "待轉出", "待確認", "已確認", "資料已轉出" })
            {
                if (status == Item.StatusString)
                    StatusUI.Status = Item.Status;
            }
        }

        private void ButtonToConfirmedStatus()
        {
            btnConfirm.Enabled = false;
            btnConfirm.Text = "已確認";
        }

        private void btnConfirm_Click(object sender, EventArgs e)
        {
            try
            {
                string msg = string.Format("確認之後「{0}」即可下載該學生資料，您確定嗎？", txtTransferTarget.Text);

                if (MessageBox.Show(msg, "ischool", MessageBoxButtons.OKCancel) != DialogResult.OK)
                    return;

                AccessHelper helper = new AccessHelper();

                List<TransferOutRecord> records = helper.Select<TransferOutRecord>(string.Format("uid='{0}'", Item.UID));

                if (records.Count > 0)
                {
                    TransferOutRecord record = records[0];
                    record.Status = 3;
                    record.Save();

                    Item.StatusString = "已確認";
                    Item.Status = 3;
                    RefreshGridData();
                    ButtonToConfirmedStatus();
                }
                else
                    throw new ArgumentException("爆炸!");
            }
            catch (Exception ex)
            {
                FISCA.RTOut.WriteError(ex);
                MessageBox.Show(ex.Message);
            }
        }
    }
}
