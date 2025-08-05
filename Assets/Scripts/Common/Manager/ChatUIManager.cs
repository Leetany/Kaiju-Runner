using UnityEngine;

public class ChatUIManager : MonoBehaviour
{
    public static ChatUIManager Instance;

    public GameObject chatPrefab;
    public GameObject Parent;


    private void Awake()
    {
        Instance = this;        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.O))
        {
            Instantiate(chatPrefab, Parent.transform);
        }
    }
}
