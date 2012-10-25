using System;
using System.Linq;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace TestCount.Core
{
    public class FileChangeInfo
    {
        public string ItemPath
        {
            get;
            set;
        }

        public int ChangesetId
        {
            get;
            set;
        }

        public string Member
        {
            get;
            set;
        }

        public ChangeType ChangeType
        {
            get;
            set;
        }

        public override string ToString()
        {
            return "ItemPath -> " + this.ItemPath;
        }
    }
}
