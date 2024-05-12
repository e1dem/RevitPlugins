using Autodesk.Revit.DB.Electrical;
using System.Resources;
using System.Windows;
using WireCalculationRevitPlugin.Properties;

namespace RevitPlugins.WireCalculationRevitPlugin.Windows
{
    /// <summary>
    /// Interaction logic for WireParamsWindow.xaml
    /// </summary>
    public partial class WireParamsWindow : Window
        
    {
        private static ResourceManager resourceManager = RevitPlugin.ResourceManager;

        public WireType GetSelectedWireType()
        {
            return (WireType)WireTypeComboBox.SelectedItem;
        }

        public WiringType GetSelectedWiringType()
        {
            return (WiringType)WiringTypeComboBox.SelectedItem;
        }

        public WireParamsWindow(IEnumerable<WireType> wireTypeList, IEnumerable<WiringType> wiringTypeList)
        {
            InitializeComponent();
            Title = resourceManager.GetString("WireParamsDialogTitle");

            foreach (WireType wireType in wireTypeList)
            {
                WireTypeComboBox.Items.Add(wireType);
            }

            WireTypeComboBox.DisplayMemberPath = "Name";
            if (wireTypeList != null && wireTypeList.Count() > 0)
            {
                WireTypeComboBox.SelectedIndex = 0;
            }

            WiringTypeComboBox.ItemsSource = wiringTypeList;
            if (wiringTypeList != null && wiringTypeList.Count() > 0)
            {
                WiringTypeComboBox.SelectedIndex = 0;
            }
        }

        private void btn_Submit_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
