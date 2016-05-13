using Ionic.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Updater.Core
{
    public sealed partial class NuGet : IEngine
    {

        #region Server definitions

        private string urlPackage
        {
            get
            {
                if (!string.IsNullOrEmpty(Package))
                    return string.Format(urlformat, "package", Package.ToLower());
                return string.Empty;
            }
        }
        private string urlVersions
        {
            get
            {
                if (!string.IsNullOrEmpty(Package))
                    return string.Format(urlformat, "package-versions", Package.ToLower());
                return string.Empty;
            }
        }
        private const string urlformat = "https://www.nuget.org/api/v2/{0}/{1}";

        #endregion

        public string Package { get; set; }
        public Version Version { get; set; }
        public string Directories { get; set; }

        #region ISource Implementation

        public string Name { get { return typeof(NuGet).Name; } }

        private IEnumerable<Version> releases;
        public IEnumerable<IRelease> Releases { get; }

        public void Initialize(object args = null)
        {
            IDictionary _args = null;

            if (args.GetType().GetInterfaces().Contains(typeof(IDictionary)))
                _args = (IDictionary)args;

            if (_args == null)
                throw new InvalidDataException("Invalid initialization arguments.");

            if (_args.Keys.Cast<string>().Contains("/d"))
                Directories = _args["/d"].ToString();

            if (_args.Keys.Cast<string>().Contains("/p"))
                Package = _args["/p"].ToString();

            if (_args.Keys.Cast<string>().Contains("/v"))
            {
                Version version = null;
                Version.TryParse(_args["/v"].ToString(), out version);
                Version = version;
            }
            else
            {
                var parts = _args["/p"].ToString().Split('/');
                if (parts.Count() > 1)
                {
                    Package = parts[0];
                    Version version = null;
                    Version.TryParse(parts[1], out version);
                    Version = version;
                }
            }

        }

        public void Help()
        {
            Engine.Help(Properties.Resources.Help);
        }

        public async Task Load(object args = null)
        {
            var uri = new UriBuilder(urlVersions);
            var collection = JsonConvert.DeserializeObject<IEnumerable<Version>>(await Engine<NuGet>.Read(uri.ToString()));
            releases = collection.OrderByDescending(r => r);
        }

        public async Task Download(object args = null)
        {
            var url = urlPackage;

            if (args != null && (args.GetType() == typeof(Version)))
                Version = (Version)args;

            if (Version != null)
                url = urlPackage + "/" + Version.ToString();

            var filename = Package.ToLower() + (Version != null ? "." + Version.ToString() : "") + ".nupkg";
            var target = Path.Combine(Engine<NuGet>.AppCache, filename);

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
                await Engine<NuGet>.Download(target, url, size);
            else
                Engine<NuGet>.WriteLine("Successfully downloaded file.", ConsoleColor.Green);

            if (!File.Exists(target))
                target = null;
        }

        public async Task Install(object args = null)
        {

            var filename = Package.ToLower() + (Version != null ? "." + Version.ToString() : "") + ".nupkg";
            var target = Path.Combine(Engine<NuGet>.AppCache, filename);

            Engine<NuGet>.WriteLine("Reading...");

            if (!File.Exists(target))
                throw new FileNotFoundException(target);

            if (!Directory.Exists(Flags.Target))
                throw new DirectoryNotFoundException();

            var zipfile = new ZipFile(target);
            var nuspec = zipfile.Entries.FirstOrDefault(e => e.FileName.EndsWith(".nuspec"));
            zipfile.Dispose();
            zipfile = null;
            await Task.Factory.StartNew(() =>
            {
                nuspec.Extract(Engine<NuGet>.AppCache, ExtractExistingFileAction.OverwriteSilently);
            });
            var nuspecFile = Path.Combine(Engine<NuGet>.AppCache, nuspec.FileName);
            var nuspecDoc = XDocument.Load(nuspecFile);
            File.Delete(nuspecFile);

            //Engine<NuGet>.WriteLine("Finding dependencies...");
            //var dependencies = nuspecDoc.Descendants(XName.Get("dependency", @"http://schemas.microsoft.com/packaging/2013/01/nuspec.xsd"));
            //foreach (var item in dependencies)
            //{
            //    var subengine = new NuGet();
            //    subengine.Package = item.Attribute("id").Value;
            //    subengine.Version = new Version(item.Attribute("version").Value);
            //    subengine.target = this.target;

            //    await subengine.Download();
            //    await subengine.Install(args);

            //}

            if (args == null && !string.IsNullOrEmpty(Directories))
            {
                var listSeparator = new string[] { CultureInfo.CurrentCulture.TextInfo.ListSeparator };
                args = Directories.Split(listSeparator, StringSplitOptions.RemoveEmptyEntries);
            }

            if (args != null)
            {

                var directory = new DirectoryInfo(Path.Combine(Engine<NuGet>.AppCache, Package));
                if (!directory.Exists) directory.Create();

                var _args = (IEnumerable)args;

                foreach (var arg in _args)
                {

                    Engine<NuGet>.WriteLine("Extracting...");

                    zipfile = new ZipFile(target);
                    await Task.Factory.StartNew(() =>
                    {
                        zipfile.ExtractSelectedEntries("*.*", arg.ToString().Replace(@"\", @"/"), directory.FullName, ExtractExistingFileAction.OverwriteSilently);
                    });
                    zipfile.Dispose();
                    zipfile = null;

                    Engine<NuGet>.WriteLine("Installing...");

                    var subdirectory = new DirectoryInfo(Path.Combine(Engine<NuGet>.AppCache, Package, arg.ToString()));

                    if (subdirectory.Exists)
                    {
                        foreach (var dir in subdirectory.GetDirectories())
                        {
                            var path = Path.Combine(Flags.Target, dir.Name);
                            if (Directory.Exists(path))
                                Directory.Delete(path, true);
                            dir.MoveTo(path);
                        }
                        foreach (var file in subdirectory.GetFiles())
                        {
                            var path = Path.Combine(Flags.Target, file.Name);
                            if (File.Exists(path))
                                File.Delete(path);
                            file.MoveTo(path);
                        }

                        subdirectory.Delete(true);
                    }

                }

                directory.Delete(true);

                Engine<NuGet>.WriteLine("Successfully installed.", ConsoleColor.Green);

            }
            else
            {
                Engine<NuGet>.WriteLine("Missing installation sequence.", ConsoleColor.Red);
            }

        }

        public async Task Update(object args = null)
        {
            if (args == null)
                args = new Version(1, 0, 0, 0);

            Version curentVersion = null;
            Version.TryParse(args.ToString(), out curentVersion);

            var lastRelease = (Version)releases.FirstOrDefault();

            if (lastRelease == null)
                throw new FileNotFoundException("Release was not found.");

            if (lastRelease > curentVersion)
            {
                await Download();
                await Install();
            }
            else
                Engine<NuGet>.WriteLine("No update available.", ConsoleColor.Green);

        }

        #endregion

    }

}
