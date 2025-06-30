using UnityEngine;

public class MicRecorder : MonoBehaviour
{
    private AudioClip micClip;
    private string micDevice;
    private int sampleWindow = 128;

    public MicVisualizer micVisualizer; 

    void Start()
    {
        if (Microphone.devices.Length > 0)
        {
            micDevice = Microphone.devices[0];
            micClip = Microphone.Start(micDevice, true, 1, 44100);
        }
        else
        {
            Debug.LogWarning("No microphone detected!");
        }
    }

    void Update()
    {
        if (micClip == null) return;

        float rms = GetRMSValue();
        Debug.Log($"Mic Volume (RMS): {rms}");

        
        if (micVisualizer != null)
        {
            micVisualizer.rmsValue = rms;
        }
    }

    float GetRMSValue()
    {
        float[] samples = new float[sampleWindow];
        int micPos = Microphone.GetPosition(micDevice) - sampleWindow + 1;
        if (micPos < 0) return 0;

        micClip.GetData(samples, micPos);

        float sum = 0;
        for (int i = 0; i < sampleWindow; i++)
            sum += samples[i] * samples[i];

        return Mathf.Sqrt(sum / sampleWindow);
    }
}

