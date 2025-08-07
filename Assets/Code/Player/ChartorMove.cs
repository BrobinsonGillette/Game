using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChartorMove : MonoBehaviour
{
    public static ChartorMove instance { get; private set; }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

   public void MoveChartor(Vector3 targetPosition, float speed)
    {
        if (targetPosition == null) return;
        // Calculate the step size based on speed and time
        float step = speed * Time.deltaTime;
        // Move the Chartor towards the target position
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, step);
    }
  


}
