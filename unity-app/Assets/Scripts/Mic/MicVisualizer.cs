using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class MicVisualizer : MonoBehaviour
{
    [Header("Mic Icon")]
    public Image micIcon;
    public Sprite level1, level2, level3, level4;

    [Header("Feedback UI")]
    public TextMeshProUGUI feedbackText;
    public TextMeshProUGUI phaseText;

    [Header("Configurações de Suavização")]
    public float smoothingFactor = 0.1f;
    public bool useAdaptiveSmoothing = false;
    public float minSmoothingFactor = 0.05f;
    public float maxSmoothingFactor = 0.3f;

    [HideInInspector]
    public float rmsValue;

    
    private float smoothedRMS = 0f;
    private float totalValidTime = 0f;
    private float startTime;

    private float lastFeedbackTime = 0f;
    public float feedbackCooldown = 1.5f;

    private string lastMessage = "";
    private BreathingPhaseManager phaseManager;

    void Start()
    {
        startTime = Time.time;

        
        phaseManager = FindAnyObjectByType<BreathingPhaseManager>();

    }

    void Update()
    {
        
        if (useAdaptiveSmoothing)
        {
            
            float variability = Mathf.Abs(rmsValue - smoothedRMS);

            
            float adaptiveFactor = Mathf.Lerp(
                minSmoothingFactor,
                maxSmoothingFactor,
                Mathf.Clamp01(variability * 10f)
            );

            smoothedRMS = Mathf.Lerp(smoothedRMS, rmsValue, adaptiveFactor);
        }
        else
        {
            
            smoothedRMS = Mathf.Lerp(smoothedRMS, rmsValue, smoothingFactor);
        }

        UpdateMicIcon(smoothedRMS);
        GiveFeedback(smoothedRMS);

        if (smoothedRMS > 0.02f)
        {
            totalValidTime += Time.deltaTime;
        }
    }

    void UpdateMicIcon(float inputRMS)
    {
        if (micIcon == null) return;

        if (inputRMS < 0.01f)
            micIcon.sprite = level1;
        else if (inputRMS < 0.03f)
            micIcon.sprite = level2;
        else if (inputRMS < 0.06f)
            micIcon.sprite = level3;
        else
            micIcon.sprite = level4;
    }

    void GiveFeedback(float inputRMS)
    {
        if (feedbackText == null) return;
        if (Time.time - lastFeedbackTime < feedbackCooldown) return;

        string message = "";
        string currentPhase = phaseManager != null ? phaseManager.GetCurrentPhase() : "unknown";

       
        switch (currentPhase)
        {
            case "inhale":
                if (inputRMS < 0.01f)
                    message = "Inspire mais profundamente!";
                else if (inputRMS < 0.03f)
                    message = "Boa inalação!";
                else
                    message = "Inalação profunda!";
                break;

            case "hold":
                if (inputRMS > 0.02f)
                    message = "Tente ficar mais quieto";
                else
                    message = "Segurando bem!";
                break;

            case "exhale":
                if (inputRMS < 0.01f)
                    message = "Exhale harder!";
                else if (inputRMS < 0.03f)
                    message = "Good exhalation!";
                else
                    message = "Excellent exhalation!";
                break;

            case "rest":
                message = "Descanse...";
                break;

            default:
                
                if (inputRMS < 0.01f)
                    message = "Tente novamente!";
                else if (inputRMS < 0.03f)
                    message = "Boa respiração!";
                else
                    message = "Respiração incrível!";
                break;
        }

        if (message != lastMessage)
        {
            feedbackText.text = message;
            lastMessage = message;
            lastFeedbackTime = Time.time;
        }
    }

    public bool ExhaleWasSuccessful()
    {
        return totalValidTime >= 2f;
    }

   
    public void ResetValidTime()
    {
        totalValidTime = 0f;
    }
}
