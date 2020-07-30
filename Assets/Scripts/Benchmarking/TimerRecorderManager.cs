using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TimerRecorderManager : MonoBehaviour {
    public static TimerRecorderManager Instance;

    private List<TimeRecorder> _timeRecorders;

    // private NativeList<float2> positionCollision;

    [SerializeField] private GameObject collisionMarker;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
        
        _timeRecorders = new List<TimeRecorder>();
    }

    public void Dump(string fileName)
    {


        try
        {
            
            using (System.IO.StreamWriter file = new StreamWriter("/home/surue/Documents/SAE/Bachelor/"+fileName+".csv", false))
            {

                file.Write("Name,");
                for (var index = 0; index < _timeRecorders.Count; index++)
                {
                    var timeRecorder = _timeRecorders[index];
                    file.Write(timeRecorder.GetName());
                    if (index < _timeRecorders.Count - 1)
                    {
                        file.Write(",");
                    }
                }
                file.Write(file.NewLine);
                
                int count = 0;

                for (int i = 0; i < _timeRecorders.Count; i++)
                {
                    if (_timeRecorders[i].GetCount() > count)
                    {
                        count = _timeRecorders[i].GetCount();
                    }
                }

                for (int i = 0; i < count; i++)
                {
                    file.Write(i + ",");
                    
                    for (var index = 0; index < _timeRecorders.Count; index++)
                    {
                        var timeRecorder = _timeRecorders[index];
                        file.Write(timeRecorder.GetLine(i));
                        if (index < _timeRecorders.Count - 1)
                        {
                            file.Write(",");
                        }
                    }
                    file.Write(file.NewLine);
                }
                
                //Line of calculus
                file.Write("Average");
                for (int i = 0; i < _timeRecorders.Count; i++)
                {
                    file.Write(",=AVERAGE(" + IntToString(i)+"2:" + IntToString(i) + (count +1).ToString()+")");
                }
                file.Write(file.NewLine);
                file.Write("Max");
                for (int i = 0; i < _timeRecorders.Count; i++)
                {
                    file.Write(",=MAX(" + IntToString(i)+"2:" + IntToString(i) + (count+1).ToString()+")");
                }
                file.Write(file.NewLine);
                file.Write("Min");
                for (int i = 0; i < _timeRecorders.Count; i++)
                {
                    file.Write(",=MIN(" + IntToString(i)+"2:" + IntToString(i) + (count +1).ToString()+")");
                }
                file.Write(file.NewLine);
                
                //Collision writing
                file.Write("CollisionFrame,CollisionPercentage");
                file.Write(file.NewLine);
                var collision = CollisionCounter.collisions;

                foreach (var keyValuePair in collision)
                {
                    file.Write(keyValuePair.Key + "," + keyValuePair.Value);
                    file.Write(file.NewLine);
                }
            }
        }
        catch (Exception e)
        {
            throw new ApplicationException("Ooops", e);
        }

        // positionCollision = CollisionCounter.positions;
        //
        // foreach (var float2 in positionCollision)
        // {
        //      Instantiate(collisionMarker, new Vector3(float2.x, 0, float2.y), Quaternion.identity);
        // }
        
        // Debug.Break();
    }

    static string IntToString(int i)
    {
        switch (i)
        {
            case 0:
                return "B";
            case 1:
                return "C";
            case 2:
                return "D";
            case 3:
                return "E";
            case 4:
                return "F";
            case 5:
                return "G";
            case 6:
                return "H";
            case 7:
                return "I";
            case 8:
                return "J";
            case 9:
                return "K";
            case 10:
                return "L";
        }

        return "ERROR";
    }

    public void RegisterRecorder(TimeRecorder newREcorder)
    {
        _timeRecorders.Add(newREcorder);
    }
}
