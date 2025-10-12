using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using TabernaNoctis.RandomSystem;

namespace TabernaNoctis.EditorTools
{
    public sealed class RandomMonitorWindow : EditorWindow
    {
        private enum StreamType
        {
            Daily,
            Persistent
        }

        [MenuItem("自制工具/随机系统/随机数监控器")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<RandomMonitorWindow>();
            wnd.titleContent = new GUIContent("Random Monitor");
            wnd.minSize = new Vector2(640, 420);
        }

        private TextField _playerIdField;
        private TextField _streamKeyField;
        private EnumField _streamTypeField;
        private Toggle _useUtcToggle;
        private TextField _dateYmdField;
        private Toggle _consumeToggle;
        private SliderInt _sampleCountSlider;
        private Label _statusLabel;

        private Button _btnLoadInit;
        private Button _btnPreview;
        private Button _btnHistogram;
        private Button _btnClear;

        private VisualElement _samplesContainer;
        private VisualElement _histContainer;

        private readonly ES3RandomStateStore _store = new ES3RandomStateStore();

        public void CreateGUI()
        {
            var root = rootVisualElement;
            // 引入全局颜色与组件样式
            root.styleSheets.Add(LoadStyle("Assets/Scripts/0_Editor/UITK/EditorColors.uss"));
            root.styleSheets.Add(LoadStyle("Assets/Scripts/0_Editor/UITK/RandomMonitorWindow.uss"));

            var wrapper = new VisualElement { name = "random-monitor" };
            wrapper.AddToClassList("random-monitor");
            wrapper.AddToClassList("app-root");

            wrapper.Add(BuildHeader());
            wrapper.Add(BuildInputs());
            wrapper.Add(BuildActions());
            wrapper.Add(BuildVisualSection());

            root.Add(wrapper);

            // defaults
            _playerIdField.value = "player001";
            _streamKeyField.value = "DailyMessage";
            _streamTypeField.value = StreamType.Daily;
            _useUtcToggle.value = false;
            _dateYmdField.value = NowDateString(false);
            _consumeToggle.value = false;
            _sampleCountSlider.value = 50;

            UpdateInteractable();
        }

        private VisualElement BuildHeader()
        {
            var header = new VisualElement();
            header.AddToClassList("section");

            var title = new Label("随机数管理监控器 (UITK)");
            title.AddToClassList("section-title");
            header.Add(title);

            _statusLabel = new Label("就绪");
            _statusLabel.AddToClassList("tag");
            _statusLabel.AddToClassList("tag-ok");
            header.Add(_statusLabel);
            return header;
        }

        private VisualElement BuildInputs()
        {
            var inputs = new VisualElement();
            inputs.AddToClassList("section");

            var row1 = new VisualElement();
            row1.AddToClassList("row");
            _playerIdField = new TextField("玩家ID");
            _playerIdField.RegisterValueChangedCallback(_ => UpdateInteractable());
            row1.Add(_playerIdField);

            _streamKeyField = new TextField("流Key");
            _streamKeyField.RegisterValueChangedCallback(_ => UpdateInteractable());
            row1.Add(_streamKeyField);

            _streamTypeField = new EnumField("类型", StreamType.Daily);
            _streamTypeField.Init(StreamType.Daily);
            _streamTypeField.RegisterValueChangedCallback(_ => UpdateInteractable());
            row1.Add(_streamTypeField);
            inputs.Add(row1);

            var row2 = new VisualElement();
            row2.AddToClassList("row");
            _useUtcToggle = new Toggle("使用UTC日期");
            _useUtcToggle.RegisterValueChangedCallback(_ => { _dateYmdField.value = NowDateString(_useUtcToggle.value); UpdateInteractable(); });
            row2.Add(_useUtcToggle);

            _dateYmdField = new TextField("日期(yyyyMMdd)");
            _dateYmdField.RegisterValueChangedCallback(_ => UpdateInteractable());
            row2.Add(_dateYmdField);

            _consumeToggle = new Toggle("采样消耗状态");
            _consumeToggle.tooltip = "开启后，预览会推进随机流并保存（防呆：如仅查看请关闭）";
            row2.Add(_consumeToggle);
            inputs.Add(row2);

            var row3 = new VisualElement();
            row3.AddToClassList("row");
            _sampleCountSlider = new SliderInt("采样数量", 10, 200) { value = 50 };
            _sampleCountSlider.showInputField = true;
            row3.Add(_sampleCountSlider);
            inputs.Add(row3);

            return inputs;
        }

