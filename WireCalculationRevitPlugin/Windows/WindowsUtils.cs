using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WireCalculationRevitPlugin.Properties;

namespace RevitPlugins.WireCalculationRevitPlugin.Windows
{
    public class WindowsUtils
    {
        private static ResourceManager resourceManager = RevitPlugin.ResourceManager;

        public static MessageBoxResult ShowWarningMessage(String? messageBoxText)
        {
            return MessageBox.Show(messageBoxText, resourceManager.GetString("WarningMessageBoxTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
