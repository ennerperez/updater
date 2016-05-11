using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Updater.Core
{
    public interface IRelease
    {

        long id { get; set; }

        string url { get; set; }

        string name { get; set; }

        bool prerelease { get; set; }
        DateTime created_at { get; set; }
        DateTime published_at { get; set; }

        Version version { get; }
    }


}