        private VisualElement BuildActions()
        {
            var actions = new VisualElement();
            actions.AddToClassList("section");

            var row = new VisualElement();
            row.AddToClassList("row");

            _btnLoadInit = new Button(OnLoadInit) { text = "加载/初始化流" };
            _btnLoadInit.AddToClassList("btn");
            _btnLoadInit.AddToClassList("btn-success");
            row.Add(_btnLoadInit);

            _btnPreview = new Button(OnPreview) { text = "预览样本" };
            _btnPreview.AddToClassList("btn");
            row.Add(_btnPreview);

            _btnHistogram = new Button(OnHistogram) { text = "直方图" };
            _btnHistogram.AddToClassList("btn");
            row.Add(_btnHistogram);

            _btnClear = new Button(OnClear) { text = "清除ES3状态" };
            _btnClear.AddToClassList("btn");
            _btnClear.AddToClassList("btn-danger");
            row.Add(_btnClear);

            actions.Add(row);
            return actions;
        }

        private VisualElement BuildVisualSection()
        {
            var vis = new VisualElement();
            vis.AddToClassList("section");

            var row1 = new VisualElement();
            row1.AddToClassList("row");
            var label1 = new Label("采样可视化");
            label1.AddToClassList("subtitle");
            row1.Add(label1);
            vis.Add(row1);

            _samplesContainer = new ScrollView();
            _samplesContainer.AddToClassList("samples");
            vis.Add(_samplesContainer);

            var row2 = new VisualElement();
            row2.AddToClassList("row");
            var label2 = new Label("直方图 (10 bins)");
            label2.AddToClassList("subtitle");
            row2.Add(label2);
            vis.Add(row2);

            _histContainer = new VisualElement();
            _histContainer.AddToClassList("hist");
            vis.Add(_histContainer);

            return vis;
        }

        private void UpdateInteractable()
        {
            bool okId = !string.IsNullOrWhiteSpace(_playerIdField?.value);
            bool okKey = !string.IsNullOrWhiteSpace(_streamKeyField?.value);
            bool isDaily = (StreamType)_streamTypeField.value == StreamType.Daily;
            bool dateValid = !isDaily || TryParseYmd(_dateYmdField?.value, out _);

            _dateYmdField.SetEnabled(isDaily);

            bool ok = okId && okKey && dateValid;
            _btnLoadInit?.SetEnabled(ok);
            _btnPreview?.SetEnabled(ok);
            _btnHistogram?.SetEnabled(ok);
            _btnClear?.SetEnabled(ok);

            _statusLabel.text = ok ? (isDaily ? "每日流：输入有效" : "持久流：输入有效") : "请填写必填项或修正日期";
            _statusLabel.RemoveFromClassList("tag-ok");
            _statusLabel.RemoveFromClassList("tag-warn");
            _statusLabel.AddToClassList(ok ? "tag-ok" : "tag-warn");
        }

        private void OnLoadInit()
        {
            if (!EnsureValidInput(out var playerId, out var streamKey, out var date, out var isDaily)) return;

            try
            {
                if (isDaily)
                    _ = RandomService.Instance.GetDailyStream(streamKey, playerId, date, autoSave: true);
                else
                    _ = RandomService.Instance.GetPersistentStream(streamKey, playerId, autoSave: true);

                ShowOk("已加载/初始化随机流并保存状态");
            }
            catch (Exception e)
            {
                ShowErr("加载失败: " + e.Message);
            }
        }

