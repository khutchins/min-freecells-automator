using System.Diagnostics;
using System.Text;
using KH.Solitaire;

const bool verbose = false;
const bool dryRun = false;
const int DEFAULT_ITERATION_COUNT = 200000;
const int BUCKET_SIZE = 1000;
const string FC_SOLVER_BIN_PATH = "T:/Apps/Freecell Solver 6.6.0/bin";
const string SOLUTIONS_ROOT_PATH = "K:/git/min-freecells";

static string DealFor(int gameNum) {
    Process process = new Process();
    process.StartInfo.FileName = $"{FC_SOLVER_BIN_PATH}/pi-make-microsoft-freecell-board.exe";
    process.StartInfo.Arguments = gameNum.ToString();
    process.StartInfo.RedirectStandardOutput = true;
    process.StartInfo.UseShellExecute = false;
    process.Start();
    string deal = process.StandardOutput.ReadToEnd();
    if (verbose) Console.WriteLine(deal);
    return deal;
}

static string TryGame(string dealPath, int numFreeCells, int maxIterations) {
    Process process = new Process();
    process.StartInfo.FileName = $"{FC_SOLVER_BIN_PATH}/fc-solve.exe";
    process.StartInfo.Arguments = $"-m -snx -l looking-glass -mi {maxIterations} --freecells-num {numFreeCells} \"{dealPath}\"";
    //process.StartInfo.Arguments = $"-p -t -sam -l looking-glass -mi {maxIterations} --freecells-num {numFreeCells} \"{dealPath}\"";
    process.StartInfo.RedirectStandardOutput = true;
    process.StartInfo.UseShellExecute = false;
    process.Start();

    string output = process.StandardOutput.ReadToEnd();
    bool solvable = output.Contains("This game is solveable");
    if (!solvable) return null;

    string[] splitted = output.Split("\r\n");
    int takeCount = splitted.Length - 6;
    string solution = string.Join("\n", splitted.Skip(2).SkipLast(4));
    if (verbose) {
        Console.WriteLine($"Solved with {numFreeCells} cells.");
        Console.WriteLine(solution);
    }

    return solution;
}

static string WriteToFile(string path, string fileName, string contents) {
    string totalPath = Path.Combine(path, fileName);
    if (!Directory.Exists(path)) {
        Directory.CreateDirectory(path);
    }
    using (FileStream fs = File.Create(totalPath)) {
        byte[] info = new ASCIIEncoding().GetBytes(contents);
        fs.Write(info, 0, info.Length);
    }
    return totalPath;
}

static string WriteDeal(int gameNum) {
    return WriteToFile(SOLUTIONS_ROOT_PATH, "scratch.txt", DealFor(gameNum));
}

static int HandleGame(int gameNum, int maxIterations) {
    if (verbose) Console.WriteLine($"Starting deal {gameNum}");
    string path = WriteDeal(gameNum);
    int fcCount = 5;
    string lastSolve = TryGame(path, 5, maxIterations);
    if (lastSolve == null) {
        Console.WriteLine($"Failed on game {gameNum}");
        WriteSolution(gameNum, -1, "BAD SOLVE");
        return -1;
    }
    int lastFCSolve = fcCount;
    string lastGoodSolve = lastSolve;
    for (fcCount = 4; fcCount >= 0 && lastSolve != null; fcCount--) {
        lastSolve = TryGame(path, fcCount, maxIterations);
        if (lastSolve != null) {
            if (!IsValid(gameNum, fcCount, lastSolve)) {
                Console.WriteLine($"Still mad about deal {gameNum}");
            }
            lastGoodSolve = lastSolve;
            lastFCSolve = fcCount;
        }
    }
    WriteSolution(gameNum, lastFCSolve, lastGoodSolve);
    return lastFCSolve;
}

static void WriteSolution(int gameNum, int fcCount, string solution) {
    WriteSolutionToDir(BucketPath(gameNum), gameNum, fcCount, solution);
}

