using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Updater.Core;
using Updater.Core.Engines;
using Platform.Support;
using Platform.Support.Reflection;

namespace Updater
{
    class Program
    {

        private static List<string> _args { get; set; }

        static void About()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var text = string.Format("{0} [{1}]", assembly.Description(), assembly.FileVersion().ToString());

            Console.WriteLine(text);
            Console.WriteLine(assembly.Copyright());
            Console.WriteLine();
        }

        static void Help()
        {
                      
            string[] lines = Properties.Resources.Help.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in lines)
            {
                Console.WriteLine(item.Replace("#","").Trim());
            }

            Console.ReadKey();

            Environment.Exit(0);
        }

        static void ProcessFlags()
        {

            if (_args.Contains("/h"))
                Help();

            Core.Flags.Force = _args.Contains("/f");
            Core.Flags.Log = _args.Contains("/l");
            if (Core.Flags.Log)
            {
                string file = string.Empty;
                try
                {
                    file = _args[_args.IndexOf("/l") + 1];
                    if (file.StartsWith("/"))
                        file = string.Empty;
                }
                catch { }

                if (!string.IsNullOrEmpty(file))
                {
                    var regex = new Regex(@"^[\w\-. ]+$");
                    if (!regex.IsMatch(file))
                        throw new Exception("Invalid log file name.");
                }

                Core.Flags.LogFile = file;

            }


            if (_args.Contains("/e"))
                Core.Flags.Engine = _args[_args.IndexOf("/e") + 1];

            if (_args.Contains("/x"))
            {
                var ext = _args[_args.IndexOf("/x") + 1];
                if (!ext.StartsWith(".")) ext = "." + ext;
                var regex = new Regex(@"\.([A-Za-z0-9]+)$");
                if (!regex.IsMatch(ext))
                    throw new Exception("Invalid cache extension.");

                Core.Flags.CacheExt = ext;

            }

            Core.Flags.Validate();
        }

        static void Main(string[] args)
        {

            About();

            try
            {
                _args = new List<string>(args);
                ProcessFlags();

                switch (Core.Flags.Engine.ToLower())
                {
                    case GitHub.Name:
                        if (_args.Contains("/r"))
                        {
                            Engine<GitHub>.Source.Repository = _args[_args.IndexOf("/r") + 1];
                            var task = Engine<GitHub>.Source.DownloadAsync(null);
                            task.Wait();
                        }
                        break;
                    default:
                        break;
                }

            }
            catch (Exception ex)
            {

                if (ex.GetType() == typeof(ArgumentOutOfRangeException))
                    ex = new InvalidProgramException("Invalid number of arguments.");

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);

                if (Core.Flags.Log)
                    File.AppendAllText(Flags.LogFile, ex.Message);

            }

#if DEBUG
            Console.ReadKey();
#endif

        }
    }
}
