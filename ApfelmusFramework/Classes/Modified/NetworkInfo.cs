//-----------------------------------------------------------------------
// <copyright file="NetworkInfo.cs" company="ZSoft">
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
using System.Windows.Media;
    

    public class NetworkInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {

            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        private int users;
        [XmlAttribute(AttributeName = "users")]
        public int Users
        {
            get { return users; }
            set
            {
                if (value != users)
                {
                    users = value;
                    OnPropertyChanged("Users");
                }
            }
        }

        private int files;
        [XmlAttribute(AttributeName = "files")]
        public int Files
        {
            get { return files; }
            set
            {
                if (value != files)
                {
                    files = value;
                    OnPropertyChanged("Files");
                }
            }
        }

        private string fileSize;
        [XmlAttribute(AttributeName = "filesize")]
        public string FileSize
        {
            get { return fileSize; }
            set
            {
                if (value != fileSize)
                {
                    fileSize = value;
                    OnPropertyChanged("FileSize");
                }
            }
        }

        private bool firewalled;
        [XmlAttribute(AttributeName = "firewalled")]
        public bool Firewalled
        {
            get { return firewalled; }
            set
            {
                if (value != firewalled)
                {
                    firewalled = value;
                    OnPropertyChanged("Firewalled");
                }
            }
        }

        private string ip;
        [XmlAttribute(AttributeName = "ip")]
        public string Ip
        {
            get { return ip; }
            set
            {
                if (value != ip)
                {
                    ip = value;
                    OnPropertyChanged("Ip");
                }
            }
        }

        [XmlAttribute(AttributeName = "tryconnecttoserver")]
        public int TryConnectToServer
        {
            get;
            set;
        }

        [XmlAttribute(AttributeName = "connectedwithserverid")]
        public int ConnectedWithServerId
        {
            get;
            set;
        }

        [XmlAttribute(AttributeName = "connectedsince")]
        public long ConnectedSince
        {
            get;
            set;
        }

        [XmlAttribute(AttributeName = "paused")]
        public bool Paused
        {
            get;
            set;
        }

        private string welcomeMessage;
        [XmlElementAttribute(ElementName = "welcomemessage")]
        public string WelcomeMessage
        {
            get { return welcomeMessage; }
            set
            {
                if (value != welcomeMessage)
                {
                    welcomeMessage = value;
                    OnPropertyChanged("WelcomeMessage");
                }
            }
        }

        public string FireColor
        {
            get
            {
                if (Firewalled)
                    return "Red";
                else
                    return "Green";
            }
        }
    }
}
