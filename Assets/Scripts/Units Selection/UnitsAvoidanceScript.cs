using System;
using System.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace Units_Selection
{
	public class UnitsAvoidanceScript : MonoBehaviour
	{
		private TransformAccessArray _transformAccessArray;
		private UnsafeList<float3> _lastPositionsOfUnits;

		private void Start()
		{
			_transformAccessArray = new TransformAccessArray(40000);
			_lastPositionsOfUnits = new UnsafeList<float3>(40000, Allocator.Persistent);
		}

		[ContextMenu("StartAvoid")]
		public void StartAvoiding() => 
					StartCoroutine(AvoidingJob());

		IEnumerator AvoidingJob()
		{
			_transformAccessArray.SetTransforms(UnitSelections.Instance.UnitList.ToArray());
			UnsafeList<float3> posList = new UnsafeList<float3>(40000, Allocator.TempJob);
			NativeQueue<AvoidanceStruct> avoidanceQueue = new NativeQueue<AvoidanceStruct>(Allocator.TempJob);
			foreach (var unit in UnitSelections.Instance.UnitList)
			{
				posList.Add(unit.position);
			}
			if (_lastPositionsOfUnits.Length <= 0) 
				_lastPositionsOfUnits.CopyFrom(posList);
			var avoidance = new AvoidanceJob
			{
						PosList = posList,
						AvoidanceStructs = avoidanceQueue,
						LastPos = _lastPositionsOfUnits
			};
                
			var moveJobHandle = avoidance.Schedule(_transformAccessArray);
			moveJobHandle.Complete();
			
			_lastPositionsOfUnits.CopyFrom(posList);
			
			while (avoidanceQueue.TryDequeue(out var avoidanceData))
			{
				EventAggregator.Post(this, new AvoidanceMove {destination = avoidanceData.DestinationPoint, indexOfUnit = avoidanceData.IndexOfUnit});
			}
			
			posList.Dispose();
			avoidanceQueue.Dispose();
			yield return new WaitForSeconds(2f);
			StartCoroutine(AvoidingJob());
		}

		[BurstCompile]
		private struct AvoidanceJob : IJobParallelForTransform
		{
			public UnsafeList<float3> PosList;
			public UnsafeList<float3> LastPos;
			[NativeDisableParallelForRestriction]
			public NativeQueue<AvoidanceStruct> AvoidanceStructs;

			private const float NEIGHBOR_DIST = 1f;

			public void Execute(int unitIndex, TransformAccess transform)
			{
				for (int index = 0; index < PosList.Length; index++)
				{
					if (index == unitIndex)
						continue;

					float3 pos = PosList[index];
					if ((transform.position - (Vector3)pos).sqrMagnitude <= NEIGHBOR_DIST * NEIGHBOR_DIST && transform.position == (Vector3)LastPos[unitIndex] && (Vector3)PosList[index] == (Vector3)LastPos[index])
					{
						float3 moveDirection =  pos - (float3)transform.position;
						var destinationPoint = new float3(
									pos.x + moveDirection.x * -2f,
									pos.y,
									pos.z + moveDirection.z * -2f);
						AvoidanceStructs.AsParallelWriter().Enqueue(new AvoidanceStruct(destinationPoint, unitIndex));
					}
				}
			}
		}
		private struct AvoidanceStruct
		{
			public readonly float3 DestinationPoint;
			public readonly int IndexOfUnit;

			public AvoidanceStruct(float3 destinationPoint, int index)
			{
				DestinationPoint = destinationPoint;
				IndexOfUnit = index;
			}
		}
	}
}