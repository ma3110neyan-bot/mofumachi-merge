using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        private enum ModalKind { None, PurchaseNotice, MergeMenu, Discard, PackFrames, Shop }
        private ModalKind modalKind;
        public bool IsModalOpen => modal != null && modal.activeSelf;
        public bool CanInteractBoard => CanPlay && CurrentScreen==ScreenId.Merge && mergeAnimation==null;
        public string ProductionItemId { get; private set; } = "tea";
        public string DiscardItemId { get; private set; }
        public int DiscardItemLevel { get; private set; }
        private int discardCell=-1;
        private readonly HashSet<string> selectedFrames=new HashSet<string>();
        private string[] pendingFrames=System.Array.Empty<string>();
        public bool IsFrameCleanupMode { get; private set; }
        public int SelectedFrameCount=>selectedFrames.Count;
        public IReadOnlyList<string> PendingFrameIds=>System.Array.AsReadOnly(pendingFrames);
        public bool IsFrameSelected(string id)=>selectedFrames.Contains(id);
        private void ClearFrameSelection(){IsFrameCleanupMode=false;selectedFrames.Clear();pendingFrames=System.Array.Empty<string>();}
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
        public TownRegionSnapshot HomeRegionSnapshot {get;private set;}
        private bool discardHomeRegionSnapshot;
        private AudioManager audioManager;
        private string view, settingsReturnView = "title", notice = "";
        private int selected = -1;
        public int SelectedCell => selected;
        private Rect previousSafeArea;
        private Vector2 previousSize;
        private Coroutine mergeAnimation,presentationAnimation;
        private MergeEffectsView mergeEffects;
        public DeliveryPresentation LastDelivery { get; private set; }
        public ChapterPresentation LastChapter { get; private set; }
        public bool RewardVisible { get; private set; }
        private bool rewardCuePlayed,growthCuePlayed;


        private void Awake()
        {
            if (session == null) Initialize(new SaveService(Application.persistentDataPath));
            ProductionItemId=UIStrings.SuggestedProduction(Game);
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
            HomeRegionSnapshot=null;
            discardHomeRegionSnapshot=true;
            ClearFrameSelection();
            var loaded = store.Load();
            session = new GameStateManager(loaded.State, store);
            ProductionItemId=UIStrings.SuggestedProduction(Game);selected=-1;feedback.Remove(ScreenId.Merge);
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
            if(CurrentScreen==ScreenId.Home&&page!=null&&page.GetComponentInChildren<TownStreetWalkView>()?.IsArmed==true){page.GetComponentInChildren<TownStreetWalkView>().CancelSelection();return;}
            if(CurrentScreen==ScreenId.Home&&page!=null&&page.GetComponentInChildren<TownLifeView>()?.DismissOutings()==true)return;
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
            ClearFrameSelection();
            if(mergeEffects!=null)mergeEffects.Cancel();
            CancelAudioEdit();if(page!=null)foreach(var town in page.GetComponentsInChildren<TownLifeView>())town.CancelInteraction();CloseUtilityModal();
            if (mergeAnimation != null) { StopCoroutine(mergeAnimation); mergeAnimation = null; }
            if(presentationAnimation!=null){StopCoroutine(presentationAnimation);presentationAnimation=null;}
            Game.Board.ReleaseLocks(); Game.Quests.RefreshProgress(); selected = -1;
            bool saved = Game.Save();
            if (!saved) { notice = Game.LastError; feedback[CurrentScreen]=notice; }
            else ClearBusyFeedback();
            return saved;
        }
        public bool AcceptQuest()
        {
            if(!CanPlay)return false;
            var quest=QuestCatalog.NextFor(Game.State);
            if(quest==null){feedback[ScreenId.Quest]=ChapterProgress.For(Game.State).AllCompleted?"すべての依頼が完了しました。":UIStrings.UnknownQuest;Render("quest");return false;}
            bool accepted = Game.Quests.AcceptQuest(quest);
            if(accepted){ProductionItemId=UIStrings.SuggestedProduction(Game);selected=-1;}
            notice = accepted ? UIStrings.DeliveryHint(Game) : Game.Quests.LastError;
            feedback[accepted?ScreenId.Merge:ScreenId.Quest]=notice;
            MergeInputDiagnostics.Record(this,"accept",-1,-1,accepted?"accepted":notice);
            Render(accepted ? "merge" : "quest");
            return accepted;
        }
        public bool DropItem(int from, int to)
        {
            MergeInputDiagnostics.Record(this,"drop-request",from,to,"requested");
            if (!CanInteractBoard)
            {
                if(mergeAnimation!=null)ReportBoardMessage(UIStrings.MergeBusy);
                MergeInputDiagnostics.Record(this,"drop-result",from,to,"input-blocked");return false;
            }
            if(mergeEffects!=null)mergeEffects.Cancel();
            var original=Game.Board.At(from);string originalId=original?.itemId;int originalLevel=original?.level??0;
            bool occupied=Game.Board.At(to)!=null;
            string reason=UIStrings.DropReason(Game.Board,from,to);
            bool merged = reason.Length==0 && occupied && Game.TryMerge(from, to);
            bool changed = reason.Length==0 && (occupied ? merged : Game.TryMove(from, to));
            notice = changed ? (merged ? "キラッ！ アイテムが育ちました。" : "移動しました。") : reason.Length>0?reason:Game.LastError.Length>0?Game.LastError:"アイテムを選び直してください。";
            feedback[ScreenId.Merge]=notice;
            MergeInputDiagnostics.Record(this,"drop-result",from,to,changed?(merged?"merged":"moved"):notice);
            if (merged)
            {
                audioManager.PlaySe(AudioCue.Merge);
                mergeAnimation = StartCoroutine(FinishMerge());
            }
            if(changed)selected=-1;Render("merge");
            if(merged&&ItemCatalog.Find(originalId)!=null)
            {
                if(mergeEffects==null)mergeEffects=UIWidgets.Node("Merge effects",layout.ContentRoot,new Rect(0,0,1,1)).gameObject.AddComponent<MergeEffectsView>();
                mergeEffects.Begin(page.GetComponentInChildren<MergeGridLayout>().GetComponent<RectTransform>(),from,to,originalId,originalLevel);
            }
            return changed;
        }
        private IEnumerator FinishMerge()
        {
            yield return new WaitForSecondsRealtime(MergeVisualTiming.LockSeconds);
            mergeAnimation = null;
            if(mergeEffects!=null)mergeEffects.FinishItemMotion();
            Game.Board.ReleaseLocks(); Game.Quests.RefreshProgress();
            bool saved=Game.Save();
            if (!saved){feedback[ScreenId.Merge] = Game.LastError;if(mergeEffects!=null)mergeEffects.Cancel();}
            else ClearBusyFeedback();
            Render(view,preserveMergeTail:saved);
        }
        private void ClearBusyFeedback()
        {
            if(FeedbackFor(ScreenId.Merge)==UIStrings.MergeBusy)feedback[ScreenId.Merge]=UIStrings.DeliveryHint(Game);
        }
        public DeliveryResult Deliver()
        {
            if(!CanPlay || (CurrentScreen!=ScreenId.Quest && CurrentScreen!=ScreenId.Merge))return new DeliveryResult(false,"この画面では納品できません。");
            if(mergeAnimation!=null){ReportBoardMessage(UIStrings.MergeBusy);return new DeliveryResult(false,UIStrings.MergeBusy);}
            if(!Game.Quests.CheckDelivery())
            {
                var hint=UIStrings.DeliveryHint(Game);feedback[CurrentScreen]=hint;
                MergeInputDiagnostics.Record(this,"delivery",-1,-1,hint);Render(view);return new DeliveryResult(false,hint);
            }
            var quest=Game.Quests.ActiveQuest;
            int coins=Game.State.coins,town=Game.State.townGrowthLevel;var result=Game.Quests.TryDeliver();
            MergeInputDiagnostics.Record(this,"delivery",-1,-1,result.Success?"delivered":result.Message);
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

        private void Render(string target,bool animateSelection=false,bool preserveMergeTail=false)
        {
            if(page!=null&&!discardHomeRegionSnapshot)
            {
                var regions=page.GetComponentInChildren<TownRegionSession>();
                if(regions!=null)HomeRegionSnapshot=regions.CaptureSettledSnapshot();
            }
            discardHomeRegionSnapshot=false;
            view = target; CurrentScreen=(ScreenId)System.Enum.Parse(typeof(ScreenId),target,true);
            if(mergeEffects!=null&&(!preserveMergeTail||CurrentScreen!=ScreenId.Merge))mergeEffects.Cancel();
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
                case ScreenId.Merge:QuestMergeScreens.BuildMerge(root,context,animateSelection);break;
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
                foreach(var cameraView in page.GetComponentsInChildren<TownViewportView>())cameraView.enabled=false;
                foreach(var growth in page.GetComponentsInChildren<TownGrowthView>())growth.enabled=false;
                foreach(var reaction in page.GetComponentsInChildren<BodyReaction>())reaction.enabled=false;
            }
            if(mergeEffects!=null&&mergeEffects.IsPlaying)mergeEffects.transform.SetAsLastSibling();
        }
        public void TapCell(int cell)
        {
            MergeInputDiagnostics.Record(this,"tap",selected,cell,"requested");
            if(!CanInteractBoard){if(mergeAnimation!=null)ReportBoardMessage(UIStrings.MergeBusy);return;}
            if(cell<0||cell>=GameState.Columns*GameState.Rows){ReportBoardMessage(UIStrings.OutsideBoard);return;}
            if(cell==selected){selected=-1;feedback[ScreenId.Merge]=UIStrings.DeliveryHint(Game);Render("merge");return;}
            var item=Game.Board.At(cell);
            if(selected<0)
            {
                if(item==null){ReportBoardMessage(UIStrings.EmptySource);return;}
                selected=cell;feedback[ScreenId.Merge]=UIStrings.ItemLabel(item)+"　"+UIStrings.MergeSelectionHint;Render("merge",animateSelection:true);return;
            }
            var reason=UIStrings.DropReason(Game.Board,selected,cell);
            if(item!=null && reason.Length>0 && !Game.Board.IsLocked(cell))
            {
                selected=cell;feedback[ScreenId.Merge]=reason+"\n"+UIStrings.ItemLabel(item)+"を選びました。";Render("merge",animateSelection:true);return;
            }
            DropItem(selected,cell);
        }
        public void ReportBoardMessage(string message)
        {
            feedback[ScreenId.Merge]=message;
            if(CurrentScreen!=ScreenId.Merge||page==null)return;
            var text=page.transform.Find("Feedback")?.GetComponent<Text>();if(text!=null)text.text=message;
            page.GetComponent<ProductionFrameLayout>()?.SetMessage(message);
        }
        public void CreateTea() => GenerateItem("tea");
        public void GenerateItem(string id)
        {
            if(!CanInteractBoard || ItemCatalog.Find(id)==null)return;
            if(!Game.State.productionFrameIds.Contains(id)){ReportBoardMessage("生成枠を置いてから作ろう。");return;}
            selected=-1;
            bool added=Game.AddItem(id,1);feedback[ScreenId.Merge]=added?ItemCatalog.Find(id).Name+" Lv.1を作りました。"+UIStrings.MergeSelectionHint:Game.LastError.Length>0?Game.LastError:UIStrings.BoardFull;
            MergeInputDiagnostics.Record(this,"generate",-1,-1,added?"created "+id:feedback[ScreenId.Merge]);
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
        public bool FocusRequirement(string id)
        {
            if(!CanInteractBoard)return false;var quest=QuestCatalog.NextFor(Game.State);
            if(quest==null||!quest.Requirements.Any(r=>r.ItemId==id))return false;
            if(!UIStrings.IsAccepted(Game.State,quest)){ShowQuest();return true;}
            ProductionItemId=id;selected=-1;feedback[ScreenId.Merge]=ItemCatalog.Find(id).Name+"を作って、同じ種類・Lv.を重ねよう。";
            if(!Game.State.productionFrameIds.Contains(id))OpenMergeActions();else Render("merge");return true;
        }
        public void ChooseProductionItem(string id)
        {
            if(modalKind!=ModalKind.MergeMenu || ItemCatalog.Find(id)==null)return;
            if(!Game.State.productionFrameIds.Contains(id) && !Game.PlaceProductionFrame(id))
            {feedback[ScreenId.Merge]=Game.LastError;OpenUtilityModal(ModalKind.MergeMenu,MergeActionMenuView.Build);return;}
            ProductionItemId=id;selected=-1;feedback[ScreenId.Merge]=ItemCatalog.Find(id).Name+"を作れます。"+UIStrings.MergeSelectionHint;
            MergeInputDiagnostics.Record(this,"choose-kind",-1,-1,id);CloseUtilityModal();Render("merge");
        }
        public void CancelMergeActions()
        { if(modalKind==ModalKind.MergeMenu){ClearFrameSelection();CloseUtilityModal();Render("merge");} }
        public void GenerateFromFrame(string id)
        { if(!CanInteractBoard || !Game.State.productionFrameIds.Contains(id))return;ProductionItemId=id;GenerateItem(id); }
        public void BeginFrameCleanup()
        {
            if(modalKind!=ModalKind.MergeMenu || appPaused || transitioning)return;
            ClearFrameSelection();IsFrameCleanupMode=true;selected=-1;feedback.Remove(ScreenId.Merge);
            OpenUtilityModal(ModalKind.MergeMenu,MergeActionMenuView.Build);
        }
        public void ToggleFrameSelection(string id)
        {
            if(!IsFrameCleanupMode || modalKind!=ModalKind.MergeMenu || appPaused || !Game.State.productionFrameIds.Contains(id))return;
            if(!selectedFrames.Add(id))selectedFrames.Remove(id);
            OpenUtilityModal(ModalKind.MergeMenu,MergeActionMenuView.Build);
        }
        public void OpenPackFramesConfirmation()
        {
            if(!IsFrameCleanupMode || modalKind!=ModalKind.MergeMenu || appPaused || selectedFrames.Count==0)return;
            pendingFrames=ItemCatalog.All.Where(i=>selectedFrames.Contains(i.Id)).Select(i=>i.Id).ToArray();
            feedback.Remove(ScreenId.Merge);OpenUtilityModal(ModalKind.PackFrames,DiscardConfirmationView.BuildFrames);
        }
        public bool ConfirmPackFrames()
        {
            if(modalKind!=ModalKind.PackFrames || CurrentScreen!=ScreenId.Merge || appPaused || transitioning || pendingFrames.Length==0)return false;
            bool saved=Game.PackProductionFrames(pendingFrames);
            if(!saved){feedback[ScreenId.Merge]=Game.LastError;OpenUtilityModal(ModalKind.PackFrames,DiscardConfirmationView.BuildFrames);return false;}
            int count=pendingFrames.Length;ClearFrameSelection();CloseUtilityModal();
            if(!Game.State.productionFrameIds.Contains(ProductionItemId))ProductionItemId=UIStrings.SuggestedProduction(Game);
            feedback[ScreenId.Merge]="生成枠"+count+"個を片づけました。";Render("merge");return true;
        }
        public void CancelPackFrames()
        {
            if(modalKind!=ModalKind.PackFrames)return;
            pendingFrames=System.Array.Empty<string>();OpenUtilityModal(ModalKind.MergeMenu,MergeActionMenuView.Build);
        }
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
            MergeInputDiagnostics.Record(this,"discard",discardCell,-1,removed?"removed":Game.LastError.Length>0?Game.LastError:"stale target");
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