static void WriteSolutionToToValidate(int gameNum, int fcCount, string solution) {
    WriteSolutionToDir(PathForToValidate(), gameNum, fcCount, solution);
}

static void WriteSolutionToDir(string dir, int gameNum, int fcCount, string solution) {
    if (dryRun) return;
    WriteToFile(dir, $"{gameNum}.txt", $"{fcCount}\n{solution}");
}

static string PathForToValidate() {
    return $"{SOLUTIONS_ROOT_PATH}/ToValidate";
}

static string PathForBadSolutions() {
    return $"{SOLUTIONS_ROOT_PATH}/ToValidate/Invalid";
}

static string PathForRedundantSolutions() {
    return $"{SOLUTIONS_ROOT_PATH}/ToValidate/Redundant";
}

static string BucketPath(int gameNum) {
    int bucket = gameNum / BUCKET_SIZE * BUCKET_SIZE;
    return $"{SOLUTIONS_ROOT_PATH}/Proofs/{bucket}/";
}

static string PathForGameNum(int gameNum) {
    return $"{BucketPath(gameNum)}/{gameNum}.txt";
}

static void CleanupScratch() {
    File.Delete(Path.Combine(SOLUTIONS_ROOT_PATH, "scratch.txt"));
}

static void RunRange(int start, int endInclusive) {
    Stopwatch stopWatch = new Stopwatch();
    stopWatch.Start();
    for (int i = start; i <= endInclusive; i++) {
        if (i % 100 == 0) {
            stopWatch.Stop();
            Console.WriteLine($"On deal {i}. {stopWatch.ElapsedMilliseconds/1000} seconds since last message.");
            stopWatch.Restart();
        }
        HandleGame(i, DEFAULT_ITERATION_COUNT);
    }
    CleanupScratch();
    Console.WriteLine($"Finished deals {stopWatch?.ElapsedMilliseconds / 1000} seconds after last message.");
}

static void RunSingle(int num) {
    HandleGame(num, DEFAULT_ITERATION_COUNT);
    CleanupScratch();
}

static void RunSelected(params int[] selected) {
    for (int i = 0; i < selected.Length; i++) {
        int num = HandleGame(selected[i], DEFAULT_ITERATION_COUNT);
        if (verbose) Console.WriteLine($"Finished {selected[i]} with {num} free cells.");
    }
    CleanupScratch();
}

/// <summary>
/// Performs a Func over each element in the range start-endInclusive. Prints out a message
/// every printInterval with some diagnostics prefixed by intervalMessagePrefix.
/// Func can exit early by returning true. Returns whether or not the Fun exited early.
/// </summary>
static bool DoThing(System.Func<int, bool> doer, int start, int endInclusive, string intervalMessagePrefix, int printInterval) {
    Stopwatch stopWatch = new Stopwatch();
    stopWatch.Start();
    long totalTime = 0;
    for (int i = start; i <= endInclusive; i++) {
        if (i % printInterval == 0) {
            stopWatch.Stop();
            long seconds = stopWatch.ElapsedMilliseconds / 1000;
            totalTime += stopWatch.ElapsedMilliseconds;
            double percentDone = (i - start) * 1.0 / (endInclusive - start);
            long remaining = (long)(totalTime / percentDone * (1 - percentDone));
            string remain = FormatTime(remaining /  1000);
            Console.WriteLine(
                $"{intervalMessagePrefix} @ {i}. {seconds}s since last. {(percentDone * 100).ToString("0.00")}% done. Est. time left: {remain}");
            stopWatch.Restart();
        }
        if(doer(i)) {
            Console.WriteLine($"Bailing out early for thing in index {i}.");
            return true;
        }
    }
    return false;
}

static string FormatTime(long time) {
    if (time < 300) return $"{time}s";
    time /= 60;
    if (time < 180) return $"{time}m";
    time /= 60;
    return $"{time}h";
}

