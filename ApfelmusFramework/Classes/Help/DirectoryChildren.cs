using System.Collections.ObjectModel;
using ApfelmusFramework.Classes.Allgemein;
using ApfelmusFramework.Classes.Logic;
using ApfelmusFramework.Classes.ExtensionSort;
using ApfelmusFramework.Classes.Directory;

namespace ApfelmusFramework.Classes.Help
{
    public class DirectoryChildren
    {

        public Dir Dir
        {
            get;
            set;
        }

        public Config.Config Config
        {
            get;
            set;
        }

        public ObservableCollection<DirectoryChildren> Childrens
        {
            get
            {
                ObservableCollection<DirectoryChildren> returnment = new ObservableCollection<DirectoryChildren>();
                AppleJuice dir = new AppleJuice();

                using (WebConnect webConnect = new WebConnect(Config.HostName, Config.Port))
                {
                    dir = dir.DeserializeToObj(webConnect.GetHttpResult(string.Format("/xml/directory.xml?directory={0}&password={1}&mode=zip", Dir.Path.Replace(" ", "%20"), Config.Password), Config.UseCompression)) as AppleJuice;
                }

                dir.Dir.Sort(a => a.Name);

                foreach (Dir _dir in dir.Dir)
                {
                    if (string.IsNullOrEmpty(_dir.Path))
                    {
                        if (dir.FileSystem.Seperator.Equals("/"))
                            _dir.Path = string.Format("{0}{1}{2}", Dir.Path, _dir.Name, dir.FileSystem.Seperator);
                        else
                            _dir.Path = string.Format("{0}{1}{2}", Dir.Path, dir.FileSystem.Seperator, _dir.Name);
                    }

                    returnment.Add(new DirectoryChildren(_dir, Config));
                }

                return returnment;
            }
        }


        public DirectoryChildren(Dir dir, Config.Config config)
        {
            Dir = dir;
            Config = config;
        }
    }
}
