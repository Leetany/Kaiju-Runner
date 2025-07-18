using UnityEngine;

public class puc_PlayerController : MonoBehaviour
{
    public Boss boss;
    public bool isCutscene = false;
    public int playerId;

    void Start()
    {

    }

    void Update()
    {
        if (isCutscene)
            return;

        // 아래는 입력 처리!
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        // 움직임 적용 코드
    }

    public void StartCutscene()
    {
        isCutscene = true;
    }

    public void EndCutscene()
    {
        isCutscene = false;
    }
}
