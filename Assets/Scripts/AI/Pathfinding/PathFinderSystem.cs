using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class PathFinderSystem : SystemBase
{
    private EntityCommandBufferSystem ecbSystem;

    private WaypointGraph _waypointGraph;

    protected override void OnCreate()
    {
        base.OnCreate();
        
        ecbSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
        
        _waypointGraph = WaypointGraph.Instance;
    }

    protected override void OnUpdate()
    {
        // var ecb = ecbSystem.CreateCommandBuffer().ToConcurrent();
        NativeList<JobHandle> jobHandlesList = new NativeList<JobHandle>(Allocator.Temp);

        if (_waypointGraph == null)
        {
            _waypointGraph = WaypointGraph.Instance;
        }
        
        //TODO Try to optimize
        Entities.WithStructuralChanges().WithoutBurst().ForEach((Entity entity, int entityInQueryIndex, DynamicBuffer<PathPositions> pathPositionBuffer, ref PathFindingRequest request, ref PathFollow pathFollow) =>
        {
            //use A* to find path
            FindPathJob findPathJob = new FindPathJob()
            {
                endPos = request.endPos,
                Neighbors = _waypointGraph.Neighbors,
                path = pathPositionBuffer,
                startPos = request.startPos,
                waypoints = _waypointGraph.Waypoints
            };

            //Setup pathFollow
            pathFollow.Value = pathPositionBuffer.Length - 1;
            
            // jobHandlesList.Add(findPathJob.Schedule());
            
            //TODO optimize
            findPathJob.Run();
            
            //Remove path finding request from the entity
            // ecb.RemoveComponent<PathFindingRequest>(entityInQueryIndex, entity);
            EntityManager.RemoveComponent<PathFindingRequest>(entity);

        }).Run();

        // ecbSystem.AddJobHandleForProducer(Dependency);
        
        JobHandle.CompleteAll(jobHandlesList);
        jobHandlesList.Dispose();
        
        // Dependency.Complete();
    }
    
    [BurstCompile]
    struct FindPathJob : IJob {

        [ReadOnly] public float2 startPos;
        [ReadOnly] public float2 endPos;
        [ReadOnly] public NativeArray<Waypoint> waypoints;
        [ReadOnly] public NativeArray<WaypointNeighbors> Neighbors;
        [WriteOnly] public DynamicBuffer<PathPositions> path;
        
        public void Execute() {
            //Create containers
            //TODO put it outside and clean after finding path to optimize allocation
            NativeArray<int> cameFrom = new NativeArray<int>(waypoints.Length, Allocator.Temp);
            NativeArray<float> totalCost = new NativeArray<float>(waypoints.Length, Allocator.Temp);

            for (int i = 0; i < totalCost.Length; i++)
            {
                totalCost[i] = float.MaxValue;
            }
            
            //TODO Change container to have something more optimized
            NativeList<int> openList = new NativeList<int>(Allocator.Temp);
            NativeList<int> closedList = new NativeList<int>(Allocator.Temp);

            //Get start and end index
            int startIndex = GetClosestNodeIndex(startPos);
            int endIndex = GetClosestNodeIndex(endPos);
            
            //Security if startIndex == endIndex
            if (startIndex == endIndex)
            {
                path.Clear();
                path.Add(new PathPositions()
                {
                    Value = waypoints[startIndex].position
                });
                return;
            }

            totalCost[startIndex] = 0;
            openList.Add(startIndex);

            int maxIteration = 20;
            
            Debug.Log("startIndex = " + startIndex);
            Debug.Log("endIndex = " + endIndex);
            
            while (maxIteration-- > 0 && openList.Length > 0)
            {
                int currentIndex = 0;
                
                //Get lowest cost node
                float lowestCost = totalCost[openList[0]];

                int indexToRemove = 0;
                for (int i = 1; i < openList.Length; i++) {
                    if (totalCost[openList[i]] < lowestCost) {
                        lowestCost = totalCost[openList[i]];
                        indexToRemove = i;
                    }
                }
    
                currentIndex = openList[indexToRemove];
                Debug.Log("    current index = " + currentIndex);
                openList.RemoveAt(indexToRemove);
                
                //Add to closed list
                closedList.Add(currentIndex);
                
                //If the current node is the end node then the algorithm is finished 
                if (currentIndex == endIndex)
                {
                    Debug.Log("OUT");
                    break;
                }
                
                //Check neighbors
                for (int i = 0; i < waypoints[currentIndex].neigborCount; i++)
                {
                    int neighborLinkIndex = i + waypoints[currentIndex].firstNeighbors;
                    Debug.Log("        index = " + neighborLinkIndex);
                    int neighborIndex = Neighbors[neighborLinkIndex].neighborsIndex;

                    Debug.Log("        neighbors index : " + neighborIndex + ", at position" + waypoints[neighborIndex].position);
                    
                    //Compute new cost
                    float newCost = 
                        totalCost[currentIndex] + //Total cost
                        Neighbors[neighborLinkIndex].moveCost + //Move cost
                        math.distance(waypoints[neighborIndex].position,waypoints[endIndex].position);  //Heuristic cost
                    Debug.Log("            totalCost[currentIndex] = " + totalCost[currentIndex]);
                    Debug.Log("            Neighbors[neighborLinkIndex].moveCost = " + Neighbors[neighborLinkIndex].moveCost);
                    Debug.Log("            math.distance(waypoints[i].position,waypoints[neighborIndex].position)" + math.distance(waypoints[neighborIndex].position,waypoints[endIndex].position));
                    Debug.Log("            new cost = " + newCost);
                    if (newCost < totalCost[neighborIndex])
                    {
                        Debug.Log("                Set new cost");
                        totalCost[neighborIndex] = newCost;
                        cameFrom[neighborIndex] = currentIndex;

                        if (!openList.Contains(neighborIndex))
                        {
                            Debug.Log("                Add " + neighborIndex + " to openList");
                            openList.Add(neighborIndex);
                        }
                    }
                }
            }

            Debug.Log("iteration = " + maxIteration);
            
            //Calculate path
            CalculatePath(cameFrom, endIndex, startIndex);
            
            //Dispose every temporary allocated container
            cameFrom.Dispose();
            totalCost.Dispose();
            openList.Dispose();
            closedList.Dispose();
        }
        
        void CalculatePath(NativeArray<int> cameFrom, int endIndex, int startIndex) {
            //Clear path
            path.Clear();
            
            //Build path
            int currentIndex = endIndex;
            int maxIteration = 20;
            while (--maxIteration > 0 && currentIndex != startIndex)
            {
                path.Add(new PathPositions()
                {
                    Value = waypoints[currentIndex].position
                });
                Debug.Log(currentIndex + " ? " + startIndex);
                currentIndex = cameFrom[currentIndex];
            }
            
            Debug.Log(maxIteration);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int GetLowestCostNodeIndex(NativeList<int> openList, NativeArray<float> totalCost)
        {
            float lowestCost = totalCost[openList[0]];

            int index = 0;
            for (int i = 1; i < openList.Length; i++) {
                if (totalCost[openList[i]] < lowestCost) {
                    lowestCost = totalCost[openList[i]];
                    index = i;
                }
            }

            return index;
        }

        int GetClosestNodeIndex(float2 pos)
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
}
