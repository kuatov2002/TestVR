using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ScenarioBuilder
{
    /// Связывает источники действий, выбранный режим, журнал и логгеры.
    /// Сам правил не знает: правила — в режиме, логика шага — в шаге, оценка — в анализаторе.
    public class ScenarioRunner : MonoBehaviour
    {
        [Tooltip("Режимы, доступные для запуска")]
        public List<ScenarioMode> modes = new();
        [Tooltip("Режим, запускаемый при старте сцены. Пусто — ждать выбора пользователя.")]
        public ScenarioMode startMode;
        [Tooltip("Куда дополнительно пишется журнал")]
        public List<ScenarioLogger> loggers = new();

        static int requestedMode = -1; // переживает перезагрузку сцены при перезапуске

        public ScenarioMode Mode { get; private set; }
        public List<LogEntry> Journal { get; private set; } = new();

        public event Action Changed;
        /// Текст и признак нарушения (false — информационное сообщение).
        public event Action<string, bool> MessageShown;
        public event Action Finished;

        float startTime;

        void OnEnable() => ActionSource.Fired += OnAction;
        void OnDisable() => ActionSource.Fired -= OnAction;

        void Start()
        {
            var mode = requestedMode >= 0 && requestedMode < modes.Count ? modes[requestedMode] : startMode;
            requestedMode = -1;
            if (mode) Run(mode);
        }

        /// Перезапуск с чистой сценой (позиции, переключатели и т.п. возвращаются в исходное состояние).
        public void Restart(int modeIndex)
        {
            requestedMode = modeIndex;
            SceneManager.LoadScene(gameObject.scene.buildIndex);
        }

        public void Run(ScenarioMode mode)
        {
            Mode = mode;
            Journal = new List<LogEntry>();
            startTime = Time.time;
            foreach (var l in loggers) if (l) l.Begin();
            mode.Begin(this);
            Changed?.Invoke();
        }

        public void Log(string type, Step step, string result = null, params LogField[] data)
        {
            var entry = new LogEntry { time = Time.time - startTime, stepId = step ? step.Id : null, type = type, result = result, data = new List<LogField>(data) };
            Journal.Add(entry);
            foreach (var l in loggers) if (l) l.Write(entry);
        }

        public void Message(string text, bool violation = true) => MessageShown?.Invoke(text, violation);

        public void NotifyFinished() => Finished?.Invoke();

        void OnAction(ActionSource action)
        {
            if (!Mode || !Mode.IsRunning) return;
            Log(LogTypes.Action, null, action.ActionName);
            Mode.Handle(action);
            Changed?.Invoke();
        }
    }
}
