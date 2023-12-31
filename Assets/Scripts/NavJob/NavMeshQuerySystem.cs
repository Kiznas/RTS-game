using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.AI;

namespace NavJob
{
    public enum PathfindingFailedReason
    {
        InvalidToOrFromLocation,
        FailedToBegin,
        FailedToResolve,
    }

    
    public class NavMeshQuerySystem : MonoBehaviour
    {

        /// <summary>
        /// How many navmesh queries are run on each update.
        /// </summary>
        public int maxQueries = 526;

        /// <summary>
        /// Maximum path size of each query
        /// </summary>
        public int maxPathSize = 2048;

        /// <summary>
        /// Maximum iteration on each update cycle
        /// </summary>
        public int maxIterations = 2048;

        /// <summary>
        /// Max map width
        /// </summary>
        public int maxMapWidth = 10000;

        /// <summary>
        /// Cache query results
        /// </summary>
        public bool useCache;


        private NavMeshWorld _world;
        private NavMeshQuery _locationQuery;
        private ConcurrentQueue<PathQueryData> _queryQueue;
        private NativeList<PathQueryData> _progressQueue;
        private ConcurrentQueue<int> _availableSlots;
        private List<int> _takenSlots;
        private List<JobHandle> _handles;
        private List<NativeArray<int>> _statuses;
        private List<NativeArray<NavMeshLocation>> _results;
        private PathQueryData[] _queryDatas;
        private NavMeshQuery[] _queries;
        private Dictionary<int, UpdateQueryStatusJob> _jobs;
        private static NavMeshQuerySystem _instance;
        private static NavMeshQuerySystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<NavMeshQuerySystem>();
                }
                return _instance;
            }
        }
        public delegate void SuccessQueryDelegate (int id, List<float3> corners);

        private delegate void FailedQueryDelegate (int id, PathfindingFailedReason reason);
        private SuccessQueryDelegate _pathResolvedCallbacks;
        private FailedQueryDelegate _pathFailedCallbacks;
        private readonly ConcurrentDictionary<int, List<float3>> _cachedPaths = new();

        private struct PathQueryData
        {
            public int ID;
            public int Key;
            public float3 From;
            public float3 To;
            public int AreaMask;
        }

        /// <summary>
        /// Request a path. The ID is for you to identify the path
        /// </summary>
        /// <param name="id"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="areaMask"></param>
        private void RequestPath (int id, Vector3 from, Vector3 to, int areaMask = -1)
        {
            var key = GetKey ((int) from.x, (int) from.z, (int) to.x, (int) to.z);
            if (useCache)
            {
                if (_cachedPaths.TryGetValue (key, out List<float3> waypoints))
                {
                    _pathResolvedCallbacks?.Invoke (id, waypoints);
                    return;
                }
            }
            var data = new PathQueryData { ID = id, From = from, To = to, AreaMask = areaMask, Key = key };
            _queryQueue.Enqueue (data);
        }

        /// <summary>
        /// Static counterpart of RegisterPathResolvedCallback.
        /// </summary>
        /// <param name="callback"></param>
        public static void RegisterPathResolvedCallbackStatic (SuccessQueryDelegate callback)
        {
            Instance._pathResolvedCallbacks += callback;
        }

        /// <summary>
        /// Static counterpart of RequestPath
        /// </summary>
        /// <param name="id"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="areaMask"></param>
        public static void RequestPathStatic (int id, Vector3 from, Vector3 to, int areaMask = -1)
        {
            Instance.RequestPath (id, from, to, areaMask);
        }

        [BurstCompile]
        private struct UpdateQueryStatusJob : IJob
        {
            public NavMeshQuery Query;
            public PathQueryData Data;
            public int MaxIterations;
            public int MaxPathSize;
            public NativeArray<int> Statuses;
            public NativeArray<NavMeshLocation> Results;

            public void Execute()
            {
                var status = Query.UpdateFindPath(MaxIterations, out _);

                if (status == PathQueryStatus.InProgress |
                    status == (PathQueryStatus.InProgress | PathQueryStatus.OutOfNodes))
                {
                    Statuses[0] = 0;
                    return;
                }

                Statuses[0] = 1;

                if (status == PathQueryStatus.Success)
                {
                    var endStatus = Query.EndFindPath(out int polySize);
                    if (endStatus == PathQueryStatus.Success)
                    {
                        var polygons = new NativeArray<PolygonId>(polySize, Allocator.Temp);
                        Query.GetPathResult(polygons);
                        var straightPathFlags = new NativeArray<StraightPathFlags>(MaxPathSize, Allocator.Temp);
                        var vertexSide = new NativeArray<float>(MaxPathSize, Allocator.Temp);
                        var cornerCount = 0;
                        var pathStatus = PathUtils.FindStraightPath(
                            Query,
                            Data.From,
                            Data.To,
                            polygons,
                            polySize,
                            ref Results,
                            ref straightPathFlags,
                            ref vertexSide,
                            ref cornerCount,
                            MaxPathSize
                        );

                        if (pathStatus == PathQueryStatus.Success)
                        {
                            Statuses[1] = 1;
                            Statuses[2] = cornerCount;
                        }
                        else
                        {
                            Statuses[0] = 1;
                            Statuses[1] = 2;
                        }

                        polygons.Dispose();
                        straightPathFlags.Dispose();
                        vertexSide.Dispose();
                    }
                    else
                    {
                        Statuses[0] = 1;
                        Statuses[1] = 2;
                    }
                }
                else
                {
                    Statuses[0] = 1;
                    Statuses[1] = 3;
                }
            }
        }

        private void Update ()
        {
            int j = 0;
            while (_queryQueue.Count > 0 && _availableSlots.Count > 0)
            {
                if (_queryQueue.TryDequeue (out PathQueryData pending))
                {
                    if (useCache && _cachedPaths.TryGetValue (pending.Key, out List<float3> waypoints))
                    {
                        _pathResolvedCallbacks?.Invoke (pending.ID, waypoints);
                    }
                    else if (_availableSlots.TryDequeue (out int index))
                    {
                        var query = new NavMeshQuery (_world, Allocator.Persistent, maxPathSize);
                        var from = query.MapLocation (pending.From, Vector3.one * 10, 0);
                        var to = query.MapLocation (pending.To, Vector3.one * 10, 0);
                        if (!query.IsValid (from) || !query.IsValid (to))
                        {
                            query.Dispose ();
                            _pathFailedCallbacks?.Invoke (pending.ID, PathfindingFailedReason.InvalidToOrFromLocation);
                            continue;
                        }
                        var status = query.BeginFindPath (from, to, pending.AreaMask);
                        if (status == PathQueryStatus.InProgress || status == PathQueryStatus.Success)
                        {
                            j++;
                            _takenSlots.Add (index);
                            _queries[index] = query;
                            _queryDatas[index] = pending;
                        }
                        else
                        {
                            _queryQueue.Enqueue (pending);
                            _availableSlots.Enqueue (index);
                            _pathFailedCallbacks?.Invoke (pending.ID, PathfindingFailedReason.FailedToBegin);
                            query.Dispose ();
                        }
                    }
                    else
                    {
                        Debug.Log ("index not available");
                        _queryQueue.Enqueue (pending);
                    }
                }
                if (j > maxQueries)
                {
                    Debug.LogError ("Infinite loop detected");
                    break;
                }
            }

            foreach (var index in _takenSlots)
            {
                var job = new UpdateQueryStatusJob ()
                {
                    MaxIterations = maxIterations,
                    MaxPathSize = maxPathSize,
                    Data = _queryDatas[index],
                    Statuses = _statuses[index],
                    Query = _queries[index],
                    Results = _results[index]
                };
                _jobs[index] = job;
                _handles[index] = job.Schedule();
            }

            for (int i = _takenSlots.Count - 1; i > -1; i--)
            {
                int index = _takenSlots[i];
                _handles[index].Complete ();
                var job = _jobs[index];
                if (job.Statuses[0] == 1)
                {
                    if (job.Statuses[1] == 1)
                    {
                        var waypoints = new List<float3>();
                        for (int k = 0; k < job.Statuses[2]; k++)
                        {
                            waypoints.Add(new float3(job.Results[k].position.x,job.Results[k].position.y, job.Results[k].position.z));
                        }
                        if (useCache)
                        {
                            _cachedPaths[job.Data.Key] = waypoints;
                        }
                        _pathResolvedCallbacks?.Invoke (job.Data.ID, waypoints);
                    }
                    else if (job.Statuses[1] == 2)
                    {
                        _pathFailedCallbacks?.Invoke (job.Data.ID, PathfindingFailedReason.FailedToResolve);
                    }
                    else if (job.Statuses[1] == 3)
                    {
                        if (maxPathSize < job.MaxPathSize * 2)
                        {
                            maxPathSize = job.MaxPathSize * 2;
                            // Debug.Log ("Setting path to: " + MaxPathSize);
                        }
                        _queryQueue.Enqueue (job.Data);
                    }
                    _queries[index].Dispose ();
                    _availableSlots.Enqueue (index);
                    _takenSlots.RemoveAt (i);
                }
            }
        }

        protected void Start()
        {
            _world = NavMeshWorld.GetDefaultWorld ();
            _locationQuery = new NavMeshQuery (_world, Allocator.Persistent);
            _availableSlots = new ConcurrentQueue<int> ();
            _progressQueue = new NativeList<PathQueryData> (maxQueries, Allocator.Persistent);
            _handles = new List<JobHandle> (maxQueries);
            _takenSlots = new List<int> (maxQueries);
            _statuses = new List<NativeArray<int>> (maxQueries);
            _results = new List<NativeArray<NavMeshLocation>> (maxQueries);
            _jobs = new Dictionary<int, UpdateQueryStatusJob> (maxQueries);
            _queries = new NavMeshQuery[maxQueries];
            _queryDatas = new PathQueryData[maxQueries];
            for (int i = 0; i < maxQueries; i++)
            {
                _handles.Add (new JobHandle ());
                _statuses.Add (new NativeArray<int> (3, Allocator.Persistent));
                _results.Add (new NativeArray<NavMeshLocation> (maxPathSize, Allocator.Persistent));
                _availableSlots.Enqueue (i);
            }
            _queryQueue = new ConcurrentQueue<PathQueryData> ();
        }

        private void OnDestroy ()
        {
            _progressQueue.Dispose ();
            _locationQuery.Dispose ();
            foreach (var t in _takenSlots)
            {
                _queries[t].Dispose();
            }

            for (int i = 0; i < maxQueries; i++)
            {
                _statuses[i].Dispose ();
                _results[i].Dispose ();
            }
        }

        private int GetKey (int fromX, int fromZ, int toX, int toZ)
        {
            var fromKey = maxMapWidth * fromX + fromZ;
            var toKey = maxMapWidth * toX + toZ;
            return maxMapWidth * fromKey + toKey;
        }
    }

    //
    // Copyright (c) 2009-2010 Mikko Mononen memon@inside.org
    //
    // This software is provided 'as-is', without any express or implied
    // warranty.  In no event will the authors be held liable for any damages
    // arising from the use of this software.
    // Permission is granted to anyone to use this software for any purpose,
    // including commercial applications, and to alter it and redistribute it
    // freely, subject to the following restrictions:
    // 1. The origin of this software must not be misrepresented; you must not
    //    claim that you wrote the original software. If you use this software
    //    in a product, an acknowledgment in the product documentation would be
    //    appreciated but is not required.
    // 2. Altered source versions must be plainly marked as such, and must not be
    //    misrepresented as being the original software.
    // 3. This notice may not be removed or altered from any source distribution.
    //

    // The original source code has been modified by Unity Technologies and Zulfa Juniadi.

    [Flags]
    public enum StraightPathFlags
    {
        Start = 0x01, // The vertex is the start position.
        End = 0x02, // The vertex is the end position.
        OffMeshConnection = 0x04 // The vertex is start of an off-mesh link.
    }

    public abstract class PathUtils
    {
        private static float Perp2D (Vector3 u, Vector3 v)
        {
            return u.z * v.x - u.x * v.z;
        }

        private static void Swap (ref Vector3 a, ref Vector3 b)
        {
            (a, b) = (b, a);
        }

        // Retrace portals between corners and register if type of polygon changes
        private static int RetracePortals (NavMeshQuery query, int startIndex, int endIndex, NativeSlice<PolygonId> path, int n, Vector3 termPos, ref NativeArray<NavMeshLocation> straightPath, ref NativeArray<StraightPathFlags> straightPathFlags, int maxStraightPath)
        {
            for (var k = startIndex; k < endIndex - 1; ++k)
            {
                var type1 = query.GetPolygonType (path[k]);
                var type2 = query.GetPolygonType (path[k + 1]);
                if (type1 != type2)
                {
                    query.GetPortalPoints (path[k], path[k + 1], out var l, out var r);
                    GeometryUtils.SegmentSegmentCpa (out var cpa1, out _, l, r, straightPath[n - 1].position, termPos);
                    straightPath[n] = query.CreateLocation (cpa1, path[k + 1]);

                    straightPathFlags[n] = (type2 == NavMeshPolyTypes.OffMeshConnection) ? StraightPathFlags.OffMeshConnection : 0;
                    if (++n == maxStraightPath)
                    {
                        return maxStraightPath;
                    }
                }
            }
            straightPath[n] = query.CreateLocation (termPos, path[endIndex]);
            straightPathFlags[n] = query.GetPolygonType (path[endIndex]) == NavMeshPolyTypes.OffMeshConnection ? StraightPathFlags.OffMeshConnection : 0;
            return ++n;
        }

        public static PathQueryStatus FindStraightPath (NavMeshQuery query, Vector3 startPos, Vector3 endPos, NativeSlice<PolygonId> path, int pathSize, ref NativeArray<NavMeshLocation> straightPath, ref NativeArray<StraightPathFlags> straightPathFlags, ref NativeArray<float> vertexSide, ref int straightPathCount, int maxStraightPath)
        {
            if (!query.IsValid (path[0]))
            {
                straightPath[0] = new NavMeshLocation (); // empty terminator
                return PathQueryStatus.Failure; // | PathQueryStatus.InvalidParam;
            }

            straightPath[0] = query.CreateLocation (startPos, path[0]);

            straightPathFlags[0] = StraightPathFlags.Start;

            var apexIndex = 0;
            var n = 1;

            if (pathSize >= 1)
            {
                var startPolyWorldToLocal = query.PolygonWorldToLocalMatrix (path[0]);

                var apex = startPolyWorldToLocal.MultiplyPoint (startPos);
                var left = new Vector3 (0, 0, 0); // Vector3.zero accesses a static readonly which does not work in burst yet
                var right = new Vector3 (0, 0, 0);
                var leftIndex = -1;
                var rightIndex = -1;

                for (var i = 1; i <= pathSize; ++i)
                {
                    var polyWorldToLocal = query.PolygonWorldToLocalMatrix (path[apexIndex]);

                    Vector3 vl, vr;
                    if (i == pathSize)
                    {
                        vl = vr = polyWorldToLocal.MultiplyPoint (endPos);
                    }
                    else
                    {
                        var success = query.GetPortalPoints (path[i - 1], path[i], out vl, out vr);
                        if (!success)
                        {
                            return PathQueryStatus.Failure; // | PathQueryStatus.InvalidParam;
                        }

                        vl = polyWorldToLocal.MultiplyPoint (vl);
                        vr = polyWorldToLocal.MultiplyPoint (vr);
                    }

                    vl = vl - apex;
                    vr = vr - apex;

                    // Ensure left/right ordering
                    if (Perp2D (vl, vr) < 0)
                        Swap (ref vl, ref vr);

                    // Terminate funnel by turning
                    if (Perp2D (left, vr) < 0)
                    {
                        var polyLocalToWorld = query.PolygonLocalToWorldMatrix (path[apexIndex]);
                        var termPos = polyLocalToWorld.MultiplyPoint (apex + left);

                        n = RetracePortals (query, apexIndex, leftIndex, path, n, termPos, ref straightPath, ref straightPathFlags, maxStraightPath);
                        if (vertexSide.Length > 0)
                        {
                            vertexSide[n - 1] = -1;
                        }

                        //Debug.Log("LEFT");

                        if (n == maxStraightPath)
                        {
                            straightPathCount = n;
                            return PathQueryStatus.Success; // | PathQueryStatus.BufferTooSmall;
                        }

                        apex = polyWorldToLocal.MultiplyPoint (termPos);
                        left.Set (0, 0, 0);
                        right.Set (0, 0, 0);
                        i = apexIndex = leftIndex;
                        continue;
                    }
                    if (Perp2D (right, vl) > 0)
                    {
                        var polyLocalToWorld = query.PolygonLocalToWorldMatrix (path[apexIndex]);
                        var termPos = polyLocalToWorld.MultiplyPoint (apex + right);

                        n = RetracePortals (query, apexIndex, rightIndex, path, n, termPos, ref straightPath, ref straightPathFlags, maxStraightPath);
                        if (vertexSide.Length > 0)
                        {
                            vertexSide[n - 1] = 1;
                        }

                        //Debug.Log("RIGHT");

                        if (n == maxStraightPath)
                        {
                            straightPathCount = n;
                            return PathQueryStatus.Success; // | PathQueryStatus.BufferTooSmall;
                        }

                        apex = polyWorldToLocal.MultiplyPoint (termPos);
                        left.Set (0, 0, 0);
                        right.Set (0, 0, 0);
                        i = apexIndex = rightIndex;
                        continue;
                    }

                    // Narrow funnel
                    if (Perp2D (left, vl) >= 0)
                    {
                        left = vl;
                        leftIndex = i;
                    }
                    if (Perp2D (right, vr) <= 0)
                    {
                        right = vr;
                        rightIndex = i;
                    }
                }
            }

            // Remove the the next to last if duplicate point - e.g. start and end positions are the same
            // (in which case we have get a single point)
            if (n > 0 && (straightPath[n - 1].position == endPos))
                n--;

            n = RetracePortals (query, apexIndex, pathSize - 1, path, n, endPos, ref straightPath, ref straightPathFlags, maxStraightPath);
            if (vertexSide.Length > 0)
            {
                vertexSide[n - 1] = 0;
            }

            if (n == maxStraightPath)
            {
                straightPathCount = n;
                return PathQueryStatus.Success; // | PathQueryStatus.BufferTooSmall;
            }

            // Fix flag for final path point
            straightPathFlags[n - 1] = StraightPathFlags.End;

            straightPathCount = n;
            return PathQueryStatus.Success;
        }
    }

    public abstract class GeometryUtils
    {
        // Calculate the closest point of approach for line-segment vs line-segment.
        public static bool SegmentSegmentCpa (out float3 c0, out float3 c1, float3 p0, float3 p1, float3 q0, float3 q1)
        {
            var u = p1 - p0;
            var v = q1 - q0;
            var w0 = p0 - q0;

            float a = math.dot (u, u);
            float b = math.dot (u, v);
            float c = math.dot (v, v);
            float d = math.dot (u, w0);
            float e = math.dot (v, w0);

            float den = (a * c - b * b);
            float sc, tc;

            if (den == 0)
            {
                sc = 0;
                tc = d / b;

                // todo: handle b = 0 (=> a and/or c is 0)
            }
            else
            {
                sc = (b * e - c * d) / (a * c - b * b);
                tc = (a * e - b * d) / (a * c - b * b);
            }

            c0 = math.lerp (p0, p1, sc);
            c1 = math.lerp (q0, q1, tc);

            return den != 0;
        }
    }
}