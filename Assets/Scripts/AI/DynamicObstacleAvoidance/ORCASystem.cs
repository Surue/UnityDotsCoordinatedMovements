using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public struct Line {
    public float2 direction;
    public float2 point;
}

public struct AgentNeighbor {
    public float2 velocity;
    public float2 position;
}

[UpdateInGroup(typeof(AiGroup))]
[UpdateAfter(typeof(VelocitySystem))]
public class ORCASystem : JobComponentSystem {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool LinearProgram1(NativeArray<Line> lines, int lineNo, float radius, float2 optVelocity,
        bool directionOpt,
        ref float2 result)
    {
        float dotProduct = math.dot(lines[lineNo].point, lines[lineNo].direction);
        float discriminant = math.pow(dotProduct, 2) + math.pow(radius, 2) - math.lengthsq(lines[lineNo].point);

        if (discriminant < 0.0f)
        {
            return false;
        }

        float sqrtDiscriminant = math.sqrt(discriminant);
        float tLeft = -dotProduct - sqrtDiscriminant;
        float tRight = -dotProduct + sqrtDiscriminant;

        for (int i = 0; i < lineNo; ++i)
        {
            float denominator = Det(lines[lineNo].direction, lines[i].direction);
            float numerator = Det(lines[i].direction, lines[lineNo].point - lines[i].point);

            //Check if line lineNo and i are //
            if (math.abs(denominator) <= 0.00001f)
            {
                if (numerator < 0.0f)
                {
                    return false;
                }

                continue;
            }

            float t = numerator / denominator;

            if (denominator >= 0.0f)
            {
                tRight = math.min(tRight, t);
            }
            else
            {
                tLeft = math.max(tLeft, t);
            }

            if (tLeft > tRight)
            {
                return false;
            }
        }

        if (directionOpt)
        {
            if (math.dot(optVelocity, lines[lineNo].direction) > 0.0f)
            {
                result = lines[lineNo].point + tRight * lines[lineNo].direction;
            }
            else
            {
                result = lines[lineNo].point + tLeft * lines[lineNo].direction;
            }
        }
        else
        {
            float t = math.dot(lines[lineNo].direction, optVelocity - lines[lineNo].point);

            if (t < tLeft)
            {
                result = lines[lineNo].point + tLeft * lines[lineNo].direction;
            }
            else if (t > tRight)
            {
                result = lines[lineNo].point + tRight * lines[lineNo].direction;
            }
            else
            {
                result = lines[lineNo].point + t * lines[lineNo].direction;
            }
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int LinearProgram2(NativeArray<Line> lines, int lineCount, float radius, float2 optVelocity,
        bool directionOpt,
        ref float2 result)
    {
        if (directionOpt)
        {
            result = optVelocity * radius;
        }
        else if (math.lengthsq(optVelocity) > math.pow(radius, 2))
        {
            result = math.normalize(optVelocity) * radius;
        }
        else
        {
            result = optVelocity;
        }

        for (int i = 0; i < lineCount; ++i)
        {
            if (!(Det(lines[i].direction, lines[i].point - result) > 0.0f)) continue;

            float2 tmpResult = result;
            if (LinearProgram1(lines, i, radius, optVelocity, directionOpt, ref result)) continue;
            result = tmpResult;
            return i;
        }

        return lineCount;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void LinearProgram3(NativeArray<Line> lines, int lineCount, int nbObstacleLine, int beginLine,
        float radius, ref float2 result)
    {
        float distance = 0.0f;

        for (int i = beginLine; i < lineCount; ++i)
        {
            if (!(Det(lines[i].direction, lines[i].point - result) > distance)) continue;

            NativeList<Line> projectedLines = new NativeList<Line>(Allocator.Temp);
            for (int j = 0; j < nbObstacleLine; j++)
            {
                projectedLines.Add(lines[j]);
            }

            for (int j = nbObstacleLine; j < i; j++)
            {
                Line line;

                float determinant = Det(lines[i].direction, lines[j].direction);

                if (math.abs(determinant) <= 0.000001f)
                {
                    if (math.dot(lines[i].direction, lines[j].direction) > 0.0f)
                    {
                        //Line i and j are in the same direction
                        continue;
                    }

                    line.point = 0.5f * (lines[i].point + lines[j].point);
                }
                else
                {
                    line.point = lines[i].point +
                                 (Det(lines[j].direction, lines[i].point - lines[j].point) /
                                  determinant) * lines[i].direction;
                }

                line.direction = math.normalize(lines[j].direction - lines[i].direction);
                projectedLines.Add(line);
            }

            float2 tmpResult = result;
            if (LinearProgram2(projectedLines, projectedLines.Length, radius,
                new float2(-lines[i].direction.y, lines[i].direction.x),
                true, ref result) < projectedLines.Length)
            {
                result = tmpResult;
            }

            distance = Det(lines[i].direction, lines[i].point - result);

            projectedLines.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Det(float2 v1, float2 v2)
    {
        return v1.x * v2.y - v1.y * v2.x;
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        EntityQuery query = GetEntityQuery(typeof(Velocity), ComponentType.ReadOnly<Translation>(),
            ComponentType.ReadOnly<ORCATag>());

        ArchetypeChunkComponentType<Translation> translationChunk = GetArchetypeChunkComponentType<Translation>(true);
        ArchetypeChunkComponentType<Velocity> velocityChunk = GetArchetypeChunkComponentType<Velocity>(false);

        ORCAJob orcaJob = new ORCAJob
        {
            translationType = translationChunk,
            velocityType = velocityChunk,
            quadrantMap = QuadrantSystem.quadrantMultiHashMap,
            maxNeighbors = Blackboard.Instance.MaxNeighbors,
            neighborsDist = Blackboard.Instance.NeighborsDist,
            radius = Blackboard.Instance.Radius,
            invTimeHorizon = 1.0f / Blackboard.Instance.TimeHorizon,
            dt = UnityEngine.Time.deltaTime,
        };

        return orcaJob.Schedule(query, inputDeps);
    }

    [BurstCompile]
    struct ORCAJob : IJobChunk {
        [ReadOnly] public ArchetypeChunkComponentType<Translation> translationType;
        public ArchetypeChunkComponentType<Velocity> velocityType;

        [ReadOnly] public NativeMultiHashMap<int, QuadrantData> quadrantMap;
        public int maxNeighbors;
        public float neighborsDist;
        public float radius;
        public float invTimeHorizon;
        public float dt;

        private const int MAX_QUADRANT_NEIGHBORS = 4;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            NativeArray<Translation> translations = chunk.GetNativeArray(translationType);
            NativeArray<Velocity> velocities = chunk.GetNativeArray(velocityType);

            NativeArray<Line> orcaLines = new NativeArray<Line>(maxNeighbors, Allocator.Temp);
            NativeArray<KeyValuePair<float, AgentNeighbor>> agentNeighbors =
                new NativeArray<KeyValuePair<float, AgentNeighbor>>(maxNeighbors, Allocator.Temp);
            NativeArray<int> quadrantKeys = new NativeArray<int>(MAX_QUADRANT_NEIGHBORS, Allocator.Temp);

            float invTimeStep = 1.0f / dt;
            float combinedRadius = radius * 2.0f;
            float combinedRadiusSqr = math.pow(combinedRadius, 2);
            float rangeSqr = neighborsDist * neighborsDist;

            for (int entityIdx = 0; entityIdx < chunk.ChunkEntityCount; entityIdx++)
            {
                float2 velocity = velocities[entityIdx].Value;

                //Early exit if the agent is not moving
                if (math.lengthsq(velocity) < 0.001f)
                {
                    continue;
                }

                float2 position = translations[entityIdx].Value.xz;

                int countNeighborQuadrant = 0;
                QuadrantSystem.GetCurrentCellAndNeighborsKeys(position, ref quadrantKeys, ref countNeighborQuadrant);

                //ORCA setup
                int neighborsCount = 0;

                int nbObstacleLine = 0;
                float2 currentPos = position;

                //Get nearest neighbors
                for (int i = 0; i < countNeighborQuadrant; i++)
                {
                    if (!quadrantMap.TryGetFirstValue(quadrantKeys[i], out var neighbor,
                        out var nativeMultiHashMapIterator))
                        continue;
                    do
                    {
                        //TODO use better condition
                        if (math.distancesq(neighbor.position, currentPos) > 0.001f)
                        {
                            float2 dir = currentPos - neighbor.position;
                            float distSqr = math.dot(dir, dir);

                            //If the other agent is under the minimum range => add it
                            if (!(distSqr < rangeSqr)) continue;

                            //If there is a free space, add it immediately
                            if (neighborsCount < maxNeighbors)
                            {
                                agentNeighbors[neighborsCount] = new KeyValuePair<float, AgentNeighbor>(distSqr,
                                    new AgentNeighbor()
                                    {
                                        position = neighbor.position,
                                        velocity = neighbor.velocity
                                    });

                                neighborsCount++;
                            }

                            //Make sure the list is sorted
                            int j = neighborsCount - 1;
                            while (j != 0 && distSqr < agentNeighbors[j - 1].Key)
                            {
                                agentNeighbors[j] = agentNeighbors[j - 1];
                                j--;
                            }

                            //Once a spot with a further agent is found, place if 
                            agentNeighbors[j] = new KeyValuePair<float, AgentNeighbor>(distSqr, new AgentNeighbor()
                            {
                                position = neighbor.position,
                                velocity = neighbor.velocity
                            });

                            //If the list is full, only check agent nearer than the farrest neighbor.
                            if (neighborsCount == maxNeighbors)
                            {
                                rangeSqr = agentNeighbors[maxNeighbors - 1].Key;
                            }
                        }
                    } while (quadrantMap.TryGetNextValue(out neighbor, ref nativeMultiHashMapIterator));
                }

                //Evaluate each neighbors
                for (int neighborIdx = 0; neighborIdx < neighborsCount; neighborIdx++)
                {
                    AgentNeighbor otherAgent = agentNeighbors[neighborIdx].Value;

                    float2 relativePosition = otherAgent.position - position;
                    float2 relativeVelocity = velocity - otherAgent.velocity;
                    float distSqr = math.lengthsq(relativePosition);

                    Line line;
                    float2 u;

                    if (distSqr > combinedRadiusSqr)
                    {
                        // No Collision
                        float2 w = relativeVelocity - invTimeHorizon * relativePosition;

                        // Vector from center to relative velocity
                        float wLengthSqr = math.lengthsq(w);
                        float dotProduct1 = math.dot(w, relativePosition);

                        if (dotProduct1 < 0.0f && math.pow(dotProduct1, 2) > combinedRadiusSqr * wLengthSqr)
                        {
                            // Project on circle
                            float wLength = math.sqrt(wLengthSqr);
                            float2 unitW = w / wLength;

                            line.direction = new float2(unitW.y, -unitW.x);
                            u = (combinedRadius * invTimeHorizon - wLength) * unitW;
                        }
                        else
                        {
                            // Projection on legs
                            float leg = math.sqrt(distSqr - combinedRadiusSqr);

                            if (Det(relativePosition, w) > 0.0f)
                            {
                                line.direction = new float2(
                                                     relativePosition.x * leg - relativePosition.y * combinedRadius,
                                                     relativePosition.x * combinedRadius + relativePosition.y * leg) /
                                                 distSqr;
                            }
                            else
                            {
                                line.direction = -new float2(
                                                     relativePosition.x * leg - relativePosition.y * combinedRadius,
                                                     -relativePosition.x * combinedRadius + relativePosition.y * leg) /
                                                 distSqr;
                            }

                            float dotProduct2 = math.dot(relativeVelocity, line.direction);
                            u = dotProduct2 * line.direction - relativeVelocity;
                        }
                    }
                    else
                    {
                        //Collision
                        float2 w = relativeVelocity - invTimeStep * relativePosition;

                        float wLength = math.length(w);
                        float2 wUnit = w / wLength;

                        line.direction = new float2(wUnit.y, -wUnit.x);
                        u = (combinedRadius * invTimeStep - wLength) * wUnit;
                    }

                    line.point = velocity + 0.5f * u;

                    orcaLines[neighborIdx] = line;
                }

                float2 optimalVel = velocity;
                float2 vel = float2.zero;
                float maxSpeed = velocities[entityIdx].maxSpeed;
                int lineFail = LinearProgram2(orcaLines, neighborsCount, maxSpeed, optimalVel, false, ref vel);

                if (lineFail < neighborsCount)
                {
                    LinearProgram3(orcaLines, neighborsCount, nbObstacleLine, lineFail, maxSpeed, ref vel);
                }

                velocities[entityIdx] = new Velocity()
                {
                    Value = vel,
                    maxSpeed = maxSpeed
                };
            }

            quadrantKeys.Dispose();
            orcaLines.Dispose();
            agentNeighbors.Dispose();
        }
    }
}