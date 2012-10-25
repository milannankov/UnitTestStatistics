using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestCount.Core
{
    public interface ISourceControlProxy
    {
        string GetFileText(string filePath, int changesetVersion);

        string DiffFileVersions(string filePath, int firstVersion, int secondVersion);

        IList<FileChangeInfo> QueryHistory(QueryParameters parameters);
    }
}
