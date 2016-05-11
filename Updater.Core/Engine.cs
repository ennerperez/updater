using Platform.Support;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Text;

namespace Updater.Core
{

    public static class Engine
    {
        public static void WriteLine(string message, ConsoleColor color = ConsoleColor.Gray, params string[] args)
        {
            Engine<ISource>.WriteLine(message, color, args);
        }

        public static bool Exists(string name)
        {
            var assembly = Assembly.GetCallingAssembly();
            var dir = new FileInfo(assembly.Location).Directory;
            var plugins = dir.GetFiles("Updater.*.dll");

            var pluginFile = plugins.FirstOrDefault(p => p.Name.ToLower().Contains(name));

            return pluginFile != null;
        }

        public static ISource Create(string name)
        {
            ISource result = null;

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
                    result = (ISource)handle.Unwrap();
                }
                catch (Exception ex)
                {
                    ex.DebugThis();
                }
            }

            return result;

        }

    }

    public sealed class Engine<TSource> where TSource : ISource
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

            var name = System.Reflection.Assembly.GetEntryAssembly().GetName().Name.ToLower();
            if (typeof(TSource) != typeof(ISource))
                name = typeof(TSource).Name.ToLower();

            Console.WriteLine("[{0}] {1}", name, string.Format(message, args));
            Console.ForegroundColor = ConsoleColor.Gray;
        }

    }
}
