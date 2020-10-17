using Employees.Desktop.Helpers;
using Employees.Desktop.Models;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Employees.Desktop.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private ObservableCollection<Employee> employeeList;
        private string filePath;

        public MainViewModel()
        {
            this.OpenFileCommand = new RelayCommand(this.OpenFile);
        }

        public RelayCommand OpenFileCommand { get; set; }

        public ObservableCollection<Employee> EmployeeList
        {
            get { return this.employeeList; }
            set
            {
                this.employeeList = value;
                this.RaisePropertyChanged();
            }
        }

        public string FilePath
        {
            get { return this.filePath; }
            set
            {
                this.filePath = value;
                this.RaisePropertyChanged();
            }
        }

        private void OpenFile() 
        {
            var dialog = new OpenFileDialog();
            dialog.ShowDialog();

            this.FilePath = dialog.FileName;
            this.EmployeeTimeCompare();
        }

        private void EmployeeTimeCompare() 
        {
            // we read file data and map to object
            List<EmployeeBase> employeeDataList = EmployeeDataHelper.MapEmployeeBase(this.FilePath).ToList();

            // we sort and group the data by project
            IEnumerable<IGrouping<int, EmployeeBase>> result = employeeDataList.OrderByDescending(x => x.TotalDays).GroupBy(y => y.ProjectId);

            // we map the visual data and populate it to the observable collection
            this.EmployeeList = new ObservableCollection<Employee>(EmployeeDataHelper.MapTwoEmployeesWorkedInMultipleProjects(result, employeeDataList));

            // this method is obsolete since it is not used, still keeping the logic for exercise purpose and references if needed
            //this.EmployeeList = new ObservableCollection<Employee>(EmployeeDataHelper.MapEmployeeVisualData(result, employeeDataList));
        }
    }
}