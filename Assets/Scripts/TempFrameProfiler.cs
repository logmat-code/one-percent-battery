// TEMP DIAGNOSTIC - not part of the game, added only to investigate a Retry frame-pacing hitch.
// Attached at runtime via eval, never referenced by any scene/prefab. Delete after investigation.
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;

public class TempFrameProfiler : MonoBehaviour
{
    private struct FrameRecord
    {
        public int frame;
        public float unscaledDeltaMs;
        public long allocDeltaBytes;
        public int gcCount0Delta;
        public string marker;
    }

    private readonly List<FrameRecord> _records = new List<FrameRecord>(2000);
    private long _lastAllocated;
    private int _lastGcCount0;
    private string _pendingMarker;

    private void Start()
    {
        _lastAllocated = Profiler.GetTotalAllocatedMemoryLong();
        _lastGcCount0 = System.GC.CollectionCount(0);
    }

    private void Update()
    {
        long currentAllocated = Profiler.GetTotalAllocatedMemoryLong();
        int currentGcCount0 = System.GC.CollectionCount(0);
        _records.Add(new FrameRecord
        {
            frame = Time.frameCount,
            unscaledDeltaMs = Time.unscaledDeltaTime * 1000f,
            allocDeltaBytes = currentAllocated - _lastAllocated,
            gcCount0Delta = currentGcCount0 - _lastGcCount0,
            marker = _pendingMarker
        });
        _pendingMarker = null;
        _lastAllocated = currentAllocated;
        _lastGcCount0 = currentGcCount0;
        if (_records.Count > 4000) _records.RemoveAt(0);
    }

    public void Mark(string label) => _pendingMarker = label;

    public void Dump(int lastN = 300)
    {
        int start = Mathf.Max(0, _records.Count - lastN);
        var sb = new StringBuilder();
        sb.Append("[TempFrameProfiler] Dump (last ").Append(_records.Count - start).Append(" frames):\n");
        for (int i = start; i < _records.Count; i++)
        {
            var r = _records[i];
            sb.Append("Frame ").Append(r.frame)
              .Append(": ").Append(r.unscaledDeltaMs.ToString("F2")).Append("ms")
              .Append(" alloc=").Append(r.allocDeltaBytes).Append("B")
              .Append(r.gcCount0Delta > 0 ? " GC_COLLECT_HAPPENED" : "")
              .Append(r.marker != null ? " <-- " + r.marker : "")
              .Append('\n');
        }
        Debug.Log(sb.ToString());
    }
}
