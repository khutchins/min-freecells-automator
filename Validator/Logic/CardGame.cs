using System.Collections;
using System.Collections.Generic;
using System.Linq;
namespace KH.Solitaire {

	public abstract class CardGame {

		public Deck Deck;
		private List<CardStack> _stacks;

		public CardGame() {
			_stacks = new List<CardStack>();
			Deck = new Deck();
		}

		/// <summary>
		/// Set up the card stacks and deck.
		/// </summary>
		/// <param name="gameIndex">Index of the game. Useful in FreeCell, where certain games require 5 cells.</param>
		public abstract void Setup(int gameIndex, int cellCount);

		/// <summary>
		/// Called when a move will be made. If you wish to modify the
		/// move that will be made, return a new move summary. Otherwise,
		/// just return the one that's passed in.
		/// </summary>
		/// <param name="ms">Moves that will be executed unless different value is returned.</param>
		/// <returns>Moves that will be executed.</returns>
		public virtual MoveSummary WillMakeMove(MoveSummary ms) { return ms; }

		/// <summary>
		/// Called when the user begins dragging the stack.
		/// </summary>
		public virtual void CardStackDragBegan(CardStack source, List<Card> movingCards) { }

		/// <summary>
		/// Called when the user finishes dragging the stack.
		/// </summary>
		/// <param name="cs">Moving card stack.</param>
		public virtual void CardStackDragEnded() { }

		/// <summary>
		/// Override tap-to-move behavior of card stack. Override to
		/// add better behavior.
		/// </summary>
		/// <param name="movingCS">Cards that are being dragged.</param>
		/// <param name="source">The source cardstack.</param>
		/// <returns>Card stack to move it to (source if no moves)</returns>
		public virtual CardStack SuggestedCardStackDestination(CardStack movingCS, CardStack source) {
			// Start at the next stack to make it so cards won't just toggle
			// back and forth between two options when there are multiple.
			int start = (_stacks.IndexOf(source) + 1) % _stacks.Count;
			for (int i = start; i != start; i = (i + 1) % _stacks.Count) {
				if (_stacks[i].CanAddCardStackToStack(movingCS, source)) return _stacks[i];
			}
			return null;
		}

		/// <summary>
		/// Checks whether or not the game has been won.
		/// </summary>
		/// <returns>True if game won</returns>
		public abstract bool CheckVictory();

		/// <summary>
		/// Checks whether or not the game has been lost.
		/// </summary>
		/// <returns>True if game lost</returns>
		public virtual bool CheckDefeat() { return false; }

		/// <summary>
		/// Which card stack the deal index should put it in.
		/// </summary>
		/// <param name="dealIndex">Deal card index</param>
		/// <returns>Card stack to deal to.</returns>
		public abstract CardStack CardStackForDeal(int dealIndex);

		/// <summary>
		/// Return true when dealing is finished. Default behavior
		/// is when the deck runs out of cards.
		/// </summary>
		/// <param name="dealIndex">Current deal index.</param>
		/// <returns>Whether done dealing</returns>
		public virtual bool IsDoneDealing(int dealIndex) { return dealIndex == Deck.Count() - 1; }

		/// <summary>
		/// Called on card being dealt.
		/// </summary>
		/// <param name="card">Card dealt.</param>
		/// <param name="dealIndex">Current deal index.</param>
		public virtual void CardDealt(Card card, int dealIndex) { }

		/// <summary>
		/// Called when dealing is finished.
		/// </summary>
		public virtual void AllCardsDealt() { }

		public virtual void MoveMade(MoveSummary summary) { }

		/// <summary>
		/// Creates the deck. By default, makes a standard playing card deck.
		/// </summary>
		/// <returns>The deck.</returns>
		public virtual Deck MakeDeck() {
			return new Deck();
        }

		protected void AddCardStack(CardStack cs) {
			_stacks.Add(cs);
		}

