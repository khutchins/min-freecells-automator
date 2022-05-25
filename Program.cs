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
    if (!System.IO.Directory.Exists(path)) {
        System.IO.Directory.CreateDirectory(path);
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

//static int HandleGame(int gameNum, int startingCount, int maxIterations) {
//    if (verbose) Console.WriteLine($"Starting deal {gameNum}");
//    if (startingCount <= 0) return 0;
//    string path = WriteDeal(gameNum);
//    int fcCount = startingCount;
//    int lastFCSolve = startingCount;
//    string lastGoodSolve = null;
//    for (fcCount = startingCount; fcCount >= 0; fcCount--) {
//        string lastSolve = TryGame(path, fcCount, maxIterations);
//        if (lastSolve != null) {
//            lastGoodSolve = lastSolve;
//            lastFCSolve = fcCount;
//        } else {
//            break;
//        }
//    }
//    string lastSolve = TryGame(path, 5, maxIterations);
//}

static void WriteSolution(int gameNum, int fcCount, string solution) {
    if (dryRun) return;
    int bucket = gameNum / BUCKET_SIZE * BUCKET_SIZE;
    WriteToFile($"{SOLUTIONS_ROOT_PATH}/Proofs/{bucket}", $"{gameNum}.txt", $"{fcCount}\n{solution}");
}

static string PathForGameNum(int gameNum) {
    int bucket = gameNum / BUCKET_SIZE * BUCKET_SIZE;
    return $"{SOLUTIONS_ROOT_PATH}/Proofs/{bucket}/{gameNum}.txt";
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

static bool DoThing(System.Func<int, bool> doer, int start, int endInclusive, string intervalMessagePrefix, int printInterval) {
    Stopwatch stopWatch = new Stopwatch();
    stopWatch.Start();
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
            Console.WriteLine("Bailing out early for thing.");
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
Reprocess(1, 10000);
//Analyze(1, 1000000);
//MakeSolutionList();
//Validate(1, 345);

static void Iterate(int start, int end) {
    // Re-run solver on existing solutions, starting at solved count - 1,
    // attempting to improve them.
}

static void Validate(int start, int end) {
    // Take all solutions in the validation folder.
    // If they are better than prior solution (or prior solution DNE)
    // overwrite prior solution with them.
    // If they are not, move to bad_proofs/ folder.
    for (int i = 1; i <= end; i++) {
        bool valid = IsValidSolution(i);
        if (!valid) {
            Console.WriteLine($"Deal {i} invalid");
        }
    }
}

static bool IsValid(int dealNum, int cellCount, string solution) {
    CardGameFreeCell game = new CardGameFreeCell();
    game.PlayNewGame(dealNum, cellCount);
    return game.PlaySolution(solution);
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

static void MakeSolutionList() {
    string totalPath = Path.Combine(SOLUTIONS_ROOT_PATH, "min_cells.txt");
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

static void Analyze(int start, int end) {
    Stopwatch watch = new Stopwatch();
    watch.Start();
    Dictionary<int, int> map = new Dictionary<int, int>();
    for (int i = start; i <= end; i++) {
        int cellCount = CachedDealCellCount(i);
        if (!map.ContainsKey(cellCount)) {
            map[cellCount] = 0;
        }
        map[cellCount]++;
        if (i % 10000 == 0) {
            Console.WriteLine($"On deal {i}");
        }
    }

    watch.Stop();
    Console.WriteLine($"Analyzed {end - start + 1} deals in {watch.ElapsedMilliseconds / 1000}s.");
    Console.WriteLine("Cell counts:");
    foreach (var kvp in map) {
        Console.WriteLine($"Cells: {kvp.Key} Number of Deals: {kvp.Value}");
    }
}