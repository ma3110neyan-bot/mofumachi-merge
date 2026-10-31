namespace Mofumachi.Presentation
{
    // Presentation-only final polish. Other characters and the reversible
    // Before configuration retain the accepted shadow exactly.
    public static class CharacterAuthoredGrounding
    {
        public static NoahGroundingProfile For(string characterId,bool refined=true)
            =>refined&&(characterId=="momo"||characterId=="miel")
                ?new NoahGroundingProfile(.07f,-.075f,.86f,.14f,.43f)
                :TownNoahDepthFocus.Grounding(true);
    }
}
