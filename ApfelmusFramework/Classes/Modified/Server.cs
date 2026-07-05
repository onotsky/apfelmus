//-----------------------------------------------------------------------
// <copyright file="Server.cs" company="ZSoft">
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
    /// Objekt das die Serverinformationen repräsentiert
    /// </summary>
    public class Server : INotifyPropertyChanged
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

        private string name;

        [XmlAttribute(AttributeName = "name")]
        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                OnPropertyChanged("Name");
            }
        }

        private string host;

        [XmlAttribute(AttributeName = "host")]
        public string Host
        {
            get { return host; }
            set
            {
                host = value;
                OnPropertyChanged("Host");
            }
        }

        private long lastSeen;

        [XmlAttribute(AttributeName = "lastseen")]
        public long LastSeen
        {
            get { return lastSeen; }
            set
            {
                lastSeen = value;
                OnPropertyChanged("LastSeen");
            }
        }

        private int port;

        [XmlAttribute(AttributeName = "port")]
        public int Port
        {
            get { return port; }
            set
            {
                port = value;
                OnPropertyChanged("Port");
            }
        }

        private int connectionTry;

        [XmlAttribute(AttributeName = "connectiontry")]
        public int ConnectionTry
        {
            get { return connectionTry; }
            set
            {
                connectionTry = value;
                OnPropertyChanged("ConnectionTry");
            }
        }

        private bool isConnected;

        public bool IsConnected
        {
            get { return isConnected; }
            set 
            { 
                isConnected = value;
                OnPropertyChanged("IsConnected");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
    }
}
