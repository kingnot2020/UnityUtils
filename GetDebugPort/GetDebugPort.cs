using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;
using System;

public class GetDebugPort : MonoBehaviour
{

    /// <summary>
    /// 返回可用端口号
    /// </summary>
    /// <returns></returns>        
    [MenuItem("Tools/GetDebugPort")]
    public static void GetDebugPort1()
    {
        string cmd = string.Format("netstat -ano | findstr {0}", Process.GetCurrentProcess().Id);
        Process p = new Process();
        p.StartInfo = new ProcessStartInfo("cmd.exe");
        p.StartInfo.CreateNoWindow = true;
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.ErrorDialog = true;
        p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        p.StartInfo.RedirectStandardInput = true;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.RedirectStandardError = true;
        p.StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
        p.StartInfo.StandardErrorEncoding = System.Text.Encoding.UTF8;
        p.Start();
        p.StandardInput.WriteLine(cmd);
        p.StandardInput.WriteLine("Exit");

        List<int> ports = new List<int>();
        string line = null;
        Regex reg = new Regex("127.0.0.1:([0-9]*)\\s*0.0.0.0.0");
        Regex portReg = new Regex("\\s+");

        while ((line = p.StandardOutput.ReadLine()) != null) {
            line = line.Trim();
            if (!reg.IsMatch(line))
                continue;
            if (line.StartsWith("TCP", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("UDP", StringComparison.OrdinalIgnoreCase)) {
                line = portReg.Replace(line, ",");
                string[] arr = line.Split(',');
                string soc = arr[1];
                int pos = soc.LastIndexOf(':');
                int port = int.Parse(soc.Substring(pos + 1));
                ports.Add(port);
            }
        }
        p.Close();
        
        foreach (int i in ports) {
            UnityEngine.Debug.LogFormat("Unity Debug Port={0}", i);
        }
    }
}
