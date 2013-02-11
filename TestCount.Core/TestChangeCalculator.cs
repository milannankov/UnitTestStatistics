using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.TeamFoundation.VersionControl.Client;
using System.IO;

namespace TestCount.Core
{
    public class TestChangeCalculator
    {
        private ISourceControlProxy sourceControlProxy;

        public TestChangeCalculator(ISourceControlProxy sourceControlProxy)
        {
            this.sourceControlProxy = sourceControlProxy;
        }

        public List<ProcessedFileChangeInfo> GetTestMethodData(IList<FileChangeInfo> fileChanges)
        {
            var globalTestInfo = new List<ProcessedFileChangeInfo>();
            var fileChangesPerFile = fileChanges.GroupBy(fc => fc.ItemPath).ToList();

            foreach (var groupFileChangeItem in fileChangesPerFile)
            {
                var itemChanges = groupFileChangeItem.ToList();
                var testInfos = this.GetTestMethodInfosForFile(groupFileChangeItem.Key, itemChanges);

                globalTestInfo.AddRange(testInfos);
            }

            var filteredInfo = globalTestInfo.Where(i => i.AttributeChanges.TestCountChange != 0).ToList();

            return filteredInfo;
        }


        private IList<FileChangeInfo> unhandledChangeInfos = new List<FileChangeInfo>();

        private IEnumerable<ProcessedFileChangeInfo> GetTestMethodInfosForFile(string filePath, IList<FileChangeInfo> changeInfos)
        {
            var testInfos = new List<ProcessedFileChangeInfo>();

            foreach (var changeInfoItem in changeInfos)
            {
                var info = new ProcessedFileChangeInfo();
                info.Member = changeInfoItem.Member;
                info.ChangesetId = changeInfoItem.ChangesetId;
                info.ItemPath = changeInfoItem.ItemPath;

                var previousVersionDelta = -1;

                if ((changeInfoItem.ChangeType & ChangeType.Branch) == ChangeType.Branch)
                    previousVersionDelta = 0;

                var cleanedChangeType = RemoveIrrelevantFlags(changeInfoItem);

                if (cleanedChangeType == 0)
                {
                    var fileText = this.sourceControlProxy.GetFileText(changeInfoItem.ItemPath, changeInfoItem.ChangesetId);
                    info.AttributeChanges = this.GetChangedAttributes(fileText);
                }
                else if (cleanedChangeType == (ChangeType.None | ChangeType.SourceRename))
                {
                    break;
                }
                else if (cleanedChangeType == (ChangeType.Delete | ChangeType.SourceRename))
                {
                    var fileText = this.sourceControlProxy.GetFileText(changeInfoItem.ItemPath, changeInfoItem.ChangesetId + previousVersionDelta);
                    var attributeChanges = this.GetChangedAttributes(fileText);
                    info.AttributeChanges = new FileAttributes() { LinesCountChange = attributeChanges.LinesCountChange, TestCountChange = attributeChanges.TestCountChange * -1 };
                }
                else if (cleanedChangeType == (ChangeType.Delete))
                {
                    var fileText = this.sourceControlProxy.GetFileText(changeInfoItem.ItemPath, changeInfoItem.ChangesetId + previousVersionDelta);
                    var attributeChanges = this.GetChangedAttributes(fileText);
                    info.AttributeChanges = new FileAttributes() { LinesCountChange = attributeChanges.LinesCountChange, TestCountChange = attributeChanges.TestCountChange * -1 };
                }
                else if (cleanedChangeType == (ChangeType.Undelete))
                {
                    var fileText = this.sourceControlProxy.GetFileText(changeInfoItem.ItemPath, changeInfoItem.ChangesetId);
                    info.AttributeChanges = this.GetChangedAttributes(fileText);
                }
                else if (cleanedChangeType == (ChangeType.Delete | ChangeType.Rename))
                {
                    break;
                }
                else if (cleanedChangeType == (ChangeType.Rename | ChangeType.SourceRename | ChangeType.Edit))
                {
                    var fileText = this.sourceControlProxy.GetFileText(changeInfoItem.ItemPath, changeInfoItem.ChangesetId);
                    info.AttributeChanges = this.GetChangedAttributes(fileText);
                }
                else if (cleanedChangeType == ChangeType.Rename)
                {
                    var fileText = this.sourceControlProxy.GetFileText(changeInfoItem.ItemPath, changeInfoItem.ChangesetId);
                    info.AttributeChanges = this.GetChangedAttributes(fileText);
                }
                else if (cleanedChangeType == (ChangeType.Rename | ChangeType.Edit))
                {
                    var fileText = this.sourceControlProxy.GetFileText(changeInfoItem.ItemPath, changeInfoItem.ChangesetId);
                    info.AttributeChanges = this.GetChangedAttributes(fileText);
                }
                else if (cleanedChangeType == ChangeType.Add)
                {
                    var fileText = this.sourceControlProxy.GetFileText(changeInfoItem.ItemPath, changeInfoItem.ChangesetId);
                    info.AttributeChanges = this.GetChangedAttributes(fileText);
                }
                else if (cleanedChangeType == (ChangeType.Add | ChangeType.Edit))
                {
                    var fileText = this.sourceControlProxy.GetFileText(changeInfoItem.ItemPath, changeInfoItem.ChangesetId);
                    info.AttributeChanges = this.GetChangedAttributes(fileText);
                }
                else if (cleanedChangeType == (ChangeType.Add | ChangeType.Edit | ChangeType.SourceRename))
                {
                    var previousVersion = changeInfoItem.ChangesetId + previousVersionDelta;
                    var currentVersion = changeInfoItem.ChangesetId;

                    var fileDiff = this.sourceControlProxy.DiffFileVersions(changeInfoItem.ItemPath, previousVersion, currentVersion);

                    info.AttributeChanges = this.GetChangedAttributesFromDiff(fileDiff);
                }
                else if (cleanedChangeType == (ChangeType.Edit))
                {
                    var previousVersion = changeInfoItem.ChangesetId + previousVersionDelta;
                    var currentVersion = changeInfoItem.ChangesetId;

                    var fileDiff = this.sourceControlProxy.DiffFileVersions(changeInfoItem.ItemPath, previousVersion, currentVersion);

                    info.AttributeChanges = this.GetChangedAttributesFromDiff(fileDiff);
                }
                else if (cleanedChangeType == (ChangeType.Encoding))
                {
                    // no need to do enything
                }
                else
                {
                    this.unhandledChangeInfos.Add(changeInfoItem);
                }

                testInfos.Add(info);
            }

            return testInfos;
        }

