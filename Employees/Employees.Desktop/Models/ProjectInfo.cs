using System.Collections.Generic;

namespace Employees.Desktop.Models
{
    public class ProjectInfo
    {
        public int ProjectId { get; set; }
        public List<EmployeeBase> Employees { get; set; }
    }
}
