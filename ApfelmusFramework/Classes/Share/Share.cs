//-----------------------------------------------------------------------
// <copyright file="Share.cs" company="ZSoft">
//     Copyright (c) ZSoft.
// </copyright>
// <author>daredevil</author>
//-----------------------------------------------------------------------
namespace ApfelmusFramework.Classes.Share
{
    using System;
    using System.ComponentModel;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Serialization;

    /// <summary>
    /// Eine einzelne freigegebene Datei (share.xml): Name, Groesse, Pruefsumme, Prioritaet sowie
    /// Statistik (Anfragen, Suchtreffer, zuletzt angefragt). Wird im "Mein Share"-Tab gelistet.
    /// INotifyPropertyChanged haelt die Bindings aktuell.
    /// </summary>
    public class Share : INotifyPropertyChanged
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

        private string filename;

        [XmlAttribute(AttributeName = "filename")]
        public string FileName
        {
            get { return filename; }
            set
            {
                filename = value;
                OnPropertyChanged("FileName");
            }
        }

        private string shortfilename;

        [XmlAttribute(AttributeName = "shortfilename")]
        public string ShortFileName
        {
            get { return shortfilename; }
            set
            {
                shortfilename = value;
                OnPropertyChanged("ShortFileName");
            }
        }

        private long size;

        [XmlAttribute(AttributeName = "size")]
        public long Size
        {
            get { return size; }
            set
            {
                size = value;
                OnPropertyChanged("Size");
            }
        }

        private string checksum;

        [XmlAttribute(AttributeName = "checksum")]
        public string CheckSum
        {
            get { return checksum; }
            set
            {
                checksum = value;
                OnPropertyChanged("CheckSum");
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

        private long lastAsked;

        [XmlAttribute(AttributeName = "lastasked")]
        public long LastAsked
        {
            get { return lastAsked; }
            set
            {
                lastAsked = value;
                OnPropertyChanged("LastAsked");
            }
        }

        private int askcount;

        [XmlAttribute(AttributeName = "askcount")]
        public int AskCount
        {
            get { return askcount; }
            set
            {
                askcount = value;
                OnPropertyChanged("AskCount");
            }
        }

        private int searchcount;

        [XmlAttribute(AttributeName = "searchcount")]
        public int SearchCount
        {
            get { return searchcount; }
            set
            {
                searchcount = value;
                OnPropertyChanged("SearchCount");
            }
        }

        private string path;

        public string Path
        {
            get
            {
                return path;
            }
            set
            {
                path = value;
                OnPropertyChanged("Path");
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
