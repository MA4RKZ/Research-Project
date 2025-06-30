using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;
using System.Text;
using System.Globalization;

public class GraphVisualizer : MonoBehaviour
{
    [Header("Graph Settings")]
    public RectTransform graphContainer;
    public GameObject pointPrefab;
    public GameObject linePrefab;
    public float pointSize = 3f;
    public float lineWidth = 2f;
    public Color exhaleColor = Color.green;
    public int maxPointsToShow = 150;

    [Header("API Settings")]
    public string apiUrl = "http://localhost:5000/process_data";


    private List<GameObject> graphObjects = new List<GameObject>();
    private int currentWindowSize = 5;

    private void Start()
    {
        Debug.Log($"API URL being used: {apiUrl}");
        if (graphContainer == null)
        {
            Debug.LogError("Graph container is not assigned!");
        }
    }

    public void OnWindowSizeChanged(float value)
    {
        currentWindowSize = Mathf.RoundToInt(value);
        // If we have a FilterSelector, it will call DisplayFilteredData
    }

    public void DisplayGraph(List<float> values, List<float> timestamps)
    {
        if (timestamps == null || values == null || timestamps.Count == 0 || values.Count == 0)
        {
            Debug.LogWarning("Cannot display graph: No data provided");
            return;
        }

        // Ensure we have the same number of timestamps and values
        int dataCount = Mathf.Min(timestamps.Count, values.Count);

        // Clear previous graph
        DestroyAllChildren();

        // Log container dimensions for debugging
        Debug.Log($"Graph container dimensions: Width={graphContainer.rect.width}, Height={graphContainer.rect.height}");

        // Find min/max values for normalization
        float minValue = float.MaxValue;
        float maxValue = float.MinValue;
        float minTime = timestamps[0];
        float maxTime = timestamps[timestamps.Count - 1];

        for (int i = 0; i < dataCount; i++)
        {
            if (values[i] < minValue) minValue = values[i];
            if (values[i] > maxValue) maxValue = values[i];
        }

        // Ensure we have a valid range
        if (maxValue - minValue < 0.0001f)
        {
            maxValue = minValue + 1f;
        }

        // Apply downsampling if needed
        int step = 1;
        if (dataCount > maxPointsToShow)
        {
            step = dataCount / maxPointsToShow;
            Debug.Log($"Downsampling: showing {dataCount / step} points out of {dataCount}");
        }

        // Add padding (5% on each side)
        float padding = 0.05f;
        float graphWidth = graphContainer.rect.width * (1f - 2f * padding);
        float graphHeight = graphContainer.rect.height * (1f - 2f * padding);
        float xOffset = graphContainer.rect.width * padding;
        float yOffset = graphContainer.rect.height * padding;

        // Create points and lines
        GameObject previousPoint = null;

        for (int i = 0; i < dataCount; i += step)
        {
            // Normalize coordinates to fit in the graph container with padding
            float normalizedX = (timestamps[i] - minTime) / (maxTime - minTime);
            float normalizedY = (values[i] - minValue) / (maxValue - minValue);

            // Calculate position within the graph container
            float xPosition = xOffset + normalizedX * graphWidth;
            float yPosition = yOffset + normalizedY * graphHeight;

            // Create point
            GameObject point = Instantiate(pointPrefab, graphContainer.transform);
            RectTransform pointRect = point.GetComponent<RectTransform>();
            pointRect.anchoredPosition = new Vector2(xPosition, yPosition);
            pointRect.sizeDelta = new Vector2(pointSize, pointSize);

            // Set point color
            Image pointImage = point.GetComponent<Image>();
            if (pointImage != null)
            {
                pointImage.color = exhaleColor;
            }

            graphObjects.Add(point);

            // Create line connecting to previous point
            if (previousPoint != null)
            {
                CreateLine(previousPoint.GetComponent<RectTransform>().anchoredPosition,
                           pointRect.anchoredPosition,
                           exhaleColor);
            }

            previousPoint = point;
        }

        Debug.Log($"Graph created with {graphObjects.Count} objects");
    }

