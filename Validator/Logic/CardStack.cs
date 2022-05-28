using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KH.Solitaire {

	public enum LayoutDir {
		Right,
		Down,
		None
	}

	public class SpotTypes {
		/// <summary>
		/// Card spot that is transparent.
		/// </summary>
		public const string NONE = "none";
		/// <summary>
		/// Card spot that is just a frame.
		/// </summary>
		public const string FRAME = "frame";
		/// <summary>
		/// Card spot that is suited for a foundation.
		/// </summary>
		public const string FOUNDATION = "foundation";
    }

	public class CardStack {
		/// <summary>
		/// Bounds of the rect. NOTE: this does not follow conventions for Rects. Instead of {x,y,w,h}, it's {x,y,max_x,max_y}
		/// </summary>
		private List<Card> _cards;

		public CardStack() {
			_cards = new List<Card>();
		}

		public CardStack(List<Card> cards) : this() {
			foreach (Card c in cards) {
				AddCard(c);
			}
		}

		public IReadOnlyCollection<Card> Cards() {
			return _cards;
		}

		public int Count { get { return _cards.Count; } }

		public void RemoveAllCards() {
			_cards.Clear();
		}

		public void AddCard(Card c) {
			_cards.Add(c);
			c.Stack = this;
		}

		/// <summary>
		/// Returns an enumerable enumerating over the bottom move.CardCount cards.
		/// Doesn't check if the stack is correct.
		/// </summary>
		public IEnumerable<Card> CardsForMove(Move move) {
			int cards = move.CardCount;
			if (cards > _cards.Count) {
				cards = _cards.Count;
            }
			return _cards.Skip(_cards.Count - cards);
        }

		public void AddCardStack(CardStack cs) {
			IReadOnlyCollection<Card> cards = cs.Cards();
			foreach(Card c in cards) {
				AddCard(c);
			}
		}

		public bool CanAddCardCountFromStack(CardStack cs, int count) {
			if (cs.NumberOfMovableCards() < count) return false;
			CardStack movable = cs.CardStackWithCount(count);
			bool canAdd = CanAddCardStackToStack(movable, cs);
			cs.AddCardStack(movable);
			return canAdd;
		}

		public bool CanAddMovableCardsFromStack(CardStack cs) {
			CardStack movable = cs.MovableCards();
			bool canAdd = CanAddCardStackToStack(movable, cs);
			cs.AddCardStack(movable);
			return canAdd;
		}

		public CardStack CardStackWithCount(int count) {
			if (count > _cards.Count) {
				// This is a soft error case.
				count = _cards.Count;
			}

			List<Card> cards = new List<Card>();
			for (int i = _cards.Count - count; i < _cards.Count; i++) {
				Card c = _cards[i];
				cards.Add(c);
			}

			_cards.RemoveAll(x => cards.Contains(x));

			return new CardStack(cards);
		}

		public int MovableCardCountStartingWithCard(Card c) {
			int idx = _cards.IndexOf(c);
			if (idx < 0) return 0;
			int maxMovableCards = NumberOfMovableCards();
			int cardsToMove = _cards.Count - idx;
			if (cardsToMove > maxMovableCards) return 0;
			return cardsToMove;
		}

		public CardStack MovableCardStackStartingWithCard(Card c) {
			int count = MovableCardCountStartingWithCard(c);
			if (count <= 0) return null;
			return CardStackWithCount(count);
        }

		public IEnumerable<Card> CardsStartingWith(Card c) {
			int idx = IndexOf(c);
			if (idx < 0) return Enumerable.Empty<Card>();
			return _cards.Skip(idx);
        }

		public int IndexOf(Card c) {
			return _cards.IndexOf(c);
		}

		public Card At(int index) {
			return _cards[index];
        }

		public CardStack MovableCards() {
			return CardStackWithCount(NumberOfMovableCards());
		}

		public CardStack AllCards() {
			return CardStackWithCount(_cards.Count);
		}

		public Card FirstCard {
			get {
				if (_cards.Count == 0) return null;
				return _cards[0];
			}
		}

		public Card LastCard {
			get {
				if (_cards.Count == 0) return null;
				return _cards[_cards.Count - 1];
			}
		}

		/// <summary>
		/// Returns whether or not the card stack is in descending order.
		/// </summary>
		public bool IsInDescendingOrder() {
			if (_cards.Count < 2) return true;
			for (int i = 0; i < _cards.Count - 1; i++) {
				if (_cards[i].Rank < _cards[i + 1].Rank) return false;
            }
			return true;
        }

		/// <summary>
		/// Returns whether or not the card stack is in descending order
		/// where the cards alternate colors and are always exactly one
		/// lower than the previous one.
		/// </summary>
		public bool IsInDescendingAlternatingRankedOrder() {
			if (_cards.Count < 2) return true;
			for (int i = 0; i < _cards.Count - 1; i++) {
				if (!_cards[i].isOneHigherThan(_cards[i + 1]) || _cards[i].isSameColor(_cards[i + 1])) return false;
			}
			return true;
		}

		public void ShowAllCards(bool anim) {
			foreach (Card card in _cards) {
				card.ShowFront(anim);
			}
		}

		public void ShowTopCard(bool anim) {
			if (_cards.Count == 0) return;
			_cards[_cards.Count - 1].ShowFront(anim);
		}

		public void HideAllCards(bool anim) {
			foreach (Card card in _cards) {
				card.ShowBack(anim);
			}
		}

		public void HideTopCard(bool anim) {
			if (_cards.Count == 0) return;
			_cards[_cards.Count - 1].ShowBack(anim);
		}

		// Methods to implement
		public virtual int NumberOfMovableCards() { return 0; }
		public virtual bool ShouldFlipMovingCards() { return false; }
		public virtual bool ShouldFlipTopCard() { return false; }
		public virtual LayoutDir LayoutDirection() { return LayoutDir.Down; }
		public virtual bool CanAddCardToStack(Card c, CardStack previous) { return false; }
		public virtual bool CanAddCardStackToStack(CardStack cards, CardStack previous) { return false; }
        public virtual bool ShouldRespondToTouchWithNoCards() { return false; }
		public virtual string CardSpotType() { return SpotTypes.FRAME; }

        // To be used by implementations to determine if cards can
        // be added.
        public virtual int CardStackType() {
			return 0;
		}

		public override string ToString() {
			StringBuilder sb = new StringBuilder($"Stack {CardStackType()}: ");
			foreach (Card c in _cards) {
				sb.Append(c);
				sb.Append(' ');
			}
			return sb.ToString();
		}

		public string CardsString() {
			return string.Join(" ", _cards.Select(x => x.ToString()));
		}
	}
}