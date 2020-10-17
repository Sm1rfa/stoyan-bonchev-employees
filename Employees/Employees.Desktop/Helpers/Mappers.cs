using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Employees.Desktop.Helpers
{
    public static class Mappers
    {
        /// <summary>
        /// Generic method to map list to collection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns>ObservableCollection</returns>
        public static ObservableCollection<T> MapListToCollection<T>(List<T> list)
        {
            return new ObservableCollection<T>(list);
        }
    }
}
