using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FCSolverAutomator.Helpers {
    public class ListHelper {
        private IOHelper _helper;

        public ListHelper(IOHelper helper) {
            _helper = helper;
        }

        /// <summary>
        /// Updates the count for the given deal in the solutions list.
        /// Do not use this directly. Use Validator.ValidateAllToPotentialSolutions
        /// instead.
        /// </summary>
        public void UpdateDealInSolutionList(int dealNum, int cellCount) {
            string totalPath = _helper.SolutionListPath();
            if (_helper.DryRun) return;
            using (FileStream fs = File.OpenWrite(totalPath)) {
                fs.Seek((dealNum - 1) * 2, SeekOrigin.Begin);
                fs.WriteByte((byte)(cellCount + '0'));
            }
        }

        private int PositionForDeal(int dealNum) {
            return (dealNum - 1) * 2;
        }

        /// <summary>
        /// Make the initial solutions list. 
        /// You should not have to use this.
        /// </summary>
        public void MakeSolutionList() {
            string totalPath = _helper.SolutionListPath();
            int end = 1_000_000;
            byte[] bytes = new byte[end * 2];
            Doer.DoThing((int dealNum) => {
                int cellCount = _helper.CachedDealCellCount(dealNum);
                int pos = PositionForDeal(dealNum);
                bytes[pos] = (byte)(cellCount + '0');
                bytes[pos + 1] = (byte)'\n';
                return false;
            }, 1, end, "Making list", 10_000);
            if (_helper.DryRun) return;
            File.WriteAllBytes(totalPath, bytes);
        }

        /// <summary>
        /// Generates counts of free cell counts by looking at the min_cells.txt.
        /// </summary>
        public void Analyze(int start, int end) {
            string totalPath = _helper.SolutionListPath();
            using (FileStream fs = new FileStream(totalPath, FileMode.Open)) {
                Dictionary<int, int> map = new Dictionary<int, int>();
                Doer.DoThing((int dealNum) => {
                    fs.Seek(PositionForDeal(dealNum), SeekOrigin.Begin);
                    int cellCount = fs.ReadByte() - '0';
                    if (!map.ContainsKey(cellCount)) {
                        map[cellCount] = 0;
                    }
                    map[cellCount]++;
                    return false;
                }, start, end, "Analyzing", 100_000);

                Console.WriteLine("Cell counts:");
                map.OrderBy(x => x.Key).ToList().ForEach(x => Console.WriteLine($"Cells: {x.Key} Number of Deals: {x.Value}"));
            }
        }
    }
}
