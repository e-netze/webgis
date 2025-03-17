using System.Collections.Generic;
using System.Linq;

namespace E.Standard.WebMapping.Core.Geometry.Topology;

internal class Graph
{
    private Dictionary<int, List<int>> _graph;
    private List<int> _leafNodes = new List<int>();

    public Graph(IEnumerable<GraphEdge> graphEdges)
    {
        BuildGraph(graphEdges);
    }

    private void BuildGraph(IEnumerable<GraphEdge> edges)
    {
        _graph = new Dictionary<int, List<int>>();

        foreach (var edge in edges)
        {
            if (!_graph.ContainsKey(edge.From))
            {
                _graph[edge.From] = new List<int>();
            }

            // both directions
            _graph[edge.From].Add(edge.To);

            if (!_graph.ContainsKey(edge.To))
            {
                _graph[edge.To] = new List<int>();
            }

            _graph[edge.To].Add(edge.From);
        }

        // finding Leaf nodes
        foreach (var edge in edges)
        {
            if (!_leafNodes.Contains(edge.From) && IsLeafNode(edge.From))
            {
                _leafNodes.Add(edge.From);
            }

            if (!_leafNodes.Contains(edge.To) && IsLeafNode(edge.To))
            {
                _leafNodes.Add(edge.To);
            }
        }
    }

    public List<List<int>> FindAllPathFromLeafNodes()
    {
        var allPaths = new List<List<int>>();

        foreach (var leafNode in _leafNodes)
        {
            allPaths.AddRange(FindAllPaths(leafNode));
        }

        return allPaths.Distinct(new PathComparer()).ToList();
    }

    public List<List<int>> FindAllPaths(int startNode)
    {
        var visited = new HashSet<int>();
        var allPaths = new List<List<int>>();

        DFS(startNode, visited, new List<int>(), allPaths);

        return allPaths;
    }

    private void DFS(int currentNode, HashSet<int> visited, List<int> currentPath, List<List<int>> allPaths)
    {
        visited.Add(currentNode);
        currentPath.Add(currentNode);

        //if (!_graph.ContainsKey(currentNode))
        if (currentPath.Count > 1 && _leafNodes.Contains(currentNode))
        {
            // reached a leaf node, add current path to all paths
            allPaths.Add(new List<int>(currentPath));
        }
        else
        {
            foreach (var neighbor in _graph[currentNode])
            {
                if (!visited.Contains(neighbor))
                {
                    DFS(neighbor, visited, currentPath, allPaths);
                }
            }
        }

        visited.Remove(currentNode);
        currentPath.RemoveAt(currentPath.Count - 1);
    }

    private bool IsLeafNode(int currentNode)
    {
        int sum = 0;

        foreach (var node in _graph.Keys)
        {
            sum += _graph[node].Where(n => n == currentNode).Count();
        }

        return sum == 1;
    }

    #region Classes

    private class PathComparer : IEqualityComparer<List<int>>
    {
        public bool Equals(List<int> x, List<int> y)
        {
            if (x.Count != y.Count)
            {
                return false;
            }

            if (Enumerable.SequenceEqual(x, y))
            {
                return true;
            }

            var z = new List<int>(x);
            z.Reverse();

            return Enumerable.SequenceEqual(z, y);
        }

        public int GetHashCode(List<int> obj)
        {
            return 0;
        }
    }

    #endregion
}
