using System;
using System.Linq;

namespace TestCount.Core
{
    public class QueryParameters
    {
        public string Path
        {
            get;
            private set;
        }

        public DateTime From
        {
            get;
            private set;
        }

        public DateTime To
        {
            get;
            private set;
        }

        public string FileExtension
        {
            get;
            set;
        }

        public QueryParameters(string path, DateTime from, DateTime to)
        {
            this.Path = path;
            this.From = from;
            this.To = to;
        }
    }
}