static void Reprocess(int start, int end) {
    DoThing((int dealNum) => {
        if (!IsValidSolution(dealNum)) {
            if (verbose) Console.WriteLine($"Deal {dealNum} invalid");
            int fcCount = CachedDealCellCount(dealNum);
            string solution = TryGame(WriteDeal(dealNum), fcCount, DEFAULT_ITERATION_COUNT);
            if (solution == null) {
                Console.WriteLine($"Couldn't resolve deal {dealNum}");
                return true;
            }
            if (!IsValid(dealNum, fcCount, solution)) {
                Console.WriteLine($"Solution still invalid for {dealNum}");
                return true;
            }
            WriteSolution(dealNum, fcCount, solution);
        } else {
            if (verbose) Console.WriteLine($"{dealNum} solution already fine.");
        }
        return false;
    }, start, end, "Reprocessing deal", 1000);
}

//RunSingle(344);
//RunRange(1, 345);
//Reprocess(1, 1_000_000);
//Analyze(1, 1000000);
//MakeSolutionList();
//Validate(1, 1_000_000);
Iterate(1, 1_000_000, DEFAULT_ITERATION_COUNT);
//ValidateAllToPotentialSolutions(false);

/// <summary>
/// Reruns fc-solve on the range of deals provided, using maxIterations
/// to solve each one. Successfully generated deals will be placed in the
/// ToValidate folder, where they can be validated using the Validate call.
/// </summary>
static void Iterate(int start, int end, int maxIterations) {
    // Re-run solver on existing solutions, starting at solved count - 1,
    // attempting to improve them.
    DoThing((int dealNum) => {
        int ogCellCount = CachedDealCellCount(dealNum);
        if (ogCellCount == 0) {
            // Can't improve on perfection.
            return false;
        }
        int cellCount = ogCellCount - 1;
        int goodCellCount = -1;
        string solution, goodSolution = null;

        string deal = WriteDeal(dealNum);
        while (cellCount >= 0 && (solution = TryGame(deal, cellCount, maxIterations)) != null) {
            goodSolution = solution;
            goodCellCount = cellCount;
            cellCount--;
        }
        if (goodSolution != null) {
            Console.WriteLine($"Improved deal {dealNum} from {ogCellCount} to {goodCellCount}");
            WriteSolutionToToValidate(dealNum, goodCellCount, goodSolution);
            if (!IsValid(dealNum, goodCellCount, goodSolution)) {
                Console.WriteLine($"Generated invalid solution for deal {dealNum}. Something has gone wrong.");
                return true;
            }
        }
        return false;
    }, start, end, "Iterating", 0_500);
}

/// <summary>
/// Validates all existing solutions in the Proofs directory.
/// </summary>
static void ValidateExisting(int start = 1, int end = 1_000_000) {
    DoThing((int dealNum) => {
        bool valid = IsValidSolution(dealNum);
        if (!valid) {
            Console.WriteLine($"Deal {dealNum} invalid");
        }
        return false;
    }, start, end, "Validating deal", 10_000);
}

static bool IsValid(int dealNum, int cellCount, string solution) {
    CardGameFreeCell game = new CardGameFreeCell();
    game.PlayNewGame(dealNum, cellCount);
    return game.PlaySolution(solution);
}

static void GetSolutionInfoFromFile(string filePath, out int cellCount, out string solution) {
    string[] lines = File.ReadAllLines(filePath);
    cellCount = int.Parse(lines[0]);
    solution = string.Join('\n', lines.Skip(1));
}

static void GetSolutionInfo(int dealNum, out int cellCount, out string solution) {
    string[] lines = File.ReadAllLines(PathForGameNum(dealNum));
    cellCount = int.Parse(lines[0]);
    solution = string.Join('\n', lines.Skip(1));
}

static bool IsValidSolution(int gameNum) {
    string[] lines = File.ReadAllLines(PathForGameNum(gameNum));
    int num = int.Parse(lines[0]);
    return IsValid(gameNum, num, string.Join('\n', lines.Skip(1)));
}

static int CachedDealCellCount(int num) {
    string first = File.ReadLines(PathForGameNum(num)).First();
    int cellCount = int.Parse(first);
    return cellCount;
}

