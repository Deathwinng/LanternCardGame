using System;
using System.Collections.Generic;
using System.Linq;

namespace LanternCardGame.Game.Cards
{
    public abstract class BaseDeck
    {
        internal List<Card> allCards;

        public BaseDeck()
        {
            allCards = new List<Card>(54);
        }

        public int CardsLeft => allCards.Count;

        public Card GetNextCard()
        {
            if (CardsLeft == 0)
            {
                throw new Exception("Deck is empty.");
            }

            var card = allCards.Last();
            allCards.Remove(card);
            return card;
        }
    }
}
