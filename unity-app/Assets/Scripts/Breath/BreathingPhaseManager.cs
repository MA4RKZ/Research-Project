using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Gerencia as fases de respiração e transição entre cenas
/// </summary>
public class BreathingPhaseManager : MonoBehaviour
{
    // Enumeration for the phases of breathing
    public enum BreathingPhase
    {
        Inhale,
        Hold,
        Exhale,
        Rest
    }

    // Setting times for each phase (in seconds)
    public float inhaleTime = 4f;
    public float holdTime = 7f;
    public float exhaleTime = 8f;
    public float restTime = 10f;  // Increased to 10 seconds as requested

    // Total number of cycles to be completed
    public int totalCycles = 4;

    // References for UI
    [Header("UI References")]
    public Image backgroundImage; // Background image that changes with the phase(I'm not using it currently)
    public Image timerImage;      // Timer sprite
    public TextMeshProUGUI cycleText; // Text to show current cycle

    // Scene setup
    [Header("Scene Configuration")]
    public string inhaleSceneName = "Breath";  
    public string holdSceneName = "Hold Breath";  
    public string exhaleSceneName = "Exhale";  
    public string restSceneName = "Repeat";  
    public string summarySceneName = "SummaryScene";  

    // Estado atual
    private BreathingPhase currentPhase = BreathingPhase.Inhale;
    private float phaseTimer = 0f;
    private int currentCycle = 1;
    private bool sessionComplete = false;
    private bool waitingForSummary = false;

    
    public bool isActive = false;

    
    void Awake()
    {
        
        BreathingPhaseManager[] managers = FindObjectsByType<BreathingPhaseManager>(FindObjectsSortMode.None);
        if (managers.Length > 1)
        {
            
            Destroy(gameObject);
            return;
        }

        
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        
        UpdateUI();
        DontDestroyOnLoad(gameObject);
        Debug.Log("BreathingPhaseManager iniciado. Configurado para " + totalCycles + " ciclos.");
    }

    void Update()
    {
        
        if (!isActive || sessionComplete) return;

        
        if (waitingForSummary)
        {
            
            phaseTimer += Time.deltaTime;
            if (phaseTimer >= restTime)
            {
                Debug.Log("Tempo de descanso final concluído. Carregando cena de resumo.");
                LoadScene(summarySceneName);
                sessionComplete = true;
                isActive = false;
                waitingForSummary = false;
                return;
            }

            
            UpdateUI();
            return;
        }

        
        phaseTimer += Time.deltaTime;

        // Checks if the current phase has finished
        float currentPhaseTime = GetCurrentPhaseTime();
        if (phaseTimer >= currentPhaseTime)
        {
           
            MoveToNextPhase();
        }

        
        UpdateUI();
    }

    // Method to start the session (called by the Start button)
    public void StartSession()
    {
        isActive = true;
        currentPhase = BreathingPhase.Inhale;
        currentCycle = 1;
        phaseTimer = 0f;
        sessionComplete = false;
        waitingForSummary = false;
        LoadScene(inhaleSceneName);
    }

    // Advance to the next stage
    private void MoveToNextPhase()
    {
        phaseTimer = 0f;

        switch (currentPhase)
        {
            case BreathingPhase.Inhale:
                currentPhase = BreathingPhase.Hold;
                LoadScene(holdSceneName);
                break;

            case BreathingPhase.Hold:
                currentPhase = BreathingPhase.Exhale;
                LoadScene(exhaleSceneName);
                break;

            case BreathingPhase.Exhale:
                currentPhase = BreathingPhase.Rest;
                Debug.Log("Transitioning to Rest phase, loading scene: " + restSceneName);
                LoadScene(restSceneName);
                break;

            case BreathingPhase.Rest:
                
                if (currentCycle >= totalCycles)
                {
                    
                    DataLogger dataLogger = FindAnyObjectByType<DataLogger>();
                    if (dataLogger != null && dataLogger.IsDataSaved())
                    {
                        Debug.Log("Último ciclo completo e dados já salvos. Carregando cena de resumo.");
                        LoadScene(summarySceneName);
                        sessionComplete = true;
                        isActive = false;
                    }
                    else
                    {
                        // Se os dados ainda não foram salvos, vamos esperar na cena de descanso
                        Debug.Log("Último ciclo completo. Aguardando na cena de descanso antes de ir para o resumo.");
                        waitingForSummary = true;
                    }
                }
                else
                {
                    currentCycle++;
                    currentPhase = BreathingPhase.Inhale;
                    LoadScene(inhaleSceneName);
                }
                break;
        }
    }

    
    private void LoadScene(string sceneName)
    {
        Debug.Log("Attempting to load scene: " + sceneName);
        try
        {
            SceneManager.LoadScene(sceneName);
            Debug.Log("Scene loaded successfully: " + sceneName);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to load scene '" + sceneName + "': " + e.Message);
        }
    }

    
    private float GetCurrentPhaseTime()
    {
        switch (currentPhase)
        {
            case BreathingPhase.Inhale:
                return inhaleTime;
            case BreathingPhase.Hold:
                return holdTime;
            case BreathingPhase.Exhale:
                return exhaleTime;
            case BreathingPhase.Rest:
                return restTime;
            default:
                return 0f;
        }
    }

    
    private void UpdateUI()
    {
        
        if (cycleText != null)
        {
            cycleText.text = "Ciclo " + currentCycle + "/" + totalCycles;
        }
    }

    
    private void CompleteSession()
    {
        sessionComplete = true;
        isActive = false;
        Debug.Log("Breathing session complete!");

        
        DataLogger dataLogger = FindAnyObjectByType<DataLogger>();
        if (dataLogger != null)
        {
            Debug.Log("DataLogger encontrado, chamando SaveDataAndFinish()");
            dataLogger.SaveDataAndFinish();

            
            Debug.Log("Dados salvos. Transitioning to Rest phase, loading scene: " + restSceneName);
            LoadScene(restSceneName);
            waitingForSummary = true;
        }
        else
        {
            Debug.LogError("DataLogger NÃO encontrado! Não foi possível salvar os dados.");
            
            LoadScene(summarySceneName);
        }
    }

    
    public string GetCurrentPhase()
    {
        switch (currentPhase)
        {
            case BreathingPhase.Inhale:
                return "inhale";
            case BreathingPhase.Hold:
                return "hold";
            case BreathingPhase.Exhale:
                return "exhale";
            case BreathingPhase.Rest:
                return "rest";
            default:
                return "unknown";
        }
    }

    
    public bool IsSessionComplete()
    {
        return sessionComplete;
    }

    
    public void ResetSession()
    {
        isActive = false;
        currentPhase = BreathingPhase.Inhale;
        currentCycle = 1;
        phaseTimer = 0f;
        sessionComplete = false;
        waitingForSummary = false;
    }

    
    public void ContinueFromRest()
    {
        if (currentPhase == BreathingPhase.Rest)
        {
            if (waitingForSummary)
            {
                
                Debug.Log("Continue pressionado na tela de descanso final. Carregando cena de resumo.");
                LoadScene(summarySceneName);
                sessionComplete = true;
                isActive = false;
                waitingForSummary = false;
            }
            else if (currentCycle >= totalCycles)
            {
                
                CompleteSession();
            }
            else
            {
                
                currentCycle++;
                currentPhase = BreathingPhase.Inhale;
                LoadScene(inhaleSceneName);
            }
        }
    }
}
