using System.Collections.Generic;
using UnityEngine;

public class Node
{
    public Vector3 position;
    public List<Ball> BallsInSide = new List<Ball>();
    public bool Asleep;

    public void AddNode(Ball ball)
    {
        BallsInSide.Add(ball);
    }
}
