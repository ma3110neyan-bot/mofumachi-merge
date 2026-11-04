using System;
using System.Collections.Generic;
using System.Linq;
namespace Mofumachi.Presentation
{
    public enum ReleaseFontRole { Unchanged, Accent, Reading, ReadingBold }

    // Roles describe which measured text may fall back; Hachi is primary for every known role.
    public static class ReleaseFontPolicy
    {
        private static readonly HashSet<string> Accent=new HashSet<string>(StringComparer.Ordinal)
        {"Page title","Quest title","Today title","Town menu title","Sound settings title","Chapter title","Delivery title","Growth title","Chosen partner name","Book expressions title",
         "Start label","Back label","Nav Merge label","Accept quest label","Deliver label","Show growth label","Return town label","Talk to character label","Confirm partner label",
         "Visit today friend label","Claim today flower label","Today visit request label","Open character book label","Close today label","Town today label","Town book label","Nav Quest label",
         "Walk to cafe label","Walk to florist label","Review chapter label","Close town menu label","Return title label","Return settings label","Reselect partner label","Chapter home label","Chapter top label",
         "Nav label","Town menu label","Today town label","Town actor name","Speaker name","Town region name","Merge actions label","Merge menu title","Notice title","Discard title","Pack title","Outing heading","Music selection heading",
         "Begin frame cleanup label","Browse scenery label","Cafe talk label","Cancel discard label","Cancel merge menu label","Cancel pack frames label","Close cafe label","Close outings label","Close shop label","Confirm discard label","Confirm pack frames label","Discard selected label","Enter facility label","Florist make flowers label","Growth character discovery label","Merge daily purpose label","New book discovery label","Notice confirm label","Pack selected frames label","Settings label","Walking partner book label"};
        private static readonly HashSet<string> Reading=new HashSet<string>(StringComparer.Ordinal)
        {"Tagline text","Title footer","Title feedback","Town growth hint","Next goal title","Feedback","Daily welcome line","Town bubble","Speaker name","Town actor name",
         "Requester","Quest chapter","Quest instruction","Merge selected item","Merge selected hint","Merge daily next action","Merge complete hint","Nav label",
         "Today greeting","Today friend destination","Today next discovery","Today town next","Today flower receipt","Town menu scroll hint","Town menu invitation","Fast travel help","Town menu help",
         "Partner description","Book conversation","Book next discovery","Book likes","Book talent","Book favorite place","Book unlocked reactions","Book collection hint","Book request connection","Book current location","Selection feedback",
         "Delivery requester name","Delivery thanks","Reward description","Growth description","Settings description","Settings feedback","Chapter closing","Chapter objective","Chapter error","Finale name",
         "Next goal detail","Cafe welcome","Shop status","Notice body","Notice error","Discard feedback","Discard warning","Merge menu help","Outing hint","Pack feedback","Pack warning","Street walking instruction"};
        private static readonly HashSet<string> Numeric=new HashSet<string>(StringComparer.Ordinal)
        {"Town value","Coins value","Today NEW","Town menu label","Today town label","Reward","Reward value","Growth levels","Chapter total","Book collection counts","QA build revision",
         "Item level","Unknown item","Merge daily progress","Current request caption","Merge reward coins","Merge brand","Reward task badge","Reward task description","Reward amount caption","Reward amount","Merge actions label","Discard item level","Frame cleanup status","Frame selected badge","Pack target"};
        private static bool Prefix(string name,params string[] values)=>values.Any(p=>name.StartsWith(p,StringComparison.Ordinal));
        public static ReleaseFontRole For(string name,int size,string copy)
        {
            if(string.IsNullOrEmpty(name))return ReleaseFontRole.Unchanged;
            if(Accent.Contains(name)||Prefix(name,"Candidate name ","Region walk ","Facility name ","Book field title ","Music title "))return ReleaseFontRole.Accent;
            if(name.EndsWith(" label",StringComparison.Ordinal)&&Prefix(name,"Generate frame ","Growth region ","Listen in scene ","Partner ","Preview music ","Produce ","Select music ","Street exit ","Today goal "))return ReleaseFontRole.Accent;
            if(Numeric.Contains(name)||Prefix(name,"Required count ","Requirement count ","Today goal count ","Requirement name ","Florist flower caption "))return ReleaseFontRole.ReadingBold;
            if(Reading.Contains(name)||Prefix(name,"Today goal name ","Today goal hint ","Book NEW ","Required name ","Music direction ","Music state "))return ReleaseFontRole.Reading;
            return ReleaseFontRole.Unchanged;
        }
        public static string Resource(ReleaseFontRole role,bool hachiFits)
        {
            if(role==ReleaseFontRole.Unchanged)return null;
            return role==ReleaseFontRole.Accent||hachiFits?"UIFontAccent":role==ReleaseFontRole.ReadingBold?"UIFontReadingBold":"UIFontReading";
        }
        public static int CompactMinimumSize(string name)=>name=="Current request caption"||name=="Merge reward coins"?8:0;
        public static int DescriptionMinimumSize(string name)=>name=="Settings description"?12:0;
    }
}
