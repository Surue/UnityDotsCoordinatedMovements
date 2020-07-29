using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(AiGroup))]
[UpdateAfter(typeof(FormationFollowerSystem))]
public class PathFinderSystem : JobComponentSystem {
    private WaypointGraph _waypointGraph;

    //Timer specific
    private TimeRecorder timerRecoder;
    static Stopwatch timer = new System.Diagnostics.Stopwatch();

    private static double time = 0;
    //Timer specific

    protected override void OnCreate()
    {
        base.OnCreate();

        _waypointGraph = WaypointGraph.Instance;

        //Timer specific
        timerRecoder = new TimeRecorder("PathFinderSystem");
        //Timer specific
    }

    [BurstCompile]
    struct FindPathJob2 : IJobChunk {
        [ReadOnly] public NativeArray<Waypoint> waypoints;
        [ReadOnly] public NativeArray<WaypointNeighbors> Neighbors;
        [ReadOnly] public ArchetypeChunkComponentType<PathFindingRequest> pathFindingRequestType;
        public ArchetypeChunkBufferType<PathPositions> pathPositionType;
        public ArchetypeChunkComponentType<PathIndex> pathIndexType;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            NativeArray<PathFindingRequest> chunkPathFindingRequest = chunk.GetNativeArray(pathFindingRequestType);
            BufferAccessor<PathPositions> chunkPathPositions = chunk.GetBufferAccessor(pathPositionType);
            NativeArray<PathIndex> chunkPathIndex = chunk.GetNativeArray(pathIndexType);

            NativeArray<int> cameFrom = new NativeArray<int>(waypoints.Length, Allocator.Temp);
            NativeArray<float> totalCost = new NativeArray<float>(waypoints.Length, Allocator.Temp);
            
            //TODO Change container to have something more optimized
            NativeList<int> openList = new NativeList<int>(Allocator.Temp);
            NativeList<int> closedList = new NativeList<int>(Allocator.Temp);
            
            for (int entityIdx = 0; entityIdx < chunk.ChunkEntityCount; entityIdx++)
            {
                for (int i = 0; i < waypoints.Length; i++)
                {
                    cameFrom[i] = 0;
                }
                
                for (int i = 0; i < totalCost.Length; i++)
                {
                    totalCost[i] = float.MaxValue;
                }
                
                openList.Clear();
                closedList.Clear();

                //Get start and end index
                int startIndex = GetClosestNodeIndex(chunkPathFindingRequest[entityIdx].startPos, waypoints);
                int endIndex = GetClosestNodeIndex(chunkPathFindingRequest[entityIdx].endPos, waypoints);

                //Security if startIndex == endIndex
                if (startIndex == endIndex)
                {
                    //Create new path
                    chunkPathPositions[entityIdx].Clear();
                    chunkPathPositions[entityIdx].Add(new PathPositions()
                    {
                        Value = chunkPathFindingRequest[entityIdx].endPos
                    });
                    chunkPathPositions[entityIdx].Add(new PathPositions()
                    {
                        Value = waypoints[startIndex].position
                    });
                    
                    //Assign index 
                    chunkPathIndex[entityIdx] = new PathIndex
                    {
                        Value = chunkPathPositions[entityIdx].Length - 1
                    };
                    return;
                }

                totalCost[startIndex] = 0;
                openList.Add(startIndex);

                int maxIteration = 20;

                while (maxIteration-- > 0 && openList.Length > 0)
                {
                    int currentIndex = 0;

                    //Get lowest cost node
                    float lowestCost = totalCost[openList[0]];

                    int indexToRemove = 0;
                    for (int i = 1; i < openList.Length; i++)
                    {
                        if (totalCost[openList[i]] < lowestCost)
                        {
                            lowestCost = totalCost[openList[i]];
                            indexToRemove = i;
                        }
                    }

                    currentIndex = openList[indexToRemove];
                    openList.RemoveAt(indexToRemove);

                    //Add to closed list
                    closedList.Add(currentIndex);

                    //If the current node is the end node then the algorithm is finished 
                    if (currentIndex == endIndex)
                    {
                        break;
                    }

                    //Check neighbors
                    for (int i = 0; i < waypoints[currentIndex].neigborCount; i++)
                    {
                        int neighborLinkIndex = i + waypoints[currentIndex].firstNeighbors;
                        int neighborIndex = Neighbors[neighborLinkIndex].neighborsIndex;

                        //Compute new cost
                        float newCost =
                            totalCost[currentIndex] + //Total cost
                            Neighbors[neighborLinkIndex].moveCost + //Move cost
                            math.distance(waypoints[neighborIndex].position,
                                waypoints[endIndex].position); //Heuristic cost

                        if (newCost < totalCost[neighborIndex])
                        {
                            totalCost[neighborIndex] = newCost;
                            cameFrom[neighborIndex] = currentIndex;

                            if (!openList.Contains(neighborIndex))
                            {
                                openList.Add(neighborIndex);
                            }
                        }
                    }
                }

                //Calculate path

                chunkPathPositions[entityIdx].Clear();
                CreatePath(chunkPathPositions[entityIdx], cameFrom, endIndex, startIndex,
                    chunkPathFindingRequest[entityIdx].endPos, waypoints);

                chunkPathIndex[entityIdx] = new PathIndex
                {
                    Value = chunkPathPositions[entityIdx].Length - 1
                };
            }
            
