using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Updater.Core
{

    public static class Engine
    {
        public static void WriteLine(string message, ConsoleColor color = ConsoleColor.Gray, params string[] args)
        {
            Engine<ISource>.WriteLine(message, color, args);
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
