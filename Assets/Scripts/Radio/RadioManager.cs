using UnityEngine;
using System.Collections.Generic;

public class RadioManager : MonoBehaviour
{
    public static RadioManager Instance;
    
    [Header("Audio Settings")]
    public AudioSource audioSource;
    public List<AudioClip> musicList = new List<AudioClip>();
    
    private int currentMusicIndex = 0;
    private bool isPlaying = false;
    
    private void Awake()
    {
        Instance = this;
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // 如果有音乐，初始化第一首
        if (musicList.Count > 0)
        {
            audioSource.clip = musicList[currentMusicIndex];
        }
    }

    public void Play()
    {
        if (audioSource != null && musicList.Count > 0)
        {
            audioSource.Play();
            isPlaying = true;
        }
    }

    public void Pause()
    {
        if (audioSource != null)
        {
            audioSource.Pause();
            isPlaying = false;
        }
    }

    public void PlayNext()
    {
        if (musicList.Count == 0) return;
        
        currentMusicIndex++;
        if (currentMusicIndex >= musicList.Count)
        {
            currentMusicIndex = 0;
        }
        
        PlayCurrent();
    }

    public void PlayPrevious()
    {
        if (musicList.Count == 0) return;
        
        currentMusicIndex--;
        if (currentMusicIndex < 0)
        {
            currentMusicIndex = musicList.Count - 1;
        }
        
        PlayCurrent();
    }

    private void PlayCurrent()
    {
        if (musicList.Count > 0)
        {
            audioSource.clip = musicList[currentMusicIndex];
            audioSource.Play();
            isPlaying = true;
        }
    }

    public void SetVolume(float volume)
    {
        if (audioSource != null)
        {
            audioSource.volume = volume;
        }
    }

    public string GetCurrentMusicName()
    {
        if (musicList.Count > 0)
        {
            return musicList[currentMusicIndex].name;
        }
        return "No Music";
    }

    public bool IsPlaying()
    {
        return isPlaying;
    }
}