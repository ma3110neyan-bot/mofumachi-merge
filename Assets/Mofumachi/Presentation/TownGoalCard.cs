using UnityEngine;
namespace Mofumachi.Presentation
{
    public static class TownGoalCard
    {
        public static void Build(ScreenContext c,RectTransform parent,float top)
        {
            var goal=TownNextGoal.For(c.Game.State);var w=c.Widgets;
            var panel=w.OrnatePanel("Next town goal",parent,new Rect(0,0,1,1),UIWidgets.Cream,12).rectTransform;UIWidgets.Top(panel,12,top,12,68);
            w.Icon("Next goal flower",UIIcon.Flower,panel,new Rect(.03f,.25f,.12f,.50f));
            w.StoryLabel("Next goal title",goal.Title,panel,new Rect(.18f,.51f,.79f,.43f),14);
            w.Label("Next goal detail",goal.IsFinale?c.Flow.Daily.NextGoal:goal.Detail,panel,new Rect(.18f,.03f,.79f,.46f),12);
        }
    }
}
