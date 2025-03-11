using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioDashboard
{
    class Utils
    {
        public static int RepeatNumber(IEnumerable<int> list, int index)
        {
            while (index > list.Count()-1) index -= list.Count();
            return list.ElementAt(index);
        }
    }
}
