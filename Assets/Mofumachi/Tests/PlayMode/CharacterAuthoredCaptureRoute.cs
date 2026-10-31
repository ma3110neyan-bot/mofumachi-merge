using Mofumachi.Core;
using Mofumachi.Presentation;
using System.Linq;
namespace Mofumachi.Tests
{
    // Shared isolated-fixture route for the pure-model preflight and native test.
    // No production navigation, meeting rules or resident behavior is changed.
    public static class CharacterAuthoredCaptureRoute
    {
        public static readonly TownPoint MomoStart=new TownPoint(.14f,-.30f);
        public static readonly TownPoint PartnerStart=new TownPoint(.43f,-.30f);
        public static readonly TownPoint[] Targets={new TownPoint(-.22f,-.30f),new TownPoint(.27f,-.30f),new TownPoint(.27f,-.02f),new TownPoint(.27f,-.30f)};
        public static readonly TownPoint ConversationTarget=new TownPoint(.34f,-.30f);
        // Navigation can initially turn sideways on a northbound route.
        // Observe the real walking pose; never manufacture a facing for capture.
        public static bool ShouldCaptureGrounding(int routeFrame,bool walking,NoahAuthoredDirection actual,NoahAuthoredDirection intended,bool alreadyCaptured)
            =>routeFrame>=12&&walking&&actual==intended&&!alreadyCaptured;
        public static bool ReadyForConversation(TownWorldWalkModel model)
        {
            var hero=model.Hero;var partner=model.Residents.Single(r=>r.Id=="noah");
            return hero.IsWalking&&TownWorldLayout.Distance(hero.Position,partner.Position)<=.13f&&
                NoahAuthoredMotion.Direction(hero.Position.X-hero.Previous.X,hero.Position.Y-hero.Previous.Y,NoahAuthoredDirection.Front)==NoahAuthoredDirection.Right;
        }
    }
}
