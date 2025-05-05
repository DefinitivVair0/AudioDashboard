using ScottPlot;

namespace AudioDashboard
{
    class Utils
    {
        public static int RepeatNumber(IEnumerable<int> list, int index)
        {
            while (index > list.Count() - 1) index -= list.Count();
            return list.ElementAt(index);
        }

        public static int ExtractNumber(string inputString)
        {
            return int.TryParse(string.Concat(inputString.Where(Char.IsDigit)), out int result) ? result : -1;
        }
    }
}
