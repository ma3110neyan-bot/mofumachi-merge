using System;
namespace Mofumachi.Presentation
{
    // Presentation-only directional hysteresis; never moves the world actor.
    public sealed class TownCharacterFacing
    {
        private NoahAuthoredDirection pending;private float stable;
        public NoahAuthoredDirection Update(NoahAuthoredDirection previous,float dx,float dy,float seconds)
        {
            if(!float.IsFinite(dx)||!float.IsFinite(dy)||!float.IsFinite(seconds)||seconds<=0)return previous;
            if(Math.Abs(dx)+Math.Abs(dy)<.000001f){pending=previous;stable=0;return previous;}
            var next=NoahAuthoredMotion.Direction(dx,dy,previous);
            bool vertical=previous==NoahAuthoredDirection.Front||previous==NoahAuthoredDirection.Back;
            if(vertical&&Math.Abs(dx)<=Math.Abs(dy)*1.35f)next=dy>0?NoahAuthoredDirection.Back:NoahAuthoredDirection.Front;
            else if(!vertical&&Math.Abs(dy)<=Math.Abs(dx)*1.15f)next=dx>0?NoahAuthoredDirection.Right:NoahAuthoredDirection.Left;
            if(next==previous){pending=previous;stable=0;return previous;}
            if(pending!=next){pending=next;stable=0;}
            stable+=Math.Min(seconds,.1f);
            if(stable<.09f)return previous;stable=0;return next;
        }
        public void Reset(){stable=0;}
    }
}
