using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace KH.Solitaire {
	public class MoveSummary {
		public readonly List<Move> Moves;
		public readonly int Cost;

		public MoveSummary(Move move, int cost = 0) {
			Moves = new List<Move>();
			Moves.Add(move);
			Cost = cost;
		}

		public MoveSummary(List<Move> moves, int cost = 0) {
			Moves = moves;
			Cost = cost;
		}

		public MoveSummary Reversed() {
			return new MoveSummary(((IEnumerable<Move>)Moves).Reverse().Select(x => x.Reversed()).ToList());
		}
	}
}