﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections;
using System.IO;

namespace Machine
{
    public partial class uctrlDeviceRecipe : UserControl
    {
        private frmMessaging2 frmMsg;
        public uctrlDeviceRecipe()
        {
            InitializeComponent();
            lv_DeviceRecipeList.Columns.Add("No", 30);
            lv_DeviceRecipeList.Columns.Add("Device ID", 100);
            lv_DeviceRecipeList.Columns.Add("1st Running Hz", 100);
            lv_DeviceRecipeList.Columns.Add("1st Running Time Limit", 130);
            lv_DeviceRecipeList.Columns.Add("2nd Running Hz", 100);
            lv_DeviceRecipeList.Columns.Add("2nd Running Time Limit", 130);
            lv_DeviceRecipeList.Columns.Add("Master Product", 90);
        }
        public static uctrlDeviceRecipe Page = new uctrlDeviceRecipe();
        public void ShowPage(Control parent)
        {
            Page.Parent = parent;
            Page.Dock = DockStyle.Fill;
            Page.Show();
            ClearListView();
            UpdateListView();
        }

        public void HidePage()
        {
            Visible = false;
        }

        private void UpdateListView()
        {
            int counter = 0;
            for (int i = 0; i < TaskDeviceRecipe.asDeviceID.Count(); i++)
            {
                string[] arr = new string[7];
                ListViewItem itm;
                try
                {
                    if (TaskDeviceRecipe.asDeviceID[i] == null) break;
                    counter++;
                    TimeSpan timeSpan = new TimeSpan();                  
                    arr[0] = counter.ToString();
                    int S, M, H; double SS;
                    arr[1] = TaskDeviceRecipe.asDeviceID[i].ToString();
                    //arr[2] = TaskDeviceRecipe.aiRunHz_1st[i].ToString(); //asBitCodeDescription
                    arr[2] = TaskBitCode.asBitCodeDescription[TaskDeviceRecipe.aiRunHz_1st[i]];
                    timeSpan = TimeSpan.FromMilliseconds(TaskDeviceRecipe.adTimeLimit_1st[i]);
                    SS = timeSpan.TotalSeconds;
                    S = timeSpan.Seconds;
                    M = timeSpan.Minutes;
                    H = timeSpan.Hours;
                    //arr[3] = H + " H " + M + " M " + S + " S ";
                    arr[3] = SS + " S ";
                    //arr[4] = TaskDeviceRecipe.aiRunHz_2nd[i].ToString();
                    arr[4] = TaskBitCode.asBitCodeDescription[TaskDeviceRecipe.aiRunHz_2nd[i]];
                    timeSpan = TimeSpan.FromMilliseconds(TaskDeviceRecipe.adTimeLimit_2nd[i]);
                    SS = timeSpan.TotalSeconds;
                    S = timeSpan.Seconds;
                    M = timeSpan.Minutes;
                    H = timeSpan.Hours;
                    //arr[5] = H + " H " + M + " M " + S + " S ";
                    arr[5] = SS + " S ";
                    arr[6] = TaskDeviceRecipe.abMasterProduct[i].ToString();
                    itm = new ListViewItem(arr);
                    lv_DeviceRecipeList.Items.Add(itm);
                }
                catch { }
            }
        }
        private void ClearListView()
        {
            foreach (ListViewItem item in lv_DeviceRecipeList.Items)
            {
                item.Remove();
            }
        }

        static frmDeviceRecipe form = new frmDeviceRecipe();
        private void btn_AddDeviceRecipe_Click(object sender, EventArgs e)
        {
            form._bEdit = false;
            form._bNew = true;
            form._sDeviceID = "";
            form._iRunHz_1st = 0;
            form._iTimeLimit_1st = 0;
            form._iRunHz_2nd = 0;
            form._iTimeLimit_2nd = 0;
            form._bMasterProduct = false;

            form.ShowDialog();
            ClearListView();
            UpdateListView();
        }

        private void btn_EditDeviceRecipe_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < lv_DeviceRecipeList.Items.Count; i++)
            {
                if (lv_DeviceRecipeList.Items[i].Selected)
                {
                    form._iIndex = i;
                    form._bEdit = true;
                    form._bNew = false;
                    form._sDeviceID = lv_DeviceRecipeList.Items[i].SubItems[1].Text;
                    //form._iRunHz_1st = Convert.ToInt32(lv_DeviceRecipeList.Items[i].SubItems[2].Text);
                    form._iRunHz_1st = TaskDeviceRecipe.aiRunHz_1st[i];
                    form._iTimeLimit_1st = Convert.ToInt32(TaskDeviceRecipe.adTimeLimit_1st[i]);
                    //form._iRunHz_2nd = Convert.ToInt32(lv_DeviceRecipeList.Items[i].SubItems[4].Text);
                    form._iRunHz_2nd = TaskDeviceRecipe.aiRunHz_2nd[i];
                    form._iTimeLimit_2nd = Convert.ToInt32(TaskDeviceRecipe.adTimeLimit_2nd[i]);
                    form._bMasterProduct = TaskDeviceRecipe.abMasterProduct[i];
                    form.ShowDialog();
                    ClearListView();
                    UpdateListView();
                }
            }
        }

        private void btn_DeleteDeviceRecipe_Click(object sender, EventArgs e)
        {
            string fileName = "";
            int count = 0;
            for (int i = 0; i < lv_DeviceRecipeList.Items.Count; i++)
            {
                if (lv_DeviceRecipeList.Items[i].Selected)
                {
                    count = i;
                    fileName = lv_DeviceRecipeList.Items[i].SubItems[1].Text;
                    goto _Continue;
                }
            }
            return;
        //Array.Clear(_asDeviceName[count], count, );
        _Continue:
            frmMsg = new frmMessaging2();
            frmMsg.ShowMsg("Are you sure to delete the selected Device Recipe?" + (char)13 +
                       "OK - Delete.", frmMessaging2.TMsgBtn.smbOK | frmMessaging2.TMsgBtn.smbCancel);
            DialogResult dialogResult = frmMsg.ShowDialog();
            if (dialogResult == DialogResult.OK)
            {
                File.Delete(GDefine.DevicePath + @"\" + fileName + ".ini");
                ClearListView();
                TaskDeviceRecipe.LoadDeviceRecipe();
                UpdateListView();
            }
        }
    }
}
