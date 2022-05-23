using System.Diagnostics;
using System.Text;

static string DealFor(int gameNum) {
    Process process = new Process();
    process.StartInfo.FileName = "T:/Apps/Freecell Solver 6.6.0/bin/pi-make-microsoft-freecell-board.exe";
    process.StartInfo.Arguments = gameNum.ToString();
    process.StartInfo.RedirectStandardOutput = true;
    process.StartInfo.UseShellExecute = false;
    process.Start();
    //if (process.ExitCode != 0) {
    //    throw new Exception($"Bad exit state for board generator for game num {gameNum}");
    //}
    return process.StandardOutput.ReadToEnd();
}

static string TryGame(string deal, int numFreeCells, int maxIterations) {
    Process process = new Process();
    process.StartInfo.FileName = "T:/Apps/Freecell Solver 6.6.0/bin/fc-solve.exe";
    process.StartInfo.Arguments = $"-m -sn -l looking-glass -mi {maxIterations} --freecells-num {numFreeCells} \"T:/Apps/Freecell Solver 6.6.0/Scratch/scratch.txt\"";
    process.StartInfo.RedirectStandardOutput = true;
    process.StartInfo.UseShellExecute = false;
    process.Start();
    //StreamWriter myStreamWriter = process.StandardInput;
    //myStreamWriter.WriteLine(deal);
    //myStreamWriter.Close();
    string output = process.StandardOutput.ReadToEnd();
    bool solvable = output.Contains("This game is solveable");
    //Console.WriteLine(solvable ? "can solve" : "can't solve");
    if (!solvable) return null;
    string[] splitted = output.Split("\r\n");
    int takeCount = splitted.Length - 6;
    string solution = string.Join("\n", splitted.Skip(2).SkipLast(4));
    //foreach (string split in output.Split("\r\n")) {
    //    Console.WriteLine(split);
    //}
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

static void HandleGame(int gameNum, int maxIterations) {
    string path = WriteDeal(gameNum);
    int fcCount = 5;
    string lastSolve = TryGame(path, 5, maxIterations);
    if (lastSolve == null) {
        Console.WriteLine($"Failed on game {gameNum}");
        WriteSolution(gameNum, 999, "BAD SOLVE");
        return;
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
    WriteSolution(gameNum, fcCount, lastGoodSolve);
}

static void WriteSolution(int gameNum, int fcCount, string solution) {
    int bucket = gameNum / 1000 * 1000;
    WriteToFile($"T:/Apps/Freecell Solver 6.6.0/Scratch/Solve/{bucket}", $"{gameNum}.txt", $"{fcCount}\n{solution}");
}

static void RunRange(int start, int endInclusive) {
    Stopwatch stopWatch = new Stopwatch();
    for (int i = start; i <= endInclusive; i++) {
        if (i % 100 == 0) {
            stopWatch.Stop();
            Console.WriteLine($"On deal {i}. {stopWatch.ElapsedMilliseconds/1000} seconds since last message.");
            
        }
        HandleGame(i, 200000);
    }
}

//string deal = DealFor(1);
//string path = "T:/Apps/Freecell Solver 6.6.0/Scratch/scratch.txt";
//using (FileStream fs = File.Create(path)) {
//    byte[] info = new ASCIIEncoding().GetBytes(deal);
//    fs.Write(info, 0, info.Length);
//}
//Console.WriteLine(deal);
RunRange(11982, 11982);
//RunRange(346207, 1000000);
//for (int i = start; i <= 1000000; i++) {
//    if (i % 100 == 0) Console.WriteLine($"On deal {i}");
//    HandleGame(i, 200000);
//}