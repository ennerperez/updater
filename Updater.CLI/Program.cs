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

        private static Dictionary<string, string> _args { get; set; }

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
                Console.ForegroundColor = ConsoleColor.DarkGray;

                if (item.StartsWith("#"))
                    Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(item.Replace("#", ""));

                Console.ForegroundColor = ConsoleColor.Gray;
            }

            Console.ReadKey();

            Environment.Exit(0);
        }

        static void ProcessFlags()
        {

            if (_args.ContainsKey("/?"))
                Help();

            Flags.Force = _args.ContainsKey("/f");
            Flags.Log = _args.ContainsKey("/l");
            if (Flags.Log)
            {
                string file = string.Empty;
                try
                {
                    file = _args["/l"];
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

                Flags.LogFile = file;

            }


            if (_args.ContainsKey("/e"))
                Flags.Engine = _args["/e"];

            if (_args.ContainsKey("/x"))
            {
                var ext = _args["/x"];
                if (!ext.StartsWith(".")) ext = "." + ext;
                var regex = new Regex(@"\.([A-Za-z0-9]+)$");
                if (!regex.IsMatch(ext))
                    throw new Exception("Invalid cache extension.");

                Flags.CacheExt = ext;

            }

            Flags.Validate();
        }

        static void Main(string[] args)
        {

            About();

            try
            {

                _args = new Dictionary<string, string>();
                foreach (var item in args)
                {
                    var values = item.Split(':');
                    var key = values[0].ToLower();
                    var value = "";
                    if (values.Length > 1)
                        value = values[1].Trim();

                    _args.Add(key, value);

                }

                ProcessFlags();

                Engine.WriteLine("Initializing engine...", ConsoleColor.Yellow);

                switch (Flags.Engine.ToLower())
                {
                    case GitHub.Name:
                        if (_args.ContainsKey("/r"))
                        {
                            Engine<GitHub>.Source.Repository = _args["/r"];
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

                Engine.WriteLine(ex.Message, ConsoleColor.Red);

                if (Flags.Log)
                    File.AppendAllText(Flags.LogFile, ex.Message);

            }

#if DEBUG
            Console.ReadKey();
#endif

        }
    }
}
