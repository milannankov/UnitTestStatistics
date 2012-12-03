using System;
using System.Linq;
using System.IO;

namespace TestCount.Core
{
    public class ProcessedFileChangeInfo : FileChangeInfo
    {
        //public string Member
        //{
        //    get;
        //    set;
        //}

        //public string ItemPath
        //{
        //    get;
        //    set;
        //}

        public string FileName
        {
            get
            {
                return Path.GetFileName(this.ItemPath);
            }
        }

        //public int ChangesetId
        //{
        //    get;
        //    set;
        //}

        public FileAttributes AttributeChanges
        {
            get;
            set;
        }

        public override string ToString()
        {
            return "Comitter -> " + this.Member + " | Count -> " + this.AttributeChanges.TestCountChange.ToString();
        }
    }
}
