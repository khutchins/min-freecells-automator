using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KH.Solitaire;

namespace FCSolverAutomator.Helpers {
    public class Validator {
        private IOHelper _helper;

        public Validator(IOHelper helper) { _helper = helper; }

        public static bool IsValid(int dealNum, int cellCount, string solution) {
            CardGameFreeCell game = new CardGameFreeCell();
            game.PlayNewGame(dealNum, cellCount);
            return game.PlaySolution(solution);
        }

        /// <summary>
        /// Validates that the solution for the given deal in the proofs directory is valid.
        /// </summary>
        public bool IsValidSolution(int dealNum) {
            _helper.GetSolutionInfo(dealNum, out int cellCount, out string solution);
            return IsValid(dealNum, cellCount, solution);
        }
        
        /// <summary>
         /// Validates the given existing solutions in the Proofs directory.
         /// </summary>
        public void ValidateExisting(int start = 1, int endInclusive = 1_000_000) {
            Doer.DoThing((int dealNum) => {
                bool valid = IsValidSolution(dealNum);
                if (!valid) {
                    Console.WriteLine($"Deal {dealNum} invalid");
                }
                return false;
            }, start, endInclusive, "Validating deal", 10_000);
        }

        /// <summary>
        /// Validates all solutions in the ToValidate/ folder and updates the
        /// proofs and list if applicable. Bad or redundant solutions are placed
        /// in the Invalid/ and Redundant/ folders, respectively.
        /// </summary>
        public void ValidateAllPotentialSolutions() {
            int totalImprovement = 0;
            int validCount = 0;
            int totalCount = 0;
            Dictionary<int, int> fromMap = new Dictionary<int, int>();
            Dictionary<int, int> improvementMap = new Dictionary<int, int>();

            ListHelper listHelper = new ListHelper(_helper);

            // Take all solutions in the validation folder.
            // If they are better than prior solution (or prior solution DNE)
            // overwrite prior solution with them.
            // If they are not, move to bad_proofs/ folder.
            foreach (string path in Directory.GetFiles(_helper.PathForToValidate())) {
                string name = Path.GetFileNameWithoutExtension(path);
                _helper.GetSolutionInfoFromFile(path, out int proposedCellCount, out string solution);
                if (!int.TryParse(name, out int dealNum)) {
                    Console.WriteLine($"Ignoring file {path}");
                }
                int existingCellCount = _helper.CachedDealCellCount(dealNum);
                totalCount++;

                if (existingCellCount <= proposedCellCount) {
                    Console.WriteLine($"Proposed proof for {dealNum} was not better than existing one.");
                    _helper.MoveToRedundant(path);
                    continue;
                } else if (!IsValid(dealNum, proposedCellCount, solution)) {
                    Console.WriteLine($"Proposed proof for {dealNum} was invalid.");
                    _helper.MoveToBadSolution(path);
                    continue;
                } else {
                    Console.WriteLine($"Deal {dealNum} updated from {existingCellCount} -> {proposedCellCount} cells.");
                    _helper.UpdateProof(path, dealNum);
                    listHelper.UpdateDealInSolutionList(dealNum, proposedCellCount);
                    int delta = existingCellCount - proposedCellCount;
                    totalImprovement += delta;

                    if (!improvementMap.ContainsKey(delta)) {
                        improvementMap[delta] = 0;
                    }
                    improvementMap[delta]++;

                    if (!fromMap.ContainsKey(existingCellCount)) {
                        fromMap[existingCellCount] = 0;
                    }
                    fromMap[existingCellCount]++;
                    validCount++;
                }
            }
            Console.WriteLine($"{validCount}/{totalCount} proofs valid and better.");
            Console.WriteLine($"Overall improvement was {totalImprovement} cells.");
            Console.WriteLine($"Improved amounts:");
            improvementMap.OrderBy(x => x.Key).ToList().ForEach(x => Console.WriteLine($"Improved by {x.Key} cells: {x.Value}"));
            Console.WriteLine($"Improved previous cell counts:");
            fromMap.OrderBy(x => x.Key).ToList().ForEach(x => Console.WriteLine($"Improved from {x.Key} cells: {x.Value}"));
        }
    }
}
