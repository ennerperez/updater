using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Updater.Core.Engines
{
    public partial class GitHub
    {

        public class Release
        {
            public string url { get; set; }
            public string assets_url { get; set; }
            public string urlupload_url { get; set; }
            public string html_url { get; set; }
            public long id { get; set; }
            public string tag_name { get; set; }
            public string target_commitish { get; set; }
            public string name { get; set; }
            public bool draft { get; set; }
            public bool prerelease { get; set; }
            public DateTime created_at { get; set; }
            public DateTime published_at { get; set; }
            public IEnumerable<Asset> assets { get; set; }
            public string body { get; set; }
            public string tarball_url { get; set; }
            public string zipball_url { get; set; }

            public Version version
            {
                get
                {
                    Version value = null;
                    Version.TryParse(tag_name.Replace("v", ""), out value);
                    return value;
                }

            }
        }

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
