using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Updater.Core
{
    public static class Flags
    {

        public static string Engine { get; set; }

        public static bool Force { get; set; } = false;

        public static bool Log { get; set; } = false;

        public static string LogFile { get; set; } = "updater.log";

        public static string CacheExt { get; set; } = ".tmp";

        public static void Validate()
        {
            if (string.IsNullOrEmpty(CacheExt) ||
                !CacheExt.StartsWith(".") ||
                CacheExt.Length < 1)
                throw new FormatException("Invalid cache extension.");

            if (!Core.Engine.Exists(Engine))
                throw new InvalidProgramException("Invalid source engine.");

        }

    }
}
