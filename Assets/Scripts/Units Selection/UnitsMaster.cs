using System;
using Unity.Jobs;
 using System.Linq;
 using UnityEngine;
 using Unity.Burst;
 using Unity.Collections;
 using Unity.Mathematics;
 using System.Collections.Generic;

namespace Units_Selection {
    public class UnitsMaster : MonoBehaviour
    {
        private Transform[] _selectedUnits = Array.Empty<Transform>();

        private float _formationAngle;
        private Vector3 _startPos;
        
        [SerializeField] private float unitSpacing = 1.0f; // Spacing between units
        [SerializeField] private float formationDepth = 5.0f; // Depth of the formation box
        
        public void Start(){
            EventAggregator.Subscribe<SendAngle>(AngleReceive);
        }

        public void OnDestroy()
        {
            EventAggregator.Unsubscribe<SendAngle>(AngleReceive);
        }

        private void AngleReceive(object arg1, SendAngle arg2)
        {
            _selectedUnits = UnitSelections.Instance.unitSelectedList;
            _formationAngle = arg2.Angle;
            _startPos = arg2.StartPos;
            UpdatePositionsWithJobs(_startPos, _formationAngle);
        }

        private void UpdatePositionsWithJobs(Vector3 destination, float angle)
        {
            var unitsNumber = _selectedUnits.Length;

            var positions = new List<float3>(_selectedUnits.Length);
            positions.AddRange(_selectedUnits.Select(unit => unit.transform.position).Select(dummy => (float3)dummy));

            var unitsStartPos = new NativeArray<float3>(positions.ToArray(), Allocator.TempJob);
            var unitsEndPos = new NativeArray<float3>(positions.Count, Allocator.TempJob);

            var numRows = (int)Mathf.Ceil(0.5f * Mathf.Sqrt(unitsNumber));

            var jobData = new PositionUpdateJob
            {
                NumRows = numRows,
                UnitSpacing = unitSpacing,
                FormationDepth = formationDepth,
                Destination = destination,
                SelectedUnitsStartPos = unitsStartPos,
                SelectedUnitsEndPos = unitsEndPos,
                FormationAngle = angle
            };

            // Schedule and run the job
            var jobHandle = jobData.Schedule(unitsNumber, 64);
            jobHandle.Complete();

            EventAggregator.Post(this, new SendDestination { PosArray = unitsEndPos , FormationAngle = _formationAngle});

            // Dispose of the NativeArray
            unitsStartPos.Dispose();
            unitsEndPos.Dispose();
        }

        [BurstCompile]
        private struct PositionUpdateJob : IJobParallelFor
        {
            public int NumRows;
            public float UnitSpacing;
            public float FormationDepth;
            public float3 Destination;
            public NativeArray<float3> SelectedUnitsStartPos;
            public NativeArray<float3> SelectedUnitsEndPos;
            public float FormationAngle;
            public void Execute(int index)
            {
                var unitsPerRow = (int)math.ceil((float)SelectedUnitsStartPos.Length / NumRows);
                var rowIndex = index / unitsPerRow;
                var columnIndex = index % unitsPerRow;

                var xOffset = (columnIndex - (unitsPerRow - 1) * 0.5f) * UnitSpacing;
                var zOffset = FormationDepth * 0.5f - rowIndex * UnitSpacing;

                float angleRad = math.radians(FormationAngle);
                float cosAngle = math.cos(angleRad);
                float sinAngle = math.sin(angleRad);

                float rotatedXOffset = xOffset * cosAngle - zOffset * sinAngle;
                float rotatedZOffset = xOffset * sinAngle + zOffset * cosAngle;

                var position = Destination + new float3(rotatedXOffset, 0f, rotatedZOffset);
                SelectedUnitsEndPos[index] = position;
            }
        }
    }
}

