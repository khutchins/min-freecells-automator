using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FCSolverAutomator.Helpers {
    public class IOHelper {
        private string _solutionsBasePath;
        private const int BUCKET_SIZE = 1000;

        public bool DryRun { get; set; }

        public IOHelper(string solutionsBasePath, bool dryRun = false) {
            _solutionsBasePath = solutionsBasePath;
            DryRun = dryRun;
        }

        private void MoveFile(string source, string dest, bool allowOverwrite) {
            if (DryRun) return;
            if (allowOverwrite && File.Exists(dest)) {
                File.Delete(dest);
            }
            string? dirName = Path.GetDirectoryName(dest);
            if (!Directory.Exists(dirName)) {
                Directory.CreateDirectory(dirName);
            }
            File.Move(source, dest);
        }

        public string WriteToFileAndBypassDryRun(string path, string fileName, string contents) {
            return WriteToFile(path, fileName, contents, true);
        }

        public string WriteToFile(string path, string fileName, string contents) {
            return WriteToFile(path, fileName, contents, false);
        }

        private string WriteToFile(string path, string fileName, string contents, bool bypassDryRun) {
            string totalPath = Path.Combine(path, fileName);
            if (DryRun && !bypassDryRun) return totalPath;

            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }
            using (FileStream fs = File.Create(totalPath)) {
                byte[] info = new ASCIIEncoding().GetBytes(contents);
                fs.Write(info, 0, info.Length);
            }
            return totalPath;
        }

        private void WriteSolutionToDir(string dir, int gameNum, int fcCount, string solution) {
            WriteToFile(dir, $"{gameNum}.txt", $"{fcCount}\n{solution}");
        }
        public void WriteSolution(int gameNum, int fcCount, string solution) {
            WriteSolutionToDir(BucketPath(gameNum), gameNum, fcCount, solution);
        }

        public void WriteSolutionToToValidate(int gameNum, int fcCount, string solution) {
            WriteSolutionToDir(PathForToValidate(), gameNum, fcCount, solution);
        }
        public string PathForToValidate() {
            return $"{_solutionsBasePath}/ToValidate";
        }

        private string PathForBadSolutions() {
            return $"{_solutionsBasePath}/ToValidate/Invalid";
        }

        private string PathForRedundantSolutions() {
            return $"{_solutionsBasePath}/ToValidate/Redundant";
        }

        private string BucketPath(int dealNum) {
            int bucket = dealNum / BUCKET_SIZE * BUCKET_SIZE;
            return $"{_solutionsBasePath}/Proofs/{bucket}/";
        }

        private string PathForGameNum(int dealNum) {
            return $"{BucketPath(dealNum)}/{dealNum}.txt";
        }

        public string SolutionListPath() {
            return Path.Combine(_solutionsBasePath, "min_cells.txt");
        }

        public string WriteDeal(string deal) {
            // This has to bypass dry run because it's used for later steps.
            return WriteToFile(_solutionsBasePath, "scratch.txt", deal, true);
        }

        public void CleanupDeal() {
            // This has to bypass dry run because it's used for later steps.
            File.Delete(Path.Combine(_solutionsBasePath, "scratch.txt"));
        }

        public int CachedDealCellCount(int num) {
            string first = File.ReadLines(PathForGameNum(num)).First();
            int cellCount = int.Parse(first);
            return cellCount;
        }
        public void GetSolutionInfo(int dealNum, out int cellCount, out string solution) {
            string[] lines = File.ReadAllLines(PathForGameNum(dealNum));
            cellCount = int.Parse(lines[0]);
            solution = string.Join('\n', lines.Skip(1));
        }

        public void MoveToRedundant(string path) {
            MoveFile(path, PathForRedundantSolutions(), true);
        }

        public void MoveToBadSolution(string path) {
            MoveFile(path, PathForBadSolutions(), true);
        }

        public void UpdateProof(string path, int dealNum) {
            MoveFile(path, PathForGameNum(dealNum), true);
        }
        public void GetSolutionInfoFromFile(string filePath, out int cellCount, out string solution) {
            string[] lines = File.ReadAllLines(filePath);
            cellCount = int.Parse(lines[0]);
            solution = string.Join('\n', lines.Skip(1));
        }
    }
}
