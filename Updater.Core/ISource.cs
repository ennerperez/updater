using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Updater.Core
{
    public interface ISource
    {

        /// <summary>
        /// Engine name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Initialize engine with desired settings
        /// </summary>
        /// <param name="args"></param>
        void Initialize(object args = null);

        /// <summary>
        /// Get all releases from server
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable> GetReleases();

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

    }
}
