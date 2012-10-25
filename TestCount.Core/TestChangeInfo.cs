using System;
using System.Linq;
using System.IO;

namespace TestCount.Core
{
    public class TestChangeInfo
    {
        public string Member
        {
            get;
            set;
        }

        public string ItemPath
        {
            get;
            set;
        }

        public string FileName
        {
            get
            {
                return Path.GetFileName(this.ItemPath);
            }
        }

        public int ChangesetId
        {
            get;
            set;
        }

        public int TestCountChage
        {
            get;
            set;
        }

        public override string ToString()
        {
            return "Comitter -> " + this.Member + " | Count -> " + this.TestCountChage.ToString();
        }
    }
}
