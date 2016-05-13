using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Updater.Core;
using Platform.Support;
using Platform.Support.Reflection;
using System.Threading.Tasks;
using System.Threading;

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
            Engine.Help(Properties.Resources.Help);

            if (!string.IsNullOrEmpty(Flags.Engine))
            {
                var engine = Engine.Create(Flags.Engine);
                if (engine == null)
                    throw new InvalidProgramException("Invalid source engine.");
                engine.Help();
            }
            else
                Plugins();

            Console.ReadKey();
            Environment.Exit(0);
        }

        static void Plugins()
        {

            Engine.WriteLine("Available plug-ins:", ConsoleColor.Yellow);
            foreach (var item in Engine.Plugins())
            {
                var plugin = Assembly.ReflectionOnlyLoadFrom(item.FullName);
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine(plugin.GetName().FullName);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            Console.ReadKey();
            Environment.Exit(0);
        }

        static void ProcessFlags()
        {

            if (_args.ContainsKey("/i"))
                Plugins();

            if (_args.ContainsKey("/e"))
                Flags.Engine = _args["/e"];

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

                    Flags.LogFile = file;
                }
            }

            if (_args.ContainsKey("/x"))
            {
                var ext = _args["/x"];
                if (!ext.StartsWith(".")) ext = "." + ext;
                var regex = new Regex(@"\.([A-Za-z0-9]+)$");
                if (!regex.IsMatch(ext))
                    throw new Exception("Invalid cache extension.");

                Flags.CacheExt = ext;

            }

            if (_args.ContainsKey("/t"))
                Flags.Target = _args["/t"];

            Flags.Validate();
        }

        static void Main(string[] args)
        {
            About();

            if (args == null || args.Count() == 0)
                Help();

            try
            {

                _args = new Dictionary<string, string>();
                foreach (var item in args)
                {
                    var values = item.Split(':');
                    var key = values[0].ToLower();
                    var value = "";
                    if (values.Length > 1)
                        value = item.Substring(key.Length + 1);

                    _args.Add(key, value);

                }

                ProcessFlags();

                Engine.WriteLine("Initializing engine...", ConsoleColor.Yellow);

                var engine = Engine.Create(Flags.Engine);
                if (engine != null)
                    engine.Initialize(_args);
                else
                    throw new InvalidProgramException("Invalid source engine.");

                // Steps : (1) Load -> (2) Update

                var loader = engine.Load();
                loader.Wait();

                var udate = engine.Update();
                udate.Wait();

            }
            catch (Exception ex)
            {

                if (ex.GetType() == typeof(ArgumentOutOfRangeException))
                    ex = new InvalidProgramException("Invalid number of arguments.");

                Engine.WriteLine(ex.Message, ConsoleColor.Red);

                if (Flags.Log)
                    File.AppendAllText(Flags.LogFile, ex.Message);

            }
            finally
            {
#if DEBUG
                Console.ReadKey();
#endif
            }
        }

    }
}
