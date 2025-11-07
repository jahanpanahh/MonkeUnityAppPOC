using System;
using System.Runtime.InteropServices;
using UnityEngine;
using AOT;

public class SpeechToText : MonoBehaviour
{
    // Callback delegate
    public delegate void OnRecognitionResult(string text);
    public event OnRecognitionResult OnTextRecognized;

    private bool isRecording = false;
    private string lastRecognizedText = "";

#if UNITY_IOS && !UNITY_EDITOR
    // Native iOS function imports
    [DllImport("__Internal")]
    private static extern void _InitSpeechRecognizer();

    [DllImport("__Internal")]
    private static extern void _RequestSpeechPermission();

    [DllImport("__Internal")]
    private static extern bool _StartSpeechRecognition(RecognitionCallback callback);

    [DllImport("__Internal")]
    private static extern void _StopSpeechRecognition();

    // Callback delegate that matches native side
    private delegate void RecognitionCallback(string text);

    // Static callback function (must be static for native calls)
    [MonoPInvokeCallback(typeof(RecognitionCallback))]
    private static void OnRecognitionCallback(string text)
    {
        // Forward to instance method on main thread
        if (Instance != null)
        {
            Instance.HandleRecognitionResult(text);
        }
    }

    private static SpeechToText Instance;
#endif

    void Awake()
    {
#if UNITY_IOS && !UNITY_EDITOR
        Instance = this;
        _InitSpeechRecognizer();
#endif
    }

    public void RequestPermission()
    {
#if UNITY_IOS && !UNITY_EDITOR
        _RequestSpeechPermission();
#else
        Debug.Log("Speech recognition only works on iOS device");
#endif
    }

    public bool StartRecording()
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (!isRecording)
        {
            Debug.Log("Starting speech recognition...");
            lastRecognizedText = ""; // Reset
            bool success = _StartSpeechRecognition(OnRecognitionCallback);
            if (success)
            {
                isRecording = true;
                Debug.Log("Speech recognition started successfully");
            }
            else
            {
                Debug.LogError("Failed to start speech recognition");
            }
            return success;
        }
        return false;
#else
        Debug.Log("Speech recognition only works on iOS device (simulating in editor)");
        // Simulate recognition in editor for testing
        HandleRecognitionResult("Hello from simulated speech recognition!");
        return true;
#endif
    }

    public void StopRecording()
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (isRecording)
        {
            Debug.Log("Stopping speech recognition...");
            _StopSpeechRecognition();
            isRecording = false;
        }
#else
        Debug.Log("Stopped speech recognition (simulated)");
#endif
    }

    private void HandleRecognitionResult(string text)
    {
        Debug.Log($"Recognized text (final): {text}");
        // Recording has automatically stopped
        isRecording = false;
        // This is now only called for final results from native plugin
        OnTextRecognized?.Invoke(text);
    }

    public bool IsRecording
    {
        get { return isRecording; }
    }

    void OnDestroy()
    {
        if (isRecording)
        {
            StopRecording();
        }
    }
}
