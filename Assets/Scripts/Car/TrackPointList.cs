using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TrackPointList : MonoBehaviour
{
    public List<Transform> Points;

    [ContextMenu(nameof(ReversePoints))]
    void ReversePoints() => Points.Reverse();
    
    [ContextMenu(nameof(UpdateNames))]
    void UpdateNames()
    {
        for (int i = 0; i < Points.Count; i++)
        {
            Points[i].gameObject.name = "Point " + i;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        for (int i = 0; i < Points.Count; i++)
        {
            var point = Points[i];
            if (point == null)
            {
                Debug.LogError($"Invalid TrackPoint at Index {i}");
                continue;
            }

            var nextPoint = i >= Points.Count ? Points[0] : Points[i + 1];
            Gizmos.DrawLine(point.position, nextPoint.position);
            Handles.Label(point.position, "Point " + i);
        }
    }
    #endif
}