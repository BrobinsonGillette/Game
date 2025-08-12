using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

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
        Animator.StringToHash("ATTACK_1"),
        Animator.StringToHash("ATTACK_2"),
        Animator.StringToHash("ATTACK_3")
    };

    private Animator animator;
    private Animations[] CurrentAnimation;
    private bool[] LayerLocked;
    private Action<int> DefaultAnimation;
    protected void Initialize(int Layers,Animations StartAnimation,Animator animator,Action<int> DefaultAnimation)
    {
        LayerLocked = new bool[Layers];
        CurrentAnimation = new Animations[Layers];
        this.DefaultAnimation = DefaultAnimation;
        this.animator = animator;

        for (int i = 0; i < Layers; i++)
        {
            LayerLocked[i] = false;
            CurrentAnimation[i] = StartAnimation;
        }

    }
    public Animations GetCurrentAnimation(int Layer)
    {
        return CurrentAnimation[Layer];
    }

    public void SetLocked(bool Locked, int Layer)
    {
        LayerLocked[Layer] = Locked;
    }
    public void Play(Animations animation, int Layer, bool lockeLayer, bool byPassLock, float crossfadeTime = 0.2f)
    {
        if (animation == global::Animations.None)
        {
            DefaultAnimation(Layer);
            return;
        }
        if (LayerLocked[Layer] && !byPassLock) return;
        //https://www.youtube.com/watch?v=Db88Bo8sZpA&ab_channel=SmallHedgeGames
        //if(byPassLock)
        //    foreach(var item in animator.GetBehaviours<OnExit>())
        //        if(item.layer == Layer)
        //            item.cancel= true;
        if (CurrentAnimation[Layer] == animation) return;
        LayerLocked[Layer] = lockeLayer;
        CurrentAnimation[Layer] = animation;
        animator.CrossFade(AnimationArray[(int)CurrentAnimation[Layer]], crossfadeTime, Layer);
    }
}
