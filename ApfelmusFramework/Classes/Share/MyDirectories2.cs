using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Apfelmus.Classes.Share
{
    public class MyDirectories2 : INotifyPropertyChanged
    {
        private string name;

        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                OnPropertyChanged("Name");
            }
        }

        private List<MyDirectories3> folder;

        public List<MyDirectories3> Folder
        {
            get { return folder; }
            set 
            { 
                folder = value;
                OnPropertyChanged("Folder");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
    }
}
