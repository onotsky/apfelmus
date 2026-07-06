//-----------------------------------------------------------------------
// <copyright file="Information.cs" company="ZSoft">
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

    /// <summary>
    /// Clientbezogene Kennzahlen des Cores (u.a. Credits, Sitzungs-/Gesamt-Up-/Download), wie sie
    /// im Start-/Uebersichts-Tab angezeigt werden. INotifyPropertyChanged aktualisiert die Bindings
    /// bei jedem Refresh. Credits sind long (Betraege koennen 2 GB uebersteigen).
    /// </summary>
    public class Information : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {

            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        private long credits;
        [XmlAttribute(AttributeName = "credits")]
        public long Credits
        {
            get
            {
                return credits;
            }
            set
            {
                if (value != credits)
                {
                    credits = value;
                    OnPropertyChanged("Credits");
                }
            }
        }

        private long sessionUpload;
        [XmlAttribute(AttributeName = "sessionupload")]
        public long SessionUpload
        {
            get
            {
                return sessionUpload;
            }
            set
            {
                if (value != sessionUpload)
                {
                    sessionUpload = value;
                    OnPropertyChanged("SessionUpload");
                }
            }
        }

        private long sessionDownload;
        [XmlAttribute(AttributeName = "sessiondownload")]
        public long SessionDownload
        {
            get
            {
                return sessionDownload;
            }
            set
            {
                if (value != sessionDownload)
                {
                    sessionDownload = value;
                    OnPropertyChanged("SessionDownload");
                }
            }
        }

        private int uploadSpeed;
        [XmlAttribute(AttributeName = "uploadspeed")]
        public int UploadSpeed
        {
            get
            {
                return uploadSpeed;
            }
            set
            {
                if (value != uploadSpeed)
                {
                    uploadSpeed = value;
                    OnPropertyChanged("UploadSpeed");
                }
            }
        }

        private int downloadSpeed;
        [XmlAttribute(AttributeName = "downloadspeed")]
        public int DownloadSpeed
        {
            get
            {
                return downloadSpeed;
            }
            set
            {
                if (value != downloadSpeed)
                {
                    downloadSpeed = value;
                    OnPropertyChanged("DownloadSpeed");
                }
            }
        }

        private int openConnections;
        [XmlAttribute(AttributeName = "openconnections")]
        public int OpenConnections
        {
            get
            {
                return openConnections;
            }
            set
            {
                if (value != openConnections)
                {
                    openConnections = value;
                    OnPropertyChanged("OpenConnections");
                }
            }
        }

        private int maxUploadPositions;
        [XmlAttribute(AttributeName = "maxuploadpositions")]
        public int MaxUploadPositions
        {
            get
            {
                return maxUploadPositions;
            }
            set
            {
                if (value != maxUploadPositions)
                {
                    maxUploadPositions = value;
                    OnPropertyChanged("MaxUploadPositions");
                }
            }
        }
    }
}
