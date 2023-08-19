using System.Collections.Generic;
using Units_Selection;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

public class MeshRendering : MonoBehaviour
{
    public Mesh mesh;
    public Material material;
    public Material selectedMaterial;

    public bool ready;

    private RenderParams _renderParamsReg;
    private RenderParams _renderParamsSelected;

    private static List<Transform> Unselected => UnitSelections.Instance.unselectedUnits;
    private static List<Transform> Selected => UnitSelections.Instance.unitSelectedList;

    [ContextMenu("Start")]
    private void Start()
    {
        _renderParamsReg = new RenderParams(material);
        _renderParamsSelected = new RenderParams(selectedMaterial);
        ready = true;
    }

    private void Update()
    {
        if (ready)
        {
            RenderUnselected();
            RenderSelected();
        }
    }

    private void RenderUnselected()
    {
        if (Unselected.Count > 0)
        {
            var unselected = Unselected;
            Transform[] array = new Transform[unselected.Count];
            unselected.CopyTo(array);
            TransformAccessArray transformAccessArray = new TransformAccessArray(array);
            for (int i = 0; i > unselected.Count; i++)
            {
                transformAccessArray.Add(unselected[i]);
            }
            UpdateTransformMatricesJob job = new UpdateTransformMatricesJob
            {
                Matrices = new NativeArray<Matrix4x4>(unselected.Count, Allocator.TempJob)
            };

            JobHandle handle = job.Schedule(transformAccessArray);
            handle.Complete();
            
            Graphics.RenderMeshInstanced(_renderParamsReg, mesh, 0, job.Matrices, unselected.Count);

            transformAccessArray.Dispose();
            job.Matrices.Dispose();
        }
    }

    private void RenderSelected()
    {
        if (Selected.Count > 0)
        {
            var selected = Selected;
            Transform[] array = new Transform[selected.Count];
            selected.CopyTo(array);
            TransformAccessArray transformAccessArray = new TransformAccessArray(array);
            UpdateTransformMatricesJob job = new UpdateTransformMatricesJob
            {
                Matrices = new NativeArray<Matrix4x4>(selected.Count, Allocator.TempJob)
            };

            JobHandle handle = job.Schedule(transformAccessArray);
            handle.Complete();
            
            Graphics.RenderMeshInstanced(_renderParamsSelected, mesh, 0, job.Matrices, selected.Count);

            transformAccessArray.Dispose();
            job.Matrices.Dispose();
        }
    }
}

[BurstCompile]
public struct UpdateTransformMatricesJob : IJobParallelForTransform
{
    public NativeArray<Matrix4x4> Matrices;
    public void Execute(int index, TransformAccess transform)
    {
        Matrices[index] = Matrix4x4.TRS(transform.position,
            transform.rotation, new float3(1, 1 , 1));
    }
}