        private ChangeType RemoveIrrelevantFlags(FileChangeInfo changeInfoItem)
        {
            var cleanedChangeType =
                                   (changeInfoItem.ChangeType & ~ChangeType.Merge) &
                                   (changeInfoItem.ChangeType & ~ChangeType.Branch) &
                                   (changeInfoItem.ChangeType & ~ChangeType.Encoding) &
                                   (changeInfoItem.ChangeType & ~ChangeType.Rollback);

            return cleanedChangeType;
        }

        private FileAttributes GetChangedAttributesFromDiff(string fileText)
        {
            var attributes = new FileAttributes();
            string line;

            using (var r = new StringReader(fileText))
            {
                while ((line = r.ReadLine()) != null)
                {
                    if (line.StartsWith("+"))
                    {
                        attributes.LinesCountChange++;

                        if (line.Contains(@"[TestMethod]"))
                        {
                            attributes.TestCountChange++;
                        }
                    }

                    if (line.StartsWith("-"))
                    {
                        attributes.LinesCountChange--;

                        if (line.Contains(@"[TestMethod]"))
                        {
                            attributes.TestCountChange--;
                        }
                    }
                }
            }

            return attributes;
        }

        private FileAttributes GetChangedAttributes(string fileText)
        {
            var attributes = new FileAttributes();
            string line;

            using (var r = new StringReader(fileText))
            {
                while ((line = r.ReadLine()) != null)
                {
                    if (line.Contains(@"[TestMethod]"))
                    {
                        attributes.TestCountChange++;
                    }

                    attributes.LinesCountChange++;
                }
            }

            return attributes;
        }
    }
}
