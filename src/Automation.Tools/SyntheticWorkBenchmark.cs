using System.Diagnostics;

namespace Automation.Tools;

public static class SyntheticWorkBenchmark
{
    public static SyntheticWorkResult Run(int actorCount, int ticks, int representativeCount = 512)
    {
        if (actorCount <= 0) throw new ArgumentOutOfRangeException(nameof(actorCount));
        if (ticks <= 0) throw new ArgumentOutOfRangeException(nameof(ticks));
        if (representativeCount < 0) throw new ArgumentOutOfRangeException(nameof(representativeCount));

        var states = new byte[actorCount];
        for (var actor = 0; actor < states.Length; actor++) states[actor] = (byte)(actor % 7);
        var stopwatch = Stopwatch.StartNew();
        for (var tick = 0; tick < ticks; tick++)
        {
            for (var actor = 0; actor < states.Length; actor++)
            {
                states[actor] = (byte)((states[actor] + 1 + ((actor ^ tick) & 1)) % 7);
            }
        }
        stopwatch.Stop();

        const ulong offset = 14_695_981_039_346_656_037UL;
        const ulong prime = 1_099_511_628_211UL;
        var checksum = offset;
        for (var actor = 0; actor < states.Length; actor++)
        {
            checksum ^= (ulong)(states[actor] + actor);
            checksum *= prime;
        }

        var sampleCount = Math.Min(actorCount, representativeCount);
        var representatives = new byte[sampleCount];
        for (var index = 0; index < sampleCount; index++)
        {
            representatives[index] = states[(int)((long)index * actorCount / sampleCount)];
        }

        return new(actorCount, ticks, (long)actorCount * ticks, checksum, stopwatch.Elapsed, representatives);
    }
}

public sealed record SyntheticWorkResult(
    int ActorCount,
    int Ticks,
    long Transitions,
    ulong Checksum,
    TimeSpan Elapsed,
    byte[] RepresentativeStates);
