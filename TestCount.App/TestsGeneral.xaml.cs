using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Telerik.Windows.Controls.ChartView;
using TestCount.Core;

namespace TestCount.App
{
    /// <summary>
    /// Interaction logic for TestsPerMember.xaml
    /// </summary>
    public partial class TestsGeneral : UserControl
    {
        private IList<AggregateTestData> groupedData;
        private IList<ProcessedFileChangeInfo> testData;

        // Using a DependencyProperty as the backing store for DataSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DataSourceProperty =
            DependencyProperty.Register("DataSource", typeof(object), typeof(TestsGeneral), new PropertyMetadata((new PropertyChangedCallback(OnDataSourceChanged))));

        public object DataSource
        {
            get { return (object)GetValue(DataSourceProperty); }
            set { SetValue(DataSourceProperty, value); }
        }

        private static void OnDataSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var testPerMemberView = d as TestsGeneral;
            var data = testPerMemberView.DataSource as IList<ProcessedFileChangeInfo>;

            if (data != null)
            {
                testPerMemberView.testData = data;

                testPerMemberView.totalTestsTextBlock.Text = testPerMemberView.CalculateTotalTests().ToString();
                testPerMemberView.totalNewLinesTextBlock.Text = testPerMemberView.CalculateTotalNewLines().ToString();
                testPerMemberView.testsPerMemberTextBlock.Text = testPerMemberView.CalculateTestsPerMember().ToString();
                testPerMemberView.codeVsTestsTextBlock.Text = testPerMemberView.CalculateTestsPerLine().ToString();
            }
        }

        private int CalculateTotalTests()
        {
            return this.testData.Sum(fi => fi.AttributeChanges.TestCountChange);
        }

        private int CalculateTotalNewLines()
        {
            return this.testData.Sum(fi => fi.AttributeChanges.LinesCountChange);
        }

        private int CalculateTestsPerMember()
        {
            var totalTests = this.CalculateTotalTests();
            var members = this.testData.Select(fi => fi.Member).Distinct().Count();

            return totalTests / members;
        }

        private double CalculateTestsPerLine()
        {
            var totalTests = (double)this.CalculateTotalTests();
            var totalLines = (double)this.testData.Sum(fi => fi.AttributeChanges.LinesCountChange);

            return Math.Round(totalTests / totalLines,2);
        }

        public TestsGeneral()
        {
            InitializeComponent();
        }

        private void CreateGroupedTestData(IList<ProcessedFileChangeInfo> sourceData)
        {
            this.groupedData = sourceData
                .GroupBy(i => i.Member)
                .Select(g => new AggregateTestData() { Comitter = g.Key, TestCountDelta = g.Sum(h => h.AttributeChanges.TestCountChange) })
                .OrderBy(v => v.TestCountDelta)
                .ToList();

            this.layoutRoot.DataContext = null;
            this.layoutRoot.DataContext = this.groupedData;
        }

        private void CleartAndHideTree()
        {
           // this.changesetsTree.ItemsSource = null;
           // this.changesetsTree.Visibility = System.Windows.Visibility.Collapsed;
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
    }
}
