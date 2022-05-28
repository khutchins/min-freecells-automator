using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KH.Solitaire {
	public class Deck {
		private List<Card> _cards = new List<Card>();

		public Deck() {
			for (int i = 0; i < 52; i++) {
				_cards.Add(new Card(i));
			}
		}

		public override string ToString() {
			StringBuilder sb = new StringBuilder();
			foreach (Card c in _cards) {
				sb.Append(c);
				sb.Append(' ');
            }
			return sb.ToString();
		}


		public IReadOnlyCollection<Card> Cards() {
			return _cards;
		}

		public int Count() {
			return _cards.Count;
		}

		public Card CardLookup(Suit suit, Rank rank) {
			foreach (Card c in _cards) {
				if (c.Suit == suit && c.Rank == rank) {
					return c;
				}
			}
			// Shouldn't happen.
			return null;
		}

		public void HideAllCards() {
			foreach (Card c in _cards) {
				c.ShowBack(true);
			}
		}

		public void MSShuffle(int seed) {
			// TODO: Clean up
			List<Card> cards = new List<Card>(_cards.Count);
			for (int i = 0; i < _cards.Count; i++) {
				int modI = _cards.Count - i;
				int suit = (i % 4) + 1;
                int rank = (i / 4) + 1;
                Card card = _cards.FirstOrDefault(x => x.Rank == (Rank)rank && x.Suit == (Suit)suit);
				cards.Add(card);
			}
			cards.Reverse();

			_cards.Clear();

			FCRand rand = new FCRand(seed);
			for (int i = 0; i < cards.Count - 1; i++) {
				int idx = cards.Count - 1 - rand.Next() % (cards.Count - i);
				Card temp = cards[idx];
				cards[idx] = cards[i];
				cards[i] = temp;
			}
			_cards = cards;
        }
	}
}