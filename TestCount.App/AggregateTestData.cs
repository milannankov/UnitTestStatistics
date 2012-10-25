using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestCount
{
    public class AggregateTestData
    {
        public string Comitter
        {
            get;
            set;
        }

        public int TestCountDelta
        {
            get;
            set;
        }

        public override string ToString()
        {
            return string.Format("{0} -> {1}", this.Comitter, this.TestCountDelta);
        }
    }
}
