using UnityEngine;
using UnityEngine.SceneManagement;

public class StartButton : MonoBehaviour
{
    public void OnButtonClick()
    {
        
        BreathingPhaseManager phaseManager = FindAnyObjectByType<BreathingPhaseManager>();

        if (phaseManager != null)
        {
            
            phaseManager.StartSession();
        }
        else
        {
            
            SceneManager.LoadScene("Scenes/Breath");
        }
    }
}
