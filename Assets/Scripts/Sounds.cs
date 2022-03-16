using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public class Sounds : MonoBehaviour
{
    public AudioClip[] clips;
    public AudioSource player;

    public void PlaySoundN(int n)
    {
        Assert.IsTrue(n < clips.Length);
        Assert.IsNotNull(clips[n]);
        player.PlayOneShot(clips[n]);
    }
}
