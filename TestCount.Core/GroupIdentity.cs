using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestCount.Core
{
    public class GroupIdentity
    {
        public string IdentityKey
        {
            get;
            set;
        }

        public string DisplayName
        {
            get;
            set;
        }

        public override string ToString()
        {
            return this.DisplayName;
        }
    }
}
