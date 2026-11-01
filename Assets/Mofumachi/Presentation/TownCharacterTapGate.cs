namespace Mofumachi.Presentation
{
    // Transient scene policy, separate from existing daily talk persistence.
    public sealed class TownCharacterTapGate
    {
        private string last;private float opened;
        public bool TryOpen(string id,float now,bool sameBubbleVisible)
        {
            if(string.IsNullOrEmpty(id)||!float.IsFinite(now))return false;
            if(last==id&&(sameBubbleVisible||now-opened<.4f))return false;
            last=id;opened=now;return true;
        }
    }
}
