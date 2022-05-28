using System.Diagnostics;
using System.Text;
using FCSolverAutomator.Helpers;
using KH.Solitaire;

const bool verbose = true;
const bool dryRun = true;
const string FC_SOLVER_BIN_PATH = "T:/Apps/Freecell Solver 6.6.0/bin";
const string SOLUTIONS_ROOT_PATH = "K:/git/min-freecells";


IOHelper helper = new IOHelper(SOLUTIONS_ROOT_PATH, dryRun);

// Run specific deals (change dry run to false to have these commands take effect).
//Generator generator = new Generator(FC_SOLVER_BIN_PATH, helper, 600_000, null, verbose);
//generator.Iterate(50, 52);

// Validate all existing deals.
Validator validator = new Validator(helper);
validator.ValidateAllPotentialSolutions();

// Get some stats.
ListHelper list = new ListHelper(helper);
list.Analyze(1, 1_000_000);

//RunSingle(344);
//RunRange(1, 345);
//Reprocess(1, 1_000_000);
//Analyze(1, 1000000);
//MakeSolutionList();
//Validate(1, 1_000_000);
//Iterate(400_000, 1_000_000, DEFAULT_ITERATION_COUNT);
//ValidateAllToPotentialSolutions(false);