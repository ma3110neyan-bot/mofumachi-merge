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
        private bool transitioning;
        private readonly Dictionary<ScreenId,string> feedback = new Dictionary<ScreenId,string>();
        public ScreenId CurrentScreen { get; private set; }
        public bool IsPurchaseNoticeOpen => modal != null && modal.activeSelf;
        private RectTransform portraitRoot;
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
        private readonly Color pink = new Color(1, .74f, .82f);
        private readonly Color ink = new Color(.31f, .22f, .38f);

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
            portraitRoot=layout.ContentRoot;
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
            if(!growthCuePlayed){growthCuePlayed=true;PlayCue(AudioCue.Growth);}Navigate("growth");
        }
        private void OnApplicationPause(bool paused)
        {
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

        private RectTransform Node(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero; return rect;
        }
        private Text Label(string text, Transform parent, Vector2 min, Vector2 max, int size = 18)
        {
            var t = Node(text, parent, min, max).gameObject.AddComponent<Text>();
            t.text = text; t.font = font; t.fontSize = size; t.color = ink;
            t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false;
            return t;
        }
        private Button Button(string text, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction action)
        {
            var rect = Node(text, page.transform, min, max);
            var image = rect.gameObject.AddComponent<Image>(); image.color = pink;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.pressedColor = new Color(.92f, .79f, .86f); button.colors = colors;
            Label(text, rect, Vector2.zero, Vector2.one);
            button.onClick.AddListener(() => { audioManager.PlaySe(AudioCue.Confirm); action(); });
            return button;
        }
        private RawImage FittedImage(string name, Vector2 min, Vector2 max, float ratio)
        {
            // FitInParent drives its own anchors. Keep the page layout on a separate
            // slot so it fits this image into its assigned area, rather than the page.
            var slot = Node(name + " slot", page.transform, min, max);
            var rect = Node(name, slot, Vector2.zero, Vector2.one);
            var image = rect.gameObject.AddComponent<RawImage>();
            image.texture = characters;
            var aspect = rect.gameObject.AddComponent<AspectRatioFitter>();
            aspect.aspectRatio = ratio;
            aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            return image;
        }
        private void Portrait(int i, Vector2 min, Vector2 max)
        {
            var image = FittedImage("Character " + i, min, max, 152f / 150);
            image.uvRect = new Rect((15 + i * 170) / 1536f, 104 / 1024f, 152 / 1536f, 150 / 1024f);
            var tap = image.gameObject.AddComponent<Button>(); tap.targetGraphic = image;
            tap.onClick.AddListener(() => audioManager.PlaySe(AudioCue.Character));
        }
        private void Render(string target)
        {
            view = target; CurrentScreen=(ScreenId)System.Enum.Parse(typeof(ScreenId),target,true);
            if (page != null) { page.SetActive(false); Destroy(page); }
            page = Node(target, portraitRoot, Vector2.zero, Vector2.one).gameObject;
            if(target=="title" || target=="home" || target=="quest" || target=="merge" || target=="result" || target=="growth")
            {
                var root=(RectTransform)page.transform;
                switch(CurrentScreen){case ScreenId.Title:TopHomeScreens.BuildTitle(root,context);break;case ScreenId.Home:TopHomeScreens.BuildHome(root,context);break;case ScreenId.Quest:QuestMergeScreens.BuildQuest(root,context);break;case ScreenId.Merge:QuestMergeScreens.BuildMerge(root,context);break;case ScreenId.Result:if(LastDelivery==null){Render("home");return;}RewardGrowthScreens.BuildResult(root,context,LastDelivery);if(!RewardVisible && presentationAnimation==null)presentationAnimation=StartCoroutine(RevealReward());break;case ScreenId.Growth:if(LastDelivery==null){Render("home");return;}RewardGrowthScreens.BuildGrowth(root,context,LastDelivery);break;}
                return;
            }
            notice=FeedbackFor(CurrentScreen);
            page.AddComponent<Image>().color = new Color(1, .96f, .90f);
            Label("もふまちメルジュ", page.transform, new Vector2(.03f, .9f), new Vector2(.97f, 1), 28);
            Label($"Coins {Game.State.coins}　街 Lv.{Game.State.townGrowthLevel}", page.transform, new Vector2(.04f, .84f), new Vector2(.96f, .9f), 16);
            Label(notice, page.transform, new Vector2(.04f, .12f), new Vector2(.96f, .23f), 14);
            if (target != "merge")
                for (int i = 0; i < 6; i++) Portrait(i, new Vector2(.025f + i * .16f, .72f), new Vector2(.175f + i * .16f, .82f));
            if (target == "title")
            {
                Label("もふもふの街で、\n小さなお茶会をはじめよう", page.transform, new Vector2(.08f, .45f), new Vector2(.92f, .68f), 23);
                Button("はじめる", new Vector2(.12f, .3f), new Vector2(.88f, .4f), BeginGame);
            }
            else if (target == "home")
            {
                var town = FittedImage("Town", new Vector2(.08f, .3f), new Vector2(.92f, .68f), 252f / 550);
                town.uvRect = new Rect(530 / 1536f, 336 / 1024f, 252 / 1536f, 550 / 1024f);
                town.raycastTarget = false;
                Button("キャラ依頼", new Vector2(.05f, .24f), new Vector2(.48f, .32f), ShowQuest);
                Button("Merge", new Vector2(.52f, .24f), new Vector2(.95f, .32f), ShowMerge);
            }
            else if (target == "quest")
            {
                Label("お茶会の準備\nお茶 Lv.2 × 1\n報酬 30 Coins", page.transform, new Vector2(.06f, .46f), new Vector2(.94f, .69f), 23);
                Button(Game.State.completedQuestIds.Contains(QuestDefinition.First.Id) ? "依頼完了" : "依頼を受ける", new Vector2(.07f, .34f), new Vector2(.93f, .44f), () => AcceptQuest()).interactable = Game.State.activeQuestId.Length == 0;
                Button("Mergeへ", new Vector2(.07f, .24f), new Vector2(.5f, .32f), ShowMerge);
                Button("納品する", new Vector2(.54f, .24f), new Vector2(.93f, .32f), () => Deliver());
            }
            else if (target == "merge")
            {
                Label("同じお茶を重ねて合成", page.transform, new Vector2(.04f, .77f), new Vector2(.96f, .83f), 17);
                for (int cell = 0; cell < GameState.Columns * GameState.Rows; cell++)
                {
                    int index = cell, x = cell % GameState.Columns, y = cell / GameState.Columns;
                    var item = Game.Board.At(cell);
                    string text = item == null ? "" : $"お茶\nLv.{item.level}";
                    var button = Button(text, new Vector2(.045f + x * .184f, .685f - y * .075f), new Vector2(.219f + x * .184f, .755f - y * .075f), () => TapCell(index));
                    button.GetComponentInChildren<Text>().fontSize = 14;
                    button.GetComponent<Image>().color = Game.Board.IsLocked(cell) ? new Color(1, .93f, .66f) : new Color(.87f, .93f, .88f);
                    button.gameObject.AddComponent<MergeBoardView>().Initialize(this, index);
                }
                Button("お茶 Lv.1 を作る", new Vector2(.05f, .24f), new Vector2(.57f, .3f), () => { notice = Game.AddItem("tea", 1) ? "お茶を作りました。" : "空きマスまたは保存先を確認してください。"; Render("merge"); });
                Button("納品する", new Vector2(.61f, .24f), new Vector2(.95f, .3f), () => Deliver());
            }
            else if (target == "result")
            {
                Label("納品完了！\n+30 Coins\n街が成長しました", page.transform, new Vector2(.08f, .4f), new Vector2(.92f, .7f), 26);
                Button("街へ戻る", new Vector2(.1f, .27f), new Vector2(.9f, .36f), ShowHome);
            }
            else if (target == "settings")
            {
                Button("BGM " + (Game.State.bgmEnabled ? "ON" : "OFF"), new Vector2(.1f, .52f), new Vector2(.9f, .63f), () => SetAudio(!Game.State.bgmEnabled, Game.State.seEnabled));
                Button("SE " + (Game.State.seEnabled ? "ON" : "OFF"), new Vector2(.1f, .36f), new Vector2(.9f, .47f), () => SetAudio(Game.State.bgmEnabled, !Game.State.seEnabled));
                Label("現在の音は試作版です", page.transform, new Vector2(.08f, .24f), new Vector2(.92f, .31f), 14);
            }
            Button(target == "title" ? "設定" : target == "settings" ? "戻る" : "ホーム", new Vector2(.04f, .025f), new Vector2(.46f, .105f), () =>
            {
                if (target == "title") ShowSettings();
                else if (target == "settings") Navigate(settingsReturnView);
                else ShowHome();
            });
            Button("設定", new Vector2(.54f, .025f), new Vector2(.96f, .105f), ShowSettings);
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
        private void SetAudio(bool bgm, bool se)
        {
            if (Game.SetAudio(bgm, se)) audioManager.ApplySettings(bgm, se,Game.State.bgmVolume,Game.State.seVolume);
            else notice = Game.LastError;
            Render("settings");
        }
    }
}
