﻿using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

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
    // protected override void OnUpdate()
    // {
    //     NativeMultiHashMap<int, QuadrantData> quadrantMap = QuadrantSystem.quadrantMultiHashMap;
    //     float radius = Blackboard.Instance.Radius;
    //     float timeHorizon = Blackboard.Instance.TimeHorizon;
    //     float neighborsDist = Blackboard.Instance.NeighborsDist;
    //     int maxNeighbors = Blackboard.Instance.MaxNeighbors;
    //     float maxSpeed = Blackboard.Instance.MaxSpeed;
    //     float dt = UnityEngine.Time.deltaTime;
    //     float invTimeHorizon = 1.0f / timeHorizon;
    //     
    //     //TODO change it to be ScheduleParallel
    //     Entities.WithReadOnly(quadrantMap).ForEach(
    //         (int nativeThreadIndex, ref Velocity velocity, in ORCATag tag, in Translation translation) =>
    //         {
    //            
    //             if (math.lengthsq(velocity.Value) < 0.001f)
    //             {
    //                 return;
    //             }
    //
    //             //DRAW
    //             // QuadrantSystem.DebugDrawQuadrant(translation.Value);
    //             
    //             NativeList<int> quadrantKeys = QuadrantSystem.GetCurrentCellAndNeighborsKeys(translation.Value);
    //
    //             //ORCA setup
    //             NativeArray<Line> orcaLines = new NativeArray<Line>(maxNeighbors, Allocator.Temp);
    //             int neighborsCount = 0;
    //             NativeArray<KeyValuePair<float, AgentNeighbor>> agentNeighbors =
    //                 new NativeArray<KeyValuePair<float, AgentNeighbor>>(maxNeighbors, Allocator.Temp);
    //             float rangeSqr = neighborsDist * neighborsDist;
    //
    //             int nbObstacleLine = 0;
    //             float2 currentPos = translation.Value.xz;
    //
    //             // if (quadrantMap.IsCreated)
    //             // {
    //             //     orcaLines.Dispose();
    //             //     agentNeighbors.Dispose();
    //             //
    //             //     quadrantKeys.Dispose();
    //             //     return;
    //             // }
    //
    //             for (int i = 0; i < quadrantKeys.Length; i++)
    //             {
    //                 if (!quadrantMap.TryGetFirstValue(quadrantKeys[i], out var neighbor, out var nativeMultiHashMapIterator))
    //                     continue;
    //                 do
    //                 {
    //                     //TODO use better condition
    //                     if (math.distancesq(neighbor.position, currentPos) > 0.001f)
    //                     {
    //                         float2 dir = currentPos - neighbor.position;
    //                         float distSqr = math.dot(dir, dir);
    //
    //                         //If the other agent is under the minimum range => add it
    //                         if (distSqr < rangeSqr)
    //                         {
    //                             //If there is a free space, add it immediately
    //                             if (neighborsCount < maxNeighbors)
    //                             {
    //                                 agentNeighbors[neighborsCount] = new KeyValuePair<float, AgentNeighbor>(distSqr, new AgentNeighbor()
    //                                 { 
    //                                     position =  neighbor.position,
    //                                     velocity = neighbor.velocity
    //                                 });
    //
    //                                 neighborsCount++;
    //                             }
    //                     
    //                             //Make sure the list is sorted
    //                             int j = neighborsCount - 1;
    //                             while (j != 0 && distSqr < agentNeighbors[j - 1].Key)
    //                             {
    //                                 agentNeighbors[j] = agentNeighbors[j - 1];
    //                                 j--;
    //                             }
    //
    //                             //Once a spot with a further agent is found, place if 
    //                             agentNeighbors[j] = new KeyValuePair<float, AgentNeighbor>(distSqr, new AgentNeighbor()
    //                             {
    //                                 position =  neighbor.position,
    //                                 velocity = neighbor.velocity
    //                             });
    //
    //                             //If the list is full, only check agent nearer than the farrest neighbor.
    //                             if (neighborsCount == maxNeighbors)
    //                             {
    //                                 rangeSqr = agentNeighbors[agentNeighbors.Length - 1].Key;
    //                             }
    //                         }
    //                     }
    //                 } while (quadrantMap.TryGetNextValue(out neighbor, ref nativeMultiHashMapIterator));
    //             }
    //             
    //             for(int i = 0; i < neighborsCount; i++)
    //             {
    //                 AgentNeighbor otherAgent = agentNeighbors[i].Value;
    //
    //                 //DRAW
    //                 // Debug.DrawLine(translation.Value,
    //                 //     new Vector3(otherAgent.position.x, translation.Value.y, otherAgent.position.y));
    //                 
    //                 float2 relativePosition = otherAgent.position - translation.Value.xz;
    //                 float2 relativeVelocity = velocity.Value - otherAgent.velocity;
    //                 float distSqr = math.lengthsq(relativePosition);
    //                 float combinedRadius = radius * 2.0f;
    //                 float combinedRadiusSqr = math.pow(combinedRadius, 2);
    //
    //                 Line line;
    //                 float2 u;
    //
    //                 if (distSqr > combinedRadiusSqr)
    //                 {
    //                     // No Collision
    //                     float2 w = relativeVelocity - invTimeHorizon * relativePosition;
    //
    //                     // Vector from center to relative velocity
    //                     float wLengthSqr = math.lengthsq(w);
    //                     float dotProduct1 = math.dot(w, relativePosition);
    //
    //                     if (dotProduct1 < 0.0f && math.pow(dotProduct1, 2) > combinedRadiusSqr * wLengthSqr)
    //                     {
    //                         //Project on circle
    //                         float wLength = math.sqrt(wLengthSqr);
    //                         float2 unitW = w / wLength;
    //
    //                         line.direction = new float2(unitW.y, -unitW.x);
    //                         u = (combinedRadius * invTimeHorizon - wLength) * unitW;
    //                     }
    //                     else
    //                     {
    //                         //Projection on legs
    //                         float leg = math.sqrt(distSqr - combinedRadiusSqr);
    //
    //                         if (Det(relativePosition, w) > 0.0f)
    //                         {
    //                             line.direction = new float2(
    //                                                  relativePosition.x * leg - relativePosition.y * combinedRadius,
    //                                                  relativePosition.x * combinedRadius + relativePosition.y * leg) /
    //                                              distSqr;
    //                         }
    //                         else
    //                         {
    //                             line.direction = -new float2(
    //                                                  relativePosition.x * leg - relativePosition.y * combinedRadius,
    //                                                  -relativePosition.x * combinedRadius + relativePosition.y * leg) /
    //                                              distSqr;
    //                         }
    //
    //                         float dotProduct2 = math.dot(relativeVelocity, line.direction);
    //                         u = dotProduct2 * line.direction - relativeVelocity;
    //                     }
    //                 }
    //                 else
    //                 {
    //                     //Collision
    //                     float invTimeStep = 1.0f / dt;
    //
    //                     float2 w = relativeVelocity - invTimeStep * relativePosition;
    //
    //                     float wLength = math.length(w);
    //                     float2 wUnit = w / wLength;
    //
    //                     line.direction = new float2(wUnit.y, -wUnit.x);
    //                     u = (combinedRadius * invTimeStep - wLength) * wUnit;
    //                 }
    //
    //                 line.point = velocity.Value + 0.5f * u;
    //
    //                 orcaLines[i] = line;
    //             }
    //
    //             float2 optimalVel = velocity.Value;
    //
    //             int lineFail = LinearProgram2(orcaLines, neighborsCount, maxSpeed, optimalVel, false, ref velocity.Value);
    //
    //             if (lineFail < neighborsCount)
    //             {
    //                 LinearProgram3(orcaLines, neighborsCount, nbObstacleLine, lineFail, maxSpeed, ref velocity.Value);
    //             }
    //
    //             orcaLines.Dispose();
    //             agentNeighbors.Dispose();
    //
    //             quadrantKeys.Dispose();
    //         }).ScheduleParallel();
    // }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool LinearProgram1(NativeArray<Line> lines, int lineNo, float radius, float2 optVelocity, bool directionOpt,
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
    private static int LinearProgram2(NativeArray<Line> lines, int lineCount, float radius, float2 optVelocity, bool directionOpt,
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
            if (Det(lines[i].direction, lines[i].point - result) > 0.0f)
            {
                float2 tmpResult = result;
                if (!LinearProgram1(lines, i, radius, optVelocity, directionOpt, ref result))
                {
                    result = tmpResult;
                    return i;
                }
            }
        }

        return lineCount;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void LinearProgram3(NativeArray<Line> lines, int lineCount, int nbObstacleLine, int beginLine, float radius, ref float2 result)
    {
        float distance = 0.0f;

        for (int i = beginLine; i < lineCount; ++i)
        {
            if (Det(lines[i].direction, lines[i].point - result) > distance)
            {
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
                if (LinearProgram2(projectedLines, projectedLines.Length, radius, new float2(-lines[i].direction.y, lines[i].direction.x),
                    true, ref result) < projectedLines.Length)
                {
                    result = tmpResult;
                }

                distance = Det(lines[i].direction, lines[i].point - result);

                projectedLines.Dispose();
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Det(float2 v1, float2 v2)
    {
        return v1.x * v2.y - v1.y * v2.x;
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        EntityQuery query = GetEntityQuery(typeof(Velocity), ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<ORCATag>());

        ArchetypeChunkComponentType<Translation> translationChunk =  GetArchetypeChunkComponentType<Translation>(true);
        ArchetypeChunkComponentType<Velocity> velocityChunk =  GetArchetypeChunkComponentType<Velocity>(false);

        ORCAJob orcaJob = new ORCAJob() {
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
        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            NativeArray<Translation> translations = chunk.GetNativeArray(translationType);
            NativeArray<Velocity> velocities = chunk.GetNativeArray(velocityType);
            
            NativeArray<Line> orcaLines = new NativeArray<Line>(maxNeighbors, Allocator.Temp);
            NativeArray<KeyValuePair<float, AgentNeighbor>> agentNeighbors = new NativeArray<KeyValuePair<float, AgentNeighbor>>(maxNeighbors, Allocator.Temp);
            NativeArray<int> quadrantKeys = new NativeArray<int>(9, Allocator.Temp);
            
            for (int entityID = 0; entityID < chunk.ChunkEntityCount; entityID++) {
                if (math.lengthsq(velocities[entityID].Value) < 0.001f)
                {
                    continue;
                }

                int countNeighborQuadrant = 0;
                QuadrantSystem.GetCurrentCellAndNeighborsKeys(translations[entityID].Value, ref quadrantKeys, ref countNeighborQuadrant);

                //ORCA setup
                int neighborsCount = 0;
                float rangeSqr = neighborsDist * neighborsDist;

                int nbObstacleLine = 0;
                float2 currentPos = translations[entityID].Value.xz;

                for (int i = 0; i < countNeighborQuadrant; i++)
                {
                    if (!quadrantMap.TryGetFirstValue(quadrantKeys[i], out var neighbor, out var nativeMultiHashMapIterator))
                        continue;
                    do
                    {
                        //TODO use better condition
                        if (math.distancesq(neighbor.position, currentPos) > 0.001f)
                        {
                            float2 dir = currentPos - neighbor.position;
                            float distSqr = math.dot(dir, dir);

                            //If the other agent is under the minimum range => add it
                            if (distSqr < rangeSqr)
                            {
                                //If there is a free space, add it immediately
                                if (neighborsCount < maxNeighbors)
                                {
                                    agentNeighbors[neighborsCount] = new KeyValuePair<float, AgentNeighbor>(distSqr, new AgentNeighbor()
                                    { 
                                        position =  neighbor.position,
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
                                    position =  neighbor.position,
                                    velocity = neighbor.velocity
                                });

                                //If the list is full, only check agent nearer than the farrest neighbor.
                                if (neighborsCount == maxNeighbors)
                                {
                                    rangeSqr = agentNeighbors[agentNeighbors.Length - 1].Key;
                                }
                            }
                        }
                    } while (quadrantMap.TryGetNextValue(out neighbor, ref nativeMultiHashMapIterator));
                }
                
                for(int i = 0; i < neighborsCount; i++)
                {
                    AgentNeighbor otherAgent = agentNeighbors[i].Value;

                    float2 relativePosition = otherAgent.position - translations[entityID].Value.xz;
                    float2 relativeVelocity = velocities[entityID].Value - otherAgent.velocity;
                    float distSqr = math.lengthsq(relativePosition);
                    float combinedRadius = radius * 2.0f;
                    float combinedRadiusSqr = math.pow(combinedRadius, 2);

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
                            //Project on circle
                            float wLength = math.sqrt(wLengthSqr);
                            float2 unitW = w / wLength;

                            line.direction = new float2(unitW.y, -unitW.x);
                            u = (combinedRadius * invTimeHorizon - wLength) * unitW;
                        }
                        else
                        {
                            //Projection on legs
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
                        float invTimeStep = 1.0f / dt;

                        float2 w = relativeVelocity - invTimeStep * relativePosition;

                        float wLength = math.length(w);
                        float2 wUnit = w / wLength;

                        line.direction = new float2(wUnit.y, -wUnit.x);
                        u = (combinedRadius * invTimeStep - wLength) * wUnit;
                    }

                    line.point = velocities[entityID].Value + 0.5f * u;

                    orcaLines[i] = line;
                }
                
                float2 optimalVel = velocities[entityID].Value;
                float2 vel = float2.zero;
                int lineFail = LinearProgram2(orcaLines, neighborsCount, velocities[entityID].maxSpeed, optimalVel, false, ref vel);

                if (lineFail < neighborsCount)
                {
                    LinearProgram3(orcaLines, neighborsCount, nbObstacleLine, lineFail, velocities[entityID].maxSpeed, ref vel);
                }
                
                velocities[entityID] = new Velocity()
                {
                    Value = vel,
                    maxSpeed = velocities[entityID].maxSpeed
                };

            }
            
            quadrantKeys.Dispose();
            orcaLines.Dispose();
            agentNeighbors.Dispose();
        }
    }
}