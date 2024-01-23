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
						Unit1LastPos = _lastPositionsOfUnits
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
			public UnsafeList<float3> Unit1LastPos;
			[NativeDisableParallelForRestriction]
			public NativeQueue<AvoidanceStruct> AvoidanceStructs;

			private const float NEIGHBOR_DIST = 1f;

			public void Execute(int unit1Index, TransformAccess transform)
			{
				float3 unit1Pos = transform.position;
				for (int unit2Index = 0; unit2Index < PosList.Length; unit2Index++)
				{
					if (unit2Index == unit1Index)
						continue;

					float3 unit2Pos = PosList[unit2Index];
					
					bool isOnSamePosition = unit1Pos.Equals(Unit1LastPos[unit1Index]);
					bool isOnPreviousPositionSelf = (Vector3)PosList[unit2Index] == (Vector3)Unit1LastPos[unit2Index];
					bool checkIfTooClose = (transform.position - (Vector3)unit2Pos).sqrMagnitude <= NEIGHBOR_DIST * NEIGHBOR_DIST;
					
					if (checkIfTooClose && isOnSamePosition && isOnPreviousPositionSelf)
					{
						float3 moveDirection =  unit2Pos - (float3)transform.position;
						var destinationPoint = new float3(
									unit2Pos.x + moveDirection.x * -2f,
									unit2Pos.y,
									unit2Pos.z + moveDirection.z * -2f);
						AvoidanceStructs.AsParallelWriter().Enqueue(new AvoidanceStruct(destinationPoint, unit1Index));
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