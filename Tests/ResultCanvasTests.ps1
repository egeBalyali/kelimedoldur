$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$stubs = @"
using System;
using System.Reflection;
namespace UnityEngine {
 public class MonoBehaviour {}
 public class SerializeField : Attribute {}
 public class Sprite {}
 public class Canvas { public bool enabled = true; }
 public class GameObject {
  public bool activeSelf; public Canvas canvas = new Canvas();
  public void SetActive(bool active) { activeSelf = active; }
  public bool TryGetComponent<T>(out T component) where T : class { component = canvas as T; return component != null; }
 }
}
namespace UnityEngine.UI {
 public class Image { public UnityEngine.Sprite sprite; }
 public class Button { public bool interactable; public Click onClick = new Click();
  public class Click { public void AddListener(Action a) {} public void RemoveListener(Action a) {} }
 }
}
public class LevelManager {
 public event Action LevelWon; public event Action<int> LevelLoaded;
 public bool CanPlay = true;
 public bool RetryFailedLevel() => CanPlay;
 public void ResumeSavedLevel() => LevelLoaded?.Invoke(1);
 public void Win() => LevelWon?.Invoke();
}
public class LevelFlowManager {
 public event Action LevelCompleted; public event Action AnswerIncorrect; public event Action TimeExpired;
 public void Win() => LevelCompleted?.Invoke(); public void Fail() => AnswerIncorrect?.Invoke(); public void Expire() => TimeExpired?.Invoke();
}
public class MainMenuController { public void ShowMainMenu() {} }
public class LevelScoreManager { public int CurrentScore; }
public class ScoreEffectController { public int total = -1; public int plays; public void ShowTotal(int points) { total = points; plays++; } }
public static class ResultCanvasChecks {
 static void Set(object o, string field, object value) => o.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(o,value);
 static void Call(object o,string method) => o.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(o,null);
 static void Check(bool value,string message) { if (!value) throw new Exception(message); }
 public static string Run() {
  var result = new ResultCanvasController(); var flow = new LevelFlowManager(); var manager = new LevelManager();
  var win = new UnityEngine.GameObject(); var lose = new UnityEngine.GameObject();
  var game = new UnityEngine.Canvas(); var controls = new UnityEngine.Canvas();
  var score = new LevelScoreManager { CurrentScore = 1205 }; var effect = new ScoreEffectController();
  var background = new UnityEngine.UI.Image();
  var timeout = new UnityEngine.Sprite(); var wrongTries = new UnityEngine.Sprite();
  Set(result,"loseBackground",background); Set(result,"timeoutPicture",timeout); Set(result,"wrongTriesPicture",wrongTries);
  Set(result,"levelManager",manager); Set(result,"flowManager",flow); Set(result,"winCanvas",win); Set(result,"loseCanvas",lose);
  Set(result,"gameplayCanvas",game); Set(result,"controlsCanvas",controls); Set(result,"scoreManager",score); Set(result,"winScoreEffect",effect);
  Call(result,"Awake"); Call(result,"OnEnable"); win.canvas.enabled = false;
  flow.Win();
  Check(win.activeSelf && win.canvas.enabled && !lose.activeSelf, "Successful answer directly opens and enables the win canvas");
  Check(!game.enabled && !controls.enabled, "Gameplay canvases hidden");
  Check(effect.total == 1205 && effect.plays == 1, "Final score captured for animation");
  score.CurrentScore = 999; manager.Win(); flow.Win();
  Check(effect.total == 1205 && effect.plays == 1, "Duplicate events cannot restart count-up or replace captured score");
  result.NextLevel(); Check(!win.activeSelf && game.enabled && controls.enabled, "Next restores gameplay and closes win");
  score.CurrentScore = 0; flow.Win(); Check(effect.total == 0 && effect.plays == 2, "Zero score still shows win");
  result.Home(); flow.Fail(); Check(lose.activeSelf && !win.activeSelf && effect.plays == 2, "Losing never starts score celebration");
  Check(background.sprite == wrongTries, "Third wrong try shows wrong-tries picture");
  flow.Expire(); Check(background.sprite == wrongTries, "Duplicate loss cannot change the original cause");
  result.Retry(); flow.Expire(); Check(lose.activeSelf && background.sprite == timeout, "Timeout shows timeout picture after retry");
  result.Retry(); flow.Fail(); Check(background.sprite == wrongTries, "Next loss switches back to wrong-tries picture");
  Call(result,"OnDisable"); flow.Win(); Check(!win.activeSelf, "Disabled controller unsubscribes");
  return "PASS: direct win signal, canvas enablement, frozen final score, duplicate events, zero score, next level, loss and cleanup.";
 }
}
"@
$source = 'using UnityEngine; using UnityEngine.UI;' + "`n" + $stubs + "`n" + ([IO.File]::ReadAllText((Join-Path $root 'Assets/Scripts/Managers/ResultCanvasController.cs')) -replace '(?m)^using [^;]+;\r?\n','')
Add-Type -TypeDefinition $source -CompilerOptions '/nowarn:0649'
[ResultCanvasChecks]::Run()
