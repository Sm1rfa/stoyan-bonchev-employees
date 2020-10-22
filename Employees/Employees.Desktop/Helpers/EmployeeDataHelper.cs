using Employees.Desktop.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
                            DateFrom = Utils.DateParse(data[2].Trim()),
                            DateTo = Utils.DateParse(data[3].Trim()),
                            TotalDays = (Utils.DateParse(data[3].Trim()) - Utils.DateParse(data[2].Trim())).Days
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

            // create pairs array with all possible combinations
            List<KeyValuePair<int, int>> pairList = GetEmployeeIds(employeeList);

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
                EmployeesProjectCounter longestTimeOnProjectByColleagues = MapPairsProjectsAndTotalDays(employeeList, distinctPairs, employees, projectList);

                // getting the information about the two employees and their requisites [no necessity of lists, reusing old logic]
                var projectId = longestTimeOnProjectByColleagues.ProjectDays.OrderByDescending(x => x.TotalDays).Select(y => y.ProjectId).Take(1).FirstOrDefault();
                List<EmployeeBase> employee1 = employeeList.Where(e => e.Id.Equals(longestTimeOnProjectByColleagues.EmployeeId) && e.ProjectId.Equals(projectId)).ToList();
                List<EmployeeBase> employee2 = employeeList.Where(e => e.Id.Equals(longestTimeOnProjectByColleagues.Employee2Id) && e.ProjectId.Equals(projectId)).ToList();

                // merge the lists
                var finalResults = employee1.Concat(employee2).ToLookup(e => e.ProjectId).Select(z => z.Aggregate((e1, e2) => new Employee
                {
                    Id = e1.Id,
                    EmployeeTwoId = e2.Id,
                    ProjectId = e1.ProjectId,
                    TotalDays = e1.TotalDays,
                    EmployeeTwoTotalDays = e2.TotalDays,
                    WorkedDays = longestTimeOnProjectByColleagues.ProjectDays.Where(g => g.ProjectId.Equals(projectId)).Select(t => t.TotalDays).FirstOrDefault()
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
        /// Method to handle the logic for pair workers, their projects and calculate the total days
        /// </summary>
        /// <param name="employeeList"></param>
        /// <param name="distinctPairs"></param>
        /// <param name="employees"></param>
        /// <param name="projectList"></param>
        /// <returns></returns>
        private static EmployeesProjectCounter MapPairsProjectsAndTotalDays(List<EmployeeBase> employeeList, List<KeyValuePair<int, int>> distinctPairs, List<EmployeesProjectCounter> employees, List<ProjectInfo> projectList)
        {
            foreach (var pair in distinctPairs)
            {
                foreach (var item in projectList)
                {
                    if (item.Employees.Any(x => x.Id == pair.Key) && item.Employees.Any(x => x.Id == pair.Value))
                    {
                        EmployeeBase empl1 = employeeList.Where(x => x.Id == pair.Key && x.ProjectId == item.ProjectId).FirstOrDefault();
                        EmployeeBase empl2 = employeeList.Where(x => x.Id == pair.Value && x.ProjectId == item.ProjectId).FirstOrDefault();
                        // check if dates overlap to see if they actually met each other on the project
                        if (Utils.CheckIfDatesOverlap(empl1.DateFrom, empl1.DateTo, empl2.DateFrom, empl2.DateTo))
                        {
                            int totalDays = Utils.CalculateDaysTogether(empl1.DateFrom, empl1.DateTo, empl2.DateFrom, empl2.DateTo);
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
            return longestTimeOnProjectByColleagues;
        }

        /// <summary>
        /// Method to create pairs
        /// </summary>
        /// <param name="employeeList"></param>
        /// <returns></returns>
        private static List<KeyValuePair<int, int>> GetEmployeeIds(List<EmployeeBase> employeeList)
        {
            int[] workerIds = employeeList.Select(e => e.Id).Distinct().ToArray();
            List<KeyValuePair<int, int>> pairList = new List<KeyValuePair<int, int>>();
            for (int i = 0; i < workerIds.Length; i++)
            {
                for (int j = 0; j < workerIds.Length; j++)
                {
                    if (!pairList.Where(x => x.Key == workerIds[j] && x.Value == workerIds[i]).Any()) 
                    {
                        pairList.Add(new KeyValuePair<int, int>(workerIds[i], workerIds[j]));
                    }
                }
            }

            return pairList;
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
    }
}
