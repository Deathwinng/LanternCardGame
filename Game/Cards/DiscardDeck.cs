using System;
using System.Linq;

namespace LanternCardGame.Game.Cards
{
    public class DiscardDeck : BaseDeck
    {
        public DiscardDeck()
            : base()
        {
        }

        public DiscardDeck(Card card)
            : this()
        {
            allCards.Add(card);
        }

        public void AddCard(Card card)
        {
            if (CardsLeft >= 54)
            {
                throw new Exception("Deck is full.");
            }
            if (allCards.Contains(card))
            {
                throw new Exception("Duplicate card.");
            }

            allCards.Add(card);
        }

        public Card PeekNextCard()
        {
            return allCards.LastOrDefault();
        }
    }
}
