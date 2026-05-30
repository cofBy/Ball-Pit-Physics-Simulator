using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class spawner : MonoBehaviour
{
    [Header("refrences")]
    public Ball ball;

    List<Ball> balls;
    Wall[] walls;

    [Header("rate")]
    public float BallsPerSecond;
    float timer;
    int done;
    bool pause;

    [Header("baumgarte stabilization")]
    [Range(0, 1)] public float percent;
    public float tolerance;
    public int solverIterations;

    [Header("sleeping agent")]
    public int framesToSleep;
    public float threshold;

    public float savePosFramesAmount;

    [Header("notion of space partitioning")]
    public int cellSize = 10;
    public float gridSize = 10f;

    float step;
    float half;

    public List<Node> nodes;

    [Header("Flags")]
    public bool useSleeping;

    [Header("UI")]
    public TextMeshProUGUI fpsCounter;
    public Color Red;

    public TextMeshProUGUI ballsCounter;
    public TextMeshProUGUI pauseText;

    int lastFrame;
    float[] deltaTimes;
    public int deltaTimesLength;

    public float refreshsPerSecond;
    float refreshTimer;

    public Slider res;
    public Slider fri;
    public Slider gra;
    public Slider qua;

    private void Awake()
    {
        Time.fixedDeltaTime = 1f / 90f;
        Time.maximumDeltaTime = 3f / 90f;

        walls = FindObjectsByType<Wall>(0);

        step = gridSize / cellSize;
        half = gridSize / 2f;

        balls = new List<Ball>();
        nodes = new List<Node>();

        deltaTimes = new float[deltaTimesLength];

        for (int i = 0; i < deltaTimesLength; i++)
        {
            deltaTimes[i] = Time.deltaTime;
        }

        for (int x = 0; x < cellSize; x++)
        {
            for (int y = 0; y < cellSize; y++)
            {
                float posX = x * step - half;
                float posY = y * step - half;

                Node node = new Node();

                node.position = new Vector2(posX, posY);

                nodes.Add(node);
            }
        }
    }
    private void Update()
    {
        SpawnBalls();

        deltaTimes[lastFrame] = Time.unscaledDeltaTime;
        lastFrame = (lastFrame + 1) % deltaTimesLength;

        if (pause == false)
        {
            displayInfo();
        }

        Time.timeScale = pause ? 0 : 1;

        solverIterations = (int)qua.value;
    }
    void displayInfo()
    {
        refreshTimer += Time.deltaTime;
        if (refreshTimer >= refreshsPerSecond)
        {
            refreshTimer = 0;
            fpsCounter.text = "FPS : " + Mathf.RoundToInt(frameRate()).ToString();
            fpsCounter.color = done < 20 ? Color.white : Red;
        }

        ballsCounter.text = "Balls: " + balls.Count.ToString();
    }
    public void pauseButton()
    {
        pause = !pause;

        pauseText.text = pause ? "play" : "pause";
    }
    public void takeAStep()
    {
        StartCoroutine(Step());
    }
    IEnumerator Step()
    {
        pause = false;
        yield return null;
        pause = true;
    }
    public void Restart()
    {
        foreach (Ball ball in balls) Destroy(ball.gameObject);
        balls.Clear();

        foreach (Node n in nodes) n.BallsInSide.Clear();

        done = 0;
    }
    float frameRate()
    {
        float sum = 0f;
        foreach (float dt in deltaTimes)
        {
            sum += dt;
        }

        return deltaTimesLength / sum;
    }
    private void FixedUpdate()
    {
        foreach (Node n in nodes) n.BallsInSide.Clear();

        foreach (Ball ball1 in balls)
        {
            ball1.restitution = res.value / 10;
            ball1.friction = fri.value / 10;
            ball1.acc = (gra.value) * Vector3.down;

            if (useSleeping == true)
            {
                ball1.framesAsleep = (ball1.prevPos - ball1.transform.position).magnitude < threshold ? ball1.framesAsleep + 1 : 0;
                ball1.Asleep = ball1.framesAsleep >= framesToSleep;
                ball1.vel = ball.Asleep ? Vector3.zero : ball1.vel;
                ball1.prevPos = (Time.frameCount % savePosFramesAmount) == 0 ? ball1.transform.position : ball1.prevPos;
            }
            else
            {
                ball1.Asleep = false;
            }

            if (ball1.Asleep == false)
            {
                ball1.vel += ball1.acc * Time.fixedDeltaTime;
                ball1.transform.position += ball1.vel * Time.fixedDeltaTime;
            }

            foreach (Wall ground in walls)
            {
                float PosAlignment = Vector3.Dot(ball1.transform.position - ground.transform.position, ground.transform.up) - ball1.radius;
                float VelAlignment = Vector3.Dot(ball1.vel, ground.transform.up);
                Vector2 bounds = ball1.inBounds(ground);

                if (Mathf.Abs(bounds.x) <= ground.size * 0.5f && Mathf.Abs(bounds.y) <= ground.size * 0.5f && PosAlignment <= 0)
                {
                    ball1.transform.position += ground.transform.up * -PosAlignment;

                    if (VelAlignment < 0)
                    {
                        ball1.bounce(ground.transform.up);
                    }
                }
            }

            int x = Mathf.FloorToInt((ball1.transform.position.x + half) / step);
            int y = Mathf.FloorToInt((ball1.transform.position.z + half) / step);

            x = Mathf.Clamp(x, 0, cellSize - 1);
            y = Mathf.Clamp(y, 0, cellSize - 1);

            int index = x * cellSize + y;

            nodes[index].AddNode(ball1);
        }

        for (int i = 0; i < solverIterations; i++)
        {
            findCollisionGrid();
        }
    }
    void findCollisionGrid()
    {
        for (int x = 0; x < cellSize; x++)
        {
            for (int y = 0; y < cellSize; y++)
            {
                int currentCell = x * cellSize + y;

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int nx = x + dx;
                        int ny = y + dy;

                        if (nx < 0 || ny < 0 || nx >= cellSize || ny >= cellSize)
                            continue;

                        int otherCell = nx * cellSize + ny;

                        if (otherCell < currentCell)
                            continue;

                        checkCells(nodes[currentCell], nodes[otherCell]);
                    }
                }
            }
        }
    }
    void checkCells(Node cell1, Node cell2)
    {
        for (int i = 0; i < cell1.BallsInSide.Count; i++)
        {
            int start = cell1 == cell2 ? i + 1 : 0;

            for (int j = start; j < cell2.BallsInSide.Count; j++)
            {
                Ball ball1 = cell1.BallsInSide[i];
                Ball ball2 = cell2.BallsInSide[j];

                if (ball1 == ball2) continue;

                if (ball1.Asleep == true && ball2.Asleep == true) continue;

                float distance = Vector3.Distance(ball1.transform.position, ball2.transform.position);

                if (distance < ball2.radius + ball1.radius)
                {
                    if (distance < 0.0001f) continue;

                    Vector3 dir = (ball1.transform.position - ball2.transform.position).normalized;

                    float overlap = (ball1.radius + ball2.radius) - distance;

                    Vector3 correction = Mathf.Max(0, overlap - tolerance) * (percent / 2) * dir;

                    ball1.transform.position += correction;
                    ball2.transform.position -= correction;

                    ball1.bounceBalls(ball1, ball2);

                    ball1.Asleep = false;
                    ball2.Asleep = false;
                }
            }
        }
    }
    void SpawnBalls()
    {
        timer += Time.deltaTime;

        if (done < 30 && timer > 1 / BallsPerSecond)
        {
            done = Mathf.RoundToInt(frameRate()) <= 55 ? done + 1 : 0;

            Ball b = Instantiate(ball, transform.position, Quaternion.identity);

            balls.Add(b);

            b.ID = balls.Count - 1;

            timer = 0;
        }
    }
}