        private void OnPreview()
        {
            if (!EnsureValidInput(out var playerId, out var streamKey, out var date, out var isDaily)) return;
            int count = Mathf.Clamp(_sampleCountSlider.value, 1, 500);
            bool consume = _consumeToggle.value;

            try
            {
                var samples = new List<float>(count);

                if (consume)
                {
                    IRandomSource rng = isDaily
                        ? RandomService.Instance.GetDailyStream(streamKey, playerId, date, autoSave: true)
                        : RandomService.Instance.GetPersistentStream(streamKey, playerId, autoSave: true);
                    for (int i = 0; i < count; i++) samples.Add(rng.Value01());
                }
                else
                {
                    var st = GetOrCreateState(playerId, streamKey, date, isDaily, createOnly: true);
                    var tmp = new UnityRandomStream(st, null);
                    for (int i = 0; i < count; i++) samples.Add(tmp.Value01());
                }

                DrawSamples(samples);
                ShowOk(consume ? "预览完成（已消耗状态）" : "预览完成（未消耗状态）");
            }
            catch (Exception e)
            {
                ShowErr("预览失败: " + e.Message);
            }
        }

        private void OnHistogram()
        {
            if (!EnsureValidInput(out var playerId, out var streamKey, out var date, out var isDaily)) return;
            int count = Mathf.Clamp(_sampleCountSlider.value, 10, 2000);
            bool consume = _consumeToggle.value;

            try
            {
                var samples = new List<float>(count);
                if (consume)
                {
                    IRandomSource rng = isDaily
                        ? RandomService.Instance.GetDailyStream(streamKey, playerId, date, autoSave: true)
                        : RandomService.Instance.GetPersistentStream(streamKey, playerId, autoSave: true);
                    for (int i = 0; i < count; i++) samples.Add(rng.Value01());
                }
                else
                {
                    var st = GetOrCreateState(playerId, streamKey, date, isDaily, createOnly: true);
                    var tmp = new UnityRandomStream(st, null);
                    for (int i = 0; i < count; i++) samples.Add(tmp.Value01());
                }

                DrawHistogram(samples, 10);
                ShowOk(consume ? "直方图完成（已消耗状态）" : "直方图完成（未消耗状态）");
            }
            catch (Exception e)
            {
                ShowErr("直方图失败: " + e.Message);
            }
        }

        private void OnClear()
        {
            if (!EnsureValidInput(out var playerId, out var streamKey, out var date, out var isDaily)) return;
            string key = isDaily ? Es3KeyDaily(playerId, date, streamKey) : Es3KeyPersist(playerId, streamKey);
            if (!_store.Exists(key))
            {
                ShowWarn("无可清除的状态");
                return;
            }

            if (!EditorUtility.DisplayDialog("清除确认", "该操作将删除该流的已保存随机状态，确定继续？", "确定", "取消"))
                return;

            try
            {
                ES3.DeleteKey(key);
                ShowOk("已清除 ES3 状态");
            }
            catch (Exception e)
            {
                ShowErr("清除失败: " + e.Message);
            }
        }

        private void DrawSamples(List<float> values)
        {
            _samplesContainer.Clear();
            var row = new VisualElement();
            row.AddToClassList("chips");
            for (int i = 0; i < values.Count; i++)
            {
                float v = Mathf.Clamp01(values[i]);
                var chip = new VisualElement();
                chip.AddToClassList("chip");
                Color c = Color.HSVToRGB(v, 0.7f, 1f);
                chip.style.backgroundColor = new StyleColor(c);
                chip.tooltip = v.ToString("0.000");
                row.Add(chip);
            }
            _samplesContainer.Add(row);
        }

        private void DrawHistogram(List<float> values, int bins)
        {
            _histContainer.Clear();
            if (bins <= 0) bins = 10;
            var counts = new int[bins];
            for (int i = 0; i < values.Count; i++)
            {
                float v = Mathf.Clamp01(values[i]);
                int b = Mathf.Clamp((int)(v * bins), 0, bins - 1);
                counts[b]++;
            }
            int max = 1;
            for (int i = 0; i < bins; i++) if (counts[i] > max) max = counts[i];

            var barRow = new VisualElement();
            barRow.AddToClassList("hist-row");
            for (int i = 0; i < bins; i++)
            {
                var barWrap = new VisualElement();
                barWrap.AddToClassList("hist-cell");

                var bar = new VisualElement();
                bar.AddToClassList("hist-bar");
                float h = (float)counts[i] / max;
                bar.style.height = new Length(h * 100f, LengthUnit.Percent);
                bar.tooltip = counts[i].ToString();
                bar.style.backgroundColor = new StyleColor(Color.HSVToRGB((float)i / bins, 0.6f, 1f));
                barWrap.Add(bar);

                var label = new Label(((float)i / bins).ToString("0.0"));
                label.AddToClassList("hist-label");
                barWrap.Add(label);

                barRow.Add(barWrap);
            }
            _histContainer.Add(barRow);
        }

