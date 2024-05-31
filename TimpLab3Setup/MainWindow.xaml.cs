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

using WinForms = System.Windows.Forms;

namespace TimpLab3Setup
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string prog_name = "\\secur.exe";
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void BtnClickP1(object sender, RoutedEventArgs e)
        {
            TextWelcome.Visibility = Visibility.Collapsed;
            BtnInstall.Visibility = Visibility.Collapsed;
            FolderBox.Visibility = Visibility.Collapsed;
            BtnFolder.Visibility = Visibility.Collapsed;
            Main.Content = new Page1(System.IO.Directory.GetParent(FolderBox.Text).FullName, prog_name);
            TextWelcome = null;
            BtnInstall = null;
            FolderBox = null;
            BtnFolder = null;
        }

        private void BtnClickFolder(object sender, RoutedEventArgs e)
        {
            using (WinForms.FolderBrowserDialog folderDialog = new WinForms.FolderBrowserDialog())
            {
                folderDialog.ShowNewFolderButton = false;
                folderDialog.SelectedPath = System.AppDomain.CurrentDomain.BaseDirectory;
                WinForms.DialogResult result = folderDialog.ShowDialog();

                if (result == WinForms.DialogResult.OK)
                {
                    FolderBox.Text = folderDialog.SelectedPath + prog_name;
                }
            }
        }

        private void BtnClickTest(object sender, RoutedEventArgs e)
        {
            ServiceInstaller.Test();
        }

        private void FolderBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(((TextBox)sender).Text))
            {
                BtnInstall.IsEnabled = true;
            }
        }
    }
}
