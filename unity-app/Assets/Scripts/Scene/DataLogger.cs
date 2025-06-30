using UnityEngine;
using System.Text;
using System.IO;
using UnityEngine.SceneManagement;

/// <summary>
/// Records breathing data during the exhalation phase and saves the data
/// after completing all cycles
/// </summary>
public class DataLogger : MonoBehaviour
{
    [Header("Referências")]
    public MicVisualizer micVisualizer;

    [Header("Configurações de Ciclos")]
    public int totalCycles = 4;      // Total cycles to be completed
    public float exhaleDuration = 8f; // Duration of exhalation phase in seconds

    [Header("Configurações de Cena")]
    public string summarySceneName = "SummaryScene"; // Summary Scene Name

    private StringBuilder csvData;
    private float startTime;
    private string filePath;
    private bool finished = false;

    // Cycle control
    private int currentCycle = 1;
    private float cycleTimer = 0f;

    void Start()
    {
        // Initialize data
        startTime = Time.time;
        csvData = new StringBuilder();
        csvData.AppendLine("timestamp,rms,phase");

        // Generate file name with timestamp
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        filePath = Path.Combine(Application.persistentDataPath, "breathing_data_" + timestamp + ".csv");

        Debug.Log("DataLogger iniciado. Salvará em: " + filePath);
        Debug.Log("Caminho completo: " + Application.persistentDataPath);
        Debug.Log("Configurado para " + totalCycles + " ciclos, com " + exhaleDuration + " segundos por exalação");

        // Ensure the object is not destroyed between scenes
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (finished || micVisualizer == null) return;

        // Collect data
        float currentTime = Time.time - startTime;
        float rms = micVisualizer.rmsValue;

        
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[DataLogger] Ciclo: {currentCycle}/{totalCycles}, Tempo: {cycleTimer:F2}/{exhaleDuration:F1}, RMS: {rms:F5}");
        }

        
        string currentPhase = "exhale";

        
        csvData.AppendLine($"{currentTime:F2},{rms:F5},{currentPhase}");

        
        cycleTimer += Time.deltaTime;

        
        if (cycleTimer >= exhaleDuration)
        {
            cycleTimer = 0f;
            Debug.Log("Ciclo " + currentCycle + " completo!");

            
            if (currentCycle >= totalCycles)
            {
                Debug.Log("Todos os ciclos completados! Salvando dados...");
                SaveDataAndFinish();
            }
            else
            {
                
                currentCycle++;
            }
        }
    }

    public void SaveDataAndFinish()
    {
        Debug.Log("SaveDataAndFinish foi chamado!");

        if (finished)
        {
            Debug.Log("Método já foi chamado anteriormente, retornando.");
            return;
        }

        Debug.Log("Dados coletados: " + csvData.Length + " caracteres");

        try
        {
            
            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            
            File.WriteAllText(filePath, csvData.ToString());
            Debug.Log("Arquivo salvo com sucesso em: " + filePath);

            
            string fixedPath = Path.Combine(Application.persistentDataPath, "breathing_data.csv");
            File.WriteAllText(fixedPath, csvData.ToString());
            Debug.Log("Cópia salva com nome fixo em: " + fixedPath);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Erro ao salvar arquivo: " + e.Message);

            
            try
            {
                string simplePath = Path.Combine(Application.persistentDataPath, "data.csv");
                File.WriteAllText(simplePath, csvData.ToString());
                Debug.Log("Arquivo salvo com nome simples em: " + simplePath);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Também falhou ao salvar com nome simples: " + ex.Message);
            }
        }

        finished = true;

        
        Debug.Log("Dados salvos com sucesso. Aguardando BreathingPhaseManager para continuar o fluxo de fases.");
    }

    
    private void LoadSummaryScene()
    {
        Debug.Log("LoadSummaryScene chamado manualmente.");
        SceneManager.LoadScene(summarySceneName);
    }

    
    void OnDestroy()
    {
        if (!finished)
        {
            Debug.Log("DataLogger sendo destruído antes de salvar. Salvando dados agora...");
            SaveDataAndFinish();
        }
    }

    // Method to check if data has been saved
    public bool IsDataSaved()
    {
        return finished;
    }
}
