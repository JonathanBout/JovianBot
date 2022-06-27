﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unosquare.RaspberryIO;
using Swan;
using System.Diagnostics;
using CliWrap;

namespace Jovian
{
    public static class ShellCommands
    {
        public static async Task<string> Execute(string command)
        {
            string[] args = command.Parse();
            if (args.Length >= 2)
            {
                try
                {
                    var stdOutBuffer = new StringBuilder();
                    var stdErrBuffer = new StringBuilder();

                    var result = await Cli.Wrap(args[0]).WithArguments(args.Skip(1))
                        .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                        .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                        .ExecuteAsync();
                    string output = stdOutBuffer.ToString();
                    string error = stdErrBuffer.ToString();
                    string ret = "";
                    if (!string.IsNullOrEmpty(output))
                    {
                        ret += "Output: " + output + "\n";
                    }
                    if (!string.IsNullOrEmpty(error))
                    {
                        ret += "Error: " + error + "\n";
                    }
                    await Program.Log(ret);
                    return ret;
                }catch (Exception ex)
                {
                    return ex.Message;
                }
            }
            else
            {
                return "Too few arguments (at least 2)";
            }
        }
    }
}
