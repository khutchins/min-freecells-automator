using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FCSolverAutomator.Helpers {
    public class Generator {
        public bool Verbose { get; set; }
        private string _solverBinPath;
        private int _iterations;
        private string? _preset;
        private IOHelper _helper;

        public Generator(string fcSolverBinPath, IOHelper helper, int iterations = 200_000, string? preset = "looking-glass", bool verbose = false) {
            _solverBinPath = fcSolverBinPath;
            _helper = helper;
            _iterations = iterations;
            _preset = preset;
            Verbose = verbose;
        }

        public string DealFor(int gameNum) {
            Process process = new Process();
            process.StartInfo.FileName = $"{_solverBinPath}/pi-make-microsoft-freecell-board.exe";
            process.StartInfo.Arguments = gameNum.ToString();
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.Start();
            string deal = process.StandardOutput.ReadToEnd();
            if (Verbose) Console.WriteLine(deal);
            return deal;
        }

        private string WriteDeal(int gameNum) {
            return _helper.WriteDeal(DealFor(gameNum));
        }

        public string? TrySolveGame(string dealPath, int numFreeCells) {
            Process process = new Process();
            process.StartInfo.FileName = $"{_solverBinPath}/fc-solve.exe";
            string presetArg = _preset != null ? $"-l {_preset}" : "";
            process.StartInfo.Arguments = $"-m -snx {presetArg} -mi {_iterations} --freecells-num {numFreeCells} \"{dealPath}\"";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            bool solvable = output.Contains("This game is solveable");
            if (!solvable) return null;

            string[] splitted = output.Split("\r\n");
            string solution = string.Join("\n", splitted.Skip(2).SkipLast(4));
            if (Verbose) {
                Console.WriteLine($"Solved with {numFreeCells} cells.");
                Console.WriteLine(solution);
            }

            return solution;
        }

        private int HandleGame(int dealNum) {
            if (Verbose) Console.WriteLine($"Starting deal {dealNum}");
            string path = WriteDeal(dealNum);
            int fcCount = 5;
            string? lastSolve = TrySolveGame(path, 5);
            if (lastSolve == null) {
                Console.WriteLine($"Failed on game {dealNum}");
                _helper.WriteSolution(dealNum, -1, "BAD SOLVE");
                return -1;
            }
            int lastFCSolve = fcCount;
            string lastGoodSolve = lastSolve;
            for (fcCount = 4; fcCount >= 0 && lastSolve != null; fcCount--) {
                lastSolve = TrySolveGame(path, fcCount);
                if (lastSolve != null) {
                    if (!Validator.IsValid(dealNum, fcCount, lastSolve)) {
                        Console.WriteLine($"Still mad about deal {dealNum}");
                    }
                    lastGoodSolve = lastSolve;
                    lastFCSolve = fcCount;
                }
            }
            _helper.WriteSolution(dealNum, lastFCSolve, lastGoodSolve);
            return lastFCSolve;
        }

        /// <summary>
        /// Generates the initial deals for the deal numbers in range.
        /// You shouldn't have to use this. Use iterate.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="endInclusive"></param>
        public void RunRange(int start, int endInclusive) {
            Doer.DoThing((int dealNum) => {
                HandleGame(dealNum);
                return false;
            }, start, endInclusive, "On deal", 100);
            _helper.CleanupDeal();
        }

        public void RunSingle(int num) {
            HandleGame(num);
            _helper.CleanupDeal();
        }

        /// <summary>
        /// Generates the initial deal for the selected deal numbers.
        /// You shouldn't have to use this. Use iterate.
        /// </summary>
        /// <param name="selected"></param>
        public void RunSelected(params int[] selected) {
            Doer.DoThing((int dealNum) => {
                HandleGame(dealNum);
                return false;
            }, selected, "On deal", 100);
            _helper.CleanupDeal();
        }

        /// <summary>
        /// Reruns fc-solve on the range of deals provided, using the iterations
        /// and preset specified to solve each one. Successfully generated deals 
        /// will be placed in the ToValidate folder, where they can be validated 
        /// using the Validate call.
        /// </summary>
        public void Iterate(int start = 1, int endInclusive = 1_000_000) {
            Doer.DoThing((int dealNum) => {
                int ogCellCount = _helper.CachedDealCellCount(dealNum);
                if (ogCellCount == 0) {
                    if (Verbose) Console.WriteLine($"Deal {dealNum} already at 0 cells.");
                    // Can't improve on perfection.
                    return false;
                }
                int cellCount = ogCellCount - 1;
                int goodCellCount = -1;
                string? solution, goodSolution = null;

                string deal = WriteDeal(dealNum);
                while (cellCount >= 0 && (solution = TrySolveGame(deal, cellCount)) != null) {
                    goodSolution = solution;
                    goodCellCount = cellCount;
                    cellCount--;
                }
                if (goodSolution != null) {
                    Console.WriteLine($"Improved deal {dealNum} from {ogCellCount} to {goodCellCount}");
                    _helper.WriteSolutionToToValidate(dealNum, goodCellCount, goodSolution);
                    if (!Validator.IsValid(dealNum, goodCellCount, goodSolution)) {
                        Console.WriteLine($"Generated invalid solution for deal {dealNum}. Something has gone wrong.");
                        return true;
                    }
                } else if (Verbose) {
                    Console.WriteLine($"Deal {dealNum} could not be improved from {ogCellCount} cells.");
                }
                return false;
            }, start, endInclusive, "Iterating", 0_500);
        }

        /// <summary>
        /// Looks at all solutions in range, validates that they work, and regenerates them if not.
        /// You should not have to use this.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public void Reprocess(int start, int end) {
            Validator validator = new Validator(_helper);
            Doer.DoThing((int dealNum) => {
                if (!validator.IsValidSolution(dealNum)) {
                    if (Verbose) Console.WriteLine($"Deal {dealNum} invalid");
                    int fcCount = _helper.CachedDealCellCount(dealNum);
                    string solution = TrySolveGame(WriteDeal(dealNum), fcCount);
                    if (solution == null) {
                        Console.WriteLine($"Couldn't resolve deal {dealNum}");
                        return true;
                    }
                    if (!Validator.IsValid(dealNum, fcCount, solution)) {
                        Console.WriteLine($"Solution still invalid for {dealNum}");
                        return true;
                    }
                    _helper.WriteSolution(dealNum, fcCount, solution);
                } else {
                    if (Verbose) Console.WriteLine($"{dealNum} solution already fine.");
                }
                return false;
            }, start, end, "Reprocessing deal", 1000);
        }
    }
}
