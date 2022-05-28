using System.Collections;
using System.Collections.Generic;

namespace KH.Solitaire {
	public class Move {
		public readonly int CardCount;
		public readonly bool FlipMovedCards;
		public readonly bool FlipRevealedCard;
		public readonly int FromStack;
		public readonly int ToStack;

		public Move(int cardCount, bool flipMovedCards, bool flipRevealedCard, int fromStack, int toStack) {
			CardCount = cardCount;
			FlipMovedCards = flipMovedCards;
			FlipRevealedCard = flipRevealedCard;
			FromStack = fromStack;
			ToStack = toStack;
		}

		public Move Reversed() {
			return new Move(CardCount, FlipMovedCards, FlipRevealedCard, ToStack, FromStack);
		}

		public override string ToString() {
			return $"Move {CardCount} cards from {FromStack} to {ToStack}";
		}
	}
}