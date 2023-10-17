using NavJob;
using Unity.Burst;
using UnityEngine;
using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;

namespace Units_Selection
{
    public class MoveJobSystem : MonoBehaviour
    {
        [SerializeField] private float rotationSpeed;
        [SerializeField] private float moveSpeed = 1f;
        
        private List<UnitMovementStruct> _units;
        private TransformAccessArray _transformAccessArray;
        private static int _lastAssignedID;

        private void Start()
        {
            EventAggregator.Subscribe<SendDestination>(SetDestination);
            _transformAccessArray = new TransformAccessArray(40000);
            _units = new List<UnitMovementStruct>(40000);
        }

        private void OnDestroy()
        {
            EventAggregator.Unsubscribe<SendDestination>(SetDestination);
            _transformAccessArray.Dispose();
        }

        private void SetDestination(object arg1, SendDestination unitsDestination)
        {
            var selectedUnitsSet = new HashSet<Transform>(UnitSelections.Instance.UnitSelectedHash);
            var destinations = unitsDestination.PosArray;

            // Clear the dictionary when starting a new destination calculation
            _unitIndexMap.Clear();

            for (int i = _transformAccessArray.length - 1; i >= 0; i--)
            {
                if (selectedUnitsSet.Contains(_transformAccessArray[i].transform))
                {
                    _units[i].DestinationPoints.Clear();
                    _units.RemoveAtSwapBack(i);
                    _transformAccessArray.RemoveAtSwapBack(i);
                }
            }

            var index = 0;
            foreach (var unit in selectedUnitsSet)
            {
                if(unit == null) 
                    return;
                UnitMovementStruct newUnit = new UnitMovementStruct
                {
                    ID = _lastAssignedID++,
                    DestinationPoints = new UnsafeList<float3>(30, Allocator.Persistent),
                    RotationAngle = unitsDestination.FormationAngle
                };

                _units.Add(newUnit);
                _transformAccessArray.Add(unit);
                
                _unitIndexMap[newUnit.ID] = _units.Count - 1;

                NavMeshQuerySystem.RequestPathStatic(newUnit.ID, unit.position, destinations[index]);
                index++;
            }

            NavMeshQuerySystem.RegisterPathResolvedCallbackStatic(AddWaypoints);
        }


        private readonly Dictionary<int, int> _unitIndexMap = new();

        private void AddWaypoints(int id, List<float3> points)
        {
            if (_unitIndexMap.TryGetValue(id, out var indexToUpdate))
            {
                if (indexToUpdate < _units.Count)
                {
                    var movementStruct = _units[indexToUpdate];
                    movementStruct.DestinationPoints.Clear();   
                    foreach (var point in points)
                    {
                        movementStruct.DestinationPoints.Add(point);
                    }

                    _units[indexToUpdate] = movementStruct;
                }
            }
            else
            {
                Debug.LogWarning($"ID {id} not found in _units list.");
            }
        }


        private void Update()
        {
            if (_transformAccessArray.length > 0)
            {
                MoveJobHandler(_transformAccessArray);
            }
        }

        private void MoveJobHandler(TransformAccessArray transformAccessArray)
        {
            if (transformAccessArray.length > 0 && _units.Count > 0)
            {
                var unsafeList = new UnsafeList<UnitMovementStruct>(_units.Count, Allocator.TempJob);
                foreach (var unit in _units)
                {
                    unsafeList.Add(unit);
                }
                var moveJob = new MoveJob
                {
                    MoveSpeed = moveSpeed,
                    DeltaTime = Time.deltaTime,
                    Units = unsafeList,
                    RotationSpeed = rotationSpeed
                };
                
                var moveJobHandle = moveJob.Schedule(transformAccessArray);
                moveJobHandle.Complete();
                
                if (moveJobHandle.IsCompleted)
                {
                    CheckAndRemoveCompleted();
                }
                
                unsafeList.Dispose();
            }
        }

        [BurstCompile]
        private void CheckAndRemoveCompleted()
        {
            for (var i = 0; i < _units.Count; i++)
            {
                var destinationsLength = _units[i].DestinationPoints.Length;
                if (destinationsLength > 0 && Vector3.Distance(_transformAccessArray[i].position, _units[i].DestinationPoints[0]) < 0.01f)
                {
                    _units[i].DestinationPoints.Dispose();
                    _units.RemoveAtSwapBack(i);
                    _transformAccessArray.RemoveAtSwapBack(i);
                    break;
                }
            }
        }

        [BurstCompile]
        private struct MoveJob : IJobParallelForTransform
        {
            public float MoveSpeed;
            public float DeltaTime;
            public float RotationSpeed;
            public UnsafeList<UnitMovementStruct> Units;

            public void Execute(int index, TransformAccess transform)
            {
                if (Units[index].DestinationPoints.Length > 0)
                {
                    var step = MoveSpeed * DeltaTime;
                    
                    var destinationPoints = Units[index].DestinationPoints;
                    transform.position = Vector3.MoveTowards(transform.position, destinationPoints[0], step);
                    
                    GetLookRotation(transform, destinationPoints, index);
                    
                    if (Vector3.Distance(transform.position, destinationPoints[0]) < 0.01f)
                    {
                        Units[index].DestinationPoints.RemoveAt(0);
                    }
                }
            }

            private void GetLookRotation(TransformAccess transform, UnsafeList<float3> destinationPoints, int index)
            {
                var direction = ((Vector3)destinationPoints[0] - transform.position).normalized;
                if (direction != Vector3.zero)
                {
                    if (Vector3.Distance(transform.position, destinationPoints[0]) > 1)
                    {
                        var lookRotation = Quaternion.LookRotation(direction);
                        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, DeltaTime * RotationSpeed);
                    }
                    else
                    {
                        var rotation = Quaternion.Euler(0, -Units[index].RotationAngle, 0);
                        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, DeltaTime * RotationSpeed * 5);
                    }
                }
            }
        }

        private struct UnitMovementStruct
        {
            public UnsafeList<float3> DestinationPoints;
            public int ID;
            public float RotationAngle;
        }
    }
}