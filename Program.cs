using System.Diagnostics;
using System.Text;

const bool verbose = false;
const int DEFAULT_ITERATION_COUNT = 200000;

static string DealFor(int gameNum) {
    Process process = new Process();
    process.StartInfo.FileName = "T:/Apps/Freecell Solver 6.6.0/bin/pi-make-microsoft-freecell-board.exe";
    process.StartInfo.Arguments = gameNum.ToString();
    process.StartInfo.RedirectStandardOutput = true;
    process.StartInfo.UseShellExecute = false;
    process.Start();
    string deal = process.StandardOutput.ReadToEnd();
    if (verbose) Console.WriteLine(deal);
    return deal;
}

static string TryGame(string deal, int numFreeCells, int maxIterations) {
    Process process = new Process();
    process.StartInfo.FileName = "T:/Apps/Freecell Solver 6.6.0/bin/fc-solve.exe";
    process.StartInfo.Arguments = $"-m -sn -l looking-glass -mi {maxIterations} --freecells-num {numFreeCells} \"T:/Apps/Freecell Solver 6.6.0/Scratch/scratch.txt\"";
    process.StartInfo.RedirectStandardOutput = true;
    process.StartInfo.UseShellExecute = false;
    process.Start();

    string output = process.StandardOutput.ReadToEnd();
    bool solvable = output.Contains("This game is solveable");
    if (!solvable) return null;

    string[] splitted = output.Split("\r\n");
    int takeCount = splitted.Length - 6;
    string solution = string.Join("\n", splitted.Skip(2).SkipLast(4));

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
    return WriteToFile("T:/Apps/Freecell Solver 6.6.0/Scratch/", "scratch.txt", DealFor(gameNum));
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
            lastGoodSolve = lastSolve;
            lastFCSolve = fcCount;
        }
    }
    WriteSolution(gameNum, lastFCSolve, lastGoodSolve);
    return lastFCSolve;
}

static void WriteSolution(int gameNum, int fcCount, string solution) {
    int bucket = gameNum / 1000 * 1000;
    WriteToFile($"T:/Apps/Freecell Solver 6.6.0/Scratch/Solve/{bucket}", $"{gameNum}.txt", $"{fcCount}\n{solution}");
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
    Console.WriteLine($"Finished deals {stopWatch?.ElapsedMilliseconds / 1000} seconds after last message.");
}

static void RunSingle(int num) {
    HandleGame(num, DEFAULT_ITERATION_COUNT);
}

static void RunSelected(params int[] selected) {
    for (int i = 0; i < selected.Length; i++) {
        int num = HandleGame(selected[i], DEFAULT_ITERATION_COUNT);
        if (verbose) Console.WriteLine($"Finished {selected[i]} with {num} free cells.");
    }
}

RunRange(346207, 1000000);