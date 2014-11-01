using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using System.Diagnostics;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

namespace PrecinctEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<CELPrecinct> CELPrecincts;
        public CELPrecinct selectedCELPrecinct;
        public VECTOR3 selectedCELPrecinctPoint;
        public bool FileLoaded = false;
        public string FilePath = null;
        public uint version, magic;

        public MainWindow()
        {
            InitializeComponent();

            CommandBindings.Add(new CommandBinding(ApplicationCommands.Open, OpenCommand_Executed, CommandCanExecute_Every));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Save, SaveCommand_Executed, CommandCanExecute_Loaded));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.SaveAs, SaveCommand_Executed, CommandCanExecute_Loaded));
        }

        private void OpenCommand_Executed(object target, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            if (openFile.ShowDialog().Value && File.Exists(openFile.FileName))
                bwLoadBinary(openFile.FileName);
        }

        private void SaveCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (!FileLoaded)
                return;
            bool result;
            if (e.Parameter == null || !bool.TryParse(e.Parameter.ToString(), out result))
                result = false;
            if (result)
            {
                SaveFileDialog saveFile = new SaveFileDialog();
                saveFile.OverwritePrompt = true;
                if (saveFile.ShowDialog().Value)
                    bwSaveBinary(saveFile.FileName);
            }
            else
            {
                if (MessageBox.Show("Перезаписать имеющийся файл?", "Save", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes)
                    return;
                bwSaveBinary(FilePath);
            }
        }

        private void CommandCanExecute_Every(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void CommandCanExecute_Loaded(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        public void bwLoadBinary(string _FilePath)
        {
            uint counts = 0;
            FilePath = _FilePath;
            using (BinaryReader br = new BinaryReader(new FileStream(FilePath, FileMode.Open, FileAccess.Read)))
            {
                version = br.ReadUInt32();
                counts = br.ReadUInt32();
                magic = br.ReadUInt32();

                mainProgressBar.Maximum = counts;
                mainProgressBar.Value = 0;
                CELPrecincts = new ObservableCollection<CELPrecinct>();
                for (int i = 0; i < counts; i++)
                {
                    CELPrecincts.Add(new CELPrecinct(br));
                    mainProgressBar.Value++;
                }
            }
            ListBox_Precincts.ItemsSource = CELPrecincts;
            FileLoaded = true;
            mainStatusBar.ItemsSource = new string[] { string.Format("Файл {0} успешно загружен. Количество точек: {1}", Path.GetFileName(FilePath), counts) };
        }

        public void bwSaveBinary(string _FilePath)
        {
            using (BinaryWriter bw = new BinaryWriter(new FileStream(_FilePath, FileMode.Create, FileAccess.Write)))
            {
                mainProgressBar.Maximum = CELPrecincts.Count;
                mainProgressBar.Value = 0;
                bw.Write(version);
                bw.Write(CELPrecincts.Count);
                bw.Write(magic);
                for (int i = 0; i < CELPrecincts.Count; i++)
                {
                    CELPrecincts[i].Write(bw);
                    mainProgressBar.Value++;
                }
            }
            mainStatusBar.ItemsSource = new string[] { string.Format("Файл {0} успешно сохранён. Количество точек: {1}", Path.GetFileName(_FilePath), CELPrecincts.Count) };
        }

        private void ListBox_Precincts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedCELPrecinct = ListBox_Precincts.SelectedItem as CELPrecinct;
        }

        private void DataGrid_Precincts_Points_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedCELPrecinctPoint = DataGrid_Precincts_Points.SelectedItem as VECTOR3;
        }

        private void ListBox_Precincts_Add_Click(object sender, RoutedEventArgs e)
        {
            if (!FileLoaded) return;
            CELPrecincts.Add(new CELPrecinct());
            mainStatusBar.ItemsSource = new string[] { string.Format("Precinct успешно добавлен. Количество precinct: {0}", CELPrecincts.Count) };
        }

        private void ListBox_Precincts_Clone_Click(object sender, RoutedEventArgs e)
        {
            if (selectedCELPrecinct == null) return;
            CELPrecincts.Add(selectedCELPrecinct.Clone());
            mainStatusBar.ItemsSource = new string[] { string.Format("Precinct успешно клонирован. Количество precinct: {1}", selectedCELPrecinct, CELPrecincts.Count) };
        }

        private void ListBox_Precincts_Remove_Click(object sender, RoutedEventArgs e)
        {
            if (selectedCELPrecinct == null) return;
            CELPrecincts.Remove(selectedCELPrecinct);
            mainStatusBar.ItemsSource = new string[] { string.Format("Precinct успешно удалён. Количество precinct: {1}", selectedCELPrecinct, CELPrecincts.Count) };
        }

        private void DataGrid_Precincts_Points_Add_Click(object sender, RoutedEventArgs e)
        {
            if (!FileLoaded) return;
            selectedCELPrecinct.m_aPoints.Add(new VECTOR3());
            mainStatusBar.ItemsSource = new string[] { string.Format("Point успешно добавлен. Количество point: {0}", selectedCELPrecinct.m_aPoints.Count) };
        }

        private void DataGrid_Precincts_Points_Clone_Click(object sender, RoutedEventArgs e)
        {
            if (selectedCELPrecinctPoint == null) return;
            selectedCELPrecinct.m_aPoints.Add(selectedCELPrecinctPoint.Clone());
            mainStatusBar.ItemsSource = new string[] { string.Format("Point успешно клонирован. Количество point: {1}", selectedCELPrecinctPoint, selectedCELPrecinct.m_aPoints.Count) };
        }

        private void DataGrid_Precincts_Points_Remove_Click(object sender, RoutedEventArgs e)
        {
            if (selectedCELPrecinctPoint == null) return;
            selectedCELPrecinct.m_aPoints.Remove(selectedCELPrecinctPoint);
            mainStatusBar.ItemsSource = new string[] { string.Format("Point успешно удалён. Количество oint: {1}", selectedCELPrecinct, selectedCELPrecinct.m_aPoints.Count) };
        }
    }
}
