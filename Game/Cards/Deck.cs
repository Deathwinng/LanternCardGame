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
                    allCards.Add(new Card((CardType)i, cardSuit));
                }
            }

            allCards.Add(new Card(CardType.Joker, CardSuit.Heart));
            allCards.Add(new Card(CardType.Joker, CardSuit.Spade));
            allCards = allCards.Shuffle().ToList();
        }

        public IEnumerable<Card> GetNextNumberOfCards(int numOfCards)
        {
            if (CardsLeft < numOfCards)
            {
                throw new Exception("Not enought cards in deck.");
            }

            var cards = allCards.Skip(CardsLeft - 1 - numOfCards).Take(numOfCards).ToHashSet();
            //this.allCards.RemoveRange(this.CardsLeft - numOfCards, numOfCards);
            //this.allCards = this.allCards.Where(x => !cards.Any(y => y.Id == x.Id)).ToList();
            foreach (var card in cards)
            {
                allCards.Remove(card);
            }

            return cards;
        }
    }
}
