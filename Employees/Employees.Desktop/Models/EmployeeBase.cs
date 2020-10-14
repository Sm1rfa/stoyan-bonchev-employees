using System;

namespace Employees.Desktop.Models
{
    public class EmployeeBase
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public int TotalDays { get; set; }
    }
}
