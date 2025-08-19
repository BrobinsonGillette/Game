using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnExit : StateMachineBehaviour
{
    [SerializeField] private  Animations animation;
    [SerializeField] private bool lockeLayer;
    [SerializeField] private float crossFade;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
  
        IEnumerator Wait()
        {
            yield return new WaitForSeconds(stateInfo.length -crossFade);
        }
    }
}
