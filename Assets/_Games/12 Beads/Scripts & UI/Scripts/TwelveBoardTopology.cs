using System;

namespace Twelve
{
    public readonly struct TwelveTopologyEdge
    {
        public readonly byte adjacent;
        public readonly byte landing;

        public TwelveTopologyEdge(byte adjacent, byte landing = byte.MaxValue)
        {
            this.adjacent = adjacent;
            this.landing = landing;
        }
    }

    public static class TwelveBoardTopology
    {
        public const int CellCount = 25;
        public const byte NoNode = byte.MaxValue;

        // This is the canonical 12 Bead graph. Runtime rules never read scene connections.
        private static readonly TwelveTopologyEdge[][] edges =
        {
            E(new TwelveTopologyEdge(1, 2), new TwelveTopologyEdge(5, 10), new TwelveTopologyEdge(6, 12)),
            E(new TwelveTopologyEdge(0), new TwelveTopologyEdge(2, 3), new TwelveTopologyEdge(6, 11)),
            E(new TwelveTopologyEdge(3, 4), new TwelveTopologyEdge(1, 0), new TwelveTopologyEdge(6, 10), new TwelveTopologyEdge(8, 14), new TwelveTopologyEdge(7, 12)),
            E(new TwelveTopologyEdge(2, 1), new TwelveTopologyEdge(4), new TwelveTopologyEdge(8, 13)),
            E(new TwelveTopologyEdge(3, 2), new TwelveTopologyEdge(9, 14), new TwelveTopologyEdge(8, 12)),
            E(new TwelveTopologyEdge(0), new TwelveTopologyEdge(6, 7), new TwelveTopologyEdge(10, 15)),
            E(new TwelveTopologyEdge(5), new TwelveTopologyEdge(1), new TwelveTopologyEdge(0), new TwelveTopologyEdge(2), new TwelveTopologyEdge(7, 8), new TwelveTopologyEdge(12, 18), new TwelveTopologyEdge(11, 16), new TwelveTopologyEdge(10)),
            E(new TwelveTopologyEdge(6, 5), new TwelveTopologyEdge(2), new TwelveTopologyEdge(8, 9), new TwelveTopologyEdge(12, 17)),
            E(new TwelveTopologyEdge(3), new TwelveTopologyEdge(4), new TwelveTopologyEdge(7, 6), new TwelveTopologyEdge(9), new TwelveTopologyEdge(13, 18), new TwelveTopologyEdge(2), new TwelveTopologyEdge(14), new TwelveTopologyEdge(12, 16)),
            E(new TwelveTopologyEdge(4), new TwelveTopologyEdge(14, 19), new TwelveTopologyEdge(8, 7)),
            E(new TwelveTopologyEdge(5, 0), new TwelveTopologyEdge(6, 2), new TwelveTopologyEdge(11, 12), new TwelveTopologyEdge(16, 22), new TwelveTopologyEdge(15, 20)),
            E(new TwelveTopologyEdge(10), new TwelveTopologyEdge(6, 1), new TwelveTopologyEdge(12, 13), new TwelveTopologyEdge(16, 21)),
            E(new TwelveTopologyEdge(11, 10), new TwelveTopologyEdge(6, 0), new TwelveTopologyEdge(7, 2), new TwelveTopologyEdge(8, 4), new TwelveTopologyEdge(13, 14), new TwelveTopologyEdge(18, 24), new TwelveTopologyEdge(17, 22), new TwelveTopologyEdge(16, 20)),
            E(new TwelveTopologyEdge(12, 11), new TwelveTopologyEdge(8, 3), new TwelveTopologyEdge(14), new TwelveTopologyEdge(18, 23)),
            E(new TwelveTopologyEdge(9, 4), new TwelveTopologyEdge(8, 2), new TwelveTopologyEdge(13, 12), new TwelveTopologyEdge(18, 22), new TwelveTopologyEdge(19, 24)),
            E(new TwelveTopologyEdge(10, 5), new TwelveTopologyEdge(16, 17), new TwelveTopologyEdge(20)),
            E(new TwelveTopologyEdge(15), new TwelveTopologyEdge(10), new TwelveTopologyEdge(11, 6), new TwelveTopologyEdge(12, 8), new TwelveTopologyEdge(17, 18), new TwelveTopologyEdge(22), new TwelveTopologyEdge(21), new TwelveTopologyEdge(20)),
            E(new TwelveTopologyEdge(16, 15), new TwelveTopologyEdge(12, 7), new TwelveTopologyEdge(18, 19), new TwelveTopologyEdge(22)),
            E(new TwelveTopologyEdge(17, 16), new TwelveTopologyEdge(12, 6), new TwelveTopologyEdge(13, 8), new TwelveTopologyEdge(14), new TwelveTopologyEdge(19), new TwelveTopologyEdge(24), new TwelveTopologyEdge(23), new TwelveTopologyEdge(22)),
            E(new TwelveTopologyEdge(18, 17), new TwelveTopologyEdge(14, 9), new TwelveTopologyEdge(24)),
            E(new TwelveTopologyEdge(15, 10), new TwelveTopologyEdge(16, 12), new TwelveTopologyEdge(21, 22)),
            E(new TwelveTopologyEdge(20), new TwelveTopologyEdge(16, 11), new TwelveTopologyEdge(22, 23)),
            E(new TwelveTopologyEdge(21, 20), new TwelveTopologyEdge(16, 10), new TwelveTopologyEdge(17, 12), new TwelveTopologyEdge(18, 14), new TwelveTopologyEdge(23, 24)),
            E(new TwelveTopologyEdge(22, 21), new TwelveTopologyEdge(18, 13), new TwelveTopologyEdge(24)),
            E(new TwelveTopologyEdge(23, 22), new TwelveTopologyEdge(18, 12), new TwelveTopologyEdge(19, 14))
        };

        public static ReadOnlySpan<TwelveTopologyEdge> GetEdges(byte node)
        {
            return node < edges.Length ? edges[node] : ReadOnlySpan<TwelveTopologyEdge>.Empty;
        }

        public static bool TryResolve(byte from, byte to, out byte capturedNode)
        {
            capturedNode = NoNode;
            if (from >= CellCount || to >= CellCount) return false;
            ReadOnlySpan<TwelveTopologyEdge> nodeEdges = GetEdges(from);
            for (int i = 0; i < nodeEdges.Length; i++)
                if (nodeEdges[i].adjacent == to) return true;
            for (int i = 0; i < nodeEdges.Length; i++)
            {
                if (nodeEdges[i].landing == to)
                {
                    capturedNode = nodeEdges[i].adjacent;
                    return true;
                }
            }
            return false;
        }

        private static TwelveTopologyEdge[] E(params TwelveTopologyEdge[] nodeEdges)
        {
            return nodeEdges;
        }
    }
}
