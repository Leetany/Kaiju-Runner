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
        {            
            return;
        }
                
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
