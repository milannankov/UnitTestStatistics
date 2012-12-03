using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestCount.Core
{
    public class UserIdentity
    {
        public UserIdentity()
        {
            this.Groups = new List<GroupIdentity>();
        }

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

        public IList<GroupIdentity> Groups
        {
            get;
            private set;
        }

        public override string ToString()
        {
            return this.DisplayName;
        }
    }
}
