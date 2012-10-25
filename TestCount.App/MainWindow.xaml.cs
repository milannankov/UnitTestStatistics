using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.Win32;
using Telerik.Windows.Controls.ChartView;
using TestCount.Core;
using System.Windows.Controls.Primitives;

namespace TestCount.App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string TeamProjectCollectionPath = "http://tfs:8082/defaultcollection";
        //private const string TeamProjectCollectionPath = "http://192.168.1.12:8080/tfs/defaultcollection";

        private TfsTeamProjectCollection teamProjectCollection;
        private VersionControlServer versionControl;
        private TestChangeCalculator infoProvider;
        private List<AggregateTestData> groupedData;
        private List<TestChangeInfo> testData;
        private ISourceControlProxy sourceControlProxy;

        public MainWindow()
        {
            InitializeComponent();
            
            this.Loaded += new RoutedEventHandler(MainWindow_Loaded);
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //var credencial = new NetworkCredential(@"TestUser1", "Passw0rd", "WIN-GS9GMUJITS8");
            //var credencial = new NetworkCredential(@"TestUser1");
            //his.teamProjectCollection = new TfsTeamProjectCollection(new Uri(TeamProjectCollectionPath), credencial);
            this.teamProjectCollection = new TfsTeamProjectCollection(new Uri(TeamProjectCollectionPath));
            //this.teamProjectCollection = new TfsTeamProjectCollection(new Uri(TeamProjectCollectionPath), new UICredentialsProvider());
            this.teamProjectCollection.Authenticate();

            this.teamProjectCollection.EnsureAuthenticated();
            this.versionControl = this.teamProjectCollection.GetService<VersionControlServer>();
            this.sourceControlProxy = new TfsProxy(this.versionControl);
            this.infoProvider = new TestChangeCalculator(this.sourceControlProxy);

            this.pickerFrom.SelectedValue = new DateTime(2012, 6, 9, 7, 0, 0);
            this.pickerTo.SelectedValue = new DateTime(2012, 10, 16, 23, 0, 0);
            this.pathTextBox.Text = "$/WPF_Scrum/Current/";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (this.pickerFrom.SelectedDate == null || this.pickerTo.SelectedDate == null)
            {
                MessageBox.Show("Choose dates, please.");

                return;
            }

            this.LoadData();
            this.RefreshUI();
        }

        private void LoadData()
        {
            var fileChanges = GetFileChanges();
            this.CalculateTestData(fileChanges);
            this.CreateGroupedTestData();
        }

        private IList<FileChangeInfo> GetFileChanges()
        {
            var parameters = new QueryParameters(this.pathTextBox.Text, this.pickerFrom.SelectedDate.Value, this.pickerTo.SelectedDate.Value);
            parameters.FileExtension = ".cs";
            var fileChanges = this.sourceControlProxy.QueryHistory(parameters);

            return fileChanges;
        }

        private void CalculateTestData(IList<FileChangeInfo> fileChanges)
        {
            this.testData = 
                this.infoProvider
                .GetTestMethodData(fileChanges)
                .OrderByDescending(i => i.TestCountChage).ToList();
        }

        private void CreateGroupedTestData()
        {
            this.groupedData = this.testData
                .GroupBy(i => i.Member)
                .Select(g => new AggregateTestData() { Comitter = g.Key, TestCountDelta = g.Sum(h => h.TestCountChage) })
                .OrderBy(v => v.TestCountDelta)
                .ToList();
        }

        private void RefreshUI()
        {
            this.DataContext = null;
            this.DataContext = this.groupedData;
        }

        private void CleartAndHideTree()
        {
            this.changesetsTree.ItemsSource = null;
            this.changesetsTree.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void LoadChangesetDataAndShowTree(string comitter)
        {
            var changes = this.GetChangesForUser(comitter);

            var detailsWindows = new DetailsWindow();
            detailsWindows.DataContext = changes;

            detailsWindows.ShowDialog();

            //this.changesetsTree.ItemsSource = changes;
            //this.changesetsTree.Visibility = System.Windows.Visibility.Visible;
        }

        private IEnumerable GetChangesForUser(string user)
        {
            return
                this.testData
                .Where(changeInfo => changeInfo.Member == user);
                //.GroupBy(changeInfo => changeInfo.ChangesetId)
                //.OrderBy(changeInfoGroup => changeInfoGroup.Key)
                //.Select(group => new { Changeset = group.Key, Items = group.OrderBy(ti => ti.ItemPath).ToList() })
                //.ToList();
        }

        private void ChartSelectionBehavior_SelectionChanged(object sender, Telerik.Windows.Controls.ChartView.ChartSelectionChangedEventArgs e)
        {
            var selectionBehavior = sender as ChartSelectionBehavior;

            if (selectionBehavior.Chart.SelectedPoints.Count == 0)
            {
                this.CleartAndHideTree();
            }
            else
            {
                var data = e.AddedPoints[0].DataItem as AggregateTestData;

                this.LoadChangesetDataAndShowTree(data.Comitter);
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog();
            dialog.FileName = "testData";
            dialog.DefaultExt = ".csv";
            dialog.Filter = "CSV documents (.csv)|*.csv";

            // Show save file dialog box
            var result = dialog.ShowDialog();

            if (result == true)
            {
                this.WriteDataToFile(dialog.FileName);
            }
        }

        private void WriteDataToFile(String fileName)
        {
            var dataBuild = new StringBuilder();
            var exportFile = File.Open(fileName, FileMode.Create);
            exportFile.Close();

            var headerInfoAsCSVTest = "Comitter, ItemPath, ChangesetId, TestMethodCountChange";
            dataBuild.AppendLine(headerInfoAsCSVTest);
            var identityService = this.versionControl.TeamProjectCollection.GetService<IIdentityManagementService>();

            foreach (var testMethodInfo in this.testData)
            {
                var tfsIdentity = identityService.ReadIdentity(
                    IdentitySearchFactor.AccountName,
                    testMethodInfo.Member,
                    MembershipQuery.Expanded,
                    ReadIdentityOptions.None);

                var infoAsCSVTest = string.Format(CultureInfo.InvariantCulture, "{0}, {1}, {2}, {3}", tfsIdentity.DisplayName, testMethodInfo.ItemPath, testMethodInfo.ChangesetId, testMethodInfo.TestCountChage);

                dataBuild.AppendLine(infoAsCSVTest);
            }

            File.WriteAllText(fileName, dataBuild.ToString());
        }
    }
}