    private void CreateLine(Vector2 startPoint, Vector2 endPoint, Color color)
    {
        GameObject line = Instantiate(linePrefab, graphContainer.transform);
        RectTransform lineRect = line.GetComponent<RectTransform>();

        // Calculate line position and rotation
        Vector2 direction = endPoint - startPoint;
        float distance = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Position line at midpoint between start and end
        lineRect.anchoredPosition = startPoint + direction / 2;
        lineRect.sizeDelta = new Vector2(distance, lineWidth);
        lineRect.localEulerAngles = new Vector3(0, 0, angle);

        // Set line color
        Image lineImage = line.GetComponent<Image>();
        if (lineImage != null)
        {
            lineImage.color = color;
        }

        graphObjects.Add(line);
    }

    private void ClearGraph()
    {
        // Destroy all graph objects and clear the list
        foreach (GameObject obj in graphObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        graphObjects.Clear();

        Debug.Log("Graph cleared");
    }

    private void DestroyAllChildren()
    {
        // First use our standard method
        ClearGraph();

        // Then make absolutely sure by destroying all children of the container
        foreach (Transform child in graphContainer.transform)
        {
            Destroy(child.gameObject);
        }

        // Clear our tracking list just to be safe
        graphObjects.Clear();

        Debug.Log("All graph children destroyed");
    }

    public void ApplyFilter(string filterType, List<float> timestamps, List<float> values)
    {
        if (timestamps == null || values == null || timestamps.Count == 0 || values.Count == 0)
        {
            Debug.LogWarning("Cannot apply filter: No data provided");
            return;
        }

        // Log data before sending to API
        Debug.Log($"Applying filter: {filterType} with window size: {currentWindowSize}");
        Debug.Log($"Data points to process: {timestamps.Count}");

        if (timestamps.Count > 0)
        {
            Debug.Log($"First timestamp: {timestamps[0]}, First value: {values[0]}");
            Debug.Log($"Last timestamp: {timestamps[timestamps.Count - 1]}, Last value: {values[values.Count - 1]}");
        }

        StartCoroutine(ProcessDataWithAPI(filterType, timestamps, values));
    }

    private IEnumerator ProcessDataWithAPI(string filterType, List<float> timestamps, List<float> values)
    {
        // Check internet connectivity
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Debug.Log("No internet connection. Using local processing.");
            List<float> filteredValues = ApplyLocalFilter(filterType, values);
            DisplayGraph(filteredValues, timestamps);
            yield break;
        }

        // Create a simple JSON structure that's compatible with Python
        StringBuilder jsonBuilder = new StringBuilder();
        jsonBuilder.Append("{\"timestamps\":[");

        // Add timestamps - always use InvariantCulture to ensure dot as decimal separator
        for (int i = 0; i < timestamps.Count; i++)
        {
            jsonBuilder.Append(string.Format(CultureInfo.InvariantCulture, "{0:F6}", timestamps[i]));
            if (i < timestamps.Count - 1)
                jsonBuilder.Append(",");
        }

        jsonBuilder.Append("],\"rms_values\":[");

        // Add RMS values - always use InvariantCulture to ensure dot as decimal separator
        for (int i = 0; i < values.Count; i++)
        {
            jsonBuilder.Append(string.Format(CultureInfo.InvariantCulture, "{0:F6}", values[i]));
            if (i < values.Count - 1)
                jsonBuilder.Append(",");
        }

        jsonBuilder.Append("]}");

        string jsonData = jsonBuilder.ToString();

        // Add filter type and window size as query parameters
        string url = $"{apiUrl}?filter={filterType}&window_size={currentWindowSize}";

        Debug.Log($"Sending request to {url} with data length: {jsonData.Length}");
        Debug.Log($"Sample of JSON data: {jsonData.Substring(0, Math.Min(100, jsonData.Length))}...");
        Debug.Log($"Full JSON data being sent: {jsonData}");

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"API request failed: {www.error}. Using local processing.");
                List<float> filteredValues = ApplyLocalFilter(filterType, values);
                DisplayGraph(filteredValues, timestamps);
            }
            else
            {
                Debug.Log($"API response received: {www.downloadHandler.text}");

                // Parse response
                try
                {
                    string responseText = www.downloadHandler.text;

                    // Simple JSON parsing for the filtered_values array
                    int startIndex = responseText.IndexOf("\"filtered_values\":[") + "\"filtered_values\":[".Length;
                    int endIndex = responseText.IndexOf("]", startIndex);

                    if (startIndex >= 0 && endIndex >= 0)
                    {
                        string valuesStr = responseText.Substring(startIndex, endIndex - startIndex);
                        string[] valueStrings = valuesStr.Split(',');

                        List<float> filteredValues = new List<float>();
                        foreach (string valueStr in valueStrings)
                        {
                            // Use InvariantCulture for parsing to handle dot decimal separator
                            if (float.TryParse(valueStr, NumberStyles.Any, CultureInfo.InvariantCulture, out float value))
                            {
                                filteredValues.Add(value);
                            }
                        }

                        if (filteredValues.Count > 0)
                        {
                            Debug.Log($"Successfully parsed {filteredValues.Count} filtered values from API response");
                            DisplayGraph(filteredValues, timestamps);
                            yield break;
                        }
                    }

                    Debug.LogWarning("Could not parse filtered_values from API response. Using local processing.");
                    List<float> localFilteredValues = ApplyLocalFilter(filterType, values);
                    DisplayGraph(localFilteredValues, timestamps);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error parsing API response: {e.Message}. Using local processing.");
                    List<float> filteredValues = ApplyLocalFilter(filterType, values);
                    DisplayGraph(filteredValues, timestamps);
                }
            }
        }
    }

    private List<float> ApplyLocalFilter(string filterType, List<float> values)
    {
        List<float> filteredValues = new List<float>(values);

        switch (filterType)
        {
            case "moving_average":
                filteredValues = ApplyMovingAverage(values, currentWindowSize);
                break;
            case "lowpass":
                filteredValues = ApplyLowPassFilter(values, 0.1f);
                break;
            case "adaptive":
                filteredValues = ApplyAdaptiveFilter(values);
                break;
            default:
                // No filter, use original values
                break;
        }

        return filteredValues;
    }

    private List<float> ApplyMovingAverage(List<float> values, int windowSize)
    {
        List<float> result = new List<float>(values);

        for (int i = 0; i < values.Count; i++)
        {
            float sum = 0;
            int count = 0;

            for (int j = Mathf.Max(0, i - windowSize / 2); j <= Mathf.Min(values.Count - 1, i + windowSize / 2); j++)
            {
                sum += values[j];
                count++;
            }

            if (count > 0)
            {
                result[i] = sum / count;
            }
        }

        return result;
    }

    private List<float> ApplyLowPassFilter(List<float> values, float alpha)
    {
        List<float> result = new List<float>(values);

        for (int i = 1; i < values.Count; i++)
        {
            result[i] = alpha * values[i] + (1 - alpha) * result[i - 1];
        }

        return result;
    }

    private List<float> ApplyAdaptiveFilter(List<float> values)
    {
        // Simple adaptive filter implementation
        List<float> result = new List<float>(values);
        float alpha = 0.1f;

        for (int i = 1; i < values.Count; i++)
        {
            // Adjust alpha based on rate of change
            float change = Mathf.Abs(values[i] - values[i - 1]);
            float adaptiveAlpha = Mathf.Clamp(alpha * (1 + change * 5), 0.1f, 0.9f);

            result[i] = adaptiveAlpha * values[i] + (1 - adaptiveAlpha) * result[i - 1];
        }

        return result;
    }
}
