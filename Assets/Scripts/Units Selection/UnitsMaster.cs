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
        private Camera _camera;
        private List<Unit> _selectedUnits = new();

        [SerializeField] private int numRows = 2; // Number of rows in the formation
        [SerializeField] private float unitSpacing = 1.0f; // Spacing between units
        [SerializeField] private float formationDepth = 5.0f; // Depth of the formation box
        
        public void Start(){
            _camera = Camera.main;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(1)) 
            {
                var ray = _camera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out var hit))
                {
                    _selectedUnits = UnitSelections.Instance.unitSelectedList;
                    if (_selectedUnits.Count > 0)
                    {
                        //TODO: Fix rotation so formation will face ray
                        Transform firstSelectedUnit = _selectedUnits[_selectedUnits.Count / 2].transform;
                        Vector3 hitDirection = hit.point - firstSelectedUnit.position;
                        Quaternion targetRotation = Quaternion.LookRotation(hitDirection, Vector3.up);

                        // Calculate the rotation angle for the entire formation
                        float formationAngle = Quaternion.Angle(firstSelectedUnit.rotation, targetRotation);
                        UpdatePositionsWithJobs(hit.point, -formationAngle);
                    }
                }
                else
                {
                    UnitSelections.Instance.DeselectAll();
                }
            }
        }

        private void UpdatePositionsWithJobs(Vector3 destination, float angle)
        {
            var unitsNumber = _selectedUnits.Count;

            var positions = new List<float3>(_selectedUnits.Count);
            positions.AddRange(_selectedUnits.Select(unit => unit.transform.position).Select(dummy => (float3)dummy));

            var unitsStartPos = new NativeArray<float3>(positions.ToArray(), Allocator.TempJob);
            var unitsEndPos = new NativeArray<float3>(positions.ToArray(), Allocator.TempJob);
            var jobData = new PositionUpdateJob
            {
                NumRows = numRows,
                UnitSpacing = unitSpacing,
                FormationDepth = formationDepth,
                Destination = destination,
                SelectedUnitsStartPos =  unitsStartPos,
                SelectedUnitsEndPos = unitsEndPos,
                FormationAngle = angle
            };

            // Schedule and run the job
            var jobHandle = jobData.Schedule(unitsNumber, 64);
            jobHandle.Complete();

            for (var i = 0; i < unitsEndPos.Length; i++)
            {
                _selectedUnits[i].agent.SetDestination(unitsEndPos[i]);
            }

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

