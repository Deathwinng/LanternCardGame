using LanternCardGame.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LanternCardGame.Game.Cards
{
    public class Deck : BaseDeck
    {
        public Deck()
            : base()
        {
            foreach (var cardSuit in Enum.GetValues<CardSuit>())
            {
                for (int i = 1; i < Enum.GetValues<CardType>().Length; i++)
                {
                    this.allCards.Add(new Card((CardType)i, cardSuit));
                }
            }

            this.allCards.Add(new Card(CardType.Joker, CardSuit.Heart));
            this.allCards.Add(new Card(CardType.Joker, CardSuit.Spade));
            this.allCards = this.allCards.Shuffle().ToList();
        }

        public IEnumerable<Card> GetNextNumberOfCards(int numOfCards)
        {
            if (this.CardsLeft < numOfCards)
            {
                throw new Exception("Not enought cards in deck.");
            }

            var cards = this.allCards.Skip(this.CardsLeft - 1 - numOfCards).Take(numOfCards).ToHashSet();
            foreach (var card in cards)
            {
                this.allCards.Remove(card);
            }

            return cards;
        }
    }
}
