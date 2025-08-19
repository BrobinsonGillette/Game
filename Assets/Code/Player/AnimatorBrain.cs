using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


public enum Animations
{
    Idle,
    Walk,
    Run,
    Jump,
    Hurt,
    Death,
    Defend,
    ATTACK_1,
    ATTACK_2, 
    ATTACK_3,
    None
}
public class AnimatorBrain : MonoBehaviour
{
    public readonly static int[] AnimationArray =
    {
        Animator.StringToHash("Idle"),
        Animator.StringToHash("Walk"),
        Animator.StringToHash("Run"),
        Animator.StringToHash("Jump"),
        Animator.StringToHash("Hurt"),
        Animator.StringToHash("Death"),
        Animator.StringToHash("Defend"),
        Animator.StringToHash("Attack_1"),
        Animator.StringToHash("Attack_2"),
        Animator.StringToHash("Attack_3")
    };

    private Animator animator;
    private Animations CurrentAnimation;
    private bool LayerLocked;
    private Action DefaultAnimation;
    protected void Initialize(Animations StartAnimation,Animator animator,Action DefaultAnimation)
    {
        LayerLocked = new bool();
        CurrentAnimation = new Animations();
        this.DefaultAnimation = DefaultAnimation;
        this.animator = animator;

        CurrentAnimation = StartAnimation;
    }



    public void Play(Animations animation, bool lockeLayer, bool byPassLock, float crossfadeTime = 0.2f)
    {
        if (animation == global::Animations.None)
        {
            Debug.LogWarning("Trying to play None animation!");
            DefaultAnimation();
            return;
        }
        if (LayerLocked && !byPassLock)
        {
            Debug.LogWarning("Trying to play animation while layer is locked!");
            return;
        }
        //https://www.youtube.com/watch?v=Db88Bo8sZpA&ab_channel=SmallHedgeGames
        if (CurrentAnimation == animation)
        {
            Debug.LogWarning("Trying to play same animation!");
            return;
        }
        LayerLocked = lockeLayer;
        CurrentAnimation = animation;
        animator.CrossFade(AnimationArray[(int)CurrentAnimation], crossfadeTime);
        Debug.Log($"Playing animation: {CurrentAnimation}");
    }
}
