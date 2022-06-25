using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unosquare.RaspberryIO;
using Swan;
using System.Diagnostics;

namespace Jovian
{
    public static class ShellCommands
    {
        public static async Task<string> Execute(string command)
        {
            ProcessStartInfo inf = new()
            {
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                FileName = "/bin/bash",
                UseShellExecute = true,
                Arguments = command,
            };
            try
            {
                if (Process.Start(inf) is Process cmd)
                {
                    //await cmd.StandardInput.WriteAsync(command);
                    //await Task.Delay(1000);
                    string ret = "Output: " + await cmd.StandardOutput.ReadToEndAsync();
                    ret += "\nError Output: " + await cmd.StandardError.ReadToEndAsync();
                    await Program.Log(ret);
                    return ret;
                }
            }catch (Exception ex)
            {
                await Program.Log(ex.Message);
            }
            return "Can't open " + inf.FileName;
        }
    }
}