		public IEnumerable<CardStack> CardStacks() {
			return _stacks;
		}

		protected Move GetMove(CardStack from, CardStack to, int count) {
			return new Move(
				count > from.NumberOfMovableCards() ? from.NumberOfMovableCards() : count,
				from.ShouldFlipMovingCards(),
				from.ShouldFlipTopCard(),
				_stacks.IndexOf(from),
				_stacks.IndexOf(to));
		}

		protected Move GetMoveWithMovableCards(CardStack from, CardStack to) {
			return GetMove(from, to, from.NumberOfMovableCards());
		}

		/// <summary>
		/// Checks whether a move of all movable cards from card stack "from"
		/// to "to" would work. Returns the move if it does, null otherwise.
		/// </summary>
		protected Move EasyMoveCheck(CardStack from, CardStack to) {
			if (to.CanAddMovableCardsFromStack(from)) {
				return GetMoveWithMovableCards(from, to);
			}
			return null;
		}

		/// <summary>
		/// Checks whether a move with the given card count from card stack "from"
		/// to "to" would work. Returns the move if it does, null otherwise.
		/// </summary>
		protected Move EasyMoveCheck(CardStack from, CardStack to, int count) {
			if (count == 0 || from.Count < count || from == null || to == null) return null;
			if (to.CanAddCardCountFromStack(from, count)) {
				return GetMove(from, to, count);
			}
			return null;
		}

		protected void MakeMove(Move move) {
			PerformMoveSummaryWithNotification(new MoveSummary(move), true);
		}

		void MoveCompleted(MoveSummary summary) {
        }

		protected void PerformMoveSummaryWithNotification(MoveSummary ms, bool animate) {
			ms = WillMakeMove(ms);
			PerformMoveSummary(ms, false, true);
			MoveCompleted(ms);
			MoveMade(ms);
		}

		bool PerformMoveSummary(MoveSummary ms, bool isUndo, bool animate) {
			foreach (Move move in ms.Moves) {
				if (!PerformMove(move, isUndo, animate)) {
					return false;
                }
			}
			return true;
		}

		bool PerformMove(Move move, bool isUndo, bool animate) {
			CardStack from = CardStackWithIndex(move.FromStack);
			CardStack to = CardStackWithIndex(move.ToStack);

			if (from == null || to == null || from.Count < move.CardCount) {
				// TODO: Handle case where move cannot be correctly performed.
				return false;
			}

			if (isUndo && move.FlipRevealedCard) to.HideTopCard(animate);
			if (!isUndo && move.FlipRevealedCard) from.ShowTopCard(animate);
			CardStack cardsToMove = from.CardStackWithCount(move.CardCount);
			Card[] cards = cardsToMove.Cards().ToArray();
			if (isUndo && move.FlipMovedCards) cardsToMove.HideAllCards(animate);
			if (!isUndo && move.FlipMovedCards) cardsToMove.ShowAllCards(animate);

			to.AddCardStack(cardsToMove);
			return true;
		}

		public CardStack CardStackWithIndex(int idx) {
			if (idx < 0 || idx >= _stacks.Count) return null;
			return _stacks[idx];
        }

		private void SetupNewGame(int seed, int cellCount) {
			_stacks.Clear();
			Setup(seed, cellCount);
			Deck.HideAllCards();
			Deck.MSShuffle(seed);
		}

		public void PlayNewGame(int seed, int cellCount) {
			SetupNewGame(seed, cellCount);
			Deck.MSShuffle(seed);
			Deal();
        }


		private void Deal() {
			foreach (CardStack stack in _stacks) {
				stack.RemoveAllCards();
			}

			int i = 0;
			int count = Deck.Count();
			foreach (Card c in Deck.Cards()) {
				CardStack stack = CardStackForDeal(i);
				stack.AddCard(c);
				CardDealt(c, i);
				if (IsDoneDealing(i)) break;
				i++;
			}
			AllCardsDealt();
		}
	}
}