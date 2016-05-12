using Ionic.Zip;
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

        private string target;
        public string Target { get { return target; } }

        #region Server definitions

        private string urlRepo
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

        internal const string urlformat = "https://api.github.com/repos/{0}/{1}/";

        #endregion

        public async Task<Release> GetLastRelease()
        {
            var collection = JsonConvert.DeserializeObject<IEnumerable<Release>>(await Engine<GitHub>.Read(urlRepo + "releases"));

            var result = collection
                .Where(r => !r.draft)
                .OrderByDescending(r => r.version)
                .FirstOrDefault();

            return result;

        }

        #region ISource Implementation

        private const string name = "GitHub";
        public string Name { get { return name; } }

        public void Initialize(object args = null)
        {

            IDictionary _args = null;

            if (args.GetType().GetInterfaces().Contains(typeof(IDictionary)))
                _args = (IDictionary)args;

            if (_args.Keys.Cast<string>().Contains("/u"))
                Username = _args["/u"].ToString();

            if (_args.Keys.Cast<string>().Contains("/r"))
            {

                if (string.IsNullOrEmpty(Username))
                {
                    var parts = _args["/r"].ToString().Split('/');
                    if (parts.Count() > 1)
                    {
                        Username = parts[0];
                        Repository = parts[1];
                    }
                }
                else
                {
                    Repository = _args["/r"].ToString();
                }

            }

        }

        public async Task<IEnumerable> GetReleases()
        {
            var collection = JsonConvert.DeserializeObject<IEnumerable<Release>>(await Engine<GitHub>.Read(urlRepo + "releases"));
            var result = collection
                .Where(r => !r.draft);
            return result;
        }

        public async Task Download(object args = null)
        {
            var collection = JsonConvert.DeserializeObject<IEnumerable<Release>>(await Engine<GitHub>.Read(urlRepo + "releases"));

            var assets = collection
                .Where(r => !r.draft)
                .SelectMany(r => r.assets);

            Asset asset = null;

            if (args != null)
                asset = assets.FirstOrDefault(r => r.id == (long)args);
            else
                asset = assets.FirstOrDefault();

            target = Path.Combine(Engine<GitHub>.AppCache, asset.name);

            if (File.Exists(target))
            {
                var file = new FileInfo(target);
                if ((double)file.Length != asset.size || Flags.Force)
                    file.Delete();
            }

            if (!File.Exists(target))
                await Engine<GitHub>.Download(target, asset.browser_download_url, asset.size);
            else
                Engine<GitHub>.WriteLine("Successfully downloaded file.", ConsoleColor.Green);

            if (!File.Exists(target))
                target = null;

        }

        public async Task Install(object args = null)
        {

            Engine<GitHub>.WriteLine("Reading...");

            if (!File.Exists(target))
                throw new FileNotFoundException(target);

            if (!Directory.Exists(Flags.Target))
                throw new DirectoryNotFoundException();

            Engine<GitHub>.WriteLine("Extracting...");

            var zipfile = new ZipFile(target);
            await Task.Factory.StartNew(() => zipfile.ExtractAll(Flags.Target, ExtractExistingFileAction.OverwriteSilently));

            zipfile.Dispose();
            zipfile = null;

            Engine<GitHub>.WriteLine("Successfully installed.", ConsoleColor.Green);

        }

        #endregion

        #region Engine vars

        public string Username { get; set; }
        public string Repository { get; set; }

        #endregion

        //static async Task<string> read(string url)
        //{

        //    string responseFromServer = string.Empty;
        //    try
        //    {
        //        Engine<GitHub>.WriteLine("Connecting...");

        //        var request = WebRequest.Create(url) as HttpWebRequest;
        //        request.ContentType = "application/json; charset=utf-8";
        //        request.Accept = "application/vnd.github.v3.raw+json";
        //        request.UserAgent = "Other";
        //        request.Method = "GET";

        //        var response = await request.GetResponseAsync() as HttpWebResponse;
        //        var data = response.GetResponseStream();
        //        var reader = new StreamReader(data);

        //        Engine<GitHub>.WriteLine("Gathering data...");

        //        responseFromServer = reader.ReadToEnd();
        //        reader.Close();
        //        response.Close();

        //    }
        //    catch (Exception ex)
        //    {
        //        Engine<GitHub>.WriteLine("ERROR -> " + ex.Message, ConsoleColor.Red);
        //    }

        //    return responseFromServer;
        //}
        //static async Task download(string fileName, string remoteUri, double size = 0)
        //{
        //    try
        //    {

        //        var cache = fileName + Flags.CacheExt;

        //        if (File.Exists(cache))
        //            File.Delete(cache);

        //        if (!File.Exists(cache))
        //        {

        //            WebClient client = new WebClient();
        //            Engine<GitHub>.WriteLine(string.Format("Downloading \"{0}\"...", remoteUri));


        //            client.DownloadFileCompleted += (sender, e) =>
        //            {
        //                if (e.Cancelled)
        //                    Engine<GitHub>.WriteLine("Download was canceled.", ConsoleColor.Yellow);

        //                if (e.Error != null)
        //                    Engine<GitHub>.WriteLine("ERROR -> " + e.Error.Message, ConsoleColor.Red);
        //                else
        //                {

        //                    if (File.Exists(fileName))
        //                        File.Delete(fileName);

        //                    File.Move(cache, fileName);
        //                    Engine<GitHub>.WriteLine("Successfully downloaded file.", ConsoleColor.Green);
        //                }
        //            };

        //            await client.DownloadFileTaskAsync(remoteUri, cache);

        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        Engine<GitHub>.WriteLine("ERROR -> " + ex.Message, ConsoleColor.Red);
        //    }

        //}

    }

}
