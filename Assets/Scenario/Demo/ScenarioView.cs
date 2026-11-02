using System;
using System.Linq;
using System.Text;
using ScenarioBuilder;
using UnityEngine;
using UnityEngine.UI;

/// Демонстрационный UI: задания и их статусы, сообщения о нарушениях, итог, журнал и повторная оценка.
public class ScenarioView : MonoBehaviour
{
    public ScenarioRunner runner;
    [Tooltip("Анализаторы, доступные для повторной оценки")]
    public ResultAnalyzer[] analyzers;
    public Text tasksText, messageText, resultText, logText;
    public GameObject resultPanel, logPanel;
    public Dropdown analyzerDropdown;

    const int LogLines = 150; // shortcut: legacy Text ограничен ~16k символов; для длинных журналов — виртуализированный список

    void OnEnable()
    {
        runner.Changed += Refresh;
        runner.MessageShown += ShowMessage;
        runner.Finished += OnFinished;
    }

    void OnDisable()
    {
        runner.Changed -= Refresh;
        runner.MessageShown -= ShowMessage;
        runner.Finished -= OnFinished;
    }

    void Start()
    {
        analyzerDropdown.ClearOptions();
        analyzerDropdown.AddOptions(analyzers.Select(a => a.title).ToList());
        resultPanel.SetActive(false);
        logPanel.SetActive(false);
        messageText.text = "";
        Refresh();
    }

    public void StartMode(int index) => runner.Restart(index);

    public void ToggleLog()
    {
        logPanel.SetActive(!logPanel.activeSelf);
        Refresh();
    }

    public void Reanalyze() => ShowResult(analyzers[analyzerDropdown.value]);

    void OnFinished()
    {
        var analyzer = runner.Mode.analyzer ? runner.Mode.analyzer : analyzers[0];
        analyzerDropdown.SetValueWithoutNotify(Math.Max(0, Array.IndexOf(analyzers, analyzer)));
        resultPanel.SetActive(true);
        ShowResult(analyzer);
        Refresh();
    }

    void ShowResult(ResultAnalyzer analyzer) => resultText.text = $"<b>{analyzer.title}</b>\n{analyzer.Analyze(runner.Journal)}";

    void ShowMessage(string text) => messageText.text = text;

    void Refresh()
    {
        var mode = runner.Mode;
        var sb = new StringBuilder();
        if (!mode) sb.Append("Выберите режим прохождения.");
        else
        {
            sb.AppendLine($"<b>{mode.title}</b>{(mode.IsRunning ? "" : " — завершено")}");
            for (int g = 0; g < mode.groups.Count; g++)
            {
                if (mode.groups.Count > 1) sb.AppendLine($"<b>{(g == mode.CurrentGroupIndex ? "» " : "")}{mode.groups[g].name}</b>");
                foreach (var e in mode.groups[g].steps.Where(e => e.step))
                    sb.AppendLine($"  {Label(mode.StatusOf(e.step))}  {e.step.title}");
            }
        }
        tasksText.text = sb.ToString();
        if (logPanel.activeSelf) logText.text = string.Join("\n", runner.Journal.Skip(Math.Max(0, runner.Journal.Count - LogLines)));
    }

    static string Label(StepStatus s) => s switch
    {
        StepStatus.Active => "<color=#ffd84a>АКТИВЕН</color>",
        StepStatus.Completed => "<color=#6fdc6f>выполнен</color>",
        StepStatus.Failed => "<color=#ff6a6a>провален</color>",
        StepStatus.Skipped => "<color=#ffa040>пропущен</color>",
        _ => "<color=#9a9a9a>ожидает</color>",
    };
}
