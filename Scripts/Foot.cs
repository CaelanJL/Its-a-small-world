using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Foot
{
    public bool isGrounded = true;
    public Vector2 previous, current, next;
    public Transform targetT; //just stored here for access from other scripts
    public Foot other;
    public float deltaTime = 0f; //used as a t value for lerping foot movement

    public Foot(Vector2 pos, ref Transform target) {
        previous = pos;
        current = pos;
        next = pos;
        targetT = target;
    }

    public float DistToGoal() {
        return Vector2.Distance(current, next);
    }

    public void setGrounded(bool grounded) {
        isGrounded = grounded;
        current = next;
        previous = current;
    }

    public void UpdateFootPos(Vector2 pos) {
        current = pos;
        targetT.position = current;
    }

    public void SetGoal(Vector2 pos) {
        next = pos;
        deltaTime = 0f;
        isGrounded = false;
    }

    public void Reset() {
        next = current;
        previous = current;
        isGrounded = true;
    }
}
