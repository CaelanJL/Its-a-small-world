using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fish : MonoBehaviour
{
    [HideInInspector] public FishSpawner spawner;
    [HideInInspector] public float moveSpeed;
    private Vector2 target;
    private bool alive = true;
    public Tentacle head, tail;

    // Start is called before the first frame update
    void Start()
    {
        target = spawner.manager.startGlobe.GetPos();
    }

    // Update is called once per frame
    void Update()
    {
        if (alive) {
            Vector2 moveDir = (target - (Vector2)transform.position).normalized;
            transform.position += (Vector3)moveDir * moveSpeed * Time.deltaTime;
        }
        else {
            ReassignLinePoints(head);
            ReassignLinePoints(tail);
        }
    }

    void OnCollisionEnter2D(Collision2D col) {
        if (col.gameObject.layer == 6) { //BUCKET
            spawner.manager.Audio.Play("Thunk");
            spawner.manager.scoreAnim.SetTrigger("score");
            spawner.manager.scoreText.text = (int.Parse(spawner.manager.scoreText.text) + 1).ToString(); //increment score
            GameObject.Destroy(gameObject);
        }
        else if (col.gameObject.layer == 7) { //GLOBE
            // GameObject.Destroy(gameObject); //testing purposes
            spawner.manager.Audio.Play("Splat");
            spawner.LoseHealth();
            head.alive = false;
            tail.alive = false;
            alive = false;
            GetComponent<CapsuleCollider2D>().enabled = false;
        }
        else { //WHAT THE FUCK ARE WE HITTING
            throw new UnityException("Fish is hitting smth naughty, name: " + col.gameObject.name);
        }    
    }

    //places the fish such that it fits to the terrain, must be called every frame after the fish stops moving
    private void ReassignLinePoints(Tentacle line) {
        Vector3[] currPoints = line.segmentPositions;
        GameManager manager = spawner.manager;
        for (int i = 0; i < currPoints.Length; i++)
        {
            currPoints[i] = Globe.GetPosOnCircle(currPoints[i], manager.startGlobe.GetPos(), manager.currRadius, manager.currAmp);
        }
        line.segmentPositions = currPoints;
        line.line.SetPositions(currPoints);
    }
}
