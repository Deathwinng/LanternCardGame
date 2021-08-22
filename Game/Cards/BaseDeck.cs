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
            this.allCards = new List<Card>(54);
        }

        public int CardsLeft => this.allCards.Count;

        public Card GetNextCard()
        {
            if (this.CardsLeft == 0)
            {
                throw new Exception("Deck is empty.");
            }

            var card = this.allCards.Last();
            this.allCards.Remove(card);
            return card;
        }
    }
}
