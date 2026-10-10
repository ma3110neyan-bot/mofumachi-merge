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
        private bool transitioning,appPaused;
        private readonly Dictionary<ScreenId,string> feedback = new Dictionary<ScreenId,string>();
        public ScreenId CurrentScreen { get; private set; }
        public bool IsPurchaseNoticeOpen => modal != null && modal.activeSelf;

        private GameObject page;
        private Font font;
        private Texture2D characters;
        private AudioManager audioManager;
        private string view, settingsReturnView = "title", notice = "";
        private int selected = -1;
        public int SelectedCell => selected;
        private Rect previousSafeArea;
        private Vector2 previousSize;
        private Coroutine mergeAnimation,presentationAnimation;
        public DeliveryPresentation LastDelivery { get; private set; }
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
            widgets.Background(Resources.Load<Texture2D>("Mofumachi/TownBackground"),layout.FullScreenRoot);
            CreateClearCamera();
            if (EventSystem.current == null)
            {
                var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform, false);
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            UpdateSafeArea();
        }
        private void Start() => Render(SceneManager.GetActiveScene().name == "TitleScene" || !Game.State.purchaseNoticeAcknowledged ? "title" : "home");
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
            if (transitioning) return;
            if (!Game.State.purchaseNoticeAcknowledged) { OpenNotice(); return; }
            if (!Persist()) { Render("title"); return; }
            transitioning=true; SceneManager.LoadScene("GameScene");
        }
        private void OpenNotice()
        {
            if(IsPurchaseNoticeOpen || transitioning) return;
            Render("title"); PurchaseNoticeView.Build(layout.SafeRoot,context);
            modal=layout.SafeRoot.GetChild(layout.SafeRoot.childCount-1).gameObject;
        }
        public bool ConfirmPurchaseNotice()
        {
            if(transitioning || !IsPurchaseNoticeOpen) return false;
            if(!Game.AcknowledgePurchaseNotice())
            { feedback[ScreenId.Title]=Game.LastError; modal.SetActive(false);Destroy(modal);modal=null;OpenNotice();return false; }
            transitioning=true; modal.SetActive(false); Destroy(modal); modal=null;
            SceneManager.LoadScene("GameScene"); return true;
        }
        public string FeedbackFor(ScreenId screen) => feedback.TryGetValue(screen,out var text)?text:"";
        public void PlayCue(AudioCue cue) => audioManager.PlaySe(cue);
        public void GoBack() { if(IsPurchaseNoticeOpen || transitioning)return; if(CurrentScreen==ScreenId.Settings)Navigate(settingsReturnView);else if(CurrentScreen!=ScreenId.Title)ShowHome(); }
        public void ShowHome() => Navigate("home");
        public void ShowQuest() => Navigate("quest");
        public void ShowMerge() => Navigate("merge");
        public void ShowSettings()
        { if(IsPurchaseNoticeOpen || transitioning)return; if(view!="settings")settingsReturnView=view??"title";Navigate("settings"); }
        private void Navigate(string target)
        {
            if(transitioning || IsPurchaseNoticeOpen)return;
            if(target!="title" && target!="settings" && !Game.State.purchaseNoticeAcknowledged){OpenNotice();return;}
            if(!Persist()){Render(view??"title");return;} Render(target);
        }
        public bool Persist()
        {
            if (session == null) return true;
            CancelAudioEdit();
            if (mergeAnimation != null) { StopCoroutine(mergeAnimation); mergeAnimation = null; }
            if(presentationAnimation!=null){StopCoroutine(presentationAnimation);presentationAnimation=null;}
            Game.Board.ReleaseLocks(); Game.Quests.RefreshProgress(); selected = -1;
            bool saved = Game.Save();
            if (!saved) { notice = Game.LastError; feedback[CurrentScreen]=notice; }
            return saved;
        }
        public bool AcceptQuest()
        {
            if(!Game.State.purchaseNoticeAcknowledged || IsPurchaseNoticeOpen)return false;
            bool accepted = Game.Quests.AcceptQuest(QuestDefinition.First);
            notice = accepted ? "依頼を受けました。お茶を合成しましょう。" : Game.Quests.LastError;
            feedback[accepted?ScreenId.Merge:ScreenId.Quest]=notice;
            Render(accepted ? "merge" : "quest");
            return accepted;
        }
        public bool DropItem(int from, int to)
        {
            if (!Game.State.purchaseNoticeAcknowledged || IsPurchaseNoticeOpen || mergeAnimation != null) return false;
            bool occupied=Game.Board.At(to)!=null;
            bool merged = occupied && Game.TryMerge(from, to);
            bool changed = occupied ? merged : Game.TryMove(from, to);
            notice = changed ? (merged ? "キラッ！ お茶が育ちました。" : "移動しました。") : "その場所には移動・合成できません。";
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
            if(!Game.State.purchaseNoticeAcknowledged || IsPurchaseNoticeOpen || (CurrentScreen!=ScreenId.Quest && CurrentScreen!=ScreenId.Merge))return new DeliveryResult(false,"この画面では納品できません。");
            int coins=Game.State.coins,town=Game.State.townGrowthLevel;var result=Game.Quests.TryDeliver();
            if(result.Success)
            {
                feedback.Remove(ScreenId.Merge); feedback.Remove(ScreenId.Quest);
                LastDelivery=new DeliveryPresentation(Game.State.coins-coins,town,Game.State.townGrowthLevel);
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
                case ScreenId.Home:TopHomeScreens.BuildHome(root,context);break;
                case ScreenId.Quest:QuestMergeScreens.BuildQuest(root,context);break;
                case ScreenId.Merge:QuestMergeScreens.BuildMerge(root,context);break;
                case ScreenId.Settings:SettingsScreen.Build(root,context);break;
                case ScreenId.Result:
                    if(LastDelivery==null){Render("home");return;}
                    RewardGrowthScreens.BuildResult(root,context,LastDelivery);
                    if(!appPaused && !RewardVisible && presentationAnimation==null)presentationAnimation=StartCoroutine(RevealReward());break;
                case ScreenId.Growth:
                    if(LastDelivery==null){Render("home");return;}
                    RewardGrowthScreens.BuildGrowth(root,context,LastDelivery);break;
            }
            if(appPaused)
            {
                foreach(var reveal in page.GetComponentsInChildren<SoftReveal>())reveal.enabled=false;
                foreach(var reaction in page.GetComponentsInChildren<CharacterReaction>())reaction.enabled=false;
                foreach(var ambient in page.GetComponentsInChildren<TownAmbientGraphic>())ambient.enabled=false;
            }
        }
        public void TapCell(int cell)
        {
            if(mergeAnimation!=null || IsPurchaseNoticeOpen)return;
            if (selected < 0) { if (Game.Board.At(cell) != null) { selected = cell; feedback[ScreenId.Merge] = "移動先のお茶または空きマスをタップ"; Render("merge"); } }
            else DropItem(selected, cell);
        }
        public void CreateTea()
        {
            if(mergeAnimation!=null || IsPurchaseNoticeOpen)return;
            bool added=Game.AddItem("tea",1); feedback[ScreenId.Merge]=added?"お茶を作りました。":Game.LastError.Length>0?Game.LastError:"空きマスを確認してください。";
            if(added)PlayCue(AudioCue.Confirm);Render("merge");
        }
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
