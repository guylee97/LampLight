using UnityEngine;

public class TitleScene : MonoBehaviour
{
    void Start()
    {
        Managers.UI.ShowSceneUI<UI_MainScreen>();
    }
}
