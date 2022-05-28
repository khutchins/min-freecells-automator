using System.Collections;
using System.Collections.Generic;

namespace KH.Solitaire {
	public enum Rank {
		Ace = 1,
		Two = 2,
		Three = 3,
		Four = 4,
		Five = 5,
		Six = 6,
		Seven = 7,
		Eight = 8,
		Nine = 9,
		Ten = 10,
		Jack = 11,
		Queen = 12,
		King = 13,
	};

	public enum Suit {
		Club = 1,
		Diamond = 2,
		Heart = 3,
		Spade = 4,
	}

	static class SuitMethods {
		public static bool isRed(this Suit suit) {
			return suit == Suit.Diamond || suit == Suit.Heart;
		}

		public static bool isBlack(this Suit suit) {
			return suit == Suit.Spade || suit == Suit.Club;
		}

		public static bool IsSameColor(this Suit s1, Suit s2) {
			return s1.isRed() == s2.isRed();
		}
	}

	public class Card {
		private bool showingFront;
		public CardStack Stack;

		public Rank Rank { get; }
		public Suit Suit { get; }
		public int Index { get {
				return ((int)Rank - 1) * 4 + (int)Suit - 1;
			}
		}
		public bool ShowingFront => showingFront;
		public Card(Suit suit, Rank rank) {
			Rank = rank;
			Suit = suit;
			showingFront = false;
		}
		public override string ToString() {
			return string.Format("{0}{1}", "A23456789TJQK"[(int)Rank-1], "♣♦♥♠"[(int)Suit-1]);
		}

		public Card(int idx) : this((Suit)((idx / 13) + 1), (Rank)((idx % 13) + 1)) { }

		public void ShowFront(bool anim) {
			if (!this.ShowingFront) {
				Flip(anim);
			}
		}

		public void ShowBack(bool anim) {
			if (this.ShowingFront) {
				Flip(anim);
			}
		}

		public void Flip(bool anim) {
			this.showingFront = !this.showingFront;
		}

		public bool isRed() {
			return Suit.isRed();
		}

		public bool isBlack() {
			return Suit.isBlack();
		}

		public bool IsOppositeColorAndOneHigher(Card other) {
			return !isSameColor(other) && isOneHigherThan(other);
        }

		public bool isSameColor(Card other) {
			return Suit.IsSameColor(other.Suit);
		}

		public bool isOneHigherThan(Card other) {
			return other != null && this.Rank == other.Rank + 1;
		}

		public bool isOneLowerThan(Card other) {
			return other != null && this.Rank == other.Rank - 1;
		}
	}
}