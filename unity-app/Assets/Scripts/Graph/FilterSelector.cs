using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Component to select and apply different filters to breathing data
/// </summary>
public class FilterSelector : MonoBehaviour
{
    public Dropdown filterDropdown;
    public Slider windowSizeSlider;
    public TextMeshProUGUI windowSizeText;

    public GraphVisualizer graphVisualizer;
    public SummarySceneManager summarySceneManager;

    void Start()
    {
        // Set up filter dropdown
        if (filterDropdown != null)
        {
            filterDropdown.ClearOptions();
            filterDropdown.AddOptions(new List<string> {
                "No Filter",
                "Moving Average",
                "Low-Pass Filter",
                "Adaptive Filter"
            });

            filterDropdown.onValueChanged.AddListener(OnFilterChanged);
        }

        // Set up window size slider
        if (windowSizeSlider != null)
        {
            windowSizeSlider.minValue = 3;
            windowSizeSlider.maxValue = 21;
            windowSizeSlider.value = 5;
            windowSizeSlider.wholeNumbers = true;

            windowSizeSlider.onValueChanged.AddListener(OnWindowSizeChanged);
            UpdateWindowSizeText((int)windowSizeSlider.value);

            // Initially disabled until moving average filter is selected
            windowSizeSlider.interactable = false;
            if (windowSizeText != null)
                windowSizeText.gameObject.SetActive(false);
        }
    }

    private void OnFilterChanged(int index)
    {
        if (graphVisualizer == null) return;

        // Enable/disable window size control
        bool isMovingAverage = (index == 1);
        if (windowSizeSlider != null)
        {
            windowSizeSlider.interactable = isMovingAverage;
            if (windowSizeText != null)
                windowSizeText.gameObject.SetActive(isMovingAverage);
        }

        
        if (graphVisualizer != null)
        {
            graphVisualizer.OnWindowSizeChanged(windowSizeSlider.value);
        }

        
        
        
        ApplySelectedFilter();
    }

    private void OnWindowSizeChanged(float value)
    {
        UpdateWindowSizeText((int)value);

        
        if (graphVisualizer != null)
        {
            graphVisualizer.OnWindowSizeChanged(value);
            Debug.Log($"Window size changed to {value}. Updating GraphVisualizer.");
        }

        
        
        
        ApplySelectedFilter();
    }

    private void UpdateWindowSizeText(int value)
    {
        if (windowSizeText != null)
        {
            windowSizeText.text = "Window Size: " + value;
        }
    }

    public void ApplySelectedFilter()
    {
        if (graphVisualizer == null || summarySceneManager == null) return;

        string filterType = filterDropdown.options[filterDropdown.value].text;

        // Map dropdown text to API filter names
        switch (filterType)
        {
            case "No Filter":
                filterType = "none";
                break;
            case "Moving Average":
                filterType = "moving_average";
                break;
            case "Low-Pass Filter":
                filterType = "lowpass";
                break;
            case "Adaptive Filter":
                filterType = "adaptive";
                break;
        }

        Debug.Log($"Applying filter: {filterType} with window size: {windowSizeSlider.value}");

        // Pass the raw data from SummarySceneManager to GraphVisualizer for filtering and display
        graphVisualizer.ApplyFilter(filterType, summarySceneManager.Timestamps, summarySceneManager.RmsValues);
    }
}
