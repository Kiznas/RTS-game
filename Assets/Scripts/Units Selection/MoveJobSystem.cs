using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace Units_Selection
{
    public class MoveJobSystem : MonoBehaviour
    {
        public float moveSpeed = 1f;
        private readonly List<Transform> _unitsTransforms = new();
        private readonly List<float3> _unitsDestinations = new();
        private TransformAccessArray _transformAccessArray;

        private void Start()
        {
            EventAggregator.Subscribe<SendDestination>(SetDestination);
            _transformAccessArray = new TransformAccessArray(40000);
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

            for (int i = _unitsTransforms.Count - 1; i >= 0; i--)
            {
                if (selectedUnitsSet.Contains(_unitsTransforms[i]))
                {
                    int destinationIndex = i < _unitsDestinations.Count ? i : _unitsDestinations.Count - 1;

                    // Remove both the unit and its corresponding destination.
                    _unitsTransforms.RemoveAt(i);
                    _unitsDestinations.RemoveAt(destinationIndex);
                }
            }
            _unitsTransforms.AddRange(selectedUnitsSet);
            _unitsDestinations.AddRange(destinations);
            _transformAccessArray.SetTransforms(_unitsTransforms.ToArray());
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
            if (transformAccessArray.length > 0 && _unitsDestinations.Count > 0)
            {
                var unitsDestination = new NativeArray<float3>(_unitsDestinations.ToArray(), Allocator.TempJob);
                var moveJob = new MoveJob
                {
                    MoveSpeed = moveSpeed,
                    DeltaTime = Time.deltaTime,
                    TargetPositions = unitsDestination
                };
                
                
                var moveJobHandle = moveJob.Schedule(transformAccessArray);
                moveJobHandle.Complete();
                
                if (moveJobHandle.IsCompleted)
                {
                    CheckAndRemoveCompleted();
                }
                
                unitsDestination.Dispose();
            }
        }

        private void CheckAndRemoveCompleted()
        {
            for (var i = 0; i < _unitsTransforms.Count; i++)
            {
                if (Vector3.Distance(_unitsTransforms[i].position, _unitsDestinations[i]) < 0.01f)
                {
                    _unitsTransforms.RemoveAtSwapBack(i);
                    _unitsDestinations.RemoveAtSwapBack(i);
                    _transformAccessArray.RemoveAtSwapBack(i);
                }
            }
        }

        [BurstCompile]
        private struct MoveJob : IJobParallelForTransform
        {
            public float MoveSpeed;
            public float DeltaTime;
            public NativeArray<float3> TargetPositions;

            public void Execute(int index, TransformAccess transform)
            {
                var step = MoveSpeed * DeltaTime; 
                transform.position = Vector3.MoveTowards(transform.position, TargetPositions[index], step);
            }
        }
    }
}