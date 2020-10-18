using Employees.Desktop.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

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

        // TODO: Split the method on smaller parts
        /// <summary>
        /// Method to find two colleagues who worked together the most on common projects
        /// </summary>
        /// <param name="groupingResult"></param>
        /// <param name="employeeList"></param>
        /// <returns>Employee collection</returns>
        public static IEnumerable<Employee> MapTwoEmployeesWorkedInMultipleProjects(IEnumerable<IGrouping<int, EmployeeBase>> groupingResult, List<EmployeeBase> employeeList)
        {
            // to not return null and result in error in the visual three, could be handled better
            ObservableCollection<Employee> employeeCollection = new ObservableCollection<Employee>();

            // create pairs array with all possible combinations - need to improve logic
            int[] workerIds = employeeList.Select(e => e.Id).Distinct().ToArray();
            List<KeyValuePair<int, int>> pairList = new List<KeyValuePair<int, int>>();
            for (int i = 0; i < workerIds.Length; i++)
            {
                for (int j = 0; j < workerIds.Length; j++)
                {
                    pairList.Add(new KeyValuePair<int, int>(workerIds[i], workerIds[j]));
                }
            }

            // distinct the values based on equallity
            var distinctPairs = pairList.Where(x => x.Key != x.Value).ToList();
            List<EmployeesProjectCounter> employees = new List<EmployeesProjectCounter>();

            List<ProjectInfo> projectList = new List<ProjectInfo>();
            try
            {
                // create a list out of the groups
                foreach (var project in groupingResult)
                {
                    projectList.Add(new ProjectInfo
                    {
                        ProjectId = project.Key,
                        Employees = employeeList.Where(x => x.ProjectId.Equals(project.Key)).ToList()
                    });
                }

                // we have nested iterration to map the data correctly
                foreach (var pair in distinctPairs)
                {
                    foreach (var item in projectList)
                    {
                        if (item.Employees.Any(x => x.Id == pair.Key) && item.Employees.Any(x => x.Id == pair.Value))
                        {
                            EmployeeBase empl1 = employeeList.Where(x => x.Id == pair.Key && x.ProjectId == item.ProjectId).FirstOrDefault();
                            EmployeeBase empl2 = employeeList.Where(x => x.Id == pair.Value && x.ProjectId == item.ProjectId).FirstOrDefault();
                            // check if dates overlap to see if they actually met each other on the project
                            if (CheckIfDatesOverlap(empl1.DateFrom, empl1.DateTo, empl2.DateFrom, empl2.DateTo))
                            {
                                int totalDays = CalculateDaysTogether(empl1.DateFrom, empl1.DateTo, empl2.DateFrom, empl2.DateTo);
                                // we make a check if the object exists, we add project to the counter, else we create a new object
                                if (employees.Where(y => y.EmployeeId == pair.Key && y.Employee2Id == pair.Value).Count() >= 1)
                                {
                                    EmployeesProjectCounter employee = employees.Where(x => x.EmployeeId.Equals(pair.Key) && x.Employee2Id.Equals(pair.Value)).FirstOrDefault();
                                    employee.ProjectsTogether += 1; // could be removed
                                    employee.ProjectDays.Add(new ProjectDays { ProjectId = item.ProjectId, TotalDays = totalDays });
                                }
                                else
                                {
                                    employees.Add(new EmployeesProjectCounter
                                    {
                                        EmployeeId = pair.Key,
                                        Employee2Id = pair.Value,
                                        ProjectsTogether = 1, // could be removed
                                        ProjectDays = new List<ProjectDays> { new ProjectDays { ProjectId = item.ProjectId, TotalDays = totalDays } }
                                    });
                                }
                            }
                        }
                    }
                }

                List<EmployeesProjectCounter> orderLongestTime = employees.OrderByDescending(x => x.ProjectDays.Max(y => y.TotalDays)).ToList();
                EmployeesProjectCounter longestTimeOnProjectByColleagues = orderLongestTime[0];

                // getting the information about the two employees and their requisites [no necessity of lists, reusing old logic]
                List<EmployeeBase> employee1 = employeeList.Where(e => e.Id.Equals(longestTimeOnProjectByColleagues.EmployeeId) && e.ProjectId.Equals(longestTimeOnProjectByColleagues.ProjectDays.Max(y => y.ProjectId))).ToList();
                List<EmployeeBase> employee2 = employeeList.Where(e => e.Id.Equals(longestTimeOnProjectByColleagues.Employee2Id) && e.ProjectId.Equals(longestTimeOnProjectByColleagues.ProjectDays.Max(y => y.ProjectId))).ToList();

                // merge the lists
                var finalResults = employee1.Concat(employee2).ToLookup(e => e.ProjectId).Select(z => z.Aggregate((e1, e2) => new Employee
                {
                    Id = e1.Id,
                    EmployeeTwoId = e2.Id,
                    ProjectId = e1.ProjectId,
                    TotalDays = e1.TotalDays,
                    EmployeeTwoTotalDays = e2.TotalDays,
                    WorkedDays = Math.Abs(e1.TotalDays - e2.TotalDays)
                })).ToList();

                foreach (var empl in finalResults)
                {
                    employeeCollection.Add((Employee)empl);
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

        /// <summary>
        /// We get grouped data and map it to Employee object, which is the type of the observable collection.
        /// In some cases we are handling specific time cases as for "today", which is defined as NULL in the text data.
        /// We return enumerable of type EmployeeBase.
        /// </summary>
        [Obsolete]
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

        /* from here, move to <utils> */

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

        private static bool CheckIfDatesOverlap(DateTime startA, DateTime endA, DateTime startB, DateTime endB)
        {
            return startA < endB && startB < endA;
        }

        private static int CalculateDaysTogether(DateTime startA, DateTime endA, DateTime startB, DateTime endB) 
        {
            if (startA < startB && endA < endB) 
            {
                return Math.Abs((startB - endA).Days);
            }
            if (startA < startB && endA > endB)
            {
                return Math.Abs((startB - endB).Days);
            }
            if (startA > startB && endA < endB)
            {
                return Math.Abs((startA - endA).Days);
            }
            if (startA > startB && endA > endB)
            {
                return Math.Abs((startB - endB).Days);
            }

            return 1;
        }
    }
}
