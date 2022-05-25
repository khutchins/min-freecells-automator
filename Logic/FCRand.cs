using System.Collections;
using System.Collections.Generic;
using System.Text;

public class FCRand {
    private long _seed;

    public FCRand(int seed) {
        _seed = seed;
    }

    public int Next() {
		_seed = _seed * 214013 + 2531011;
        _seed &= int.MaxValue;
        return (int)_seed >> 16;
    }
}