﻿/*
   Copyright 2011 repetier repetierdev@googlemail.com

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using RepetierHost.model;
using Microsoft.Win32;

namespace RepetierHost.view
{
    public partial class LogView : UserControl
    {
        public Color errorColor = Color.FromArgb(238, 198, 198);
        public Color warningColor = Color.FromArgb(223,175,83);
        public Color infoColor = Color.FromArgb(140, 179, 251);
        RegistryKey key;
        public LogView()
        {
            InitializeComponent();
            key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Repetier");
            RegToForm();
        }
        public void FormToReg()
        {
            key.SetValue("logSend", toolSend.Checked ? 1 : 0);
            key.SetValue("logErrors", toolErrors.Checked ? 1 : 0);
            key.SetValue("logWarning", toolWarning.Checked ? 1 : 0);
            key.SetValue("logACK", toolACK.Checked ? 1 : 0);
            key.SetValue("logAutoscroll", toolAutoscroll.Checked ? 1 : 0);

        }
        public void RegToForm()
        {
            toolSend.Checked = 1==(int) key.GetValue("logSend", toolSend.Checked ? 1 : 0);
            toolErrors.Checked = 1 == (int)key.GetValue("logErrors", toolErrors.Checked ? 1 : 0);
            toolWarning.Checked = 1 == (int)key.GetValue("logWarning", toolWarning.Checked ? 1 : 0);
            toolACK.Checked = 1 == (int)key.GetValue("logACK", toolACK.Checked ? 1 : 0);
            toolAutoscroll.Checked = 1 == (int)key.GetValue("logAutoscroll", toolAutoscroll.Checked ? 1 : 0);
        }
        private void filter()
        {
            listLog.Clear();
            lock (Main.conn.logList)
            {
                foreach (LogLine l in Main.conn.logList)
                {
                    logAppend(l);
                }
            }
            if (toolAutoscroll.Checked)
            {
                listLog.ScrollBottom();
            }
            listLog.UpdateBox();
        }
        private void logUpdate(LogLine line)
        {
            logAppend(line);
            listLog.UpdateBox();
        }
        private bool isAck(string t)
        {
            if (t.StartsWith("ok") || t.StartsWith("wait")) return true;
            if (t.IndexOf("SD printing byte")!=-1) return true;
            if (t.IndexOf("Not SD printing")!=-1) return true;
            if (t.IndexOf("SpeedMultiply:")!=-1) return true;
            if (t.IndexOf("TargetExtr0:")!=-1) return true;
            if (t.IndexOf("TargetExtr1:")!=-1) return true;
            if (t.IndexOf("TargetBed:")!=-1) return true;
            if (t.IndexOf("Fanspeed:")!=-1) return true;
            return false;
        }
        private void UpdateNewEntries(object sender, EventArgs e)
        {
            LinkedList<LogLine> nl = null;
            lock (Main.conn.newLogs)
            {
                LinkedList<LogLine> l = Main.conn.newLogs;
                if (l.Count == 0) return;
                nl = new LinkedList<LogLine>(l);
                l.Clear();
            }
            while (nl.Count > 0)
            {
                LogLine line = nl.First.Value;
                if (toolACK.Checked == false && isAck(line.text)) nl.RemoveFirst();
                else if (line.level == 0 && line.response==false && toolSend.Checked == false) nl.RemoveFirst();
                else if (line.level == 1 && toolWarning.Checked == false) nl.RemoveFirst();
                else if (line.level == 2 && toolErrors.Checked == false) nl.RemoveFirst();
                else if (line.level == 3 && toolInfo.Checked == false) nl.RemoveFirst();
                else break;
            }
            if (nl.Count == 0) return;
            foreach (LogLine line in nl)
                logAppend(line);
            listLog.UpdateBox();
        }
        private void logAppend(LogLine line)
        {
            if (toolACK.Checked == false && isAck(line.text)) return;
            if (line.level == 0 && line.response==false && toolSend.Checked == false) return;
            if (line.level == 1 && toolWarning.Checked == false) return;
            if (line.level == 2 && toolErrors.Checked == false) return;
            if (line.level == 3 && toolInfo.Checked == false) return;
            listLog.Add(line);
        }

        private void listLog_Resize(object sender, EventArgs e)
        {
        }

        private void toolSend_CheckStateChanged(object sender, EventArgs e)
        {
            filter();
            FormToReg();
        }

        private void toolInfo_CheckStateChanged(object sender, EventArgs e)
        {
            filter();
            FormToReg();
        }

        private void toolWarning_CheckStateChanged(object sender, EventArgs e)
        {
            filter();
            FormToReg();
        }

        private void toolErrors_CheckStateChanged(object sender, EventArgs e)
        {
            filter();
            FormToReg();
        }

        private void toolClear_Click(object sender, EventArgs e)
        {
            Main.conn.clearLog();
        }

        private void toolACK_Click(object sender, EventArgs e)
        {
            filter();
            FormToReg();
        }

        private void toolCopy_Click(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            string sel = listLog.getSelection();
            if(sel.Length>0)
                Clipboard.SetText(sel);
        }

        private void LogView_Load(object sender, EventArgs e)
        {
            Main.conn.eventLogCleared += filter;
            Main.conn.eventLogUpdate += logUpdate;
            Application.Idle += new EventHandler(UpdateNewEntries);
        }

        private void toolAutoscroll_CheckedChanged(object sender, EventArgs e)
        {
            listLog.Autoscroll = toolAutoscroll.Checked;
        }
    }
}
