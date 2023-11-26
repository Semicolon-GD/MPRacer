using System.Collections.Generic;
using UnityEngine;

public class FollowPath : MonoBehaviour
{
    public float Turn { get; private set; }

    Transform _nextPoint;
    int _pointIndex;
    public float DistanceToNextPoint;
    public List<Transform> CurrentTrackPoints;

    [ContextMenu(nameof(ResetNextPoint))]
    void ResetNextPoint() => _nextPoint = CurrentTrackPoints[0];

    void Awake()
    {
        CurrentTrackPoints = FindFirstObjectByType<TrackPointList>().Points;
    }

    void Update()
    {
        if (_nextPoint == null)
            PickNextPoint();
        
        DistanceToNextPoint = Vector3.Distance(transform.position, _nextPoint.position);
        
        if (DistanceToNextPoint < 5f)
            PickNextPoint();
        
        TurnTowardsNextPoint();
    }

    void PickNextPoint()
    {
        var pastIndex = _pointIndex;
        _pointIndex++;
        if (_pointIndex >= CurrentTrackPoints.Count)
            _pointIndex = 0;

        _nextPoint = CurrentTrackPoints[_pointIndex];
        Debug.LogError($"Switching from point {pastIndex} to {_pointIndex}");
    }

    void TurnTowardsNextPoint()
    {
        var directionToPoint = _nextPoint.position - transform.position;
        directionToPoint.Normalize();
        var cross = Vector3.Cross(transform.forward, directionToPoint);
        Turn = cross.y;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, _nextPoint.position);
    }
}