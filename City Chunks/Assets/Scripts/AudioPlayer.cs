using System.Collections;
using UnityEngine;

public
class AudioPlayer : MonoBehaviour {
 public
  AudioClip clip;
  [Range(0.0f, 1.0f)] public float volume = 0.5f;
 public
  bool loop = false;

 private
  AudioSource source;
 private
  bool started = false;

 public
  void Update() {
    if (GameData.soundEffects) {
      if (source == null || !source.isPlaying) {
        if (started) {
          Destroy(gameObject);
        } else {
          source = gameObject.AddComponent<AudioSource>() as AudioSource;
          source.spatialBlend = 1.0f;
          source.clip = clip;
          source.volume = volume;
          source.loop = loop;
          source.Play();
          started = true;
        }
      }
    } else if (source.isPlaying) {
      Destroy(gameObject);
    }
  }
}
