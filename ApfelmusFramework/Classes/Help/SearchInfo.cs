using System;
using System.ComponentModel;

namespace ApfelmusFramework.Classes.Help
{
    public class SearchInfo : INotifyPropertyChanged
    {
        private int sumSearches;

        public int SumSearches
        {
            get { return sumSearches; }
            set
            {
                sumSearches = value;
                OnPropertyChanged("SumSearches");
            }
        }

        private int users;

        public int Users
        {
            get { return users; }
            set
            {
                users = value;
                OnPropertyChanged("Users");
            }
        }

        private int openSearches;

        public int OpenSearches
        {
            get { return openSearches; }
            set
            {
                openSearches = value;
                OnPropertyChanged("OpenSearches");
            }
        }

        private int foundFiles;

        public int FoundFiles
        {
            get { return foundFiles; }
            set
            {
                foundFiles = value;
                OnPropertyChanged("FoundFiles");
            }
        }

        private bool running;

        public bool Running
        {
            get { return running; }
            set
            {
                running = value;
                OnPropertyChanged("Running");
            }
        }

        private int id;

        public int Id
        {
            get { return id; }
            set
            {
                id = value;
                OnPropertyChanged("Id");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
    }
}
