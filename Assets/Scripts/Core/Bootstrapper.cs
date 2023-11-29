using Unity.Netcode;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrapper : MonoBehaviour
{
    static bool BootstrapperInitialized;

    void Awake()
    {
        if (BootstrapperInitialized)
        {
            Destroy(gameObject);
            return;
        }

        BootstrapperInitialized = true;
    }

    async void Start()
    {
        Application.runInBackground = true;
        await UnityServices.InitializeAsync();
    }

//    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static async void Init()
    {
        // cache loaded track here to reload later
        //if (SceneManager.GetSceneByName("Bootstrapper").IsValid() != true)
        {
            Debug.Log("Loading Bootstrapper");
            await SceneManager.LoadSceneAsync("Bootstrapper", LoadSceneMode.Single);
        }

        //if (SceneManager.GetSceneByName("UserInterface").IsValid() != true)
        {
            Debug.Log("Loading UserInterface");
            await SceneManager.LoadSceneAsync("UserInterface", LoadSceneMode.Additive);
        }
    }
}