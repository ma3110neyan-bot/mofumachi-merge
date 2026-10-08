using System.Collections;
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
        private RectTransform safeRoot;
        private GameObject page;
        private Font font;
        private Texture2D characters;
        private AudioManager audioManager;
        private string view, settingsReturnView = "title", notice = "";
        private int selected = -1;
        private Rect previousSafeArea;
        private Vector2 previousSize;
        private Coroutine mergeAnimation;
        private readonly Color pink = new Color(1, .74f, .82f);
        private readonly Color ink = new Color(.31f, .22f, .38f);

        private void Awake()
        {
            if (session == null) Initialize(new SaveService(Application.persistentDataPath));
            font = Resources.Load<Font>("Mofumachi/UIFont");
            characters = Resources.Load<Texture2D>("Mofumachi/CharacterMaster");
            if (font == null || characters == null)
                throw new System.InvalidOperationException("Mofumachi UI resources are missing.");
            audioManager = gameObject.AddComponent<AudioManager>();
            audioManager.ApplySettings(Game.State.bgmEnabled, Game.State.seEnabled);
            var canvas = new GameObject("Mofumachi Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.transform.SetParent(transform, false);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(360, 640);
            scaler.matchWidthOrHeight = 0;
            safeRoot = Node("Safe area", canvas.transform, Vector2.zero, Vector2.one);
            if (EventSystem.current == null)
            {
                var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform, false);
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            UpdateSafeArea();
        }
        private void Start() => Render(SceneManager.GetActiveScene().name == "TitleScene" ? "title" : "home");
        public void Initialize(IStateStore store)
        {
            var loaded = store.Load();
            session = new GameStateManager(loaded.State, store);
            notice = loaded.Reason;
        }
        private void Update() => UpdateSafeArea();
        private void UpdateSafeArea()
        {
            var size = new Vector2(Screen.width, Screen.height);
            var area = Screen.safeArea;
            if (safeRoot == null || size.x <= 0 || size.y <= 0 || (size == previousSize && area == previousSafeArea)) return;
            safeRoot.anchorMin = area.position / size;
            safeRoot.anchorMax = (area.position + area.size) / size;
            safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
            previousSize = size; previousSafeArea = area;
        }
        public void BeginGame()
        {
            if (!Persist()) { Render("title"); return; }
            SceneManager.LoadScene("GameScene");
        }
        public void ShowHome() => Navigate("home");
        public void ShowQuest() => Navigate("quest");
        public void ShowMerge() => Navigate("merge");
        public void ShowSettings()
        {
            if (view != "settings") settingsReturnView = view ?? "title";
            Navigate("settings");
        }
        private void Navigate(string target) { Persist(); Render(target); }
        public bool Persist()
        {
            if (session == null) return true;
            if (mergeAnimation != null) { StopCoroutine(mergeAnimation); mergeAnimation = null; }
            Game.Board.ReleaseLocks(); Game.Quests.RefreshProgress(); selected = -1;
            bool saved = Game.Save();
            if (!saved) notice = Game.LastError;
            return saved;
        }
        public bool AcceptQuest()
        {
            bool accepted = Game.Quests.AcceptQuest(QuestDefinition.First);
            notice = accepted ? "依頼を受けました。お茶を合成しましょう。" : Game.Quests.LastError;
            Render(accepted ? "merge" : "quest");
            return accepted;
        }
        public bool DropItem(int from, int to)
        {
            if (mergeAnimation != null) return false;
            bool merged = Game.TryMerge(from, to);
            bool changed = merged || Game.TryMove(from, to);
            notice = changed ? (merged ? "キラッ！ お茶が育ちました。" : "移動しました。") : "その場所には移動・合成できません。";
            if (!changed && Game.LastError.Length > 0) notice = Game.LastError;
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
            if (!Game.Save()) notice = Game.LastError;
            Render(view);
        }
        public DeliveryResult Deliver()
        {
            var result = Game.Quests.TryDeliver(); notice = result.Message;
            if (result.Success) { audioManager.PlaySe(AudioCue.Reward); Render("result"); }
            else Render(view ?? "quest");
            return result;
        }
        private void OnApplicationPause(bool paused)
        {
            if (paused) Persist();
            // Rebuild also disables drag views, removing their transient ghost on pause.
            if (page != null && session != null) Render(view);
        }
        private void OnApplicationQuit() => Persist();
        private void OnDestroy() { if (session != null) Persist(); }

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
        private void Portrait(int i, Vector2 min, Vector2 max)
        {
            var rect = Node("Character " + i, page.transform, min, max);
            var image = rect.gameObject.AddComponent<RawImage>(); image.texture = characters;
            image.uvRect = new Rect((15 + i * 170) / 1536f, 104 / 1024f, 152 / 1536f, 150 / 1024f);
            var aspect = rect.gameObject.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent; aspect.aspectRatio = 152f / 150;
            var tap = rect.gameObject.AddComponent<Button>(); tap.targetGraphic = image;
            tap.onClick.AddListener(() => audioManager.PlaySe(AudioCue.Character));
        }
        private void Render(string target)
        {
            view = target;
            if (page != null) { page.SetActive(false); Destroy(page); }
            page = Node(target, safeRoot, Vector2.zero, Vector2.one).gameObject;
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
                var town = Node("Town", page.transform, new Vector2(.08f, .3f), new Vector2(.92f, .68f)).gameObject.AddComponent<RawImage>();
                town.texture = characters; town.uvRect = new Rect(530 / 1536f, 336 / 1024f, 252 / 1536f, 550 / 1024f);
                town.raycastTarget = false;
                town.gameObject.AddComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                town.GetComponent<AspectRatioFitter>().aspectRatio = 252f / 550;
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
        private void TapCell(int cell)
        {
            if (selected < 0) { if (Game.Board.At(cell) != null) { selected = cell; notice = "移動先のお茶または空きマスをタップ"; Render("merge"); } }
            else DropItem(selected, cell);
        }
        private void SetAudio(bool bgm, bool se)
        {
            if (Game.SetAudio(bgm, se)) audioManager.ApplySettings(bgm, se);
            else notice = Game.LastError;
            Render("settings");
        }
    }
}
