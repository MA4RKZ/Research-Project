using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System;
using System.Text.RegularExpressions;

/// <summary>
/// Manages the summary scene, displaying session statistics and graph
/// </summary>
public class SummarySceneManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI dateTimeText;
    public TextMeshProUGUI sessionStatsText;
    public GraphVisualizer graphVisualizer;

    [Header("Scene Configuration")]
    public string summarySceneName = "SummaryScene";

    // Session data
    public List<float> Timestamps { get; private set; } = new List<float>();
    public List<float> RmsValues { get; private set; } = new List<float>();
    private string sessionDateTime = "";

    void Start()
    {
        Debug.Log("SummarySceneManager Start() called.");
        // Load the most recent session data
        LoadMostRecentSessionData();

        // Display session information
        UpdateUI();
    }

    // Load data from the most recent session file
    private void LoadMostRecentSessionData()
    {
        string directory = Application.persistentDataPath;
        string[] files = new string[0];

        try
        {
            files = Directory.GetFiles(directory, "breathing_data_*.csv");
            Debug.Log($"Found {files.Length} breathing data files in {directory}");
        }
        catch (Exception e)
        {
            Debug.LogError("Error accessing data directory: " + e.Message);
            DisplayErrorMessage();
            return;
        }

        if (files.Length > 0)
        {
            // Sort by date (most recent first)
            System.Array.Sort(files, (a, b) => File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));

            // Load the most recent file
            string filePath = files[0];
            Debug.Log("Loading most recent session data: " + filePath);

            // Extract date from filename using regex
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            Debug.Log($"Extracted filename: {fileName}");

            // Try to extract date using regex pattern for "breathing_data_yyyyMMdd_HHmmss"
            Regex regex = new Regex(@"breathing_data_(\d{8})_(\d{6})");
            Match match = regex.Match(fileName);

            if (match.Success && match.Groups.Count >= 3)
            {
                try
                {
                    string datePart = match.Groups[1].Value; // yyyyMMdd
                    string timePart = match.Groups[2].Value; // HHmmss

                    Debug.Log($"Regex extracted date part: {datePart}, time part: {timePart}");

                    // Parse date components
                    string year = datePart.Substring(0, 4);
                    string month = datePart.Substring(4, 2);
                    string day = datePart.Substring(6, 2);

                    // Parse time components
                    string hour = timePart.Substring(0, 2);
                    string minute = timePart.Substring(2, 2);
                    string second = timePart.Substring(4, 2);

                    sessionDateTime = $"{day}/{month}/{year} {hour}:{minute}";
                    Debug.Log($"Successfully parsed date/time: {sessionDateTime}");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Error parsing date/time components: {e.Message}");
                    // Fallback to file creation time
                    sessionDateTime = File.GetCreationTime(filePath).ToString("dd/MM/yyyy HH:mm");
                    Debug.Log($"Using file creation time instead: {sessionDateTime}");
                }
            }
            else
            {
                // Fallback to simple split method if regex fails
                Debug.Log("Regex pattern did not match, trying simple split method");
                if (fileName.Contains("_"))
                {
                    string[] parts = fileName.Split('_');
                    if (parts.Length >= 2)
                    {
                        string dateTimePart = parts[1];
                        if (parts.Length >= 3)
                        {
                            dateTimePart += "_" + parts[2]; // Include the time part if it was split
                        }

                        Debug.Log($"Split method found date/time part: {dateTimePart}");

                        if (dateTimePart.Length >= 8)
                        {
                            try
                            {
                                // Try to parse date from filename (format: yyyyMMdd_HHmmss)
                                string year = dateTimePart.Substring(0, 4);
                                string month = dateTimePart.Substring(4, 2);
                                string day = dateTimePart.Substring(6, 2);

                                if (dateTimePart.Length >= 15 && dateTimePart.Contains("_"))
                                {
                                    string timePart = dateTimePart.Split('_')[1];
                                    string hour = timePart.Substring(0, 2);
                                    string minute = timePart.Substring(2, 2);

                                    sessionDateTime = $"{day}/{month}/{year} {hour}:{minute}";
                                    Debug.Log($"Split method parsed date/time: {sessionDateTime}");
                                }
                                else
                                {
                                    sessionDateTime = $"{day}/{month}/{year}";
                                    Debug.Log($"Split method parsed date only: {sessionDateTime}");
                                }
                            }
                            catch (Exception e)
                            {
                                Debug.LogWarning($"Error in split method: {e.Message}");
                                // Final fallback to file creation time
                                sessionDateTime = File.GetCreationTime(filePath).ToString("dd/MM/yyyy HH:mm");
                                Debug.Log($"Using file creation time as final fallback: {sessionDateTime}");
                            }
                        }
                    }
                }

                // If all parsing attempts fail, use current date/time
                if (string.IsNullOrEmpty(sessionDateTime))
                {
                    sessionDateTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                    Debug.Log($"All parsing attempts failed, using current date/time: {sessionDateTime}");
                }
            }

            // Load data from CSV
            LoadDataFromCSV(filePath);
            Debug.Log($"Session Date Time after parsing: {sessionDateTime}");
        }
        else
        {
            Debug.LogWarning("No session data files found.");
            DisplayErrorMessage();
        }
    }

    // Load data from CSV file with robust error handling
    private void LoadDataFromCSV(string filePath)
    {
        try
        {
            string[] lines = File.ReadAllLines(filePath);
            Timestamps.Clear();
            RmsValues.Clear();

            Debug.Log($"CSV file has {lines.Length} lines. First line: {(lines.Length > 0 ? lines[0] : "empty")}");

            // Skip header
            for (int i = 1; i < lines.Length; i++)
            {
                string[] values = lines[i].Split(',');
                if (values.Length >= 2)
                {
                    // Replace comma with dot for decimal parsing
                    string timeStr = values[0].Replace(',', '.');
                    string rmsStr = values[1].Replace(',', '.');

                    if (float.TryParse(timeStr, NumberStyles.Any, CultureInfo.InvariantCulture, out float time) &&
                        float.TryParse(rmsStr, NumberStyles.Any, CultureInfo.InvariantCulture, out float rms))
                    {
                        Timestamps.Add(time);
                        RmsValues.Add(rms);

                        // Debug first few values to verify parsing
                        if (i <= 5)
                        {
                            Debug.Log($"Parsed CSV row {i}: time={time}, rms={rms} (from '{values[0]}' and '{values[1]}')");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to parse values at line {i}: '{values[0]}', '{values[1]}'");
                    }
                }
            }

            Debug.Log($"Loaded {Timestamps.Count} data points for summary.");

            // Debug first and last data points to verify data integrity
            if (Timestamps.Count > 0)
            {
                Debug.Log($"First data point: time={Timestamps[0]}, rms={RmsValues[0]}");
                Debug.Log($"Last data point: time={Timestamps[Timestamps.Count - 1]}, rms={RmsValues[RmsValues.Count - 1]}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error loading CSV data: " + e.Message);
            Timestamps.Clear();
            RmsValues.Clear();
            DisplayErrorMessage();
        }
    }

    // Update UI elements with session data
    private void UpdateUI()
    {
        // Update date/time
        if (dateTimeText != null)
        {
            dateTimeText.text = $"Session: {sessionDateTime}";
            Debug.Log($"Updated dateTimeText with: {sessionDateTime}");
        }
        else
        {
            Debug.LogError("dateTimeText reference is null!");
        }

        // Update session statistics
        if (sessionStatsText != null)
        {
            if (Timestamps.Count > 0 && RmsValues.Count > 0)
            {
                // Calculate statistics
                int completedCycles = CalculateCompletedCycles();
                float avgRMS = CalculateAverageRMS();
                float maxRMS = CalculateMaxRMS();
                float sessionDuration = CalculateSessionDuration();

                // Format and display statistics
                sessionStatsText.text = $"Cycles completed: {completedCycles}\n" +
                                       $"Average exhale intensity: {avgRMS:F3}\n" +
                                       $"Maximum exhale intensity: {maxRMS:F3}\n" +
                                       $"Total duration: {sessionDuration:F1}s";

                Debug.Log($"Updated sessionStatsText with statistics: cycles={completedCycles}, avg={avgRMS:F3}, max={maxRMS:F3}, duration={sessionDuration:F1}s");
            }
            else
            {
                sessionStatsText.text = "No valid session data available.";
                Debug.LogWarning("No data available for statistics");
            }
        }
        else
        {
            Debug.LogError("sessionStatsText reference is null!");
        }

        // Update graph if available
        if (graphVisualizer != null && Timestamps.Count > 0 && RmsValues.Count > 0)
        {
            graphVisualizer.DisplayGraph(RmsValues, Timestamps);
            Debug.Log("Called graphVisualizer.DisplayGraph with data");
        }
        else if (graphVisualizer == null)
        {
            Debug.LogError("graphVisualizer reference is null!");
        }
    }

    // Calculate number of completed breathing cycles
    private int CalculateCompletedCycles()
    {
        if (Timestamps.Count < 10) return 0;

        
        float totalDuration = Timestamps[Timestamps.Count - 1] - Timestamps[0];
        int estimatedCycles = Mathf.FloorToInt(totalDuration / 8.0f); // Assuming 8 seconds per cycle

        return Mathf.Max(0, estimatedCycles);
    }

    // Calculate average RMS value
    private float CalculateAverageRMS()
    {
        if (RmsValues.Count == 0) return 0;

        float sum = 0;
        foreach (float value in RmsValues)
        {
            sum += value;
        }

        return sum / RmsValues.Count;
    }

    // Calculate maximum RMS value
    private float CalculateMaxRMS()
    {
        if (RmsValues.Count == 0) return 0;

        float max = float.MinValue;
        foreach (float value in RmsValues)
        {
            max = Mathf.Max(max, value);
        }

        return max;
    }

    // Calculate total session duration
    private float CalculateSessionDuration()
    {
        if (Timestamps.Count < 2) return 0;

        return Timestamps[Timestamps.Count - 1] - Timestamps[0];
    }

    // Display error message when data cannot be loaded
    private void DisplayErrorMessage()
    {
        if (dateTimeText != null)
        {
            dateTimeText.text = "Session: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        }

        if (sessionStatsText != null)
        {
            sessionStatsText.text = "Error loading session data.\nPlease try again.";
        }
    }

    // Method to handle new session button click
    public void OnNewSessionButtonClicked()
    {
        // Load the initial scene to start a new session
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    // Method to handle share button click
    public void OnShareButtonClicked()
    {
        // Implement sharing functionality
        Debug.Log("Share button clicked - functionality to be implemented");

        // Example of how you might implement sharing on mobile
#if UNITY_ANDROID || UNITY_IOS
        // Mobile sharing code would go here
        // This would typically use native plugins or Unity's social sharing features
#endif
    }
}
