using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KH;


namespace KH.Solitaire {

	enum FreeCellType {
		Cell = 0,
		Foundation = 1,
		Tableau = 2,
	}

	class CardStackFreeCellCell : CardStack {

		public override int NumberOfMovableCards() { return 1; }
		public override bool ShouldFlipMovingCards() { return false; }
		public override bool ShouldFlipTopCard() { return false; }
		public override LayoutDir LayoutDirection() { return LayoutDir.None; }
		public override bool CanAddCardToStack(Card c, CardStack previous) { return Cards().Count == 0; }
		public override bool CanAddCardStackToStack(CardStack cards, CardStack previous) { 
			return cards.Count == 1 && CanAddCardToStack(cards.Cards().First(), previous); 
		}
		public override bool ShouldRespondToTouchWithNoCards() { return false; }
		public override int CardStackType() { return (int)FreeCellType.Cell; }
		public override string ToString() { return "Cell: " + CardsString(); }
	}

	class CardStackFreeCellFoundation : CardStack {
		public override int NumberOfMovableCards() { return 0; }
		public override bool ShouldFlipMovingCards() { return false; }
		public override bool ShouldFlipTopCard() { return false; }
		public override LayoutDir LayoutDirection() { return LayoutDir.None; }
		public override bool CanAddCardToStack(Card c, CardStack previous) {
			return (Count == 0 && c.Rank == Rank.Ace)
				|| (Count > 0 && c.isOneHigherThan(Cards().Last()) && c.Suit == Cards().Last().Suit); }
		public override bool CanAddCardStackToStack(CardStack cards, CardStack previous) { 
			return cards.Count == 1 && CanAddCardToStack(cards.Cards().First(), previous); 
		}
		public override bool ShouldRespondToTouchWithNoCards() { return false; }
		public override int CardStackType() { return (int)FreeCellType.Foundation; }
        public override string CardSpotType() {
            return SpotTypes.FOUNDATION;
        }
        public override string ToString() { return "Foundation: " + CardsString(); }
    }

	class CardStackFreeCellTableau : CardStack {
		private CardGameFreeCell _game;

		public CardStackFreeCellTableau(CardGameFreeCell game) {
			_game = game;
		}

		private bool movablePile(Card top, Card bottom) {
			return top.isOneHigherThan(bottom) && !top.isSameColor(bottom);
		}

		public override int NumberOfMovableCards() {
			return Math.Min(NumberOfCardsInRun(), _game.NumberOfMovableCards(this, null));
		}

		public int NumberOfCardsInRun() {
			if (Count < 2) return Count;

			int movableCards = 1;
			Card lastCard = Cards().Last();
			foreach (Card c in Cards().Reverse().Skip(1)) {
				if (!movablePile(c, lastCard)) {
					break;
				}
				movableCards++;
				lastCard = c;
			}
			return movableCards;
		}

		public bool IsTopOfRun(Card card) {
			int idx = IndexOf(card);
			if (idx < 0) return false;
			if (idx == 0) return true;
			return At(idx - 1).IsOppositeColorAndOneHigher(card);
        }

		public override bool ShouldFlipMovingCards() { return false; }
		public override bool ShouldFlipTopCard() { return false; }
		public override LayoutDir LayoutDirection() { return LayoutDir.Down; }
		public override bool CanAddCardToStack(Card c, CardStack previous) {
			if (Cards().Count == 0) return true;
			return movablePile(Cards().Last(), c);

		}
		public override bool CanAddCardStackToStack(CardStack cards, CardStack previous) {
			// Exceeds the number of movable cards permitted.
			if (cards.Count > _game.NumberOfMovableCards(previous, this)) return false;

			// Can add any cards to empty tableau stack
			if (Count == 0) return true;

			// Make sure top card in cardstack can be added to bottom card of this.
			// We assume the rest is well-formed.
			return movablePile(Cards().Last(), cards.Cards().First());
		}
		public override bool ShouldRespondToTouchWithNoCards() { return false; }
		public override int CardStackType() { return (int)FreeCellType.Tableau; }
		public override string ToString() { return "Tableau: " + CardsString(); }
	}

	public class CardGameFreeCell : CardGame {
		public const bool verbose = false;

		private List<CardStackFreeCellTableau> _tableaus = new List<CardStackFreeCellTableau>();
		private List<CardStackFreeCellFoundation> _foundations = new List<CardStackFreeCellFoundation>();
		private List<CardStackFreeCellCell> _cells = new List<CardStackFreeCellCell>();

