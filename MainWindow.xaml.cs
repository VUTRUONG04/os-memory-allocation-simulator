using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OSMemoryAllocationSimulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnTabSimulation_Click(
            object sender, RoutedEventArgs e)
        {
            ViewSimulation.Visibility = Visibility.Visible;
            ViewEvaluation.Visibility = Visibility.Collapsed;
            BtnTabSimulation.Style = (Style)FindResource("BtnPrimary");
            BtnTabEvaluation.Style = (Style)FindResource("BtnSecondary");
        }

        private void BtnTabEvaluation_Click(
            object sender, RoutedEventArgs e)
        {
            ViewSimulation.Visibility = Visibility.Collapsed;
            ViewEvaluation.Visibility = Visibility.Visible;
            BtnTabSimulation.Style = (Style)FindResource("BtnSecondary");
            BtnTabEvaluation.Style = (Style)FindResource("BtnPrimary");
        }
    }
}