        private bool EnsureValidInput(out string playerId, out string streamKey, out DateTime date, out bool isDaily)
        {
            playerId = _playerIdField.value?.Trim();
            streamKey = _streamKeyField.value?.Trim();
            isDaily = (StreamType)_streamTypeField.value == StreamType.Daily;
            date = _useUtcToggle.value ? DateTime.UtcNow : DateTime.Now;
            if (isDaily)
            {
                if (!TryParseYmd(_dateYmdField.value, out date))
                {
                    ShowWarn("日期格式应为 yyyyMMdd");
                    return false;
                }
                if (_useUtcToggle.value)
                {
                    // interpret as UTC date if toggle set
                    date = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc);
                }
            }

            if (string.IsNullOrWhiteSpace(playerId) || string.IsNullOrWhiteSpace(streamKey))
            {
                ShowWarn("请填写 玩家ID 与 流Key");
                return false;
            }
            return true;
        }

        private UnityEngine.Random.State GetOrCreateState(string playerId, string streamKey, DateTime date, bool isDaily, bool createOnly)
        {
            string key = isDaily ? Es3KeyDaily(playerId, date, streamKey) : Es3KeyPersist(playerId, streamKey);
            if (_store.Exists(key))
                return _store.Load(key);

            int seed = isDaily ? HashUtil.SeedFrom(playerId, date, streamKey) : HashUtil.SeedFrom(playerId, new DateTime(2000, 1, 1), streamKey + "|PERSIST");

            var original = UnityEngine.Random.state;
            try
            {
                UnityEngine.Random.InitState(seed);
                var st = UnityEngine.Random.state;
                if (!createOnly) _store.Save(key, st);
                return st;
            }
            finally { UnityEngine.Random.state = original; }
        }

        private static string Es3KeyDaily(string playerId, DateTime date, string streamKey)
        {
            return "rng/daily/" + (playerId ?? string.Empty) + "/" + date.ToString("yyyyMMdd") + "/" + (streamKey ?? string.Empty);
        }

        private static string Es3KeyPersist(string playerId, string streamKey)
        {
            return "rng/persistent/" + (playerId ?? string.Empty) + "/" + (streamKey ?? string.Empty);
        }

        private static bool TryParseYmd(string ymd, out DateTime date)
        {
            date = DateTime.Now;
            if (string.IsNullOrEmpty(ymd) || ymd.Length != 8) return false;
            if (!int.TryParse(ymd.Substring(0, 4), out int y)) return false;
            if (!int.TryParse(ymd.Substring(4, 2), out int m)) return false;
            if (!int.TryParse(ymd.Substring(6, 2), out int d)) return false;
            try { date = new DateTime(y, m, d); return true; }
            catch { return false; }
        }

        private static string NowDateString(bool utc)
        {
            var dt = utc ? DateTime.UtcNow : DateTime.Now;
            return dt.ToString("yyyyMMdd");
        }

        private static StyleSheet LoadStyle(string path)
        {
            var ss = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            return ss;
        }

        private void ShowOk(string msg)
        {
            _statusLabel.text = msg;
            _statusLabel.RemoveFromClassList("tag-warn");
            _statusLabel.RemoveFromClassList("tag-err");
            _statusLabel.AddToClassList("tag-ok");
        }

        private void ShowWarn(string msg)
        {
            _statusLabel.text = msg;
            _statusLabel.RemoveFromClassList("tag-ok");
            _statusLabel.RemoveFromClassList("tag-err");
            _statusLabel.AddToClassList("tag-warn");
        }

        private void ShowErr(string msg)
        {
            _statusLabel.text = msg;
            _statusLabel.RemoveFromClassList("tag-ok");
            _statusLabel.RemoveFromClassList("tag-warn");
            _statusLabel.AddToClassList("tag-err");
        }
    }
}


