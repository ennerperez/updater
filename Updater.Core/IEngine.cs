using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Updater.Core
{
    public interface IEngine
    {

        void Help();

        /// <summary>
        /// Engine name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// An collection of releases from server
        /// </summary>
        IEnumerable<IRelease> Releases { get; }

        /// <summary>
        /// Initialize engine with desired settings
        /// </summary>
        /// <param name="args"></param>
        void Initialize(object args = null);

        /// <summary>
        /// Load engine minimum data
        /// </summary>
        /// <param name="args"></param>
        Task Load(object args = null);

        /// <summary>
        /// Download the selected version to cache
        /// </summary>
        /// <param name="args"></param>
        Task Download(object args = null);

        /// <summary>
        /// Install the selected version to final path
        /// </summary>
        /// <param name="args"></param>
        Task Install(object args = null);

        /// <summary>
        /// Start update process from base version
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        Task Update(object args = null);

    }
}
