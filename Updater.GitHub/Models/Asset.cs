using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Updater.Core
{
    public partial class GitHub
    {

        public class Asset
        {
            public string url { get; set; }
            public long id { get; set; }
            public string name { get; set; }
            public string label { get; set; }
            public string content_type { get; set; }
            public string state { get; set; }
            public double size { get; set; }
            public int download_count { get; set; }
            public DateTime created_at { get; set; }
            public DateTime updated_at { get; set; }
            public string browser_download_url { get; set; }
        }
    }
}
