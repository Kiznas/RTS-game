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
        public  List<Transform> _unitsTransforms = new();
        public  List<float3> _unitsDestinations = new();
        private TransformAccessArray _transformAccessArray;

        private void Start()
        {
            EventAggregator.Subscribe<SendDestination>(SetDestination);
            _transformAccessArray = new TransformAccessArray(10000);
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
        }

        private void Update()
        {
            if (_unitsTransforms.Count > 0)
            {
                _transformAccessArray.SetTransforms(_unitsTransforms.ToArray());
            }

            if (_transformAccessArray.length > 0)
            {
                MoveJobHandler(_transformAccessArray);
            }
        }

        private void MoveJobHandler(TransformAccessArray transformAccessArray)
        {
            if (transformAccessArray.length > 0 && _unitsDestinations.Count > 0)
            {
                var unitsToDelete = new NativeQueue<int>(Allocator.TempJob);
                var moveJob = new MoveJob
                {
                    MoveSpeed = moveSpeed,
                    DeltaTime = Time.deltaTime,
                    TargetPositions = _unitsDestinations.ToNativeArray(Allocator.Persistent),
                    UnitsToDelete = unitsToDelete
                };

                var moveJobHandle = moveJob.Schedule(transformAccessArray);
                moveJobHandle.Complete();
                CheckAndRemoveCompleted(unitsToDelete);
                

                moveJob.TargetPositions.Dispose();
                unitsToDelete.Dispose();
            }
        }

        private void CheckAndRemoveCompleted(NativeQueue<int> unitsToDelete)
        {
            while (unitsToDelete.TryDequeue(out var unit))
            {
                if (unit < _unitsTransforms.Count)
                {
                    _unitsTransforms.RemoveAt(unit);
                    _unitsDestinations.RemoveAt(unit);
                }
            }
        }

        [BurstCompile]
        private struct MoveJob : IJobParallelForTransform
        {
            public float MoveSpeed;
            public float DeltaTime;
            public NativeArray<float3> TargetPositions;
            [NativeDisableParallelForRestriction] public NativeQueue<int> UnitsToDelete;

            public void Execute(int index, TransformAccess transform)
            {
                var step = MoveSpeed * DeltaTime;
                transform.position = Vector3.MoveTowards(transform.position, TargetPositions[index], step);
                if (Vector3.Distance(transform.position, TargetPositions[index]) < 0.1f && step >= Vector3.Distance(transform.position, TargetPositions[index]))
                {
                    UnitsToDelete.Enqueue(index);
                }
            }
        }
    }
}