using System;
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
            if (args.Length >= 1)
            {
                string ret = "";
                var stdOutBuffer = new StringBuilder();
                var stdErrBuffer = new StringBuilder();
                Command resultCommand = Cli.Wrap(args[0]);
                if (args.Length > 1)
                {
                    resultCommand = resultCommand.WithArguments(args.Skip(1));
                }
                await resultCommand.WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                    .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                    .WithValidation(CommandResultValidation.None)
                    .ExecuteAsync();
                string output = stdOutBuffer.ToString();
                string error = stdErrBuffer.ToString();

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
            }
            else
            {
                return "Too few arguments (at least 1)";
            }
        }
    }
}
