using System.Windows;
using TwinCAT.TypeSystem;

namespace SimpleAdsClient
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private MainWindowViewModel viewModel;

		public MainWindow()
		{
			InitializeComponent();

			DataContext = viewModel = new MainWindowViewModel();
		}

		private void treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			viewModel.UpdateSelection(e.NewValue as DynamicSymbol);
		}
	}
}