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

        string GetName();

        void Initialize(IDictionary args = null);

        Task<IEnumerable> GetReleases();

        Task DownloadAsync(object key);

    }
}
