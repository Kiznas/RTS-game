using System.Collections.Generic;
using System.Linq;
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
        private TransformAccessArray _transformAccessArray;
        private MoveJob _moveJob;
        private List<Transform> _selectedUnits = new();
        private Transform[] _transforms;
        private float3[] _destinations;

        private bool _destinationSet;

        private void Start(){
            EventAggregator.Subscribe<SendDestination>(SetDestination);
        }
        private void OnDestroy(){
            EventAggregator.Unsubscribe<SendDestination>(SetDestination);
        }

        private void SetDestination(object arg1, SendDestination unitsDestination)
        {
            _destinationSet = true;
            _selectedUnits = UnitSelections.Instance.unitSelectedList;
            _transforms = _selectedUnits.Select(go => go.transform).ToArray();
            _destinations = unitsDestination.posArray;
            _transformAccessArray = new TransformAccessArray(_transforms);
        }

        private void Update()
        {
            if (_destinationSet)
            {
                if (_transformAccessArray.length > 0)
                {
                    NativeArray<float3> float3S = new NativeArray<float3>(_destinations, Allocator.Persistent);
                    _moveJob = new MoveJob
                    {
                        MoveSpeed = moveSpeed,
                        DeltaTime = Time.deltaTime,
                        TargetPositions = float3S
                    };

                    var moveJobHandle = _moveJob.Schedule(_transformAccessArray);
                    moveJobHandle.Complete();
                    float3S.Dispose();
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
                transform.position =  Vector3.MoveTowards(transform.position, TargetPositions[index], step);
            }
        }
    }
}