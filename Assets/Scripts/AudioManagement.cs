using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MonitorBreak;

[IntializeAtRuntime("AudioManagement")]
public class AudioManagement : MonoBehaviour
{
    private static AudioManagement _instance;
    private static List<(GameObject, AudioSource)> currentSources = new List<(GameObject, AudioSource)>();

    [System.Serializable]
    public class Sound
    {
        public string name;
        public AudioClip actualSound;
        public float volume = 1.0f;
    }

    public List<Sound> sounds = new List<Sound>();

    [System.Serializable]
    public class Music
    {
        public string name;
        public AudioClip music;
    }

    [Header("Music")]
    public AudioSource jukeBox;
    public List<Music> music = new List<Music>();
    private Music nextMusic = null;

    private void Awake() {
        _instance = this; 
    }

    private void LateUpdate() {
        for(int i = 0; i < currentSources.Count;)
        {
            (GameObject, AudioSource) entry = currentSources[i];

            if(!entry.Item2.isPlaying)
            {
                currentSources.RemoveAt(i);

                Destroy(entry.Item1);
            }
            else
            {
                i++;
            }
        }

        if (jukeBox != null)
        {
            if (nextMusic != null)
            {
                //Fade out current music and queue up next
                if (jukeBox.volume > 0.0f)
                {
                    jukeBox.volume = Mathf.Clamp01(jukeBox.volume - Time.deltaTime);
                }
                else
                {
                    jukeBox.clip = nextMusic.music;
                    jukeBox.Play();
                    nextMusic = null;
                }
            }
            else if (jukeBox.volume < 1.0f)
            {
                jukeBox.volume = Mathf.Clamp01(jukeBox.volume + Time.deltaTime);
            }
        }
    }

    public static AudioSource PlaySound(string soundName)
    {
        foreach(Sound sound in _instance.sounds)
        {
            if(sound.name.ToLower() == soundName.ToLower())
            {
                //Create new sound
                GameObject newObject = new GameObject();
                AudioSource newSource = newObject.AddComponent(typeof(AudioSource)) as AudioSource;
                newSource.clip = sound.actualSound;
                newSource.playOnAwake = false;
                newSource.volume = sound.volume;
                newObject.transform.parent = _instance.transform;

                newSource.Play();

                currentSources.Add((newObject, newSource));

                return newSource;
            }
        }

        return null;
    }

    public static void PlayMusic(string trackName)
    {
        foreach(Music music in _instance.music)
        {
            if(music.name.ToLower() == trackName.ToLower())
            {
                _instance.nextMusic = music;
                return;
            }
        }
    }
}
