using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManagerEx
{
    GameObject _player;
    //Dictionary<int, GameObject> _players = new Dictionary<int, GameObject>();
    HashSet<GameObject> _monsters = new HashSet<GameObject>();

    public Action<int> OnSpawnEvent;
    public bool IsGameOver { get; private set; }

    public GameObject GetPlayer() { return _player; }

    public void GameOver()
    {
        if (IsGameOver)
            return;

        IsGameOver = true;
        Managers.UI.ShowPopupUI<UI_GameOver>();
    }

    public void Clear()
    {
        IsGameOver = false;
        _player = null;
        _monsters.Clear();
        Time.timeScale = 1.0f;
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
}
