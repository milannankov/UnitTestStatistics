using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Common;

namespace TestCount.Core
{
    public class TfsProxy : ISourceControlProxy
    {
        private readonly VersionControlServer versionControl;

        public TfsProxy(VersionControlServer versionControl)
        {
            this.versionControl = versionControl;
        }

        public string GetFileText(string filePath, int changeSetVersion)
        {
            var fileText = string.Empty;

            try
            {
                this.versionControl.DownloadFile(filePath, 0, new ChangesetVersionSpec(changeSetVersion), "temp.hhh");
                fileText = File.ReadAllText("temp.hhh");
            }
            catch
            {

            }

            return fileText;
        }

        public IList<FileChangeInfo> QueryHistory(QueryParameters parameters)
        {
            var queryPath = parameters.Path;
            var queryDateFrom = new DateVersionSpec(parameters.From);
            var queryDateTo = new DateVersionSpec(parameters.To);

            var query = versionControl.QueryHistory(queryPath, VersionSpec.Latest, 0, RecursionType.Full, null, queryDateFrom, queryDateTo, int.MaxValue, true, false, false, true);
            var changesetsInChronologicalOrder = query.OfType<Changeset>().OrderBy(cs => cs.ChangesetId).ToList();

            var fileChanges = this.GetFileChanges(changesetsInChronologicalOrder, parameters);

            return fileChanges;
        }

        private Func<Change, bool> GetChangeFilter(QueryParameters parameters)
        {
            if (string.IsNullOrEmpty(parameters.FileExtension))
            {
                return (Change a) => a.Item.ItemType != ItemType.Folder;
            }
            else
            {
                return (Change a) => a.Item.ItemType != ItemType.Folder && a.Item.ServerItem.EndsWith(parameters.FileExtension);
            }
        }

        private IList<FileChangeInfo> GetFileChanges(List<Changeset> changesets, QueryParameters parameters)
        {
            var fileChanges = new List<FileChangeInfo>();

            foreach (var changesetItem in changesets)
            {
                var filterPredicate = this.GetChangeFilter(parameters);
                var validItemChanges = changesetItem.Changes.Where(filterPredicate);

                foreach (var changeItem in validItemChanges)
                {
                    var newInfo = CreateFileChange(changesetItem, changeItem);

                    fileChanges.Add(newInfo);
                }
            }

            return fileChanges;
        }
  
        private static FileChangeInfo CreateFileChange(Changeset changesetItem, Change changeItem)
        {
            var newInfo = new FileChangeInfo();

            newInfo.ChangesetId = changesetItem.ChangesetId;
            newInfo.ChangeType = changeItem.ChangeType;
            newInfo.Member = changesetItem.Committer;
            newInfo.ItemPath = changeItem.Item.ServerItem;

            return newInfo;
        }

        public string DiffFileVersions(string filePath, int firstVersion, int secondVersion)
        {
            var server = this.versionControl;
            var firstChangeset = new ChangesetVersionSpec(firstVersion);
            var secondChangeset = new ChangesetVersionSpec(secondVersion);

            var item1 = Difference.CreateTargetDiffItem(server, filePath, firstChangeset, 0, firstChangeset);
            var item2 = Difference.CreateTargetDiffItem(server, filePath, secondChangeset, 0, secondChangeset);

            var memoryStream = new MemoryStream();
            var writer = new StreamWriter(memoryStream);

            Difference.DiffFiles(server, item1, item2, new DiffOptions() { OutputType = DiffOutputType.Unified, StreamWriter = writer }, "TEST", true);

            writer.Flush();
            memoryStream.Position = 0;
            var reader = new StreamReader(memoryStream);
            var result = reader.ReadToEnd();

            return result;
        }
    }
}
