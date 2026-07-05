//-----------------------------------------------------------------------
// <copyright file="Download.cs" company="ZSoft">
//     Copyright (c) ZSoft.
// </copyright>
// <author>daredevil</author>
//-----------------------------------------------------------------------
namespace ApfelmusFramework.Classes.Modified
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Serialization;
    using System.ComponentModel;

    public class Download : INotifyPropertyChanged
    {
        [XmlAttribute(AttributeName = "id")]
        public int Id
        {
            get;
            set;
        }

        [XmlAttribute(AttributeName = "shareid")]
        public int ShareId
        {
            get;
            set;
        }

        private int speed;

        public int Speed
        {
            get { return speed; }
            set
            {
                speed = value;
                OnPropertyChanged("Speed");
            }
        }

        [XmlAttribute(AttributeName = "hash")]
        public string Hash
        {
            get;
            set;
        }

        [XmlAttribute(AttributeName = "size")]
        public string Size
        {
            get;
            set;
        }

        private int status;
        [XmlAttribute(AttributeName = "status")]
        public int Status
        {
            get { return GetStatus(); }
            set
            {
                status = value;
                OnPropertyChanged("Status");
            }
        }

        private int powerDownload;

        [XmlAttribute(AttributeName = "powerdownload")]
        public int PowerDownload
        {
            get { return powerDownload; }
            set
            {
                powerDownload = value;
                OnPropertyChanged("PowerDownload");
            }
        }

        [XmlAttribute(AttributeName = "temporaryfilenumber")]
        public int TemporaryFileNumber
        {
            get;
            set;
        }

        private string fileName;

        [XmlAttribute(AttributeName = "filename")]
        public string FileName
        {
            get { return fileName; }
            set
            {
                fileName = value;
                OnPropertyChanged("FileName");
            }
        }

        [XmlAttribute(AttributeName = "targetdirectory")]
        public string TargetDirectory
        {
            get;
            set;
        }

        [XmlAttribute(AttributeName = "ready")]
        public string Ready
        {
            get;
            set;
        }

        private string percentages;

        public string Percentages
        {
            get { return percentages; }
            set
            {
                percentages = value;
                OnPropertyChanged("Percentages");
            }
        }


        private string checkIfIsOver;

        public string CheckIfIsOver
        {
            get { return checkIfIsOver; }
            set
            {
                checkIfIsOver = value;
                OnPropertyChanged("CheckIfIsOver");
            }
        }

        private string downloadedFileSize;

        public string DownloadedFilesize
        {
            get { return downloadedFileSize; }
            set
            {
                downloadedFileSize = value;
                OnPropertyChanged("DownloadedFilesize");
            }
        }

        private string downloadRest;

        public string DownloadRest
        {
            get { return downloadRest; }
            set
            {
                downloadRest = value;
                OnPropertyChanged("DownloadRest");
            }
        }

        private int timeToEnd;

        public int TimeToEnd
        {
            get { return timeToEnd; }
            set
            {
                timeToEnd = value;
                OnPropertyChanged("TimeToEnd");
            }
        }

        private int downloadUsers;

        public int DownloadUsers
        {
            get { return downloadUsers; }
            set
            {
                downloadUsers = value;
                OnPropertyChanged("DownloadUsers");
            }
        }

        private int GetStatus()
        {
            if (status == 0 && activeUsers > 0)
            {
                return 2;
            }
            return status;
        }

        private int activeUsers;

        public int ActiveUsers
        {
            get { return activeUsers; }
            set
            {
                activeUsers = value;
                OnPropertyChanged("ActiveUsers");
            }
        }

        private int allUsers;

        public int AllUsers
        {
            get { return allUsers; }
            set
            {
                allUsers = value;
                OnPropertyChanged("AllUsers");
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