        // Set up the card stacks and deck.
        public override void Setup(int gameIndex, int cellCount = 4) {
			_tableaus.Clear();
			_foundations.Clear();
			_cells.Clear();

			for (int i = 0; i < 8; i++) {
				_tableaus.Add(new CardStackFreeCellTableau(this));
				this.AddCardStack(_tableaus[i]);
			}

			for (int i = 0; i < 4; i++) {
				_foundations.Add(new CardStackFreeCellFoundation());
				this.AddCardStack(_foundations[i]);
			}


			for (int i = 0; i < cellCount; i++) {
				_cells.Add(new CardStackFreeCellCell());
				this.AddCardStack(_cells[i]);
			}
		}


		public int NumberOfMovableCards(CardStack from, CardStack to) {
			int numFreeCells = _cells.Where(s => s.Count == 0).Count();
			int numFreeTableaus = _tableaus.Where(s => s != from && s != to && s.Count == 0).Count();

			return (int)((1 + numFreeCells) * System.Math.Pow(2, numFreeTableaus));
		}

		public override CardStack CardStackForDeal(int dealIndex) {
			return _tableaus[dealIndex % _tableaus.Count];
		}

		public override void MoveMade(MoveSummary summary) {
		}

        public override bool CheckVictory() {
            foreach (CardStack stack in _tableaus) {
				if (stack.Cards().Count > 0) return false;
			}
			foreach (CardStack stack in _cells) {
				if (stack.Cards().Count > 0) return false;
			}
			return true;
		}

		public override void AllCardsDealt() { 
			foreach (CardStack stack in _tableaus) {
				stack.ShowAllCards(true);
			}
		}

		private Move TryMakeMove(CardStack from, CardStack to) {
			int numMovable = from.NumberOfMovableCards();
			Move move = null;
			while (numMovable > 0 && (move = EasyMoveCheck(from, to, numMovable)) == null) {
				numMovable--;
            }
			return move;
        }

		private CardStack FoundationForSuit(Suit suit) {
			CardStack empty = null;
			foreach (CardStack stack in _foundations) {
				if (stack.Count == 0) {
					if (empty == null) empty = stack;
				} else if (stack.LastCard.Suit == suit) return stack;
            }
			return empty;
        }

		public bool PlaySolution(string solution) {
			PlaySolution(SolutionStringToMoves(solution));
			return CheckVictory();
        }


		private string[] SolutionStringToMoves(string solution) {
			return solution.Trim().Split(null).Where(x => x.Length > 0).ToArray();
		}

		private void PlaySolution(string[] solutionMoves) {
			foreach (string move in solutionMoves) {
				if (!DoSNMove(move)) break;
			}
		}


		public bool DoSNMove(string sn) {
			sn = sn.ToLower();
			CardStack from = StackForSNIndex(sn[0]);
			CardStack to = StackForSNIndex(sn[1]);
			int cards = 1;
			if (sn.Length > 3) {
				char amt = sn[3];
				if (amt > '0' && amt <= '9') cards = amt - '0';
				else if (amt >= 'a' && amt <= 'f') cards = amt - 'a' + 10;
            }

			if (from == null || to == null || from.Count == 0) {
				if (verbose) Console.WriteLine($"Invalid SN move {sn}");
				return false;
			}
			if (to is CardStackFreeCellFoundation) {
				to = FoundationForSuit(from.LastCard.Suit);
			}
			Move move;
			if (to is CardStackFreeCellTableau && to.Count == 0) {
				move = EasyMoveCheck(from, to, cards);
			} else {
				move = TryMakeMove(from, to);
            }
			if (move == null) {
				if (verbose) Console.WriteLine($"Invalid SN move {sn}");
				return false;
			}
			MakeMove(move);
			return true;
		}

		private CardStack StackForSNIndex(char c) {
			if (c >= '1' && c <= '8') {
				return _tableaus[c - '1'];
			} else if (c == 'h') {
				return _foundations[0];
            } else if (c >= 'a' && c <= 'e') {
				int idx = c - 'a';
				if (idx >= _cells.Count) {
					if (verbose) Console.WriteLine($"Bad bounds on SN notation {c}");
					idx = _cells.Count - 1;
                }
				return _cells[idx];
            } else {
				if (verbose) Console.WriteLine($"Unrecognized char {c}");
				return null;
            }
		}
	}
}