using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Text.RegularExpressions;

public class SpeechManager : MonoBehaviour
{
    public CharacterSpeech lisaSpeech;
    public CharacterSpeech oscarSpeech;
    public CharacterSpeech emmaSpeech;
    public CharacterSpeech derekSpeech;

    private Dictionary<string, Queue<string>> messageQueues = new Dictionary<string, Queue<string>>();
    private Dictionary<string, bool> isSpeaking = new Dictionary<string, bool>();
    private Dictionary<string, Coroutine> speakingCoroutines = new Dictionary<string, Coroutine>();
    private float maxWaitTime = 15f;
    private float interMessageDelay = 0.1f;

    void Start()
    {
        messageQueues["lisa"] = new Queue<string>();
        messageQueues["oscar"] = new Queue<string>();
        messageQueues["emma"] = new Queue<string>();
        messageQueues["derek"] = new Queue<string>();

        isSpeaking["lisa"] = false;
        isSpeaking["oscar"] = false;
        isSpeaking["emma"] = false;
        isSpeaking["derek"] = false;
    }

    private string CleanupText(string text)
    {
        text = Regex.Replace(text, @"\[CONVERSATION\]", "");
        text = Regex.Replace(text, @"\s+", " ").Trim();
        return text;
    }

    public void HandleSpeech(string text, string speakerName)
    {
        string cleanText = CleanupText(text);
        string speaker = speakerName.ToLower();
        if (!messageQueues.ContainsKey(speaker)) return;

        messageQueues[speaker].Enqueue(cleanText);

        if (!isSpeaking[speaker])
        {
            speakingCoroutines[speaker] = StartCoroutine(ProcessSpeechQueue(speaker));
        }
    }

    private IEnumerator ProcessSpeechQueue(string speaker)
    {
        isSpeaking[speaker] = true;

        while (messageQueues[speaker].Count > 0)
        {
            string nextMessage = messageQueues[speaker].Dequeue();
            CharacterSpeech currentSpeaker = GetSpeakerComponent(speaker);

            if (currentSpeaker != null)
            {
                float waitStartTime = Time.time;
                bool speechCompleted = false;

                if (currentSpeaker.audioSource.isPlaying)
                {
                    currentSpeaker.StopSpeaking();
                    yield return new WaitForSeconds(0.05f);
                }

                currentSpeaker.SpeakText(nextMessage);

                float startWaitTime = Time.time;
                while (!currentSpeaker.audioSource.isPlaying && Time.time - startWaitTime < 1f)
                {
                    yield return null;
                }

                while (!speechCompleted && Time.time - waitStartTime < maxWaitTime)
                {
                    if (!currentSpeaker.audioSource.isPlaying)
                    {
                        speechCompleted = true;
                        yield return new WaitForSeconds(interMessageDelay);
                        break;
                    }
                    yield return null;
                }

                if (!speechCompleted)
                {
                    currentSpeaker.StopSpeaking();
                }
            }

            if (messageQueues[speaker].Count == 0)
            {
                yield return new WaitForSeconds(0.1f);
                if (messageQueues[speaker].Count == 0)
                {
                    break;
                }
            }
        }

        isSpeaking[speaker] = false;
        speakingCoroutines[speaker] = null;
    }

    private CharacterSpeech GetSpeakerComponent(string speaker)
    {
        return speaker.ToLower() switch
        {
            "lisa" => lisaSpeech,
            "oscar" => oscarSpeech,
            "emma" => emmaSpeech,
            "derek" => derekSpeech,
            _ => null
        };
    }

    public void StopAllSpeech()
    {
        foreach (var coroutine in speakingCoroutines.Values)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
        speakingCoroutines.Clear();

        foreach (var queue in messageQueues.Values)
        {
            queue.Clear();
        }

        foreach (var key in isSpeaking.Keys)
        {
            isSpeaking[key] = false;
        }

        if (lisaSpeech != null) lisaSpeech.StopSpeaking();
        if (oscarSpeech != null) oscarSpeech.StopSpeaking();
        if (emmaSpeech != null) emmaSpeech.StopSpeaking();
        if (derekSpeech != null) derekSpeech.StopSpeaking();
    }

    void OnDisable()
    {
        StopAllSpeech();
    }
}