static string SolutionListPath() {
    return Path.Combine(SOLUTIONS_ROOT_PATH, "min_cells.txt");
}

static void MakeSolutionList() {
    string totalPath = SolutionListPath();
    int end = 1_000_000;
    byte[] bytes = new byte[end * 2];
    DoThing((int dealNum) => {
        int cellCount = CachedDealCellCount(dealNum);
        bytes[(dealNum - 1) * 2] = (byte)(cellCount + '0');
        bytes[(dealNum - 1) * 2 + 1] = (byte)'\n';
        return false;
    }, 1, end, "Making list", 10_000);
    File.WriteAllBytes(totalPath, bytes);
}

static void MoveToRedundant(string path) {
    MoveFile(path, PathForRedundantSolutions(), true);
}

static void MoveToBadSolution(string path) {
    MoveFile(path, PathForBadSolutions(), true);
}

static void UpdateProof(string path, int dealNum) {
    MoveFile(path, PathForGameNum(dealNum), true);
}

static void MoveFile(string source, string dest, bool allowOverwrite) {
    if (allowOverwrite && File.Exists(dest)) {
        File.Delete(dest);
    }
    string dirName = Path.GetDirectoryName(dest);
    if (!Directory.Exists(dirName)) {
        Directory.CreateDirectory(dirName);
    }
    File.Move(source, dest);
}

static void ValidateAllToPotentialSolutions(bool dryRun = false) {
    int totalImprovement = 0;
    int validCount = 0;
    int totalCount = 0;
    Dictionary<int, int> fromMap = new Dictionary<int, int>();
    Dictionary<int, int> improvementMap = new Dictionary<int, int>();
    // Take all solutions in the validation folder.
    // If they are better than prior solution (or prior solution DNE)
    // overwrite prior solution with them.
    // If they are not, move to bad_proofs/ folder.
    foreach (string path in Directory.GetFiles(PathForToValidate())) {
        string name = Path.GetFileNameWithoutExtension(path);
        GetSolutionInfoFromFile(path, out int proposedCellCount, out string solution);
        int dealNum = int.Parse(name);
        int existingCellCount = CachedDealCellCount(dealNum);
        totalCount++;

        if (existingCellCount <= proposedCellCount) {
            Console.WriteLine($"Proposed proof for {dealNum} was not better than existing one.");
            if (!dryRun) MoveToRedundant(path);
            continue;
        } else if (!IsValid(dealNum, proposedCellCount, solution)) {
            Console.WriteLine($"Proposed proof for {dealNum} was invalid.");
            if (!dryRun) MoveToBadSolution(path);
            continue;
        } else {
            Console.WriteLine($"Deal {dealNum} updated from {existingCellCount} -> {proposedCellCount} cells.");
            if (!dryRun) {
                UpdateProof(path, dealNum);
                UpdateDealInSolutionList(dealNum, proposedCellCount);
            }
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

static void UpdateDealInSolutionList(int dealNum, int cellCount) {
    string totalPath = SolutionListPath();
    using (FileStream fs = File.OpenWrite(totalPath)) {
        fs.Seek((dealNum - 1) * 2, SeekOrigin.Begin);
        fs.WriteByte((byte)(cellCount + '0'));
    }
}

/// <summary>
/// Generates counts of free cell counts by looking at the Proofs/
/// folder. This is out of date, since it'd be more efficient to just
/// look at min_cells.txt.
/// </summary>
static void Analyze(int start, int end) {

    Dictionary<int, int> map = new Dictionary<int, int>();
    DoThing((int dealNum) => {
        int cellCount = CachedDealCellCount(dealNum);
        if (!map.ContainsKey(cellCount)) {
            map[cellCount] = 0;
        }
        map[cellCount]++;
        return false;
    }, start, end, "Analyzing", 10_000);

    Console.WriteLine("Cell counts:");
    map.OrderBy(x => x.Key).ToList().ForEach(x => Console.WriteLine($"Cells: {x.Key} Number of Deals: {x.Value}"));
}