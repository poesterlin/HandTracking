using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mortar : MonoBehaviour
{
    public Animator m_Animator;
    public ParticleSystem dust;

    public AudioSource audio1;
    public AudioSource audio2;

    public float dist = 5f;

    void Start()
    {
         m_Animator = gameObject.GetComponent<Animator>();
    }


    public void AddPotion(){
        m_Animator.SetTrigger("AddPotion");
        dust.Play();
        audio1.Play();
        audio2.PlayDelayed(0.8f);
    }
}
