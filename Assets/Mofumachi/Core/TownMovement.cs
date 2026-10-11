using System;
namespace Mofumachi.Core
{
    public sealed class TownMovement
    {
        private readonly TownNavigation navigation;private TownPoint target;private bool hasTarget;
        public TownPoint Position { get; private set; }
        public int? OwnedPointerId { get; private set; }
        public TownMovement(TownNavigation navigation,TownPoint initialPosition)
        {this.navigation=navigation??throw new ArgumentNullException(nameof(navigation));Position=navigation.InitialPosition(initialPosition);}
        public bool TryBeginDrag(int pointerId)
        {if(OwnedPointerId.HasValue)return false;OwnedPointerId=pointerId;hasTarget=false;return true;}
        public void SetTarget(int pointerId,TownPoint target)
        {if(OwnedPointerId!=pointerId||!target.Finite)return;this.target=target;hasTarget=true;}
        public void Tick(float seconds)
        {if(!OwnedPointerId.HasValue||!hasTarget||!float.IsFinite(seconds)||seconds<=0)return;Position=navigation.Move(Position,target,.12f*Math.Min(seconds,1f/30));}
        public void ReleasePointer(int pointerId){if(OwnedPointerId==pointerId)CancelInput();}
        public void CancelInput(){OwnedPointerId=null;hasTarget=false;}
    }
}
