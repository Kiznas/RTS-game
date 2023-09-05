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
        public float moveSpeed = 1f;
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
            var selectedUnitsSet = new HashSet<Transform>(UnitSelections.Instance.unitSelectedList);
            var destinations = unitsDestination.posArray;

            for (int i = _transformAccessArray.length - 1; i >= 0; i--)
            {
                if (selectedUnitsSet.Contains(_transformAccessArray[i].transform))
                {
                    _units.RemoveAtSwapBack(i);
                    _transformAccessArray.RemoveAtSwapBack(i);
                }
            }

            var index = 0;
            foreach (var unit in selectedUnitsSet)
            {
                UnitMovementStruct newUnit = new UnitMovementStruct{
                    ID = index };
                
                _units.Add(newUnit);
                _transformAccessArray.Add(unit);
                NavMeshQuerySystem.RequestPathStatic(newUnit.ID, unit.position, destinations[index]);
                index++;
            }

            NavMeshQuerySystem.RegisterPathResolvedCallbackStatic(AddWaypoints);
        }

        private void AddWaypoints(int id, List<float3> corners)
        {
            var movementStruct = _units[id];
            movementStruct.DestinationPoints = new UnsafeList<float3>(corners.Count, Allocator.TempJob);
            for (int i = 1; i < corners.Count; i++)
            {
                movementStruct.DestinationPoints.Add(corners[i]);
            }
            _units[id] = movementStruct;
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
                    Units = unsafeList
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
            for (var i = 0; i < _transformAccessArray.length; i++)
            {
                if (_units[i].DestinationPoints.Length > 0)
                {
                    if (Vector3.Distance(_transformAccessArray[i].position, _units[i].DestinationPoints[^1]) < 0.01f)
                    {
                        _units[i].DestinationPoints.Dispose();
                        _units.RemoveAtSwapBack(i);
                        _transformAccessArray.RemoveAtSwapBack(i);
                    }
                }
            }
        }

        [BurstCompile]
        private struct MoveJob : IJobParallelForTransform
        {
            public float MoveSpeed;
            public float DeltaTime;
            [NativeDisableParallelForRestriction]
            public UnsafeList<UnitMovementStruct> Units;

            public void Execute(int index, TransformAccess transform)
            {
                var step = MoveSpeed * DeltaTime; 
                transform.position = Vector3.MoveTowards(transform.position, Units[index].DestinationPoints[0], step);
                if (Units[index].DestinationPoints.Length > 0 && Vector3.Distance(transform.position, Units[index].DestinationPoints[0]) < 0.01f)
                {
                    Units[index].DestinationPoints.RemoveAt(0);
                    Debug.Log("deleted");
                }
            }
        }

        private struct UnitMovementStruct
        {
            public UnsafeList<float3> DestinationPoints;
            public int ID;
        }
    }
}