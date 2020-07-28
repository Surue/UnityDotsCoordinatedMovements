using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Entities.CodeGeneratedJobForEach;
using Unity.Jobs;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class TimeRecorder {
    private string _name;

    private List<double> _times;

    private bool registered = false;
    
    public TimeRecorder(string name)
    {
        _name = name;
        _times = new List<double>();
    }

    public void RegisterTimeInMS(double newTime)
    {
        //Ignore first frame because timer == 0
        if (!registered)
        {
            TimerRecorderManager.Instance.RegisterRecorder(this);
            registered = true;
        }
        else
        {
            _times.Add(newTime);
        }
    }

    public int GetCount()
    {
        return _times.Count;
    }

    public string GetName()
    {
        return _name;
    }

    public double GetLine(int index)
    {
        return _times[index];
    }

    public void DumpData()
    {
        double avg = 0;
        double max = _times[0];
        double min = _times[0];
        
        Debug.Log(_times[0]);
        
        foreach (var time in _times)
        {
            avg += time;

            if (time > max)
            {
                max = time;
            }else if (time < min)
            {
                min = time;
            }
        }

        avg /= _times.Count;
        
        Debug.Log(_name + " : " + "\t Average = " + avg + "\t Min = " + min + " \t Max = " + max);
    }
}
