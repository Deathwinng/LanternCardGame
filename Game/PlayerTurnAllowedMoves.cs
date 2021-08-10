namespace LanternCardGame.Game
{
    public class PlayerTurnAllowedMoves
    {
        private readonly bool drawFromDeck;
        private readonly bool drawFromEmptyDeck;
        private readonly bool putToEmptyDeck;
        private readonly bool lightUp;

        public PlayerTurnAllowedMoves()
        {
            drawFromDeck = false;
            drawFromEmptyDeck = false;
            putToEmptyDeck = false;
            lightUp = false;
        }

        public PlayerTurnAllowedMoves(
            bool drawFromDeck,
            bool drawFromEmptyDeck,
            bool putToEmptyDeck,
            bool lightUp)
        {
            this.drawFromDeck = drawFromDeck;
            this.drawFromEmptyDeck = drawFromEmptyDeck;
            this.putToEmptyDeck = putToEmptyDeck;
            this.lightUp = lightUp;
        }

        public bool DrawFromDeck => drawFromDeck;

        public bool DrawFromEmptyDeck => drawFromEmptyDeck;

        public bool PutToEmptyDeck => putToEmptyDeck;

        public bool LightUp => lightUp;
    }
}
