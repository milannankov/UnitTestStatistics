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
    public partial class TestsPerMember : UserControl
    {
        private IList<AggregateTestData> groupedData;
        private IList<ProcessedFileChangeInfo> testData;

        // Using a DependencyProperty as the backing store for DataSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DataSourceProperty =
            DependencyProperty.Register("DataSource", typeof(object), typeof(TestsPerMember), new PropertyMetadata((new PropertyChangedCallback(OnDataSourceChanged))));

        public object DataSource
        {
            get { return (object)GetValue(DataSourceProperty); }
            set { SetValue(DataSourceProperty, value); }
        }

        private static void OnDataSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var testPerMemberView = d as TestsPerMember;
            var data = testPerMemberView.DataSource as IList<ProcessedFileChangeInfo>;

            if (data != null)
            {
                testPerMemberView.testData = data;
                testPerMemberView.CreateGroupedTestData(data);
            }
        }

        public TestsPerMember()
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
    }
}
