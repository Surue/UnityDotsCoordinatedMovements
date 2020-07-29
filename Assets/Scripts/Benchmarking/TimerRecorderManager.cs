using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Packages.Rider.Editor.UnitTesting;
using UnityEngine;

public class TimerRecorderManager : MonoBehaviour {
    public static TimerRecorderManager Instance;

    private List<TimeRecorder> _timeRecorders;
    
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

    private void OnDestroy()
    {


        try
        {
            
            using (System.IO.StreamWriter file = new StreamWriter("/home/surue/Documents/SAE/Bachelor/test.csv", false))
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
            }
        }
        catch (Exception e)
        {
            throw new ApplicationException("Ooops", e);
        }  
        
    }

    public void RegisterRecorder(TimeRecorder newREcorder)
    {
        _timeRecorders.Add(newREcorder);
    }
}
