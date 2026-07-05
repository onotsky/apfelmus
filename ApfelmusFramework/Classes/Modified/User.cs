//-----------------------------------------------------------------------
// <copyright file="User.cs" company="ZSoft">
//     Copyright (c) ZSoft.
// </copyright>
// <author>daredevil</author>
//-----------------------------------------------------------------------
namespace ApfelmusFramework.Classes.Modified
{
    using System;
    using System.ComponentModel;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Serialization;

    public class User : INotifyPropertyChanged
    {
        private int id;

        [XmlAttribute(AttributeName = "id")]
        public int Id
        {
            get { return id; }
            set
            {
                id = value;
                OnPropertyChanged("Id");
            }
        }

        private int source;

        [XmlAttribute(AttributeName = "source")]
        public int Source
        {
            get { return source; }
            set
            {
                source = value;
                OnPropertyChanged("Source");
            }
        }

        private int downloadId;

        [XmlAttribute(AttributeName = "downloadid")]
        public int DownloadId
        {
            get { return downloadId; }
            set
            {
                downloadId = value;
                OnPropertyChanged("DownloadId");
            }
        }

        private int status;

        [XmlAttribute(AttributeName = "status")]
        public int Status
        {
            get { return status; }
            set
            {
                status = value;
                OnPropertyChanged("Status");
            }
        }


        private int directState;

        [XmlAttribute(AttributeName = "directstate")]
        public int DirectState
        {
            get { return directState; }
            set
            {
                directState = value;
                OnPropertyChanged("DirectState");
            }
        }

        private long downloadFrom;

        [XmlAttribute(AttributeName = "downloadfrom")]
        public long DownloadFrom
        {
            get { return downloadFrom; }
            set
            {
                downloadFrom = value;
                OnPropertyChanged("DownloadFrom");
            }
        }

        private long downloadTo;

        [XmlAttribute(AttributeName = "downloadto")]
        public long DownloadTo
        {
            get { return downloadTo; }
            set
            {
                downloadTo = value;
                OnPropertyChanged("DownloadTo");
            }
        }

        private long actualDownloadPosition;

        [XmlAttribute(AttributeName = "actualdownloadposition")]
        public long ActualDownloadPosition
        {
            get { return actualDownloadPosition; }
            set
            {
                actualDownloadPosition = value;
                OnPropertyChanged("ActualDownloadPosition");
            }
        }

        private int speed;

        [XmlAttribute(AttributeName = "speed")]
        public int Speed
        {
            get { return speed; }
            set
            {
                speed = value;
                OnPropertyChanged("Speed");
            }
        }

        private string version;

        [XmlAttribute(AttributeName = "version")]
        public string Version
        {
            get { return version; }
            set
            {
                version = value;
                OnPropertyChanged("Version");
            }
        }

        private int operatingSystem;

        [XmlAttribute(AttributeName = "operatingsystem")]
        public int OperatingSystem
        {
            get { return operatingSystem; }
            set
            {
                operatingSystem = value;
                OnPropertyChanged("OperatingSystem");
            }
        }

        private int queuePosition;

        [XmlAttribute(AttributeName = "queueposition")]
        public int QueuePosition
        {
            get { return queuePosition; }
            set
            {
                queuePosition = value;
                OnPropertyChanged("QueuePosition");
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

        private string nickName;

        [XmlAttribute(AttributeName = "nickname")]
        public string NickName
        {
            get { return nickName; }
            set
            {
                nickName = value;
                OnPropertyChanged("NickName");
            }
        }

        private long fileSizeToGet;

        public long FileSizeToGet
        {
            get { return fileSizeToGet; }
            set
            {
                fileSizeToGet = value;
                OnPropertyChanged("FileSizeToGet");
            }
        }

        private long fileSize;

        public long FileSize
        {
            get { return fileSize; }
            set
            {
                fileSize = value;
                OnPropertyChanged("FileSize");
            }
        }

        private long actualFileSize;

        public long ActualFileSize
        {
            get { return actualFileSize; }
            set
            {
                actualFileSize = value;
                OnPropertyChanged("ActualFileSize");
            }
        }

        private string percetages;

        public string Percentages
        {
            get { return percetages; }
            set
            {
                percetages = value;
                OnPropertyChanged("Percentages");
            }
        }

        public string OS
        {
            get
            {
                switch (OperatingSystem)
                {
                    case 1:
                        return @"Images\windows_alt.png";
                    case 2:
                        return @"Images\Linux-icon.png";
                    case 3:
                        return @"Images\mac.png";
                    default:
                        return @"Images\stock_unknown.png";
                }
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

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string info)
        {
            if(PropertyChanged != null)
            {
                PropertyChanged(this,new PropertyChangedEventArgs(info));
            }
        }
    }
}
