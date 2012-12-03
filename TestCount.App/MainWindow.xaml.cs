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

        private TfsTeamProjectCollection teamProjectCollection;
        private VersionControlServer versionControl;
        private TestChangeCalculator infoProvider;
        private List<ProcessedFileChangeInfo> testData;
        private IIdentityManagementService identityService;
        private ISourceControlProxy sourceControlProxy;

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(MainWindow_Loaded);
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.teamProjectCollection = new TfsTeamProjectCollection(new Uri(TeamProjectCollectionPath));
            this.teamProjectCollection.Authenticate();

            this.teamProjectCollection.EnsureAuthenticated();
            this.versionControl = this.teamProjectCollection.GetService<VersionControlServer>();
            this.sourceControlProxy = new TfsProxy(this.versionControl);
            this.infoProvider = new TestChangeCalculator(this.sourceControlProxy);
            this.identityService = this.teamProjectCollection.GetService<IIdentityManagementService>();

            //this.pickerFrom.SelectedValue = new DateTime(2012, 10, 19, 7, 0, 0);
            this.pickerFrom.SelectedValue = new DateTime(2012, 11, 1, 23, 0, 0);
            //this.pickerTo.SelectedValue = new DateTime(2012, 11, 28, 23, 0, 0);
            this.pickerTo.SelectedValue = new DateTime(2012, 11, 10, 23, 0, 0);
            this.pathTextBox.Text = "$/WPF_Scrum/Current/Core/Data";

            this.testsPerGroupView.IdentityService = new TfsIdentityServiceProxy(this.identityService);

            var iden = this.testsPerGroupView.IdentityService.GetUserIdentities();
            iden = this.testsPerGroupView.IdentityService.GetUserIdentities();

            //2.Read the group represnting the root node
            var rootIdentity = this.identityService.ReadIdentity(IdentitySearchFactor.AccountName, @"[WPF_Scrum]\Contributors", MembershipQuery.Direct, ReadIdentityOptions.None);

            //3.Recursively parse the members of the group
            DisplayGroupTree(this.identityService, rootIdentity, 0);
        }

         private static void DisplayGroupTree(IIdentityManagementService ims, TeamFoundationIdentity node, 
             int level)
         {
             DisplayNode(node, level);
  
             if (!node.IsContainer)
                 return;
  
             TeamFoundationIdentity[] nodeMembers = ims.ReadIdentities(node.Members, MembershipQuery.Direct, 
                 ReadIdentityOptions.None);
  
             int newLevel = level + 1;
             foreach (TeamFoundationIdentity member in nodeMembers)
                 DisplayGroupTree(ims, member, newLevel);
         }
  
         private static void DisplayNode(TeamFoundationIdentity node, int level)
         {
             for (int tabCount = 0; tabCount < level; tabCount++) Console.Write("\t");
  
             Console.WriteLine(node.DisplayName);
         }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (this.pickerFrom.SelectedDate == null || this.pickerTo.SelectedDate == null)
            {
                MessageBox.Show("Choose dates, please.");

                return;
            }

            this.LoadData();
            //this.RefreshUI();
        }

        private void LoadData()
        {
            var fileChanges = GetFileChanges();
            this.CalculateTestData(fileChanges);
            //this.CreateGroupedTestData();

            this.DataContext = null;
            this.DataContext = this.testData;
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
                .OrderByDescending(i => i.AttributeChanges.TestCountChange).ToList();
        }

        private void RefreshUI()
        {
            this.DataContext = null;
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
                    ReadIdentityOptions.ExtendedProperties);

                var infoAsCSVTest = string.Format(CultureInfo.InvariantCulture, "{0}, {1}, {2}, {3}", tfsIdentity.DisplayName, testMethodInfo.ItemPath, testMethodInfo.ChangesetId, testMethodInfo.AttributeChanges.TestCountChange);

                //DisplayGroupTree2(this.identityService, tfsIdentity, 0);

                dataBuild.AppendLine(infoAsCSVTest);
            }

            File.WriteAllText(fileName, dataBuild.ToString());
        }
    }
}
