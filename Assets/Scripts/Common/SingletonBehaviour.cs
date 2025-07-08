using UnityEngine;

public class SingletonBehaviour<T> : MonoBehaviour where T : SingletonBehaviour<T>
{
    protected bool m_IsDestroyOnLoad = false;
    protected static T m_Instance;

    public static T Instance
    {
        get
        {
            if (m_Instance == null)
            {
                Debug.LogWarning($"{typeof(T).Name} has not been initialized yet.");
            }
            return m_Instance;
        }
    }

    protected virtual void Awake()
    {
        Init();
    }

    protected virtual void Init()
    {
        if (m_Instance != null && m_Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        m_Instance = (T)this;

        if (!m_IsDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        if (m_Instance == this)
            m_Instance = null;
    }

    protected virtual void Dispose()
    {
        m_Instance = null;
    }
}