using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ScenarioBuilder.Tests
{
    /// Контрольные примеры из задания на минимальном сценарии без сцены.
    public class ScenarioTests
    {
        readonly List<GameObject> created = new();

        T Make<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            created.Add(go);
            return go.AddComponent<T>();
        }

        [TearDown]
        public void TearDown() => created.ForEach(Object.DestroyImmediate);

        ActionStep MakeStep(string id, ActionSource action, ActionSource forbidden = null)
        {
            var s = Make<ActionStep>(id);
            s.actions.Add(action);
            if (forbidden) s.forbidden.Add(forbidden);
            return s;
        }

        static StepGroup Group(string name, params (Step step, bool withPrevious)[] steps) => new()
        {
            name = name,
            steps = steps.Select(s => new StepEntry { step = s.step, withPrevious = s.withPrevious }).ToList(),
        };

        [Test]
        public void Training_FirstSecondThirdAttempt_Gives15()
        {
            var a = Enumerable.Range(1, 3).Select(i => Make<ActionSource>("a" + i)).ToArray();
            var f = Enumerable.Range(1, 3).Select(i => Make<ActionSource>("f" + i)).ToArray();
            var steps = Enumerable.Range(0, 3).Select(i => MakeStep("S" + i, a[i], f[i])).ToArray();
            var mode = Make<TrainingMode>("training");
            mode.groups.Add(Group("all", (steps[0], false), (steps[1], true), (steps[2], true)));
            var runner = Make<ScenarioRunner>("runner");

            runner.Run(mode);
            foreach (var act in new[] { a[0], f[1], a[1], f[2], f[2], a[2] }) mode.Handle(act);

            Assert.IsFalse(mode.IsRunning);
            var result = Make<TrainingAnalyzer>("ta").Analyze(runner.Journal);
            Assert.AreEqual(15, result.score);
            Assert.AreEqual(30, result.maxScore);
            Assert.IsEmpty(result.warnings);
        }

        [Test]
        public void Exam_TwoViolationsInGroupOfFour_SkipsRest_Gives70()
        {
            var a = Enumerable.Range(1, 5).Select(i => Make<ActionSource>("a" + i)).ToArray();
            var f = Make<ActionSource>("f");
            var g = Make<ActionSource>("g");
            var s = Enumerable.Range(0, 5).Select(i => MakeStep("S" + i, a[i], i == 0 ? f : i == 1 ? g : null)).ToArray();
            var mode = Make<ExamMode>("exam");
            mode.groups.Add(Group("1", (s[0], false), (s[1], true), (s[2], true), (s[3], true)));
            mode.groups.Add(Group("2", (s[4], false)));
            var runner = Make<ScenarioRunner>("runner");

            runner.Run(mode);
            mode.Handle(f);
            mode.Handle(g);
            Assert.AreEqual(StepStatus.Skipped, mode.StatusOf(s[2]));
            Assert.AreEqual(StepStatus.Active, mode.StatusOf(s[4]));
            mode.Handle(a[4]);

            Assert.IsFalse(mode.IsRunning);
            Assert.AreEqual(70, Make<ExamAnalyzer>("ea").Analyze(runner.Journal).score);
        }

        [Test]
        public void MixedActivation_BatchesOpenInOrder()
        {
            var a = Enumerable.Range(1, 5).Select(i => Make<ActionSource>("a" + i)).ToArray();
            var s = Enumerable.Range(0, 5).Select(i => MakeStep("S" + i, a[i])).ToArray();
            var mode = Make<TrainingMode>("training");
            mode.outOfTurn = OutOfTurnPolicy.Ignore;
            mode.groups.Add(Group("all", (s[0], false), (s[1], true), (s[2], false), (s[3], false), (s[4], true)));
            var runner = Make<ScenarioRunner>("runner");
            runner.Run(mode);

            mode.Handle(a[2]); // вне очереди и игнорируется
            Assert.AreEqual(StepStatus.Pending, mode.StatusOf(s[2]));
            mode.Handle(a[1]);
            mode.Handle(a[0]);
            Assert.AreEqual(StepStatus.Active, mode.StatusOf(s[2]));
            Assert.AreEqual(StepStatus.Pending, mode.StatusOf(s[3]));
            mode.Handle(a[2]);
            Assert.AreEqual(StepStatus.Active, mode.StatusOf(s[3]));
            Assert.AreEqual(StepStatus.Active, mode.StatusOf(s[4]));
            Assert.IsFalse(runner.Journal.Any(e => e.type == LogTypes.Violation));
        }

        [Test]
        public void OutOfTurn_DefaultIsViolation()
        {
            var a = Enumerable.Range(1, 2).Select(i => Make<ActionSource>("a" + i)).ToArray();
            var s = Enumerable.Range(0, 2).Select(i => MakeStep("S" + i, a[i])).ToArray();
            var mode = Make<TrainingMode>("training");
            mode.groups.Add(Group("all", (s[0], false), (s[1], false)));
            var runner = Make<ScenarioRunner>("runner");
            runner.Run(mode);

            mode.Handle(a[1]);
            Assert.AreEqual(1, runner.Journal.Count(e => e.type == LogTypes.Violation && e.stepId == "S1"));
        }

        [Test]
        public void Exam_ParallelGroups_HaveOwnThresholds()
        {
            var a = Enumerable.Range(1, 4).Select(i => Make<ActionSource>("a" + i)).ToArray();
            var f = Make<ActionSource>("f");
            var s = Enumerable.Range(0, 4).Select(i => MakeStep("S" + i, a[i], i == 2 ? f : null)).ToArray();
            var mode = Make<ExamMode>("exam");
            mode.violationThreshold = 1;
            mode.groups.Add(Group("A", (s[0], false), (s[1], false)));
            var b = Group("B", (s[2], false));
            b.withPrevious = true;
            mode.groups.Add(b);
            mode.groups.Add(Group("C", (s[3], false)));
            var runner = Make<ScenarioRunner>("runner");
            runner.Run(mode);

            Assert.AreEqual(StepStatus.Active, mode.StatusOf(s[0]));
            Assert.AreEqual(StepStatus.Active, mode.StatusOf(s[2]));
            mode.Handle(f); // порог группы B, группа A продолжается
            Assert.AreEqual(StepStatus.Failed, mode.StatusOf(s[2]));
            Assert.AreEqual(StepStatus.Active, mode.StatusOf(s[0]));
            Assert.AreEqual(StepStatus.Pending, mode.StatusOf(s[3]));
            mode.Handle(a[0]);
            mode.Handle(a[1]);
            Assert.AreEqual(StepStatus.Active, mode.StatusOf(s[3]));
        }

        [Test]
        public void Exam_OutOfTurnFailure_IsLoggedWithStepsOwnGroup()
        {
            var a = Enumerable.Range(1, 2).Select(i => Make<ActionSource>("a" + i)).ToArray();
            var s = Enumerable.Range(0, 2).Select(i => MakeStep("S" + i, a[i])).ToArray();
            var mode = Make<ExamMode>("exam");
            mode.groups.Add(Group("1", (s[0], false)));
            mode.groups.Add(Group("2", (s[1], false)));
            var runner = Make<ScenarioRunner>("runner");
            runner.Run(mode);

            mode.Handle(a[1]);
            mode.Handle(a[0]);

            Assert.IsFalse(mode.IsRunning);
            var failed = runner.Journal.Single(e => e.type == LogTypes.StepFinished && e.stepId == "S1");
            Assert.AreEqual("Failed", failed.result);
            Assert.AreEqual("2", failed.Get("group"));
        }

        class BrokenLogger : ScenarioLogger
        {
            public override void Write(LogEntry entry) => throw new System.IO.IOException("disk full");
        }

        [Test]
        public void BrokenLogger_DoesNotStopScenario()
        {
            var a = Make<ActionSource>("a");
            var s = MakeStep("S0", a);
            var mode = Make<TrainingMode>("training");
            mode.groups.Add(Group("all", (s, false)));
            var runner = Make<ScenarioRunner>("runner");
            runner.loggers.Add(Make<BrokenLogger>("logger"));
            LogAssert.ignoreFailingMessages = true;

            runner.Run(mode);
            mode.Handle(a);

            LogAssert.ignoreFailingMessages = false;
            Assert.IsFalse(mode.IsRunning);
            Assert.AreEqual(StepStatus.Completed, mode.StatusOf(s));
        }

        [Test]
        public void TrainingAnalyzer_PointsAreConfigurable()
        {
            var a = Make<ActionSource>("a");
            var f = Make<ActionSource>("f");
            var s = MakeStep("S0", a, f);
            var mode = Make<TrainingMode>("training");
            mode.groups.Add(Group("all", (s, false)));
            var runner = Make<ScenarioRunner>("runner");
            runner.Run(mode);
            mode.Handle(f);
            mode.Handle(a);

            var analyzer = Make<TrainingAnalyzer>("ta");
            analyzer.pointsByAttempt = new[] { 3, 2, 1 };
            var result = analyzer.Analyze(runner.Journal);
            Assert.AreEqual(2, result.score);
            Assert.AreEqual(3, result.maxScore);
        }

        [Test]
        public void Analyzer_MissingOptionalData_WarnsInsteadOfThrowing()
        {
            var journal = new List<LogEntry> { new() { stepId = "X", type = "SomethingNew" } };
            var result = Make<ExamAnalyzer>("ea").Analyze(journal);
            Assert.IsNotEmpty(result.warnings);
            Assert.AreEqual(95, result.score);
        }
    }
}
