# FreeCell Solver Automator

Automates the use of the FreeCell solver available [here](https://github.com/shlomif/fc-solve), which is required to be installed to generate and iterate minimum free cell counts. Was used to generate the min free cell counts available [here](https://github.com/khutchins/min-freecells).

## Common Usage

Run Program.cs. Different capabilities are commented out. A preview:

### Try to improve existing solutions
```
IOHelper helper = new IOHelper(SOLUTIONS_ROOT_PATH);

// Run specific deals (change dry run to false to have these commands take effect).
Generator generator = new Generator(FC_SOLVER_BIN_PATH, helper, 200000, "looking-glass");
generator.Iterate();
```

`SOLUTIONS_ROOT_PATH` refers to the base path where the Proofs directory exists. If you're using it in conjunction with the min-freecells repo, it'll be the root. FC_SOLVER_BIN_PATH is where you installed the FreeCell solver mentioned above.

This reads the existing proofs and attempts to improve the existing free cell counts, starting at one below the previous one mentioned. These proofs are put in the `ToValidate/` folder, to be checked with a validate called mentioned below.

The preset passed in ("looking-glass" in the example) is from a list enumerated [here](https://fc-solve.shlomifish.org/docs/distro/USAGE.html) (see the --load-config documentation).

### Validate generated solutions
```
IOHelper helper = new IOHelper(SOLUTIONS_ROOT_PATH);

Validator validator = new Validator(helper);
validator.ValidateAllPotentialSolutions();
```

Validates all solutions in the `ToValidate/` folder, and replaces the proofs in the `Proofs/` folder as necessary, as well as updating the min_cells.txt file. Bad and redundant solutions will be places in the `Invalid/` and `Redundant/` directories.

Solutions in the `ToValidate/` folder are expected to have the cell count on the first line, followed by the solution in SNX notation, as output by the solver mentioned above.

### Get basic stats
```
IOHelper helper = new IOHelper(SOLUTIONS_ROOT_PATH);

ListHelper list = new ListHelper(helper);
list.Analyze(1, 1_000_000);
```

Analyzes the solutions and spits out frequencies.

## Uncommon usage

This documentation largely expects you to operate off of the existing repository. If for some reason you want to regenerate everything yourself, this later stuff is what you care about.

### Generate initial counts
```
IOHelper helper = new IOHelper(SOLUTIONS_ROOT_PATH);

// Run specific deals (change dry run to false to have these commands take effect).
Generator generator = new Generator(FC_SOLVER_BIN_PATH, helper, 200000, "looking-glass");
generator.RunRange(1, 1_000_000);
```

Generates the initial proofs and places them in the `Proofs/` folder, not checking for existing solutions. Starts checking at 5 free cells, and lowers until it cannot solve any more. Solutions are validated after generation. This is unlikely to be necessary, since I've already worked out the kinks, but validation is fast enough that it doesn't make much of a difference.

### Generate min cell list

```
IOHelper helper = new IOHelper(SOLUTIONS_ROOT_PATH);

ListHelper list = new ListHelper(helper);
list.MakeSolutionList();
```

Generates min_cell.txt in the solutions folder by examining each proof for the minimum deal count and writing it all to the file. There's no reason to rerun this if you already have the list, as the validator will update this list more efficiently.