            //Dispose every temporary allocated container
            cameFrom.Dispose();
            totalCost.Dispose();
            openList.Dispose();
            closedList.Dispose();
        }

        //TODO use a native array => better performance i think
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CreatePath(DynamicBuffer<PathPositions> path, NativeArray<int> cameFrom, int endIndex, int startIndex, float2 endPos,
            NativeArray<Waypoint> waypoints)
        {
            //Clear path
            path.Clear();

            //Build path
            int currentIndex = endIndex;
            int maxIteration = 20;

            path.Add(new PathPositions()
            {
                Value = endPos
            });

            while (--maxIteration > 0 && currentIndex != startIndex)
            {
                path.Add(new PathPositions()
                {
                    Value = waypoints[currentIndex].position
                });
                currentIndex = cameFrom[currentIndex];
            }

            path.Add(new PathPositions()
            {
                Value = waypoints[startIndex].position
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int GetClosestNodeIndex(float2 pos, NativeArray<Waypoint> waypoints)
        {
            float minDistance = math.distancesq(pos, waypoints[0].position);

            int index = 0;
            for (int i = 1; i < waypoints.Length; i++)
            {
                float distance = math.distancesq(pos, waypoints[i].position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    index = i;
                }
            }

            return index;
        }
    }
    
    struct StartTimerJob : IJob {
        public void Execute()
        {
            timer.Start();
        }
    }

    struct EndTimerJob : IJob {
        public void Execute()
        {
            double ticks = timer.ElapsedTicks;
            double milliseconds = (ticks / Stopwatch.Frequency) * 1000;

            time = milliseconds;
            timer.Stop();
            timer.Reset();
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        //Timer specific
        var startTimerJob = new StartTimerJob();
        var handle = startTimerJob.Schedule(inputDeps);
        //Timer specific

        if (_waypointGraph == null)
        {
            _waypointGraph = WaypointGraph.Instance;
        }

        EntityQuery query = GetEntityQuery(ComponentType.ReadOnly(typeof(PathFindingRequest)));

        if (query.CalculateEntityCount() == 0)
        {
            //Timer specific
            var endTimerJob2 = new EndTimerJob();
            timerRecoder.RegisterTimeInMS(time);
            //Timer specific

            return endTimerJob2.Schedule(handle);
        }

        ArchetypeChunkBufferType<PathPositions> pathPositionBufferChunk = GetArchetypeChunkBufferType<PathPositions>();
        ArchetypeChunkComponentType<PathFindingRequest> pathFindingRequestChunk =
            GetArchetypeChunkComponentType<PathFindingRequest>(true);
        ArchetypeChunkComponentType<PathIndex> pathIndexChunk = GetArchetypeChunkComponentType<PathIndex>();

        //Update quadrants data
        FindPathJob2 findPathJob = new FindPathJob2()
        {
            waypoints = _waypointGraph.Waypoints,
            Neighbors = _waypointGraph.Neighbors,
            pathFindingRequestType = pathFindingRequestChunk,
            pathPositionType = pathPositionBufferChunk,
            pathIndexType = pathIndexChunk
        };

        var handle2 = findPathJob.Schedule(query, handle);

        //Timer specific
        var endTimerJob = new EndTimerJob();
        timerRecoder.RegisterTimeInMS(time);
        //Timer specific

        return endTimerJob.Schedule(handle2);
    }
}