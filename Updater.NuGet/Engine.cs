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
    public sealed partial class NuGet : ISource
    {

        public string Package { get; set; }
        public Version Version { get; set; }

        public string UrlPackage
        {
            get
            {
                if (!string.IsNullOrEmpty(Package))
                    return string.Format(urlformat, "package", Package.ToLower());
                return string.Empty;
            }
        }
        public string UrlVersions
        {
            get
            {
                if (!string.IsNullOrEmpty(Package))
                    return string.Format(urlformat, "package-versions", Package.ToLower());
                return string.Empty;
            }
        }

        public string GetName() { return NuGet.Name; }

        public const string Name = "NuGet";

        internal const string urlformat = "https://www.nuget.org/api/v2/{0}/{1}";

        static async Task<string> read(string url)
        {

            string responseFromServer = string.Empty;
            try
            {
                Engine<NuGet>.WriteLine("Connecting...");

                var request = WebRequest.Create(url) as HttpWebRequest;
                request.ContentType = "application/json; charset=utf-8";
                request.UserAgent = "Other";
                request.Method = "GET";

                var response = await request.GetResponseAsync() as HttpWebResponse;
                var data = response.GetResponseStream();
                var reader = new StreamReader(data);

                Engine<NuGet>.WriteLine("Gathering data...");

                responseFromServer = reader.ReadToEnd();
                reader.Close();
                response.Close();

            }
            catch (Exception ex)
            {
                Engine<NuGet>.WriteLine("ERROR -> " + ex.Message, ConsoleColor.Red);
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
                    Engine<NuGet>.WriteLine(string.Format("Downloading \"{0}\"...", remoteUri));


                    client.DownloadFileCompleted += (sender, e) =>
                    {
                        if (e.Cancelled)
                            Engine<NuGet>.WriteLine("Download was canceled.", ConsoleColor.Yellow);

                        if (e.Error != null)
                            Engine<NuGet>.WriteLine("ERROR -> " + e.Error.Message, ConsoleColor.Red);
                        else
                        {

                            if (File.Exists(fileName))
                                File.Delete(fileName);

                            File.Move(cache, fileName);
                            Engine<NuGet>.WriteLine("Successfully downloaded file.", ConsoleColor.Green);
                        }
                    };

                    await client.DownloadFileTaskAsync(remoteUri, cache);

                }

            }
            catch (Exception ex)
            {
                Engine<NuGet>.WriteLine("ERROR -> " + ex.Message, ConsoleColor.Red);
            }

        }

        public async Task<IEnumerable> GetReleases()
        {
            var collection = JsonConvert.DeserializeObject<IEnumerable<Version>>(await read(UrlVersions));

            return collection;

        }
        public async Task<Version> GetLastRelease()
        {
            var collection = JsonConvert.DeserializeObject<IEnumerable<Version>>(await read(UrlVersions));

            var result = collection
                .OrderByDescending(r => r)
                .FirstOrDefault();

            return result;

        }

        public async Task DownloadAsync(object key)
        {

            var url = UrlPackage;

            if (key != null && (key.GetType() == typeof(Version)))
                Version = (Version)key;

            if (Version != null)
                url = UrlPackage + "/" + Version.ToString();

            var target = Package.ToLower() + (Version != null ? "." + Version.ToString() : "") + ".nupkg";

            Engine<NuGet>.WriteLine("Connecting...");

            var request = WebRequest.Create(url) as HttpWebRequest;
            var response = await request.GetResponseAsync() as HttpWebResponse;

            Engine<NuGet>.WriteLine("Gathering data...");

            var size = response.ContentLength;

            if (File.Exists(target))
            {
                var file = new FileInfo(target);
                if ((double)file.Length != size || Flags.Force)
                    file.Delete();
            }

            if (!File.Exists(target))
                await download(target, url, size);
            else
                Engine<NuGet>.WriteLine("Successfully downloaded file.", ConsoleColor.Green);

        }

        public void Initialize(IDictionary args = null)
        {
            if (args.Keys.Cast<string>().Contains("/p"))
                Package = args["/p"].ToString();

            if (args.Keys.Cast<string>().Contains("/v"))
            {
                Version version = null;
                Version.TryParse(args["/v"].ToString(), out version);
                Version = version;
            }
            else
            {
                var parts = args["/p"].ToString().Split('/');
                if (parts.Count() > 1)
                {
                    Package = parts[0];
                    Version version = null;
                    Version.TryParse(parts[1], out version);
                    Version = version;
                }
            }

        }
    }

}
