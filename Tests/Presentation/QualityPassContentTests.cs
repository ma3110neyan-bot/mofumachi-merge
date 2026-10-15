using System;
using System.Collections;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class QualityPassContentTests
    {
        private static Type Content(string name)
        {var type=typeof(MergeVisualTiming).Assembly.GetType("Mofumachi.Presentation."+name);Assert.That(type,Is.Not.Null,"Missing playable presentation content: "+name);return type;}
        [Test] public void EveryExistingQuestHasDistinctBeforeThanksAndStreetReunion()
        {
            var type=Content("QuestStoryPresentation");var lookup=type.GetMethod("For");
            var before=new System.Collections.Generic.HashSet<string>();
            foreach(var quest in QuestCatalog.All)
            {
                var story=lookup.Invoke(null,new object[]{quest.Id});Assert.That(story,Is.Not.Null,quest.Id);
                foreach(string key in new[]{"Before","Thanks","Reunion","TownMoment"})Assert.That((string)story.GetType().GetProperty(key).GetValue(story),Is.Not.Empty,quest.Id+" "+key);
                Assert.That(before.Add((string)story.GetType().GetProperty("Before").GetValue(story)),Is.True,"Character-specific dialogue");
            }
            Assert.That(lookup.Invoke(null,new object[]{"unknown"}),Is.Null,"Unknown saved quests must not invent completed stories");
        }
        [Test] public void GrowthAddsDecorationsInEveryRegionWithoutChangingProgress()
        {
            var type=Content("TownGrowthPresentation");var method=type.GetMethod("For");var state=GameState.CreateInitial();string json=StateCodec.Encode(state);
            foreach(TownRegionId region in Enum.GetValues(typeof(TownRegionId)))
            {
                int previous=0;
                for(int level=0;level<=6;level++)
                {
                    var props=((IEnumerable)method.Invoke(null,new object[]{region,level})).Cast<object>().ToArray();
                    Assert.That(props.Length,Is.EqualTo(level),region+" should visibly gain something at every level");
                    Assert.That(props.Length,Is.GreaterThanOrEqualTo(previous));previous=props.Length;
                }
            }
            Assert.That(StateCodec.Encode(state),Is.EqualTo(json));
        }
        [Test] public void ReunionRequiresThatCharactersActualCompletedQuest()
        {
            var type=Content("QuestStoryPresentation");var method=type.GetMethod("ReunionFor");
            Assert.That((string)method.Invoke(null,new object[]{"lulu",Array.Empty<string>()}),Is.Empty);
            Assert.That((string)method.Invoke(null,new object[]{"lulu",new[]{"bread-01"}}),Is.Empty);
            Assert.That((string)method.Invoke(null,new object[]{"lulu",new[]{"tea-01"}}),Is.Not.Empty);
            Assert.That((string)method.Invoke(null,new object[]{"unknown",new[]{"tea-01"}}),Is.Empty);
        }
    }
}
