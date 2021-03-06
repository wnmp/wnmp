﻿/*
 * Copyright (c) 2012 - 2021, Kurt Cancemi (kurt@x64architecture.com)
 *
 * This file is part of Wnmp.
 *
 *  Wnmp is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Wnmp is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with Wnmp.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Wnmp.Programs
{
    public class WnmpProgram
    {
        public string ExeFileName { get; set; }            // Location of the executable file
        public Log.LogSection ProgLogSection { get; set; } // LogSection of the program
        public string StartArgs { get; set; }              // Start Arguments
        public string StopArgs { get; set; }               // Stop Arguments
        public string ConfDir { get; set; }                // Directory where all the programs configuration files are
        public string LogDir { get; set; }                 // Directory where all the programs log files are
        public string WorkingDir { get; set; }             // Working directory of the program

        private readonly string processName;

        public WnmpProgram(string exeFile)
        {
            ExeFileName = exeFile;
            processName = Path.GetFileNameWithoutExtension(ExeFileName);
        }

        public static void StartProcess(
            string exe,
            string args,
            string workingDir = null,
            bool waitforexit = false,
            Dictionary<string, string> envvariables = null)
        {
            using Process process = new();
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            if (workingDir == null)
            {
                workingDir = Program.StartupPath;
            }
            process.StartInfo.WorkingDirectory = workingDir;
            process.StartInfo.FileName = exe;
            process.StartInfo.Arguments = args;
            if (envvariables != null)
            {
                foreach (var v in envvariables)
                    process.StartInfo.EnvironmentVariables.Add(v.Key, v.Value);
            }
            process.Start();
            if (waitforexit)
                process.WaitForExit();
        }

        public static void StartProcessAsAdmin(
            string exe,
            string args,
            bool waitforexit = false)
        {
            using Process process = new();
            process.StartInfo.WorkingDirectory = Program.StartupPath;
            process.StartInfo.FileName = exe;
            process.StartInfo.Arguments = args;
            process.StartInfo.Verb = "runas";
            process.Start();
            if (waitforexit)
                process.WaitForExit();
        }

        public virtual void Start()
        {
            try
            {
                if (IsRunning())
                {
                    Log.Error(Language.Resource.ALREADY_RUNNING, ProgLogSection);
                    return;
                }
                StartProcess(ExeFileName, StartArgs, WorkingDir);
            }
            catch (Exception ex)
            {
                Log.Error($"Start(): {ex.Message}", ProgLogSection);
            }
            Log.Notice(Language.Resource.STARTED, ProgLogSection);
        }

        public virtual void Stop()
        {
            try
            {
                if (!IsRunning())
                {
                    Log.Error(Language.Resource.NOT_RUNNING, ProgLogSection);
                    return;
                }
                if (StopArgs != null)
                {
                    StartProcess(ExeFileName, StopArgs, WorkingDir, true);
                }
                Process[] procs = Process.GetProcessesByName(processName);
                for (int i = 0; i < procs.Length; i++)
                {
                    procs[i].Kill();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Stop(): {ex.Message}", ProgLogSection);
            }
            Log.Notice(Language.Resource.STOPPED, ProgLogSection);
        }

        public virtual void Restart()
        {

            Stop();
            Thread.Sleep(1000);
            Start();
            Log.Notice(Language.Resource.RESTARTED, ProgLogSection);
        }

        public virtual bool IsRunning()
        {
            return Process.GetProcessesByName(processName).Length != 0;
        }
    }
}
