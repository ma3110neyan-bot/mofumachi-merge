using System;
using System.Linq;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class MusicRoleTests
    {
        private sealed class Store:IMusicPreferenceStore
        {public string Value;public bool Fail;public string Load()=>Value;public void Save(string id){if(Fail)throw new InvalidOperationException();Value=id;}}
        [TestCase("beginning",MusicRole.Title)] [TestCase("merge-time",MusicRole.Merge)]
        public void UpgradeReadsTheActualLegacyStoreUntilAnExplicitRoleChoice(string oldId,MusicRole role)
        {
            var legacy=new Store{Value=oldId};var title=new Store();var town=new Store();var merge=new Store();
            var roles=new MusicRoleSelection(title,town,merge,legacy);
            Assert.That(roles.For(role),Is.EqualTo(oldId));Assert.That(title.Value,Is.Null);Assert.That(town.Value,Is.Null);Assert.That(merge.Value,Is.Null);
            string choice=role==MusicRole.Title?"title-garden":"merge-ribbon";Assert.That(roles.Select(choice),Is.True);
            Assert.That(legacy.Value,Is.EqualTo(oldId));Assert.That(new MusicRoleSelection(title,town,merge,legacy).For(role),Is.EqualTo(choice));
        }
        [Test] public void EachReplacementRoleHasTwoRealDistinctCandidatesAndTownIsUnchanged()
        {
            foreach(var role in new[]{MusicRole.Title,MusicRole.Merge})
            {var songs=MusicCatalog.Candidates.Where(t=>t.Role==role).ToArray();Assert.That(songs.Length,Is.EqualTo(2));Assert.That(songs.Select(t=>t.Resource).Distinct().Count(),Is.EqualTo(2));}
            Assert.That(MusicCatalog.Candidates.Single(t=>t.Role==MusicRole.Town).Resource,Is.EqualTo("bgm-town-loop"));
        }
        [Test] public void RoleChoicesSurviveRestartWithoutReplacingOtherRolesOrTheMainSave()
        {
            var title=new Store();var town=new Store();var merge=new Store();var roles=new MusicRoleSelection(title,town,merge);
            string townId=roles.For(MusicRole.Town);string mergeId=roles.For(MusicRole.Merge);
            Assert.That(roles.Select("title-garden"),Is.True);Assert.That(roles.For(MusicRole.Town),Is.EqualTo(townId));Assert.That(roles.For(MusicRole.Merge),Is.EqualTo(mergeId));
            Assert.That(new MusicRoleSelection(title,town,merge).For(MusicRole.Title),Is.EqualTo("title-garden"));
        }
        [Test] public void LegacyIdsStillLoadAndFailedRoleSavesKeepTheCurrentSong()
        {
            var a=new Store{Value="beginning"};var b=new Store{Value="town"};var c=new Store{Value="merge-time",Fail=true};var roles=new MusicRoleSelection(a,b,c);
            Assert.That(roles.For(MusicRole.Title),Is.EqualTo("beginning"));Assert.That(roles.For(MusicRole.Merge),Is.EqualTo("merge-time"));
            Assert.That(roles.Select("merge-picnic"),Is.False);Assert.That(roles.For(MusicRole.Merge),Is.EqualTo("merge-time"));Assert.That(roles.Error,Is.Not.Empty);
            Assert.That(roles.Select("unknown"),Is.False);
        }
    }
}
