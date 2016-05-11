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

namespace Updater.Core
{
    public sealed partial class GitHub : ISource
    {

        public string Username { get; set; }
        public string Repository { get; set; }

        public string Url
        {
            get
            {
                if (!string.IsNullOrEmpty(Username))
                    return string.Format(urlformat, Username, Repository);
                else if (!string.IsNullOrEmpty(Repository))
                    return string.Format(urlformat, Repository.Split('/'));
                return string.Empty;
            }
        }

        public string GetName() { return GitHub.Name; }

        public const string Name = "GitHub";

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

                var cache = fileName + Flags.CacheExt;

                if (File.Exists(cache))
                    File.Delete(cache);

                if (!File.Exists(cache))
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

                            File.Move(cache, fileName);
                            Engine<GitHub>.WriteLine("Successfully downloaded file.", ConsoleColor.Green);
                        }
                    };

                    await client.DownloadFileTaskAsync(remoteUri, cache);

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
                var file = new FileInfo(target);
                if ((double)file.Length != asset.size || Flags.Force)
                    file.Delete();
            }

            if (!File.Exists(target))
                await download(target, asset.browser_download_url, asset.size);
            else
                Engine<GitHub>.WriteLine("Successfully downloaded file.", ConsoleColor.Green);


        }

        public void Initialize(IDictionary args = null)
        {

            if (args.Keys.Cast<string>().Contains("/u"))
                Username = args["/u"].ToString();

            if (args.Keys.Cast<string>().Contains("/r"))
            {

                if (string.IsNullOrEmpty(Username))
                {
                    var parts = args["/r"].ToString().Split('/');
                    if (parts.Count() > 1)
                    {
                        Username = parts[0];
                        Repository = parts[1];
                    }
                }
                else
                {
                    Repository = args["/r"].ToString();
                }

            }

        }
    }

}
