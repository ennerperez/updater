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


        Task<IEnumerable> GetReleases();

        Task DownloadAsync(object key);

    }
}
