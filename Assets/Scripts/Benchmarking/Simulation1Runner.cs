using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Simulation1Runner : MonoBehaviour {
    [SerializeField] private string testName;
    [SerializeField] private string _scene;

    [SerializeField] private float simulationTime = 1;
    private float timer = 0;

    private static bool created = false;

    [SerializeField] private int minDistance = 1;
    [SerializeField] private int maxDistance = 10;
    [SerializeField] private int minNeighbors = 1;
    [SerializeField] private int maxNeighbors = 50;
    [SerializeField] private int minSizeCell = 1;
    [SerializeField] private int maxSizeCell = 50;
    [SerializeField] private int minSpeed = 1;
    [SerializeField] private int maxSpeed = 50;
    [SerializeField] private int minTimeFuture = 1;
    [SerializeField] private int maxTimeFuture = 50;
    
    enum State : uint {
        DEFAULT,
        DISTANCE_NEIGHBORS,
        MAX_NEEIGHBORS,    
        SIZE_CELL,
        SPEED,
        TIME_FUTUR,
        SHAPE,
    }

    private State _state = State.DEFAULT;

    private float testedValue;

    private const int NB_STEP_PER_TEST = 10;
    private int currentTestIndex = -1;
    
    private void Awake()
    {
        if (!created)
        {
            created = true;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Step();
    }

    private void Update()
    {
        timer += Time.deltaTime;

        //end simulation
        if (timer > simulationTime)
        {
            Debug.Log(testName + "_" + StateToString(_state) + "_" + testedValue);
            TimerRecorderManager.Instance.Dump(testName + "_" + StateToString(_state) + "_" + testedValue);
            World.DisposeAllWorlds();
            SceneManager.LoadScene(_scene);
            DefaultWorldInitialization.Initialize("Default World", false);
            Blackboard.Instance.ResetDefaultValues();
            Step();
            timer = 0;
        }
    }

    private void Step()
    {
        currentTestIndex++;
        if (currentTestIndex == NB_STEP_PER_TEST)
        {
            switch (_state)
            {
                case State.DEFAULT:
                    _state = State.DISTANCE_NEIGHBORS;
                    break;
                case State.DISTANCE_NEIGHBORS:
                    _state = State.MAX_NEEIGHBORS;
                    break;
                case State.MAX_NEEIGHBORS:
                    _state = State.SIZE_CELL;
                    break;
                case State.SIZE_CELL:
                    _state = State.SPEED;
                    break;
                case State.SPEED:
                    _state = State.TIME_FUTUR;
                    break;
                case State.TIME_FUTUR:
                    _state = State.SHAPE;
                    break;
                case State.SHAPE:
                {
                    Application.Quit();
                }
                    break;
            }

            currentTestIndex = 0;
        }
        switch (_state)
        {
            case State.DEFAULT:
                testedValue = currentTestIndex;
                break;
            case State.DISTANCE_NEIGHBORS:
            {
                float newValue = LerpValue(minDistance, maxDistance, currentTestIndex / (float) NB_STEP_PER_TEST);

                Blackboard.Instance.NeighborsDist = newValue;
                testedValue = newValue;
            }
                break;
            case State.MAX_NEEIGHBORS:
            {
                int newValue = LerpValue(minNeighbors, maxNeighbors, currentTestIndex / (float) NB_STEP_PER_TEST);

                Blackboard.Instance.MaxNeighbors = newValue;
                testedValue = newValue;
            }
                break;
            case State.SIZE_CELL:
            {
                float newValue = LerpValue(minSizeCell, maxSizeCell, currentTestIndex / (float) NB_STEP_PER_TEST);

                Blackboard.Instance.QuadrantSize = newValue;
                testedValue = newValue;
            }
                break;
            case State.SPEED:
            {
                float newValue = LerpValue(minSpeed, maxSpeed, currentTestIndex / (float) NB_STEP_PER_TEST);

                Blackboard.Instance.MaxSpeed = newValue;
                testedValue = newValue;
            }
                break;
            case State.TIME_FUTUR:
            {
                float newValue = LerpValue(minTimeFuture, maxTimeFuture, currentTestIndex / (float) NB_STEP_PER_TEST);

                Blackboard.Instance.TimeHorizon = newValue;
                testedValue = newValue;
            }
                break;
            case State.SHAPE:
            {}
                break;
        }
    }

    static float LerpValue(float min, float max, float t)
    {
        return Mathf.Max(min + (max - min + 1) * t, 1);
    }
    
    static int LerpValue(int min, int max, float t)
    {
        t += 0.1f;
        return min + Mathf.FloorToInt((max - 1) * t);
    }

    static string StateToString(State state)
    {
        switch (state)
        {
            case State.DEFAULT:
                return "default";
            case State.DISTANCE_NEIGHBORS:
                return "distanceNeighbors";
            case State.MAX_NEEIGHBORS:
                return "maxNeighbors";
            case State.SIZE_CELL:
                return "sizeCell";
            case State.SHAPE:
                return "shape";
            case State.SPEED:
                return "speed";
            case State.TIME_FUTUR:
                return "timeFuture";
            default:
                return "noName";
        }
    }
}
