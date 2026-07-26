using UnityEngine;

public class TitleScene : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Managers.UI.ShowSceneUI<UI_MainScreen>();
    }
}