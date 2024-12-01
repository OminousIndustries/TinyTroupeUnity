using System;
using System.Collections;
using UnityEngine;
using Microsoft.CognitiveServices.Speech;

public class CharacterSpeech : MonoBehaviour
{
    public AudioSource audioSource;
    public Animator characterAnimator;
    public string characterVoice;

    [SerializeField]
    private string SubscriptionKey = "";
    [SerializeField]
    private string Region = "";

    private const int SampleRate = 24000;
    private SpeechConfig speechConfig;
    private SpeechSynthesizer synthesizer;
    private bool isSpeaking = false;
    private Coroutine currentSpeechCoroutine = null;
    private bool isAnimating = false;

    void Start()
    {
        InitializeSpeechConfig();
    }

    private void InitializeSpeechConfig()
    {
        try
        {
            speechConfig = SpeechConfig.FromSubscription(SubscriptionKey, Region);
            speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Raw24Khz16BitMonoPcm);
            speechConfig.SpeechSynthesisVoiceName = characterVoice;
            synthesizer = new SpeechSynthesizer(speechConfig, null);
            Debug.Log($"{gameObject.name} initialized with voice {characterVoice}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to initialize {gameObject.name}: {ex.Message}");
        }
    }

    public void SpeakText(string textToSpeak)
    {
        if (string.IsNullOrEmpty(textToSpeak)) return;
        if (isSpeaking)
        {
            StopSpeaking();
        }

        currentSpeechCoroutine = StartCoroutine(SpeakTextCoroutine(textToSpeak));
    }

    private IEnumerator SpeakTextCoroutine(string textToSpeak)
    {
        Debug.Log($"{gameObject.name} starting speech: {textToSpeak}");
        isSpeaking = true;

        // Only trigger StartTalking if we're not already animating
        if (!isAnimating)
        {
            characterAnimator.SetTrigger("StartTalking");
            isAnimating = true;
        }

        AudioClip audioClip = null;

        try
        {
            var result = synthesizer.StartSpeakingTextAsync(textToSpeak).Result;
            var audioDataStream = AudioDataStream.FromResult(result);
            audioClip = AudioClip.Create(
                "Speech",
                SampleRate * 600,
                1,
                SampleRate,
                true,
                (float[] audioChunk) =>
                {
                    var chunkSize = audioChunk.Length;
                    var audioChunkBytes = new byte[chunkSize * 2];
                    var readBytes = audioDataStream.ReadData(audioChunkBytes);

                    for (int i = 0; i < chunkSize; ++i)
                    {
                        if (i < readBytes / 2)
                        {
                            audioChunk[i] = (short)(audioChunkBytes[i * 2 + 1] << 8 | audioChunkBytes[i * 2]) / 32768.0F;
                        }
                        else
                        {
                            audioChunk[i] = 0.0f;
                        }
                    }
                });
        }
        catch (Exception ex)
        {
            Debug.LogError($"Speech error for {gameObject.name}: {ex.Message}");
            StopSpeaking();
            yield break;
        }

        if (audioClip != null)
        {
            audioSource.clip = audioClip;
            audioSource.Play();

            while (audioSource.isPlaying)
            {
                yield return null;
            }

            // Small delay before stopping animation
            yield return new WaitForSeconds(0.2f);
        }

        StopSpeaking();
    }

    private void Update()
    {
        if (isSpeaking && !audioSource.isPlaying)
        {
            StopSpeaking();
        }
    }

    public void StopSpeaking()
    {
        if (currentSpeechCoroutine != null)
        {
            StopCoroutine(currentSpeechCoroutine);
            currentSpeechCoroutine = null;
        }

        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        if (isAnimating)
        {
            characterAnimator.SetTrigger("StopTalking");
            isAnimating = false;
        }

        isSpeaking = false;

        Debug.Log($"{gameObject.name} stopped speaking");
    }

    void OnDisable()
    {
        StopSpeaking();
    }

    void OnDestroy()
    {
        if (synthesizer != null)
        {
            synthesizer.Dispose();
        }
    }
}