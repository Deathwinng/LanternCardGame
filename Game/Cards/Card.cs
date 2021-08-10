namespace LanternCardGame.Game.Cards
{
    public class Card
    {
        private readonly CardType cardType;
        private readonly CardSuit cardSuit;
        private readonly int id;

        public Card(CardType cardType, CardSuit cardSuit)
        {
            this.cardType = cardType;
            this.cardSuit = cardSuit;
            id = int.Parse($"{(int)this.cardSuit}{(int)this.cardType}");
        }

        public int Id => id;

        public CardType CardType => cardType;

        public CardSuit CardSuit => cardSuit;
    }
}
