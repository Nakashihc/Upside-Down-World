using TarodevController;
using UnityEngine;

public class GravityControll : MonoBehaviour 
{
    public float Interval;
    private float timer;
    ControlPlayer player;

    private void Start()
    {
        player = FindAnyObjectByType<ControlPlayer>();
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= Interval)
        {
            if(!player.IsGravityReversed)
                player.IsGravityReversed = true;
            else
                player.IsGravityReversed = false;
            timer = 0;
        }
    }
}
