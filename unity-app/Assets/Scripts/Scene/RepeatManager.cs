using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;

public class RepeatManager : MonoBehaviour
{
    public float autoDelay = 6f;
    public Button continueButton;
    public string breathSceneName = "Breath";
    public string finalSceneName = "Summary";
    public int maxCycles = 4;

    void Start()
    {
        if (!PlayerPrefs.HasKey("cycle"))
            PlayerPrefs.SetInt("cycle", 0);

        if (continueButton != null)
            continueButton.onClick.AddListener(StartCycle);

        Invoke("StartCycle", autoDelay);
    }

    void StartCycle()
    {
        int currentCycle = PlayerPrefs.GetInt("cycle");

        if (currentCycle >= maxCycles)
        {
            Debug.Log("All cycles completed.");
            PlayerPrefs.DeleteKey("cycle");

            MergeCSVFiles(); 

            SceneManager.LoadScene(finalSceneName);
        }
        else
        {
            currentCycle++;
            PlayerPrefs.SetInt("cycle", currentCycle);
            Debug.Log("Starting cycle " + currentCycle);
            SceneManager.LoadScene(breathSceneName);
        }
    }

    void MergeCSVFiles()
    {
        string path1 = Path.Combine(Application.persistentDataPath, "breath_data.csv");
        string path2 = Path.Combine(Application.persistentDataPath, "exhale_data.csv");
        string outputPath = Path.Combine(Application.persistentDataPath, "merged_breathing.csv");

        if (File.Exists(path1) && File.Exists(path2))
        {
            var merged = new List<string>();


            merged.Add("timestamp,rms,type");

            foreach (var line in File.ReadAllLines(path1))
            {
                if (!line.StartsWith("timestamp"))
                    merged.Add(line + ",inhale");
            }

            foreach (var line in File.ReadAllLines(path2))
            {
                if (!line.StartsWith("timestamp"))
                    merged.Add(line + ",exhale");
            }

            File.WriteAllLines(outputPath, merged.ToArray());
            Debug.Log(" Merged CSV saved to: " + outputPath);
        }
        else
        {
            Debug.LogWarning(" Breath or Exhale CSV not found to merge.");
        }
    }
}
