﻿using Infrastructure;
using SeqServer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace Machine
{
    public partial class uctrlAuto : UserControl
    {
        public uctrlAuto()
        {
            InitializeComponent();
            frmMain.MainEvent.AddUISubscription(new EventHandler(OnSeqTriggerMachine));
            InitializeDataGridView();
            InitTimer();
            SetMcState(eMcState.MC_INITIALIZING);
            frmMain.MainEvent.UITriggerEvent(EV_TYPE.InitSeq);
        }

        public static uctrlAuto Page = new uctrlAuto();
        public static bool AutoPageShow = false;
        private static readonly object WriteLock = new object();
        private eMcState mcState = eMcState.MC_BEGIN;
        private eMcState mcPreState = eMcState.MC_BEGIN;
        private Timer tmrUpdateDisplay = new Timer();
        private Timer tmrSignal = new Timer();
        private Timer tmrUPH = new Timer();

        private void InitTimer()
        {
            tmrUpdateDisplay.Interval = 100;
            tmrUpdateDisplay.Tick += tmrUpdateDisplay_Tick;
            tmrUpdateDisplay.Start();

            tmrSignal.Interval = 100;
            tmrSignal.Tick += tmrSignal_Tick;
            tmrSignal.Start();

            tmrUPH.Interval = 1000;
            tmrUPH.Tick += tmrUPH_Tick;
            tmrUPH.Start();
        }

        public void ShowPage(Control parent)
        {
            Page.Parent = parent;
            Page.Dock = DockStyle.Fill;
            Page.Show();

            //SM.RecipeName = TaskDeviceRecipe._LotInfo.PartNum;
            AutoPageShow = true;
        }

        public void HidePage()
        {
            AutoPageShow = false;
            Visible = false;
        }

        private void InitializeDataGridView()
        {
            DataGridViewTextBoxColumn column1 = new DataGridViewTextBoxColumn();
            column1.HeaderText = "Sequence Name";
            column1.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; // or DataGridViewAutoSizeColumnMode.AllCellsExceptHeader
            column1.FillWeight = 1;

            DataGridViewTextBoxColumn column2 = new DataGridViewTextBoxColumn();
            column2.HeaderText = "Running Sequence";
            column2.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; // or DataGridViewAutoSizeColumnMode.AllCellsExceptHeader
            column2.FillWeight = 1;

            // Add columns to the DataGridView
            dgvSeqNum.Columns.Add(column1);
            dgvSeqNum.Columns.Add(column2);

            // Set the number of columns and rows
            dgvSeqNum.ColumnCount = 2;
            dgvSeqNum.RowCount = (int)TotalModule.MaxModule;
            TotalModule[] totalModules = (TotalModule[])Enum.GetValues(typeof(TotalModule));

            // Populate data (for example purposes)            
            for (int row = 0; row < (int)TotalModule.MaxModule; row++)
            {
                dgvSeqNum.Rows[row].Cells[0].Value = $"{totalModules[row]}";
            }
        }

        private void ShowRunningNumbers()
        {
            var listrunning = frmMain.SequenceRun.GetSeqNum();

            // Populate data (for example purposes)            
            for (int row = 0; row < (int)TotalModule.MaxModule; row++)
            {
                if (listrunning[row] != "MC_ResumeReq")
                    dgvSeqNum.Rows[row].Cells[1].Value = $"{listrunning[row]}";
            }
        }

        public void AddToLog(string S)
        {
            if (lbox_Log.Items.Count >= 100)
            {
                try
                {
                    if (lbox_Log.InvokeRequired)
                    {
                        lbox_Log.Invoke(new Action(() => lbox_Log.Items.RemoveAt(0)));
                    }
                    else
                    {
                        lbox_Log.Items.RemoveAt(0);
                    }
                }
                catch { }
            }

            string Date = DateTime.Now.Date.ToString("yyyyMMdd");
            string Time = DateTime.Now.ToString("HH:mm:ss");
            string MM = DateTime.Now.Month.ToString();
            if (MM.Length == 1) { MM = "0" + MM; }
            string YYYY = DateTime.Now.Year.ToString();
            string DD = DateTime.Now.Day.ToString();
            if (DD.Length == 1) { DD = "0" + DD; }

            S = Time + (char)9 + S;
            try
            {
                if (lbox_Log.InvokeRequired)
                {
                    // If we are not in the UI's thread we use BeginInvoke to run this method in the UI's thred.

                    lbox_Log.Invoke(new Action(() => lbox_Log.Items.Add(S)));
                    //lbox_Log.Invoke(new Action(() => lbox_Log.TopIndex = lbox_Log.Items.IndexOf(S)));
                    lbox_Log.Invoke(new Action(() => lbox_Log.TopIndex = lbox_Log.Items.Count - 1));
                }
                else
                {
                    lbox_Log.Items.Add(S);
                    //lbox_Log.SetSelected(lbox_Log.Items.Count - 1, true);
                    lbox_Log.TopIndex = lbox_Log.Items.Count - 1;
                    //lbox_Log.Update();
                    //lbox_Log.Refresh(); 
                }
            }
            catch
            { }
            string LogDir = GDefine.LogPath + @"\";
            if (!Directory.Exists(LogDir)) { Directory.CreateDirectory(LogDir); }
            string LogFile = LogDir + Date + ".log";

            FileStream F = new FileStream(LogFile, FileMode.Append, FileAccess.Write, FileShare.Write);
            StreamWriter W = new StreamWriter(F);
            W.WriteLine(S);
            W.Close();

            try
            {
                string[] SplitErr = S.Split((char)13);
                if (SplitErr.Count() > 1)
                {
                    try
                    {
                        string[] SplitErrAgain = SplitErr[0].Split((char)10);
                        if (SplitErrAgain.Count() > 1)
                            S = Time + (char)9 + SplitErrAgain[0] + (char)9 + SplitErrAgain[1];
                        else
                            S = Time + (char)9 + "" + (char)9 + "" + (char)9 + SplitErrAgain[0];
                    }
                    catch { }
                }
                else
                {
                    string[] SplitErrAgain = SplitErr[0].Split((char)10);
                    if (SplitErrAgain.Count() > 1)
                        S = SplitErrAgain[0] + (char)9 + SplitErrAgain[1];
                    else
                        S = SplitErrAgain[0];
                }
            }
            catch { }
            string OverallLogDir = GDefine.OveralLogPath + @"\";
            if (!Directory.Exists(OverallLogDir)) { Directory.CreateDirectory(OverallLogDir); }
            string OverallErrFile = OverallLogDir + Date + ".log";
            FileStream F1 = new FileStream(OverallErrFile, FileMode.Append, FileAccess.Write, FileShare.Write);
            StreamWriter W1 = new StreamWriter(F1);

            W1.WriteLine(S);
            W1.Close();


            try
            {
                string[] SplitErr = S.Split((char)13);
                if (SplitErr.Count() > 1)
                {
                    try
                    {
                        string[] SplitErrAgain = SplitErr[0].Split((char)10);
                        if (SplitErrAgain.Count() > 1)
                            S = Time + (char)9 + SplitErrAgain[0] + (char)9 + SplitErrAgain[1];
                        else
                            S = Time + (char)9 + "" + (char)9 + "" + (char)9 + SplitErrAgain[0];
                    }
                    catch { }
                }
                else
                {
                    string[] SplitErrAgain = SplitErr[0].Split((char)10);
                    if (SplitErrAgain.Count() > 1)
                        S = SplitErrAgain[0] + (char)9 + SplitErrAgain[1];
                    else
                        S = SplitErrAgain[0];
                    //S = Time + (char)9 + "" + (char)9 + "" + (char)9 + S;
                }
            }
            catch { }
            string Month = DateTime.Now.ToString("MMM");
            string Year = DateTime.Now.Year.ToString();
            string OverallLogDir2 = GDefine.OveralMonthLogPath + @"\";
            if (!Directory.Exists(OverallLogDir2)) { Directory.CreateDirectory(OverallLogDir2); }
            string OverallErrFile2 = OverallLogDir2 + Month + Year + ".log";
            FileStream F2 = new FileStream(OverallErrFile2, FileMode.Append, FileAccess.Write, FileShare.Write);
            StreamWriter W2 = new StreamWriter(F2);

            W2.WriteLine(S);
            W2.Close();
            frmMain.frmLog.AddToLog(S);
        }

        public void PromptMessage(MessageEventArg msgFormat)
        {
            var strmsg = msgFormat;
            InvokeHelper.Enable(this, false);
            AddToLog($"{strmsg}");
            frmMessaging msgForm = new frmMessaging(msgFormat);
            msgForm.StartPosition = FormStartPosition.CenterParent;
            msgForm.ShowMsg(strmsg.Message, frmMessaging.TMsgBtn.smbOK);
            InvokeHelper.Enable(this, true);
        }

        public eMcState GetMcState()
        {
            return this.mcState;
        }

        private void SetMcState(eMcState mcState)
        {
            this.mcPreState = this.mcState;
            this.mcState = mcState;
            OnMcStateChanged(mcState);
        }

        #region Machine Status Control
        public bool Initialized = false;
        public bool EndLot = false;
        private void OnMcStateChanged(eMcState iMcState)
        {
            var mcState = (eMcState)iMcState;
            SM.McState = mcState;

            switch (mcState)
            {
                case eMcState.MC_BEGIN:
                    //InvokeHelper.Enable(btn_SystemInit, true);
                    InvokeHelper.Text(lbl_MachineState, "Non Initialized");
                    InvokeHelper.BackColor(lbl_MachineState, Color.Yellow);
                    InvokeHelper.ForeColor(lbl_MachineState, Color.Black);
                    break;
                case eMcState.MC_IDLE:
                    InvokeHelper.Enable(btn_Start, true);
                    InvokeHelper.Text(lbl_MachineState, "Idle");
                    InvokeHelper.BackColor(lbl_MachineState, Color.Blue);
                    InvokeHelper.ForeColor(lbl_MachineState, Color.Black);
                    break;
                case eMcState.MC_INITIALIZING:
                    InvokeHelper.Enable(btn_Start, false);
                    InvokeHelper.Enable(btn_Stop, true);
                    InvokeHelper.Text(lbl_MachineState, "Initializing");
                    InvokeHelper.BackColor(lbl_MachineState, Color.Yellow);
                    InvokeHelper.ForeColor(lbl_MachineState, Color.White);
                    break;
                case eMcState.MC_INIT_ERR:
                    InvokeHelper.Enable(btn_Start, true);
                    InvokeHelper.Enable(btn_Stop, false);
                    InvokeHelper.Text(lbl_MachineState, "Init Error");
                    InvokeHelper.BackColor(lbl_MachineState, Color.Yellow);
                    InvokeHelper.ForeColor(lbl_MachineState, Color.White);
                    break;
                case eMcState.MC_RUNNING:
                    InvokeHelper.Enable(btn_Start, false);
                    InvokeHelper.Enable(btn_Stop, true);
                    InvokeHelper.Text(lbl_MachineState, "Running");
                    InvokeHelper.BackColor(lbl_MachineState, Color.LimeGreen);
                    InvokeHelper.ForeColor(lbl_MachineState, Color.White);
                    break;
                case eMcState.MC_STOP:
                    InvokeHelper.Enable(btn_Start, true);
                    InvokeHelper.Text(lbl_MachineState, "Stop");
                    InvokeHelper.BackColor(lbl_MachineState, Color.Red);
                    InvokeHelper.ForeColor(lbl_MachineState, Color.White);
                    break;
                case eMcState.MC_STOP_INIT:
                    InvokeHelper.Enable(btn_Start, true);
                    InvokeHelper.Text(lbl_MachineState, "Stop");
                    InvokeHelper.BackColor(lbl_MachineState, Color.Red);
                    InvokeHelper.ForeColor(lbl_MachineState, Color.White);
                    break;
                case eMcState.MC_INITIALIZED:
                    InvokeHelper.Enable(btn_Start, true);
                    InvokeHelper.Text(lbl_MachineState, "Init Done");
                    if (EndLot)
                    {
                        TaskLotInfo.LotInfo.Activated = false;
                        TaskLotInfo.SaveLotInfo(false);
                        //PromptMessageOk("Lot Finished!");
                        EndLot = false;
                    }
                    InvokeHelper.Enable(gbLotInfo, true);
                    //InvokeHelper.Text(txtLotNo, string.Empty);
                    InvokeHelper.Text(txtDeviceID, string.Empty);
                    InvokeHelper.Focus(txtDeviceID, true);
                    InvokeHelper.BackColor(lbl_MachineState, Color.Yellow);
                    InvokeHelper.ForeColor(lbl_MachineState, Color.Black);
                    break;
                case eMcState.MC_RUN_SOFTJAM:
                    //EnableButton(true);
                    //TaskIO.SetAlarm();
                    //InvokeHelper.Enable(btn_Stop, false);
                    //InvokeHelper.Text(lbl_MachineState, "Stop");
                    //InvokeHelper.BackColor(lbl_MachineState, Color.Red);
                    //InvokeHelper.ForeColor(lbl_MachineState, Color.Black);
                    break;
                case eMcState.MC_RUN_HARDJAM:
                    SM.McAssistCount++;
                    InvokeHelper.Enable(btn_Start, true);
                    InvokeHelper.Enable(btn_Stop, false);
                    InvokeHelper.Text(lbl_MachineState, "Stop");
                    InvokeHelper.BackColor(lbl_MachineState, Color.Red);
                    InvokeHelper.ForeColor(lbl_MachineState, Color.White);
                    break;
                case eMcState.MC_OPT_REQ:
                    SM.McAssistCount++;
                    InvokeHelper.Enable(btn_Start, true);
                    InvokeHelper.Text(lbl_MachineState, "Stop");
                    InvokeHelper.BackColor(lbl_MachineState, Color.Yellow);
                    InvokeHelper.ForeColor(lbl_MachineState, Color.Black);
                    break;
            }

            if (mcState == eMcState.MC_STOP || mcState == eMcState.MC_STOP_INIT || mcState == eMcState.MC_IDLE || mcState == eMcState.MC_INITIALIZED || mcState == eMcState.MC_BEGIN)
            {
                //InvokeHelper.Enable(btn_NewLot, !SM.IsLotOpened);
                //InvokeHelper.Enable(btn_EndLot, SM.IsLotOpened);
            }
            else
            {
                //InvokeHelper.Enable(btn_NewLot, false);
                //InvokeHelper.Enable(btn_EndLot, false);
            }
        }
        private void OnSeqTriggerMachine(object sender, EventArgs e)
        {
            var eventtype = e as MessageEventArg;
            switch (eventtype.MachineStatus)
            {
                case eMcState.MC_INITIALIZED:
                    {
                        Initialized = true;
                        //PromptMessage(eventtype);
                        //var axis = frmMain.AdvantechModule.GetAxisList(0);
                        SetMcState(eMcState.MC_INITIALIZED);
                    }
                    break;
                case eMcState.MC_INIT_ERR:
                    {
                        PromptMessage(eventtype);
                        frmMain.MainEvent.UITriggerEvent(EV_TYPE.MCStopReq);
                        SetMcState(eMcState.MC_INIT_ERR);
                    }
                    break;
                case eMcState.MC_RUN_SOFTJAM:
                    {
                        PromptMessage(eventtype);
                        frmMain.MainEvent.UITriggerEvent(EV_TYPE.MCStopReq);
                        SetMcState(eMcState.MC_RUN_SOFTJAM);
                    }
                    break;
                case eMcState.MC_RUN_HARDJAM:
                    {
                        //Initialized = false;
                        PromptMessage(eventtype);
                        frmMain.MainEvent.UITriggerEvent(EV_TYPE.MCStopReq);
                        SetMcState(eMcState.MC_RUN_HARDJAM);
                    }
                    break;
                case eMcState.MC_OPT_REQ:
                    {
                        PromptMessageOkRetry(eventtype);
                        frmMain.MainEvent.UITriggerEvent(EV_TYPE.MCStopReq);
                        SetMcState(eMcState.MC_OPT_REQ);
                        //frmMain.MainEvent.UITriggerEvent(EV_TYPE.RetryReq, eventtype);
                    }
                    break;
                case eMcState.MC_WARNING:
                    {
                        PromptMessage(eventtype);
                        SetMcState(eMcState.MC_WARNING);
                    }
                    break;


                case eMcState.MC_RUNNING: break; //Add log purpose
            }
            AddToLog($"{eventtype.StationName} = {eventtype.MachineStatus}, {eventtype.Message}");
        }
        #endregion

        private frmMessaging2 msgForm;
        public DialogResult PromptMessageOk(string strmsg)
        {
            string message = strmsg;
            InvokeHelper.Enable(this, false);
            AddToLog($"{strmsg}");
            msgForm = new frmMessaging2();
            msgForm.StartPosition = FormStartPosition.CenterParent;
            msgForm.ShowMsg(strmsg, frmMessaging2.TMsgBtn.smbOK | frmMessaging2.TMsgBtn.smbAlmClr);
            DialogResult dialogResult = msgForm.ShowDialog();
            InvokeHelper.Enable(this, true);
            AddToLog($"{strmsg} = {dialogResult}");
            return dialogResult;
        }

        public DialogResult PromptMessageOkCancel(string strmsg)
        {
            string message = strmsg;
            InvokeHelper.Enable(this, false);
            AddToLog($"{strmsg}");
            msgForm = new frmMessaging2();
            msgForm.StartPosition = FormStartPosition.CenterParent;
            msgForm.ShowMsg(strmsg, frmMessaging2.TMsgBtn.smbOK | frmMessaging2.TMsgBtn.smbCancel | frmMessaging2.TMsgBtn.smbAlmClr);
            DialogResult dialogResult = msgForm.ShowDialog();
            InvokeHelper.Enable(this, true);
            AddToLog($"{strmsg} = {dialogResult}");
            return dialogResult;
        }

        public void PromptMessageOkRetry(MessageEventArg strmsg)
        {

            InvokeHelper.Enable(this, false);
            AddToLog($"{strmsg}");
            string message = strmsg.Message;
            frmMessaging msgForm = new frmMessaging(strmsg);
            msgForm.StartPosition = FormStartPosition.CenterParent;
            msgForm.ShowMsg(strmsg.Message, frmMessaging.TMsgBtn.smbOK | frmMessaging.TMsgBtn.smbRetry | frmMessaging.TMsgBtn.smbAlmClr);
            //DialogResult dialogResult = msgForm.ShowDialog();
            InvokeHelper.Enable(this, true);
        }

        bool CheckLotInfoTextBox()
        {
            if (txtLotNo.Text == string.Empty)
            {
                msgForm = new frmMessaging2();
                msgForm.StartPosition = FormStartPosition.CenterParent;
                msgForm.ShowMsg("Lot Number Not Allow To Empty!", frmMessaging2.TMsgBtn.smbOK);
                DialogResult dialogResult = msgForm.ShowDialog();
                if (dialogResult == DialogResult.OK)
                {
                    return false;
                }
            }

            if (txtDeviceID.Text == string.Empty)
            {
                msgForm = new frmMessaging2();
                msgForm.StartPosition = FormStartPosition.CenterParent;
                msgForm.ShowMsg("Device ID Is Not Allow to Empty!", frmMessaging2.TMsgBtn.smbOK);
                DialogResult dialogResult = msgForm.ShowDialog();
                if (dialogResult == DialogResult.OK)
                {
                    return false;
                }
            }

            return true;
        }

        bool CheckDeviceRecipe(string text)
        {
            DirectoryInfo d = new DirectoryInfo(GDefine.DevicePath);
            if (!Directory.Exists(GDefine.DevicePath))
            {
                goto _Err;
            }
            FileInfo[] files = d.GetFiles("*" + GDefine.DeviceRecipeExt);
            foreach (FileInfo file in files)
            {
                if (text == file.Name.Replace(GDefine.DeviceRecipeExt, ""))
                {
                    return true;
                }
            }
        _Err:
            //msgForm = new frmMessaging2();
            //msgForm.StartPosition = FormStartPosition.CenterParent;
            //msgForm.ShowMsg("Device Recipe Not Found!", frmMessaging2.TMsgBtn.smbOK);
            //DialogResult dialogResult = msgForm.ShowDialog();
            return false; ;
        }

        bool CheckDeviceCounter()
        {
            if (TaskDeviceRecipe._LotInfo._RecipeInfo.Counter >= GDefine._iMaxCounter)
            {
                msgForm = new frmMessaging2();
                msgForm.StartPosition = FormStartPosition.CenterParent;
                msgForm.ShowMsg("Product " + txtDeviceID.Text + " Is Ran Out Of Today Limit Count " + GDefine._iMaxCounter + "!", frmMessaging2.TMsgBtn.smbOK);
                DialogResult dialogResult = msgForm.ShowDialog();
                if (dialogResult == DialogResult.OK)
                {
                    return false;
                }
                goto _Err;
            }
            TaskDeviceRecipe._LotInfo._RecipeInfo.Counter++;
            return true;
        _Err:
            return false;
        }

        public void StartMcCtrl()
        {
            switch (GetMcState())
            {
                case eMcState.MC_INITIALIZING: break;
                case eMcState.MC_RUNNING: break;
                case eMcState.MC_INIT_ERR:
                    {
                        AddToLog($"Start Click");
                        SetMcState(eMcState.MC_INITIALIZING);
                        frmMain.MainEvent.UITriggerEvent(EV_TYPE.MCResumeSeq);
                    }
                    break;
                case eMcState.MC_RUN_SOFTJAM:
                    {
                        AddToLog($"Start Click");
                        SetMcState(eMcState.MC_RUNNING);
                        frmMain.MainEvent.UITriggerEvent(EV_TYPE.MCResumeSeq);
                    }
                    break;
                case eMcState.MC_RUN_HARDJAM:
                    {
                        AddToLog($"Start Click");
                        SetMcState(eMcState.MC_RUNNING);
                        frmMain.MainEvent.UITriggerEvent(EV_TYPE.MCResumeSeq);
                    }
                    break;
                case eMcState.MC_INITIALIZED:
                    {
                        //if (!StartLot())
                        //{
                        //    return;
                        //}
                        InvokeHelper.Enable(gbLotInfo, false);
                        AddToLog($"Start Click");
                        SetMcState(eMcState.MC_RUNNING);
                        frmMain.MainEvent.UITriggerEvent(EV_TYPE.BeginSeq);
                    }
                    break;
                case eMcState.MC_STOP:
                    {
                        AddToLog($"Start Click");
                        SetMcState(eMcState.MC_RUNNING);
                        frmMain.MainEvent.UITriggerEvent(EV_TYPE.MCResumeSeq);
                    }
                    break;
                case eMcState.MC_OPT_REQ:
                    {
                        AddToLog($"Start Click");
                        SetMcState(eMcState.MC_RUNNING);
                        frmMain.MainEvent.UITriggerEvent(EV_TYPE.MCResumeSeq);
                    }
                    break;

            }
        }

        public void StoptMcCtrl()
        {
            switch (GetMcState())
            {
                case eMcState.MC_INITIALIZING:
                    {
                        SetMcState(eMcState.MC_STOP);
                        AddToLog($"Stop Click");
                    }
                    break;
                case eMcState.MC_WARNING:
                    {
                        SetMcState(eMcState.MC_STOP);
                        AddToLog($"Stop Click");
                    }
                    break;
                case eMcState.MC_INITIALIZED:
                    {
                        //SetMcState(eMcState.MC_STOP);
                        //AddToLog($"Stop Click");
                    }
                    break;
                case eMcState.MC_INIT_ERR:
                    {
                        SetMcState(eMcState.MC_STOP);
                        AddToLog($"Stop Click");
                    }
                    break;
                case eMcState.MC_RUN_SOFTJAM:
                    {
                        SetMcState(eMcState.MC_STOP);
                        AddToLog($"Stop Click");
                    }
                    break;
                case eMcState.MC_RUN_HARDJAM:
                    {
                        SetMcState(eMcState.MC_STOP);
                        AddToLog($"Stop Click");
                    }
                    break;
                case eMcState.MC_STOP:
                    {
                        SetMcState(eMcState.MC_STOP);
                        AddToLog($"Stop Click");
                    }
                    break;
                case eMcState.MC_RUNNING:
                    {
                        SetMcState(eMcState.MC_STOP);
                        AddToLog($"Stop Click");
                    }
                    break;
            }
        }

        public bool StartLot()
        {
            //if (!CheckLotInfoTextBox())
            //{
            //    return false;
            //}

            //if (!CheckDeviceRecipe())
            //{
            //    return false;
            //}

            TaskDeviceRecipe.LoadDeviceRecipe(GDefine.DevicePath, txtDeviceID.Text + GDefine.DeviceRecipeExt);

            if (!CheckDeviceCounter())
            {
                return false;
            }

            TaskDeviceRecipe.SaveDeviceRecipe(GDefine.DevicePath, txtDeviceID.Text);

            GDefine.SaveDefaultFile();

            string D = DateTime.Now.Date.ToString("dd-MM-yyyy");
            string T = DateTime.Now.ToString("HH:mm:ss tt");

            //TaskLotInfo.LotInfo.LotNum = txtLotNo.Text;
            TaskLotInfo.LotInfo.PartNum = txtDeviceID.Text;
            TaskLotInfo.LotInfo.DateIn = D;
            TaskLotInfo.LotInfo.TimeIn = T;

            TaskLotInfo.LotInfo.Activated = true;

            return true;
        }

        public bool StartLot2()
        {
            DateTime dateTime = DateTime.Now;
            string year = dateTime.Date.ToString("yyyy");
            string month = dateTime.Date.ToString("MM");
            string D = dateTime.Date.ToString("dd-MM-yyyy");
            string T = dateTime.ToString("HH:mm:ss tt");

            if (!TaskLotInfo.CheckLotCounter(txtDeviceID.Text, dateTime))
            {
                msgForm = new frmMessaging2();
                msgForm.StartPosition = FormStartPosition.CenterParent;
                msgForm.ShowMsg("Lot " + txtDeviceID.Text + " Is Ran Out Of Today Limit Count " + GDefine._iMaxCounter + "!", frmMessaging2.TMsgBtn.smbOK);
                DialogResult dialogResult = msgForm.ShowDialog();
                if (dialogResult == DialogResult.OK)
                {
                    goto _Abort;
                }
            }

            TaskLotInfo.LotInfo.LotNum = txtDeviceID.Text;
            TaskLotInfo.LotInfo.PartNum = txtDeviceID.Text.Substring(0, 3);
            TaskLotInfo.LotInfo.DateIn = D;
            TaskLotInfo.LotInfo.TimeIn = T;

            TaskLotInfo.LotInfo.Activated = true;

            return true;
        _Abort:
            return false;
        }

        public void ResetMcCtrl()
        {
            AddToLog($"Reset Click");
        }

        private void tmrUpdateDisplay_Tick(object sender, EventArgs e)
        {
            if (!Visible) return;
            lbl_Mode.Text = TaskIO.ReadBit_AutoMode() ? "Auto Mode" : "Manual Mode";
            lbl_Mode.BackColor = TaskIO.ReadBit_AutoMode() ? Color.Yellow : Color.Blue;
            //lbl_Mode.Text = "Auto Mode";
            //lbl_Mode.BackColor = Color.Blue;
            ShowRunningNumbers();
        }

        private void tmrSignal_Tick(object sender, EventArgs e)
        {
            if (!Visible) return;
            tmrSignal.Stop();

            if (TaskLotInfo.LotInfo.Activated)
#if !SIMULATION
            if (TaskIO.ReadBit_StartTimer())
#endif
            {
                MessageEventArg msg = new MessageEventArg();
                msg.StationName = "GeneralControl";
                frmMain.MainEvent.UITriggerEvent(EV_TYPE.WorkReq, msg);
            }

            if (TaskLotInfo.LotInfo.Activated)
#if !SIMULATION
            if (TaskIO.ReadBit_EndLot())
#endif
            {
                MessageEventArg msg = new MessageEventArg();
                msg.StationName = "GeneralControl";
                frmMain.MainEvent.UITriggerEvent(EV_TYPE.FarProcComp, msg);
                EndLot = true;
            }

        _End:
            tmrSignal.Start();
        }

        private void tmrUPH_Tick(object sender, EventArgs e)
        {
            lock (WriteLock)
            {
                #region Machine Info
                SM.McRunTime++;

                lbl_McRunTime.Text = TickConverter.Convert2DHMS(SM.McRunTime, 1);

                if (SM.McState == eMcState.MC_RUNNING/* || SM.McState == eMcState.MC_INITIALIZING*/) SM.McOpTime++;

                lbl_McOPTime.Text = TickConverter.Convert2DHMS(SM.McOpTime, 1);

                if (SM.McState != eMcState.MC_RUNNING && SM.McState != eMcState.MC_INITIALIZING) SM.McIdleTime++;

                lbl_McIdleTime.Text = TickConverter.Convert2DHMS(SM.McIdleTime, 1);

                #endregion
            }
        }

        private void btn_Start_Click(object sender, EventArgs e)
        {
            Thread.Sleep(100);
            StartMcCtrl();
        }

        private void btn_Stop_Click(object sender, EventArgs e)
        {
            Thread.Sleep(100);
            frmMain.MainEvent.UITriggerEvent(EV_TYPE.MCStopReq);
            StoptMcCtrl();
        }

        private void txtDeviceID_TextChanged(object sender, EventArgs e)
        {
            //InvokeHelper.Visible(lblProdNotFound, false);

            //if (!CheckDeviceRecipe(txtDeviceID.Text))
            //{
            //    bool visible = txtDeviceID.Text != string.Empty ? true : false;
            //    InvokeHelper.Visible(lblProdNotFound, visible);
            //    return;
            //}

            //if (!StartLot())
            //{
            //    return;
            //}

            //StartMcCtrl();
        }

        private bool CheckValidLotNumberLength(string LotNumber)
        {
            if (LotNumber.Length < 8)
            {
                return false;
                //msgForm = new frmMessaging2();
                //msgForm.StartPosition = FormStartPosition.CenterParent;
                //msgForm.ShowMsg("Invalid Lot Number " + txtDeviceID.Text + "! (Length < 8)", frmMessaging2.TMsgBtn.smbOK);
                //DialogResult dialogResult = msgForm.ShowDialog();
                //if (dialogResult == DialogResult.OK)
                //{
                //    return false;
                //}
            }

            return true;
        }

        private void txtDeviceID_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) { return; }

            string deviceID = string.Empty;
            InvokeHelper.Visible(lblInvalid, false);
            if (!CheckValidLotNumberLength(txtDeviceID.Text))
            {
                InvokeHelper.Text(lblInvalid, "** Invalid Lot Number Length! **");
                InvokeHelper.Visible(lblInvalid, true);
                return;
            }

            deviceID = txtDeviceID.Text.Substring(0, 3);

            if (!CheckDeviceRecipe(deviceID))
            {
                InvokeHelper.Text(lblInvalid, "** Product Not Found! **");
                InvokeHelper.Visible(lblInvalid, true);
                return;
            }

            TaskDeviceRecipe.LoadDeviceRecipe(GDefine.DevicePath, deviceID + GDefine.DeviceRecipeExt);
            TaskDeviceRecipe.SaveDeviceRecipe();
            GDefine.SaveDefaultFile();

            if (!StartLot2())
            {
                return;
            }

            StartMcCtrl();
        }
    }
}
