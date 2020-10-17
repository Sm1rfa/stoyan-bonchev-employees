using System.Collections.Generic;

namespace Employees.Desktop.Models
{
    public class EmployeesProjectCounter
    {
        public int EmployeeId { get; set; }
        public int Employee2Id { get; set; }
        public int ProjectsTogether { get; set; }
        public List<ProjectDays> ProjectDays { get; set; }
    }
}
