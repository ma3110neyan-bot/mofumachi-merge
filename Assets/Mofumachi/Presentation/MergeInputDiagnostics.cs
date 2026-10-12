using System.Diagnostics;
using Mofumachi.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Mofumachi.Presentation
{
    // Development-player QA only. Editor tests keep strict checks for unexpected logs.
    public static class MergeInputDiagnostics
    {
        public static bool ShouldRecord(bool isEditor,bool isDevelopment)=>!isEditor&&isDevelopment;

        [Conditional("UNITY_EDITOR"),Conditional("DEVELOPMENT_BUILD")]
        public static void Record(UIFlowController flow,string action,int from,int to,string result,PointerEventData pointer=null)
        {
            if(!ShouldRecord(Application.isEditor,UnityEngine.Debug.isDebugBuild)||flow==null||flow.Game==null)return;
            var game=flow.Game;
            string input=pointer==null?"":$" pointer={pointer.pointerId} position={pointer.position} pressCamera={pointer.pressEventCamera?.name??"overlay"}";
            string module=EventSystem.current?.currentInputModule?.GetType().Name??"none";
            UnityEngine.Debug.Log($"MOFUMACHI_MERGE_INPUT action={action} screen={flow.CurrentScreen} from={from}:{Describe(game,from)} to={to}:{Describe(game,to)} selected={flow.SelectedCell} production={flow.ProductionItemId} board={game.State.mergeBoard.Count}/30 quest={game.State.activeQuestId} questState={game.State.questState} inputAllowed={flow.CanInteractBoard} viewport={Screen.width}x{Screen.height} safeArea={Screen.safeArea} module={module}{input} result={result.Replace('\n',' ')}");
        }
        private static string Describe(GameStateManager game,int cell)
        {
            var item=game.Board.At(cell);return item==null?"empty":item.itemId+"/Lv"+item.level+(game.Board.IsLocked(cell)?"/locked":"");
        }
    }
}
