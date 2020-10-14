using Employees.Desktop.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Employees.Desktop.Helpers
{
    public static class EmployeeDataHelper
    {
        /// <summary>
        /// We read the file and map the data to the object.
        /// In some cases we are handling specific time cases as for "today", which is defined as NULL in the text data.
        /// We return enumerable of type EmployeeBase.
        /// </summary>
        public static IEnumerable<EmployeeBase> MapEmployeeBase(string filePath) 
        {
            List<EmployeeBase> employeeList = new List<EmployeeBase>();
            string line;
            using (StreamReader reader = new StreamReader(filePath))
            {
                try
                {
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] data = line.Split(',');
                        employeeList.Add(new EmployeeBase
                        {
                            Id = int.Parse(data[0].Trim()),
                            ProjectId = int.Parse(data[1].Trim()),
                            DateFrom = DateParse(data[2].Trim()),
                            DateTo = DateParse(data[3].Trim()),
                            TotalDays = (DateParse(data[3].Trim()) - DateParse(data[2].Trim())).Days
                        });
                    }

                    return employeeList;
                }
                catch (Exception ex)
                {
                    // It is make like this to simulate logging, which will not interrupt the end user
                    Debug.WriteLine($"Error message: {ex.Message}\n Inner exception: {ex.InnerException}");
                }
            }

            return employeeList;
        }

        /// <summary>
        /// We get grouped data and map it to Employee object, which is the type of the observable collection.
        /// In some cases we are handling specific time cases as for "today", which is defined as NULL in the text data.
        /// We return enumerable of type EmployeeBase.
        /// </summary>
        public static IEnumerable<Employee> MapEmployeeVisualData(IEnumerable<IGrouping<int, EmployeeBase>> groupingResult, List<EmployeeBase> employeeList) 
        {
            ObservableCollection<Employee> employeeCollection = new ObservableCollection<Employee>();
                try
                {
                    foreach (var item in groupingResult)
                    {
                        List<EmployeeBase> firstTwoEmployees = employeeList.Where(x => x.ProjectId == item.Key).Take(2).ToList();
                        if (firstTwoEmployees.Count == 2)
                        {
                            employeeCollection.Add(new Employee
                            {
                                Id = firstTwoEmployees[0].Id,
                                EmployeeTwoId = firstTwoEmployees[1].Id,
                                ProjectId = item.Key,
                                WorkedDays = Math.Abs(firstTwoEmployees[1].TotalDays - firstTwoEmployees[0].TotalDays)
                            });
                        }
                    }

                    return employeeCollection;
                }
                catch (Exception ex)
                {
                    // It is make like this to simulate logging, which will not interrupt the end user
                    Debug.WriteLine($"Error message: {ex.Message}\n Inner exception: {ex.InnerException}");
                }

            return employeeCollection;
        }

        private static DateTime DateParse(string date) 
        {
            DateTime dateValue;
            // to handle the "today" state
            if (date.Equals("NULL")) 
            {
                return DateTime.Today;
            }
            // to handle eventual user input case, with different culture
            if (DateTime.TryParse(date, CultureInfo.CurrentCulture, DateTimeStyles.None, out dateValue))
            {
                return dateValue;
            }

            // to handle if formatting and parsing are done from various cultures
            dateValue = DateTime.Parse(date, CultureInfo.InvariantCulture);

            return dateValue;
        }
    }
}
