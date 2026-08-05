using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManagerEx
{
    GameObject _player;
    HashSet<GameObject> _monsters = new HashSet<GameObject>();

    public Action<int> OnSpawnEvent;
    public Action<Define.StageResult> OnStageEnded;
    public Action<bool> OnPauseChanged;

    public bool IsGameOver { get; private set; }
    public Define.StageResult Result { get; private set; }
    public bool IsPaused { get; private set; }
    public bool IsPlaying { get { return Result == Define.StageResult.None && IsPaused == false; } }

    public int CurrentLevel { get; private set; } = LevelTable.MinLevel;
    public int MaxUnlockedLevel { get; private set; } = LevelTable.MinLevel;
    public LevelConfig Level { get { return LevelTable.Get(CurrentLevel); } }

    public bool UsedRun { get; private set; }
    public int RunnerEvasions { get; private set; }
    public int LastScore { get; private set; }
    public int LastArtifacts { get; private set; }
    public string LastGrade { get; private set; } = "C";
    public int ConsecutiveFailures { get; private set; }

    public Action<int> OnLevelChanged;

    public GameObject GetPlayer() { return _player; }

    public void SetPlayer(GameObject go) { _player = go; }

    public void SetLevel(int level)
    {
        CurrentLevel = LevelTable.Clamp(level);

        if (CurrentLevel > MaxUnlockedLevel)
            MaxUnlockedLevel = CurrentLevel;

        if (OnLevelChanged != null)
            OnLevelChanged.Invoke(CurrentLevel);
    }

    public bool HasNextLevel { get { return CurrentLevel < LevelTable.MaxLevel; } }

    public void AdvanceLevel()
    {
        if (HasNextLevel)
            SetLevel(CurrentLevel + 1);
    }

    public void ReportRunUsed()
    {
        UsedRun = true;
    }

    public void ReportRunnerEvaded()
    {
        RunnerEvasions++;
    }

    public void BeginStage()
    {
        IsGameOver = false;
        Result = Define.StageResult.None;
        UsedRun = false;
        RunnerEvasions = 0;
        LastScore = 0;
        LastGrade = "C";
        SetPaused(false);
        Time.timeScale = 1.0f;
    }

    public void GameOver()
    {
        if (IsGameOver)
            return;

        if (DebugOverlay.Invulnerable)
            return;

        IsGameOver = true;
        ConsecutiveFailures++;

        GameObject player = GetPlayer();
        if (player != null)
        {
            Managers.Sound.PlayAtPointOptional(
                "death_contact",
                "moster growl (4)",
                player.transform.position,
                Define.Sound.Threat
            );
        }
        Managers.UI.ShowPopupUI<UI_GameOver>();
        EndStage(Define.StageResult.Caught);
    }

    public void ReportEscaped()
    {
        ReportEscaped(0, 0.0f, 0.0f);
    }

    public void ReportEscaped(int artifacts, float weightedArtifacts, float lampRemaining)
    {
        LastArtifacts = artifacts;
        LastScore = ScoreRules.Total(CurrentLevel, weightedArtifacts, lampRemaining, UsedRun, RunnerEvasions);
        LastGrade = LevelTable.Grade(CurrentLevel, LastScore);
        ConsecutiveFailures = 0;

        EndStage(Define.StageResult.Cleared);
    }

    void EndStage(Define.StageResult result)
    {
        if (Result != Define.StageResult.None)
            return;

        Result = result;

        if (OnStageEnded != null)
            OnStageEnded.Invoke(result);
    }

    public void SetPaused(bool paused)
    {
        Time.timeScale = paused ? 0 : 1;

        if (IsPaused == paused)
            return;

        IsPaused = paused;

        if (OnPauseChanged != null)
            OnPauseChanged.Invoke(paused);
    }

    public void TogglePause()
    {
        if (Result != Define.StageResult.None)
            return;

        SetPaused(IsPaused == false);
    }

    public GameObject Spawn(Define.WorldObject type, string path, Transform parent = null)
    {
        GameObject go = Managers.Resource.Instantiate(path, parent);

        switch (type)
        {
            case Define.WorldObject.Enemy:
                _monsters.Add(go);
                if (OnSpawnEvent != null)
                    OnSpawnEvent.Invoke(1);
                break;
            case Define.WorldObject.Player:
                _player = go;
                break;
        }

        return go;
    }

    public Define.WorldObject GetWorldObjectType(GameObject go)
    {
        BaseController bc = go.GetComponent<BaseController>();
        if (bc != null)
            return bc.WorldObjectType;

        EnemyBase enemy = go.GetComponent<EnemyBase>();
        if (enemy != null)
            return enemy.WorldObjectType;

        return Define.WorldObject.Unknown;
    }

    public void Despawn(GameObject go)
    {
        Define.WorldObject type = GetWorldObjectType(go);

        switch (type)
        {
            case Define.WorldObject.Enemy:
                {
                    if (_monsters.Contains(go))
                    {
                        _monsters.Remove(go);
                        if (OnSpawnEvent != null)
							OnSpawnEvent.Invoke(-1);
					}
                }
                break;
            case Define.WorldObject.Player:
                {
					if (_player == go)
						_player = null;
				}
                break;
        }

        Managers.Resource.Destroy(go);
    }

    public void Clear()
    {
        IsGameOver = false;
        Result = Define.StageResult.None;
        IsPaused = false;
        _player = null;
        _monsters.Clear();
        Time.timeScale = 1.0f;
        OnStageEnded = null;
        OnPauseChanged = null;
        OnSpawnEvent = null;
    }
}
