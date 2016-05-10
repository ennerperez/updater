using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Updater.Core.Engines
{
    public sealed partial class GitHub : ISource
    {
        public const string Name = "github";

        public string Repository { get; set; }
        public string Url
        {
            get
            {
                if (!string.IsNullOrEmpty(Repository))
                    return string.Format(urlformat, Repository.Split('/'));
                return string.Empty;
            }
        }

        internal const string urlformat = "https://api.github.com/repos/{0}/{1}/";

        static async Task<string> read(string url)
        {

            string responseFromServer = string.Empty;
            try
            {
                Engine<GitHub>.WriteLine("Connecting...");

                var request = WebRequest.Create(url) as HttpWebRequest;
                request.ContentType = "application/json; charset=utf-8";
                request.Accept = "application/vnd.github.v3.raw+json";
                request.UserAgent = "Other";
                request.Method = "GET";

                var response = await request.GetResponseAsync() as HttpWebResponse;
                var data = response.GetResponseStream();
                var reader = new StreamReader(data);

                Engine<GitHub>.WriteLine("Gathering data...");

                responseFromServer = reader.ReadToEnd();
                reader.Close();
                response.Close();

            }
            catch (Exception ex)
            {
                Engine<GitHub>.WriteLine("ERROR -> " + ex.Message, ConsoleColor.Red);
            }

            return responseFromServer;
        }

        static async Task download(string fileName, string remoteUri, double size = 0)
        {
            try
            {

                var target = fileName + Flags.CacheExt;

                if (File.Exists(target))
                {
                    var cache = new FileInfo(target);
                    if ((double)cache.Length != size || size == 0 || Flags.Force)
                        cache.Delete();
                }

                if (!File.Exists(target))
                {

                    WebClient client = new WebClient();
                    Engine<GitHub>.WriteLine(string.Format("Downloading \"{0}\"...", remoteUri));


                    client.DownloadFileCompleted += (sender, e) =>
                    {
                        if (e.Cancelled)
                            Engine<GitHub>.WriteLine("Download was canceled.", ConsoleColor.Yellow);

                        if (e.Error != null)
                            Engine<GitHub>.WriteLine("ERROR -> " + e.Error.Message, ConsoleColor.Red);
                        else
                        {

                            if (File.Exists(fileName))
                                File.Delete(fileName);

                            File.Move(target, fileName);
                            Engine<GitHub>.WriteLine("Successfully downloaded file.", ConsoleColor.Green);
                        }
                    };

                    await client.DownloadFileTaskAsync(remoteUri, target);

                }

            }
            catch (Exception ex)
            {
                Engine<GitHub>.WriteLine("ERROR -> " + ex.Message, ConsoleColor.Red);
            }

        }


        public async Task<IEnumerable> GetReleases()
        {
            var collection = JsonConvert.DeserializeObject<IEnumerable<Release>>(await read(Url + "releases"));

            var result = collection
                .Where(r => !r.draft);

            return result;

        }
        public async Task<Release> GetLastRelease()
        {
            var collection = JsonConvert.DeserializeObject<IEnumerable<Release>>(await read(Url + "releases"));

            var result = collection
                .Where(r => !r.draft)
                .OrderByDescending(r => r.version)
                .FirstOrDefault();

            return result;

        }

        public async Task DownloadAsync(object key)
        {
            var collection = JsonConvert.DeserializeObject<IEnumerable<Release>>(await read(Url + "releases"));

            var assets = collection
                .Where(r => !r.draft)
                .SelectMany(r => r.assets);

            Asset asset = null;

            if (key != null)
                asset = assets.FirstOrDefault(r => r.id == (long)key);
            else
                asset = assets.FirstOrDefault();

            var target = asset.name;

            if (File.Exists(target))
            {
                var cache = new FileInfo(target);
                if ((double)cache.Length != asset.size || Flags.Force)
                    cache.Delete();
            }

            if (!File.Exists(target))
                await download(target, asset.browser_download_url, asset.size);
            else
                Engine<GitHub>.WriteLine("Successfully downloaded file.", ConsoleColor.Green);


        }

    }

}
