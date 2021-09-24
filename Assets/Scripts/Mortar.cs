using UnityEngine;
using UnityEngine.Events;

public class Mortar : MonoBehaviour
{
    public Animator m_Animator;
    public ParticleSystem dust;

    public AudioSource audio1;
    public AudioSource audio2;
    public UnityEvent OnPotion = new UnityEvent();
    public float dist = 5f;

    void Start()
    {
         m_Animator = gameObject.GetComponent<Animator>();
    }

     void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, dist);
    }


    public void AddPotion(){
        m_Animator.SetTrigger("AddPotion");
        dust.Play();
        audio1.Play();
        audio2.PlayDelayed(0.8f);
        OnPotion.Invoke();
    }
}
