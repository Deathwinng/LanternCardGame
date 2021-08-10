using System;
using System.Collections.Generic;
using System.Linq;

namespace LanternCardGame.Game.Cards
{
    public class PlayerDeck
    {
        private List<Card> cards;

        public PlayerDeck()
        {
            cards = new List<Card>(10);
        }

        public PlayerDeck(ICollection<Card> cards)
        {
            this.cards = cards.ToList();
        }

        public int CardsCount => cards.Count;

        public ICollection<Card> AllCards => cards.AsReadOnly();

        public void AddCardToDeck(Card card)
        {
            if (CardsCount >= 10)
            {
                throw new Exception("Player's deck is full.");
            }
            if (cards.Contains(card))
            {
                throw new Exception("Duplicate card.");
            }

            cards.Add(card);
        }

        public void AddCardsToDeck(IEnumerable<Card> cards)
        {
            if (CardsCount + cards.Count() > 10)
            {
                throw new Exception("Player's deck is full.");
            }
            if (this.cards.Intersect(cards).Any())
            {
                throw new Exception("Duplicate card.");
            }

            this.cards.AddRange(cards);
        }

        public Card GetCardFromDeck(int cardId)
        {
            if (!cards.Any(x => x.Id == cardId))
            {
                throw new Exception("Card not present in player's deck.");
            }
            if (CardsCount < 10)
            {
                throw new Exception("Not enought cards.");
            }

            var card = cards.First(x => x.Id == cardId);
            cards.Remove(card);
            return card;
        }

        public ICollection<Card> RearrangeCards(ICollection<Card> cards)
        {
            var numOfIntersectingCards = this.cards.Intersect(cards).ToList();
            if (CardsCount != numOfIntersectingCards.Count || CardsCount != cards.Count)
            {
                throw new Exception("Cards given don't match with cards in player's deck.");
            }

            var cardsIds = cards.Select(x => x.Id).ToList();
            this.cards = this.cards.OrderBy(x => cardsIds.IndexOf(x.Id)).ToList();
            return this.cards.AsReadOnly();
        }
    }
}
