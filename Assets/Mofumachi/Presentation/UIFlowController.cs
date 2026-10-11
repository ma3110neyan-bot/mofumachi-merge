using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using Mofumachi.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Mofumachi.Presentation
{
    public sealed class UIFlowController : MonoBehaviour
    {
        private static GameStateManager session;
        public GameStateManager Game => session;
        private ResponsiveUILayout layout;
        private UIWidgets widgets;
        private ScreenContext context;
        private GameObject modal;
        private enum ModalKind { None, PurchaseNotice, MergeMenu, Discard, Shop }
        private ModalKind modalKind;
        public bool IsModalOpen => modal != null && modal.activeSelf;
        public bool CanInteractBoard => CanPlay && CurrentScreen==ScreenId.Merge && mergeAnimation==null;
        public string ProductionItemId { get; private set; } = "tea";
        public string DiscardItemId { get; private set; }
        public int DiscardItemLevel { get; private set; }
        private int discardCell=-1;
        private bool transitioning,appPaused;
        private readonly Dictionary<ScreenId,string> feedback = new Dictionary<ScreenId,string>();
        public ScreenId CurrentScreen { get; private set; }
        public bool IsPurchaseNoticeOpen => modalKind==ModalKind.PurchaseNotice && IsModalOpen;
        public bool SettingsOpenedFromGame => settingsReturnView != "title";
        public string PreviewFavoriteId { get; private set; } = "momo";
        private string selectionReturnView = "home";
        public bool IsAppPaused => appPaused;
        private bool CanPlay => !appPaused && Game.State.purchaseNoticeAcknowledged && CharacterCatalog.IsKnown(Game.State.favoriteCharacterId) && !IsModalOpen && !transitioning;

        private GameObject page;
        private Font font;
        private Texture2D characters;
        public RawImage TownBackground { get; private set; }
        private AudioManager audioManager;
        private string view, settingsReturnView = "title", notice = "";
        private int selected = -1;
        public int SelectedCell => selected;
        private Rect previousSafeArea;
        private Vector2 previousSize;
        private Coroutine mergeAnimation,presentationAnimation;
        public DeliveryPresentation LastDelivery { get; private set; }
        public ChapterPresentation LastChapter { get; private set; }
        public bool RewardVisible { get; private set; }
        private bool rewardCuePlayed,growthCuePlayed;


        private void Awake()
        {
            if (session == null) Initialize(new SaveService(Application.persistentDataPath));
            font = Resources.Load<Font>("Mofumachi/UIFont");
            characters = Resources.Load<Texture2D>("Mofumachi/CharacterMaster");
            if (font == null || characters == null)
                throw new System.InvalidOperationException("Mofumachi UI resources are missing.");
            audioManager = AudioManager.GetOrCreate();
            audioManager.ApplySettings(Game.State.bgmEnabled, Game.State.seEnabled,Game.State.bgmVolume,Game.State.seVolume);
            var canvas = new GameObject("Mofumachi Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.transform.SetParent(transform, false);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(360, 640);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            layout=canvas.AddComponent<ResponsiveUILayout>(); layout.Initialize(canvas.GetComponent<Canvas>());

            widgets=new UIWidgets(font,characters); context=new ScreenContext(this,widgets);
            TownBackground=widgets.Background(Resources.Load<Texture2D>("Mofumachi/TownBackground"),layout.FullScreenRoot);
            CreateClearCamera();
            if (EventSystem.current == null)
            {
                var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform, false);
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            UpdateSafeArea();
        }
        private void Start()
        {
            PreviewFavoriteId = CharacterCatalog.IsKnown(Game.State.favoriteCharacterId) ? Game.State.favoriteCharacterId : "momo";
            Render(SceneManager.GetActiveScene().name == "TitleScene" || !Game.State.purchaseNoticeAcknowledged ? "title" :
                CharacterCatalog.IsKnown(Game.State.favoriteCharacterId) ? "home" : "characterSelection");
        }
        public void Initialize(IStateStore store)
        {
            var loaded = store.Load();
            session = new GameStateManager(loaded.State, store);
            notice = loaded.Reason;feedback[ScreenId.Title]=loaded.Reason;
        }
        private void Update() { UpdateSafeArea(); if(Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) GoBack(); }
        private void UpdateSafeArea()
        {
            var size = new Vector2(Screen.width, Screen.height);
            var area = Screen.safeArea;
            if (layout == null || size.x <= 0 || size.y <= 0 || (size == previousSize && area == previousSafeArea)) return;
            layout.ApplyViewport(size,area);
            previousSize = size; previousSafeArea = area;
        }
        public void BeginGame()
        {
            if (transitioning || IsModalOpen) return;
            if (!Game.State.purchaseNoticeAcknowledged) { OpenNotice(); return; }
            if (!Persist()) { Render("title"); return; }
            transitioning=true; SceneManager.LoadScene("GameScene");
        }
        private void OpenNotice()
        {
            if(IsModalOpen || transitioning) return;
            Render("title"); PurchaseNoticeView.Build(layout.SafeRoot,context);
            modal=layout.SafeRoot.GetChild(layout.SafeRoot.childCount-1).gameObject;modalKind=ModalKind.PurchaseNotice;
        }
        public bool ConfirmPurchaseNotice()
        {
            if(transitioning || !IsPurchaseNoticeOpen) return false;
            if(!Game.AcknowledgePurchaseNotice())
            { feedback[ScreenId.Title]=Game.LastError; modal.SetActive(false);Destroy(modal);modal=null;modalKind=ModalKind.None;OpenNotice();return false; }
            transitioning=true; modal.SetActive(false); Destroy(modal); modal=null;modalKind=ModalKind.None;
            SceneManager.LoadScene("GameScene"); return true;
        }
        public string FeedbackFor(ScreenId screen) => feedback.TryGetValue(screen,out var text)?text:"";
        public void PlayCue(AudioCue cue) => audioManager.PlaySe(cue);
        public void GoBack()
        {
            if(IsModalOpen || transitioning)return;
            if(CurrentScreen==ScreenId.Settings)Navigate(settingsReturnView);
            else if(CurrentScreen==ScreenId.CharacterSelection)
            { if(selectionReturnView=="settings")Navigate("settings"); else ReturnToTitle(); }
            else if(CurrentScreen==ScreenId.Home)ReturnToTitle();
            else if(CurrentScreen!=ScreenId.Title)ShowHome();
        }
        public bool ReturnToTitle()
        {
            if(IsModalOpen || transitioning)return false;
            if(CurrentScreen==ScreenId.Title)return true;
            if(!Persist()){Render(view??"home");return false;}
            transitioning=true;
            SceneManager.LoadScene("TitleScene");
            return true;
        }
        public void ShowHome() => Navigate("home");
        public void ShowQuest() => Navigate("quest");
        public void ShowMerge() => Navigate("merge");
        public void ShowCharacterSelection()
        {
            if(transitioning || IsModalOpen || !Game.State.purchaseNoticeAcknowledged)return;
            string returnView = CurrentScreen==ScreenId.Settings ? "settings" : "home";
            if(!Persist()){Render(view??"title");return;}
            selectionReturnView=returnView;
            PreviewFavoriteId=CharacterCatalog.IsKnown(Game.State.favoriteCharacterId)?Game.State.favoriteCharacterId:"momo";
            Render("characterSelection");
        }
        public void PreviewFavorite(string id)
        {
            if(CurrentScreen!=ScreenId.CharacterSelection || transitioning || IsModalOpen || !CharacterCatalog.IsKnown(id))return;
            PreviewFavoriteId=id;PlayCue(AudioCue.Character);Render("characterSelection");
        }
        public bool ConfirmFavorite()
        {
            if(CurrentScreen!=ScreenId.CharacterSelection || transitioning || IsModalOpen || !Game.State.purchaseNoticeAcknowledged)return false;
            bool saved=Game.SelectFavorite(PreviewFavoriteId);
            feedback[ScreenId.CharacterSelection]=saved?"":Game.LastError;
            if(saved){PlayCue(AudioCue.Confirm);Render(selectionReturnView);}else Render("characterSelection");
            return saved;
        }
        public void ShowSettings()
        {
            if(IsModalOpen || transitioning)return;
            bool reselectingFromSettings=CurrentScreen==ScreenId.CharacterSelection && selectionReturnView=="settings";
            if(view!="settings" && !reselectingFromSettings)settingsReturnView=view??"title";
            Navigate("settings");
        }
        private void Navigate(string target)
        {
            if(transitioning || IsModalOpen)return;
            if(target!="title" && target!="settings" && !Game.State.purchaseNoticeAcknowledged){OpenNotice();return;}
            if(target!="title" && target!="settings" && target!="characterSelection" && !CharacterCatalog.IsKnown(Game.State.favoriteCharacterId))
                target="characterSelection";
            if(!Persist()){Render(view??"title");return;} Render(target);
        }
        public bool Persist()
        {
            if (session == null) return true;
            CancelAudioEdit();if(page!=null)foreach(var town in page.GetComponentsInChildren<TownLifeView>())town.CancelInteraction();CloseUtilityModal();
            if (mergeAnimation != null) { StopCoroutine(mergeAnimation); mergeAnimation = null; }
            if(presentationAnimation!=null){StopCoroutine(presentationAnimation);presentationAnimation=null;}
            Game.Board.ReleaseLocks(); Game.Quests.RefreshProgress(); selected = -1;
            bool saved = Game.Save();
            if (!saved) { notice = Game.LastError; feedback[CurrentScreen]=notice; }
            return saved;
        }
        public bool AcceptQuest()
        {
            if(!CanPlay)return false;
            var quest=QuestCatalog.NextFor(Game.State);
            if(quest==null){feedback[ScreenId.Quest]=ChapterProgress.For(Game.State).AllCompleted?"すべての依頼が完了しました。":UIStrings.UnknownQuest;Render("quest");return false;}
            bool accepted = Game.Quests.AcceptQuest(quest);
            notice = accepted ? "依頼を受けました。アイテムを合成しましょう。" : Game.Quests.LastError;
            feedback[accepted?ScreenId.Merge:ScreenId.Quest]=notice;
            Render(accepted ? "merge" : "quest");
            return accepted;
        }
        public bool DropItem(int from, int to)
        {
            if (!CanPlay || mergeAnimation != null) return false;
            bool occupied=Game.Board.At(to)!=null;
            bool merged = occupied && Game.TryMerge(from, to);
            bool changed = occupied ? merged : Game.TryMove(from, to);
            notice = changed ? (merged ? "キラッ！ アイテムが育ちました。" : "移動しました。") : "その場所には移動・合成できません。";
            if (!changed && Game.LastError.Length > 0) notice = Game.LastError;
            feedback[ScreenId.Merge]=notice;
            if (merged)
            {
                audioManager.PlaySe(AudioCue.Merge);
                mergeAnimation = StartCoroutine(FinishMerge());
            }
            selected = -1; Render("merge"); return changed;
        }
        private IEnumerator FinishMerge()
        {
            yield return new WaitForSecondsRealtime(.22f);
            mergeAnimation = null;
            Game.Board.ReleaseLocks(); Game.Quests.RefreshProgress();
            if (!Game.Save()) feedback[ScreenId.Merge] = Game.LastError;
            Render(view);
        }
        public DeliveryResult Deliver()
        {
            if(!CanPlay || (CurrentScreen!=ScreenId.Quest && CurrentScreen!=ScreenId.Merge))return new DeliveryResult(false,"この画面では納品できません。");
            var quest=Game.Quests.ActiveQuest;
            int coins=Game.State.coins,town=Game.State.townGrowthLevel;var result=Game.Quests.TryDeliver();
            if(result.Success)
            {
                feedback.Remove(ScreenId.Merge); feedback.Remove(ScreenId.Quest);
                LastDelivery=new DeliveryPresentation(quest,Game.State.coins-coins,town,Game.State.townGrowthLevel);
                RewardVisible=false;rewardCuePlayed=growthCuePlayed=false;PlayCue(AudioCue.Delivery);Render("result");
            }
            else {feedback[CurrentScreen]=result.Message;Render(view);}
            return result;
        }
        private IEnumerator RevealReward()
        {
            yield return new WaitForSecondsRealtime(1.2f);presentationAnimation=null;
            if(CurrentScreen!=ScreenId.Result || LastDelivery==null)yield break;
            RewardVisible=true;if(!rewardCuePlayed){rewardCuePlayed=true;PlayCue(AudioCue.Reward);}Render("result");
        }
        public void ShowGrowth()
        {
            if(LastDelivery==null || !RewardVisible)return;
            Navigate("growth");if(CurrentScreen==ScreenId.Growth && !growthCuePlayed){growthCuePlayed=true;PlayCue(AudioCue.Growth);}
        }
        public void ContinueAfterGrowth()
        {
            if(!CanPlay||CurrentScreen!=ScreenId.Growth||LastDelivery==null||!RewardVisible)return;
            var chapter=ChapterCatalog.ForQuest(LastDelivery.QuestId);
            if(chapter!=null&&ChapterProgress.For(Game.State).IsComplete(chapter.Id))OpenChapterSummary(chapter.Id);
            else ShowHome();
        }
        public bool OpenChapterSummary(string id)
        {
            if(!CanPlay||(CurrentScreen!=ScreenId.Home&&CurrentScreen!=ScreenId.Growth)||!ChapterProgress.For(Game.State).IsComplete(id))return false;
            if(!Persist()){Render(view);return false;}
            LastChapter=ChapterPresentation.Create(Game.State,id);feedback.Remove(ScreenId.ChapterSummary);Render("chapterSummary");return true;
        }
        public bool ReviewLastChapter()
        {var chapter=ChapterProgress.For(Game.State).LastCompletedChapter;return chapter!=null&&OpenChapterSummary(chapter.Id);}
        private void OnApplicationPause(bool paused)
        {
            appPaused=paused;
            if (paused) Persist();
            // Rebuild also disables drag views, removing their transient ghost on pause.
            if (page != null && session != null) Render(view);
        }
        private void OnApplicationQuit() => Persist();
        private void OnDestroy() { if (session != null && !transitioning) Persist(); }

        private void CreateClearCamera()
        {
            // The canvas draws the UI; this camera clears the display and removes the
            // Editor's "No cameras rendering" notice without drawing world objects.
            var camera = new GameObject("Mofumachi UI Camera", typeof(Camera)).GetComponent<Camera>();
            camera.transform.SetParent(transform, false);
            camera.transform.localPosition = new Vector3(0, 0, -10);
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(1, .96f, .90f);
            camera.cullingMask = 0;
            camera.depth = -100;
            camera.useOcclusionCulling = false;
        }

        private void Render(string target)
        {
            view = target; CurrentScreen=(ScreenId)System.Enum.Parse(typeof(ScreenId),target,true);
            if (page != null) { page.SetActive(false); Destroy(page); }
            page = layout.CreatePage(target).gameObject;
            var root=(RectTransform)page.transform;
            if(CurrentScreen==ScreenId.Home || CurrentScreen==ScreenId.Title)
            {
                var ambient=UIWidgets.Node("Town water and glints",root,new Rect(0,0,1,1)).gameObject.AddComponent<TownAmbientGraphic>();
                ambient.Bind(layout.FullScreenRoot.GetComponentInChildren<RawImage>());
            }
            switch(CurrentScreen)
            {
                case ScreenId.Title:TopHomeScreens.BuildTitle(root,context);break;
                case ScreenId.CharacterSelection:CharacterSelectionScreen.Build(root,context);break;
                case ScreenId.Home:TopHomeScreens.BuildHome(root,context);break;
                case ScreenId.Quest:QuestMergeScreens.BuildQuest(root,context);break;
                case ScreenId.Merge:QuestMergeScreens.BuildMerge(root,context);break;
                case ScreenId.Settings:SettingsScreen.Build(root,context);break;
                case ScreenId.Result:
                    if(LastDelivery==null){Render("home");return;}
                    RewardGrowthScreens.BuildResult(root,context,LastDelivery);
                    if(!appPaused && !RewardVisible && presentationAnimation==null)presentationAnimation=StartCoroutine(RevealReward());break;
                case ScreenId.ChapterSummary:
                    if(LastChapter==null){Render("home");return;}
                    ChapterSummaryScreen.Build(root,context,LastChapter);break;
                case ScreenId.Growth:
                    if(LastDelivery==null){Render("home");return;}
                    RewardGrowthScreens.BuildGrowth(root,context,LastDelivery);break;
            }
            if(appPaused)
            {
                foreach(var reveal in page.GetComponentsInChildren<SoftReveal>())reveal.enabled=false;
                foreach(var reaction in page.GetComponentsInChildren<CharacterReaction>())reaction.enabled=false;
                foreach(var ambient in page.GetComponentsInChildren<TownAmbientGraphic>())ambient.enabled=false;
                foreach(var town in page.GetComponentsInChildren<TownLifeView>())town.enabled=false;
                foreach(var growth in page.GetComponentsInChildren<TownGrowthView>())growth.enabled=false;
                foreach(var reaction in page.GetComponentsInChildren<BodyReaction>())reaction.enabled=false;
            }
        }
        public void TapCell(int cell)
        {
            if(!CanPlay || mergeAnimation!=null)return;
            if (selected < 0) { if (Game.Board.At(cell) != null) { selected = cell; feedback[ScreenId.Merge] = "同じアイテムか空きマスをタップ"; Render("merge"); } }
            else DropItem(selected, cell);
        }
        public void CreateTea() => GenerateItem("tea");
        public void GenerateItem(string id)
        {
            if(!CanInteractBoard || ItemCatalog.Find(id)==null)return;
            bool added=Game.AddItem(id,1);feedback[ScreenId.Merge]=added?ItemCatalog.Find(id).Name+"を作りました。":Game.LastError.Length>0?Game.LastError:"空きマスを確認してください。";
            if(added)PlayCue(AudioCue.Confirm);Render("merge");
        }
        private void OpenUtilityModal(ModalKind kind,System.Action<RectTransform,ScreenContext> build)
        {
            if(page!=null)foreach(var town in page.GetComponentsInChildren<TownLifeView>())town.CancelInteraction();
            CloseUtilityModal();build(layout.SafeRoot,context);
            modal=layout.SafeRoot.GetChild(layout.SafeRoot.childCount-1).gameObject;modalKind=kind;
        }
        private void CloseUtilityModal()
        {
            if(modalKind==ModalKind.PurchaseNotice)return;
            if(modal!=null){modal.SetActive(false);Destroy(modal);}modal=null;modalKind=ModalKind.None;
        }
        public void OpenMergeActions()
        { if(CanInteractBoard)OpenUtilityModal(ModalKind.MergeMenu,MergeActionMenuView.Build); }
        public void ChooseProductionItem(string id)
        {
            if(modalKind!=ModalKind.MergeMenu || ItemCatalog.Find(id)==null)return;
            ProductionItemId=id;CloseUtilityModal();Render("merge");
        }
        public void CancelMergeActions()
        { if(modalKind==ModalKind.MergeMenu){CloseUtilityModal();Render("merge");} }
        public void OpenDiscardConfirmation(int cell)
        {
            if(!(CanInteractBoard || (modalKind==ModalKind.MergeMenu && CurrentScreen==ScreenId.Merge)))return;
            var item=Game.Board.At(cell);if(item==null || Game.Board.IsLocked(cell))return;
            discardCell=cell;DiscardItemId=item.itemId;DiscardItemLevel=item.level;feedback.Remove(ScreenId.Merge);
            OpenUtilityModal(ModalKind.Discard,DiscardConfirmationView.Build);
        }
        public bool ConfirmDiscard()
        {
            if(modalKind!=ModalKind.Discard || CurrentScreen!=ScreenId.Merge || transitioning)return false;
            bool removed=Game.TryRemoveItem(discardCell,DiscardItemId,DiscardItemLevel);
            if(removed){selected=-1;CloseUtilityModal();feedback[ScreenId.Merge]="アイテムを片づけました。";Render("merge");}
            else {feedback[ScreenId.Merge]=Game.LastError.Length>0?Game.LastError:"アイテムが変わりました。選び直してください。";OpenUtilityModal(ModalKind.Discard,DiscardConfirmationView.Build);}
            return removed;
        }
        public void CancelDiscard()
        { if(modalKind==ModalKind.Discard){CloseUtilityModal();Render("merge");} }
        public void OpenShopPreview()
        {
            if(!CanPlay || CurrentScreen!=ScreenId.Home)return;
            OpenUtilityModal(ModalKind.Shop,(root,c)=>
            {
                var overlay=c.Widgets.Panel("Shop preview",root,new Rect(0,0,1,1),new Color(.25f,.15f,.25f,.6f)).rectTransform;overlay.GetComponent<RoundedPanelGraphic>().raycastTarget=true;
                var card=c.Widgets.OrnatePanel("Shop preview card",overlay,new Rect(0,0,1,1),UIWidgets.BlushPaper).rectTransform;card.anchorMin=card.anchorMax=new Vector2(.5f,.5f);card.sizeDelta=new Vector2(320,280);
                c.Widgets.Ribbon("Shop title","ショップ",card,new Rect(.08f,.72f,.84f,.18f),20);
                c.Widgets.Label("Shop status","お店は準備中です。\n販売機能は未実装です。",card,new Rect(.08f,.3f,.84f,.35f),16);
                var close=c.Widgets.Button("Close shop","街へ戻る",card,new Rect(0,0,1,1),CloseShopPreview,true);UIWidgets.Bottom((RectTransform)close.transform,24,12,24,48);
            });
        }
        public void CloseShopPreview(){if(modalKind==ModalKind.Shop)CloseUtilityModal();}
        private void ApplySavedAudio() => audioManager.ApplySettings(Game.State.bgmEnabled,Game.State.seEnabled,Game.State.bgmVolume,Game.State.seVolume);
        private void CancelAudioEdit()
        {
            if(page!=null)foreach(var control in page.GetComponentsInChildren<VolumeControl>())control.CancelPending();
            if(audioManager!=null)ApplySavedAudio();
        }
        public void PreviewAudioVolumes(float bgmVolume,float seVolume) => audioManager.ApplySettings(Game.State.bgmEnabled,Game.State.seEnabled,bgmVolume,seVolume);
        public bool CommitAudioPreferences(bool bgm,bool se,float bgmVolume,float seVolume)
        {
            bool saved=Game.SetAudioPreferences(bgm,se,bgmVolume,seVolume);
            feedback[ScreenId.Settings]=saved?"":Game.LastError;
            if(page!=null && CurrentScreen==ScreenId.Settings)Render("settings");
            // Disabling old sliders can invoke cancellation previews; saved audio wins last.
            ApplySavedAudio();return saved;
        }
    }
}
