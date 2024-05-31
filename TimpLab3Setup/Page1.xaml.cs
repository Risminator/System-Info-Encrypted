using System;
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

namespace TimpLab3Setup
{
    /// <summary>
    /// Логика взаимодействия для Page1.xaml
    /// </summary>
    public partial class Page1 : Page
    {
        public Page1(string dir_path, string prog_name)
        {
            InitializeComponent();
            StartInstall(dir_path, prog_name);
        }
        private async void StartInstall(string dir_path, string prog_name)
        {
            await Install.MyInstall(dir_path, prog_name);
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
            TextInstall.Visibility = Visibility.Collapsed;
            TextInstall = null;
            pbStatus.Visibility = Visibility.Collapsed;
            pbStatus = null;
            mainWindow.Main.Content = new Page2();
        }
    }
}
