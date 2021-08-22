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
            this.id = int.Parse($"{(int)this.cardSuit}{(int)this.cardType}");
        }

        public int Id => this.id;

        public CardType CardType => this.cardType;

        public CardSuit CardSuit => this.cardSuit;
    }
}
