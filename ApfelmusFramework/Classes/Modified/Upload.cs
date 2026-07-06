//-----------------------------------------------------------------------
// <copyright file="Upload.cs" company="ZSoft">
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


    /// <summary>
    /// Ein laufender Upload an eine andere Quelle (Datei, Ziel-Nutzer, uebertragene Menge,
    /// Geschwindigkeit, Status). Wird im Upload-Tab angezeigt; INotifyPropertyChanged haelt die
    /// Bindings aktuell.
    /// </summary>
    public class Upload : INotifyPropertyChanged
    {
        [XmlAttribute(AttributeName = "id")]
        public int Id
        {
            get;
            set;
        }

        [XmlAttribute(AttributeName = "shareid")]
        public int Shareid
        {
            get;
            set;
        }

        [XmlAttribute(AttributeName = "version")]
        public string Version
        {
            get;
            set;
        }

        [XmlAttribute(AttributeName = "operatingsystem")]
        public int Operatingsystem
        {
            get;
            set;
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
        public int Directstate
        {
            get { return directState; }
            set
            {
                directState = value;
                OnPropertyChanged("DirectState");
            }
        }

        private int priority;
        [XmlAttribute(AttributeName = "priority")]
        public int Priority
        {
            get { return priority; }
            set
            {
                priority = value;
                OnPropertyChanged("Priority");
            }
        }

        [XmlAttribute(AttributeName = "nick")]
        public string Nick
        {
            get;
            set;
        }

        private int uploadFrom;

        [XmlAttribute(AttributeName = "uploadfrom")]
        public int UploadFrom
        {
            get { return uploadFrom; }
            set
            {
                uploadFrom = value;
                OnPropertyChanged("UploadFrom");
            }
        }

        private int actualUploadPosition;

        [XmlAttribute(AttributeName = "actualuploadposition")]
        public int ActualUploadPosition
        {
            get { return actualUploadPosition; }
            set
            {
                actualUploadPosition = value;
                OnPropertyChanged("ActualUploadPosition");
            }
        }

        private int uploadTo;

        [XmlAttribute(AttributeName = "uploadto")]
        public int UploadTo
        {
            get { return uploadTo; }
            set
            {
                uploadTo = value;
                OnPropertyChanged("UploadTo");
            }
        }

        private long lastConnection;

        [XmlAttribute(AttributeName = "lastconnection")]
        public long LastConnection
        {
            get { return lastConnection; }
            set
            {
                lastConnection = value;
                OnPropertyChanged("LastConnection");
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

        private double loaded;

        [XmlAttribute(AttributeName = "loaded")]
        public double Loaded
        {
            get { return loaded; }
            set
            {
                loaded = value;
                OnPropertyChanged("Loaded");
            }
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

        private string wPercentages;

        public string WPercentages
        {
            get { return wPercentages; }
            set
            {
                wPercentages = value;
                OnPropertyChanged("WPercentages");
            }
        }

        private string fileName;

        public string FileName
        {
            get { return fileName; }
            set
            {
                fileName = value;
                OnPropertyChanged("FileName");
            }
        }

        public string OS
        {
            get
            {
                switch (Operatingsystem)
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
