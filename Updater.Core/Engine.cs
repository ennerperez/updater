using Platform.Support;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Remoting;
using System.Text;
using System.Threading.Tasks;

namespace Updater.Core
{

    public static class Engine
    {
        public static void WriteLine(string message, ConsoleColor color = ConsoleColor.Gray, params string[] args)
        {
            Engine<IEngine>.WriteLine(message, color, args);
        }
        public static bool Exists(string name)
        {
            var plugins = Plugins();
            var pluginFile = plugins.FirstOrDefault(p => p.Name.ToLower().Contains(name));
            return pluginFile != null;
        }
        public static IEngine Create(string name)
        {
            IEngine result = null;

            var assembly = Assembly.GetCallingAssembly();
            var dir = new FileInfo(assembly.Location).Directory;
            var plugins = dir.GetFiles("Updater.*.dll");

            var pluginFile = plugins.FirstOrDefault(p => p.Name.ToLower().Contains(name));

            if (pluginFile != null)
            {
                try
                {
                    var plugin = Assembly.LoadFile(pluginFile.FullName);
                    name = plugin.GetName().Name.Split('.').Last();
                    ObjectHandle handle = Activator.CreateInstance(plugin.FullName, string.Format("{0}.Core.{1}", assembly.GetName().Name, name));
                    result = (IEngine)handle.Unwrap();
                }
                catch (Exception ex)
                {
                    ex.DebugThis();
                }
            }

            return result;

        }

        public static IEnumerable<FileInfo> Plugins()
        {
            var assembly = Assembly.GetCallingAssembly();
            var dir = new FileInfo(assembly.Location).Directory;
            var plugins = dir.GetFiles("Updater.*.dll").Where(p => p.Name != "Updater.Core.dll");
            return plugins;
        }

        public static void Help(string source)
        {
            string[] lines = source.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in lines)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;

                if (item.StartsWith("#"))
                    Console.ForegroundColor = ConsoleColor.Gray;

                var result = item.Replace("# ", "").Replace("#", "").Replace("- ", "");
                Console.WriteLine(result);

                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }

    }

    public sealed class Engine<TSource> where TSource : IEngine
    {

        #region Multi-threaded Singleton

        private static volatile Engine<TSource> instance;
        private static object syncRoot = new Object();

        private Engine() { }

        public static Engine<TSource> Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new Engine<TSource>();
                    }
                }

                return instance;
            }
        }

        #endregion

        private static TSource source;
        public static TSource Source
        {
            get
            {
                if (source == null)
                    source = Activator.CreateInstance<TSource>();
                return source;
            }
        }

        public static void WriteLine(string message, ConsoleColor color = ConsoleColor.Gray, params string[] args)
        {
            Console.ForegroundColor = color;

            var name = Assembly.GetEntryAssembly().GetName().Name.ToLower();
            if (typeof(TSource) != typeof(IEngine))
                name = typeof(TSource).Name.ToLower();

            Console.WriteLine("[{0}] {1}", name, string.Format(message, args));
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public static async Task<string> Read(string url)
        {

            string responseFromServer = string.Empty;
            try
            {
                Engine<TSource>.WriteLine("Connecting...");

                var request = WebRequest.Create(url) as HttpWebRequest;
                request.ContentType = "application/json; charset=utf-8";
                request.UserAgent = "Other";
                request.Method = "GET";

                var response = await request.GetResponseAsync() as HttpWebResponse;
                var data = response.GetResponseStream();
                var reader = new StreamReader(data);

                Engine<TSource>.WriteLine("Gathering data...");

                responseFromServer = reader.ReadToEnd();
                reader.Close();
                response.Close();

            }
            catch (Exception ex)
            {
                Engine<TSource>.WriteLine("ERROR -> " + ex.Message, ConsoleColor.Red);
            }

            return responseFromServer;
        }

        public static async Task Download(string fileName, string remoteUri, double size = 0)
        {
            try
            {

                var cache = fileName + Flags.CacheExt;
                var fileinfo = new FileInfo(cache);

                if (!fileinfo.Directory.Exists)
                    Directory.CreateDirectory(fileinfo.Directory.FullName);

                if (File.Exists(cache))
                    File.Delete(cache);

                if (!File.Exists(cache))
                {

                    WebClient client = new WebClient();
                    Engine<TSource>.WriteLine(string.Format("Downloading \"{0}\"...", remoteUri));

                    client.DownloadFileCompleted += (sender, e) =>
                    {
                        if (e.Cancelled)
                            Engine<TSource>.WriteLine("Download was canceled.", ConsoleColor.Yellow);

                        if (e.Error != null)
                            Engine<TSource>.WriteLine("ERROR -> " + e.Error.Message, ConsoleColor.Red);
                        else
                        {

                            if (File.Exists(fileName))
                                File.Delete(fileName);

                            File.Move(cache, fileName);
                            Engine<TSource>.WriteLine("Successfully downloaded file.", ConsoleColor.Green);
                        }
                    };

                    await client.DownloadFileTaskAsync(remoteUri, cache);

                }

            }
            catch (Exception ex)
            {
                Engine<TSource>.WriteLine("ERROR -> " + ex.Message, ConsoleColor.Red);
            }

        }

        private static string appCache = "";
        public static string AppCache
        {
            get
            {
                if (string.IsNullOrEmpty(appCache))
                {
                    var appLocalData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    var appName = Assembly.GetEntryAssembly().GetName().Name;
                    appCache = Path.Combine(appLocalData, appName, "Cache");
                }

                return appCache;
            }
        }

    }
}
