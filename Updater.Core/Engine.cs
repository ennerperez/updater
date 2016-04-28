using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Updater.Core
{
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

    }
}
