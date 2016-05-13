using Ionic.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Updater.Core
{
    public sealed partial class GitHub : IEngine
    {

        #region Server definitions

        private string urlRepo
        {
            get
            {
                if (!string.IsNullOrEmpty(UserName))
                    return string.Format(urlformat, UserName, Repository);
                else if (!string.IsNullOrEmpty(Repository))
                    return string.Format(urlformat, Repository.Split('/'));
                return string.Empty;
            }
        }
        private const string urlformat = "https://api.github.com/repos/{0}/{1}/";

        #endregion

        public string UserName { get; set; }
        public string Repository { get; set; }
        public bool Drafts { get; set; }

        #region ISource Implementation

        public string Name { get { return typeof(GitHub).Name; } }

        private IEnumerable<Release> releases;
        public IEnumerable<IRelease> Releases { get { return releases; } }

        public void Initialize(object args = null)
        {

            IDictionary _args = null;

            if (args.GetType().GetInterfaces().Contains(typeof(IDictionary)))
                _args = (IDictionary)args;

            if (_args.Keys.Cast<string>().Contains("/u"))
                UserName = _args["/u"].ToString();

            if (_args.Keys.Cast<string>().Contains("/d"))
                Drafts = true;

            if (_args.Keys.Cast<string>().Contains("/r"))
            {

                if (string.IsNullOrEmpty(UserName))
                {
                    var parts = _args["/r"].ToString().Split('/');
                    if (parts.Count() > 1)
                    {
                        UserName = parts[0];
                        Repository = parts[1];
                    }
                }
                else
                {
                    Repository = _args["/r"].ToString();
                }

            }

        }

        public void Help()
        {
            Engine.Help(Properties.Resources.Help);
        }

        public async Task Load(object args = null)
        {
            var uri = new UriBuilder(urlRepo);
            uri.Path += "releases";

            var collection = JsonConvert.DeserializeObject<IEnumerable<Release>>(await Engine<GitHub>.Read(uri.ToString()));

            if (!Drafts)
                releases = collection.Where(r => !r.draft).OrderByDescending(r => r.version);
            else
                releases = collection.OrderByDescending(r => r.version);
        }

        public async Task Download(object args = null)
        {

            var lastRelease = (Release)releases.FirstOrDefault();

            if (lastRelease == null)
                throw new FileNotFoundException("Release was not found.");

            IEnumerable<Asset> assets = null;

            if (args != null)
            {
                if (args.GetType() == typeof(long))
                    assets = lastRelease.assets.Where(r => r.id == (long)args);
                else
                    assets = lastRelease.assets.Where(r => r.name == args.ToString());
            }
            else
                assets = lastRelease.assets;

            if (assets == null || assets.Count() == 0)
                throw new FileNotFoundException("Downloads was not found.");

            foreach (var asset in assets)
            {

                var target = Path.Combine(Engine<GitHub>.AppCache, asset.name);

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

        }

        public async Task Install(object args = null)
        {

            var lastRelease = (Release)releases.FirstOrDefault();

            if (lastRelease == null)
                throw new FileNotFoundException("Release was not found.");

            IEnumerable<Asset> assets = null;

            if (args != null)
            {
                if (args.GetType() == typeof(long))
                    assets = lastRelease.assets.Where(r => r.id == (long)args);
                else
                    assets = lastRelease.assets.Where(r => r.name == args.ToString());
            }
            else
                assets = lastRelease.assets;

            if (assets == null || assets.Count() == 0)
                throw new FileNotFoundException("Downloads was not found.");

            foreach (var asset in assets)
            {

                var target = Path.Combine(Engine<GitHub>.AppCache, asset.name);

                Engine<GitHub>.WriteLine("Reading...");

                if (!File.Exists(target))
                    throw new FileNotFoundException(target);

                if (!Directory.Exists(Flags.Target))
                    throw new DirectoryNotFoundException();

                if (target.EndsWith(".zip"))
                {
                    Engine<GitHub>.WriteLine("Extracting...");
                    var zipfile = new ZipFile(target);
                    await Task.Factory.StartNew(() => zipfile.ExtractAll(Flags.Target, ExtractExistingFileAction.OverwriteSilently));
                    zipfile.Dispose();
                    zipfile = null;
                }
                else
                {
                    var targetFile = Path.Combine(Flags.Target, asset.name);
                    if (File.Exists(targetFile)) File.Delete(targetFile);
                    File.Move(target, targetFile);
                }

                Engine<GitHub>.WriteLine("Successfully installed.", ConsoleColor.Green);

            }

        }

        public async Task Update(object args = null)
        {
            if (args == null)
                args = new Version(1, 0, 0, 0);

            Version curentVersion = null;
            Version.TryParse(args.ToString(), out curentVersion);

            var lastRelease = (Release)releases.FirstOrDefault();

            if (lastRelease == null)
                throw new FileNotFoundException("Release was not found.");

            if (lastRelease.version > curentVersion)
            {
                await Download();
                await Install();
            }
            else
                Engine<GitHub>.WriteLine("No update available.", ConsoleColor.Green);

        }

        #endregion

    }

}
