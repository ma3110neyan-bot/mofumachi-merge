using Mofumachi.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Mofumachi.Presentation
{
    // Uses the existing Text rectangles and font policy; no second wallet or progress cache.
    public sealed class ProgressUiBinding:MonoBehaviour
    {
        private GameStateManager game;
        private Text wallet,title,detail,level;
        public void Bind(GameStateManager owner,Text walletText=null,Text titleText=null,Text detailText=null,Text levelText=null)
        {
            Unsubscribe();game=owner;wallet=walletText;title=titleText;detail=detailText;level=levelText;
            if(isActiveAndEnabled&&game!=null)game.StateCommitted+=Refresh;
            Refresh();
        }
        private void OnEnable(){if(game!=null){game.StateCommitted+=Refresh;Refresh();}}
        private void OnDisable()=>Unsubscribe();
        private void Unsubscribe(){if(game!=null)game.StateCommitted-=Refresh;}
        private void Refresh()
        {
            if(game==null)return;
            if(wallet!=null)wallet.text=UIStrings.GenerationStatus(game);
            if(title!=null||detail!=null)
            {
                var goal=TownNextGoal.For(game.State);
                if(title!=null)title.text=goal.Title;
                if(detail!=null)detail.text=goal.Detail;
            }
            if(level!=null)level.text="街 Lv."+game.State.townGrowthLevel;
        }
    }
}
