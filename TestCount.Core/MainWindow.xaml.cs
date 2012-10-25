using System;
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

namespace TestCount
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
        private TestMethodInfoProvider infoProvider;
        private List<GroupedTestData> groupedData;
        private List<TestChangeInfo> testData;
        private TfsInfoProvider provider;

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
            this.provider = new TfsInfoProvider(this.versionControl);
            this.infoProvider = new TestMethodInfoProvider(this.provider);

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

            this.GetData();
            this.RefreshUI();
        }

        private void GetData()
        {
            var parameters = new QueryHistoryParameters(this.pathTextBox.Text, this.pickerFrom.SelectedDate.Value, this.pickerTo.SelectedDate.Value);
            parameters.FileExtension = ".cs";
            var fileChanges = this.provider.QueryHistory(parameters);

            this.testData = this.infoProvider.GetTestMethodData(fileChanges).OrderByDescending(i => i.TestCountChage).ToList();
            this.groupedData = this.testData
                .GroupBy(i => i.Comitter)
                .Select(g => new GroupedTestData() { Comitter = g.Key, TestCountDelta = g.Sum(h => h.TestCountChage) })
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
            var changes = this.testData
                .Where(d => d.Comitter == comitter)
                .GroupBy(f => f.ChangesetId)
                .OrderBy(hhh => hhh.Key)
                .Select(bb => new { Changeset = bb.Key, Items = bb.OrderBy(ti => ti.ItemPath).ToList() })
                .ToList();

            this.changesetsTree.ItemsSource = changes;

            this.changesetsTree.Visibility = System.Windows.Visibility.Visible;
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
                var data = e.AddedPoints[0].DataItem as GroupedTestData;

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
            var ggg = File.Open(fileName, FileMode.Create);
            ggg.Close();
            //FileInfo fff = new FileInfo();
            //File.
            var headerInfoAsCSVTest = "Comitter, ItemPath, ChangesetId, TestMethodCountChange";
            dataBuild.AppendLine(headerInfoAsCSVTest);
            var identityService = this.versionControl.TeamProjectCollection.GetService<IIdentityManagementService>();

            foreach (var testMethodInfo in this.testData)
            {
                var tfsIdentity = identityService.ReadIdentity(
       IdentitySearchFactor.AccountName,
       testMethodInfo.Comitter,
       MembershipQuery.Expanded,
       ReadIdentityOptions.None);

                var infoAsCSVTest = string.Format(CultureInfo.InvariantCulture, "{0}, {1}, {2}, {3}", tfsIdentity.DisplayName, testMethodInfo.ItemPath, testMethodInfo.ChangesetId, testMethodInfo.TestCountChage);



                dataBuild.AppendLine(infoAsCSVTest);
            }

            File.WriteAllText(fileName, dataBuild.ToString());
        }
    }
}
