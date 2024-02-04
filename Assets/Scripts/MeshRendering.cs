using System.Linq;
using UnityEngine;
using Unity.Burst;
using Units_Selection;
using UnityEngine.Jobs;
using Unity.Collections;

public class MeshRendering : MonoBehaviour
{
    public Mesh mesh;
    public Material material;
    public Material selectedMaterial;

    private RenderParams _renderParamsReg;
    private RenderParams _renderParamsSelected;

    private readonly Vector3 _meshOffset = new(0, 0.5f, 0);

    private static Transform[] Unselected => UnitSelections.Instance.UnselectedUnitsHash.ToArray();
    private static Transform[] Selected => UnitSelections.Instance.UnitSelectedHash.ToArray();
    
    private TransformAccessArray _selectedTransformAccess;
    private TransformAccessArray _unselectedTransformAccess;
    private bool _changeSelected;

    private void Start()
    {
        _renderParamsReg = new RenderParams(material);
        _renderParamsSelected = new RenderParams(selectedMaterial);
        EventAggregator.Subscribe<UnitsChanged>(ChangeSelected);
    }

    private void ChangeSelected(object arg1, UnitsChanged arg2)
    {
        ChangeRendered(true);
    }

    private void Update()
    {
        RenderUnselected();
        RenderSelected();
        ChangeRendered(false);
    }

    private void ChangeRendered(bool status)
    {
        _changeSelected = status;
    }

    private void RenderUnselected()
    {
        var unselectedCount = Unselected.Length;
        if (unselectedCount > 0)
        {
            if (_changeSelected)
            {
                if (!_unselectedTransformAccess.isCreated){
                    _unselectedTransformAccess = new TransformAccessArray(unselectedCount);
                }
                _unselectedTransformAccess.SetTransforms(Unselected);
            }

            var matrices = new NativeArray<Matrix4x4>(unselectedCount, Allocator.TempJob);

            var job = new UpdateTransformMatricesJob {
                Matrices = matrices,
                Offset = _meshOffset
            };

            var handle = job.Schedule(_unselectedTransformAccess);
            handle.Complete();

            Graphics.RenderMeshInstanced(_renderParamsReg, mesh, 0, matrices, unselectedCount);
            
            matrices.Dispose();
        }
    }

    private void RenderSelected()
    {
        var selectedCount = Selected.Length;
        if (selectedCount > 0)
        {
            if (_changeSelected)
            {
                if (!_selectedTransformAccess.isCreated){
                    _selectedTransformAccess = new TransformAccessArray(selectedCount);
                }
                _selectedTransformAccess.SetTransforms(Selected);
            }
            
            var matrices = new NativeArray<Matrix4x4>(selectedCount, Allocator.TempJob);
            var job = new UpdateTransformMatricesJob{
                Matrices = matrices,
                Offset = _meshOffset
            };

            var handle = job.Schedule(_selectedTransformAccess);
            handle.Complete();

            Graphics.RenderMeshInstanced(_renderParamsSelected, mesh, 0, matrices, selectedCount);
            
            matrices.Dispose();
        }
    }
}

[BurstCompile]
public struct UpdateTransformMatricesJob : IJobParallelForTransform
{
    public NativeArray<Matrix4x4> Matrices;
    public Vector3 Offset;

    public void Execute(int index, TransformAccess transform)
    {
        Matrices[index] = Matrix4x4.TRS(transform.position + Offset,
            transform.rotation, transform.localScale);
    }
}