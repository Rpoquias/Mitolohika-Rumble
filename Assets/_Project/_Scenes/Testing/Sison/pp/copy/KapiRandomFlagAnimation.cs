
using UnityEngine;

public class KapiRandomFlagAnimation : MonoBehaviour
{
    void Start()
    {
        Animator animator = GetComponent<Animator>();

        if (animator == null) return;

        // Give each flag a different speed
        animator.speed = Random.Range(0.9f, 1.3f);

        // Give each flag a different starting point
        AnimatorStateInfo state =
            animator.GetCurrentAnimatorStateInfo(0);

        animator.Play(
            state.fullPathHash,
            0,
            Random.Range(0f, 1f)
        );

        animator.Update(0f);
    }
}
