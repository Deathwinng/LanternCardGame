namespace LanternCardGame.Game
{
    public class PlayerTurnAllowedMoves
    {
        private readonly bool drawFromDeck;
        private readonly bool drawFromDiscardDeck;
        private readonly bool putToDiscardDeck;
        private readonly bool lightUp;

        public PlayerTurnAllowedMoves()
        {
            this.drawFromDeck = false;
            this.drawFromDiscardDeck = false;
            this.putToDiscardDeck = false;
            this.lightUp = false;
        }

        public PlayerTurnAllowedMoves(
            bool drawFromDeck,
            bool drawFromDiscardDeck,
            bool putToDiscardDeck,
            bool lightUp)
        {
            this.drawFromDeck = drawFromDeck;
            this.drawFromDiscardDeck = drawFromDiscardDeck;
            this.putToDiscardDeck = putToDiscardDeck;
            this.lightUp = lightUp;
        }

        public bool DrawFromDeck => this.drawFromDeck;

        public bool DrawFromDiscardDeck => this.drawFromDiscardDeck;

        public bool PutToDiscardDeck => this.putToDiscardDeck;

        public bool LightUp => this.lightUp;
    }
}
