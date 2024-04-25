using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MonitorBreak;

/// <summary>
/// <c>AudioManagement</c> handles the playing of both sound and music. It uses <c>IntializeAtRuntime</c> to load and instantiate the prefab containing the list of sounds from Resources. Access points are <c>PlaySound</c> and <c>PlayMusic</c>.
/// </summary>
[IntializeAtRuntime("AudioManagement")]
public class AudioManagement : MonoBehaviour
{
    private static AudioManagement _instance;
    private static List<(GameObject, AudioSource)> currentSources = new List<(GameObject, AudioSource)>();

    /// <summary>
    /// Class <c>Sound</c> contains all neccesary data <c>AudioManagement</c> needs to play a sound.
    /// </summary>
    [System.Serializable]
    public class Sound
    {
        public string name;
        public AudioClip actualSound;
        public float volume = 1.0f;
    }

    /// <summary>
    /// A serialized list of sounds displayed in the inspector.
    /// </summary>
    public List<Sound> sounds = new List<Sound>();

    /// <summary>
    /// Class <c>Music</c> contains all neccesary data <c>AudioManagement</c> needs to play music.
    /// </summary>
    [System.Serializable]
    public class Music
    {
        public string name;
        public AudioClip music;
    }

    [Header("Music")]

    /// <summary>
    /// The <c>AudioSource</c> music will be played from.
    /// </summary>
    
    public AudioSource jukeBox;
    /// <summary>
    /// A serialized list of music displayed in the inspector.
    /// </summary>
    public List<Music> music = new List<Music>();
    private Music nextMusic = null;

    private void Awake() {
        _instance = this; 
    }

    private void LateUpdate() {
        for(int i = 0; i < currentSources.Count;)
        {
            //Destroy gameobject once it has finished playing its sound
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

    /// <summary>
    /// <c>PlaySound</c> plays a sound recognized by AudioManagement.
    /// </summary>
    /// <param name="soundName"></param>
    /// <returns>The new AudioSource playing the sound. If sound was not recognized returns null.</returns>
    public static AudioSource PlaySound(string soundName)
    {
        if (_instance == null)
        {
            return null;
        }

        foreach(Sound sound in _instance.sounds)
        {
            //If sound is recognized
            if(sound.name.ToLower() == soundName.ToLower())
            {
                //Create new sound object
                GameObject newObject = new GameObject();
                AudioSource newSource = newObject.AddComponent(typeof(AudioSource)) as AudioSource;
                newSource.clip = sound.actualSound;
                newSource.playOnAwake = false;
                newSource.volume = sound.volume;
                newObject.transform.parent = _instance.transform;

                newSource.Play();

                //Add new object to list so we can keep track of it
                currentSources.Add((newObject, newSource));

                return newSource;
            }
        }

        return null;
    }

    /// <summary>
    /// <c>PlayMusic</c> plays music recognized by AudioManagement.
    /// </summary>
    /// <param name="trackName"></param>
    public static void PlayMusic(string trackName)
    {
        if (_instance == null)
        {
            return;
        }

        foreach (Music music in _instance.music)
        {
            if(music.name.ToLower() == trackName.ToLower())
            {
                _instance.nextMusic = music;
                return;
            }
        }
    }
}
