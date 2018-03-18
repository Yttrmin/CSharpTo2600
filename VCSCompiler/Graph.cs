using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Collections.Immutable;

namespace VCSCompiler
{
	/// <summary>
	/// An immutable directed acyclic graph.
	/// </summary>
	internal class ImmutableGraph<T> where T: class
	{
		private readonly IImmutableSet<T> Nodes;
		private readonly IImmutableDictionary<T, IImmutableSet<T>> Edges;

		public static ImmutableGraph<T> Empty { get; } = new ImmutableGraph<T>(ImmutableHashSet<T>.Empty, ImmutableDictionary<T, IImmutableSet<T>>.Empty);

		private ImmutableGraph(IImmutableSet<T> nodes, IImmutableDictionary<T, IImmutableSet<T>> edges)
		{
			Nodes = nodes;
			Edges = edges;
		}

		public ImmutableGraph<T> AddNode(T node)
		{
			if (Nodes.Contains(node))
			{
				throw new ArgumentException($"Graph already contains node: {node}");
			}

			var newNodes = Nodes.Add(node);
			return new ImmutableGraph<T>(newNodes, Edges);
		}

		public ImmutableGraph<T> AddEdge(T from, T to)
		{
			if (!Nodes.Contains(from))
			{
				throw new ArgumentException($"From node: '{from}' not in graph.");
			}
			if (!Nodes.Contains(to))
			{
				throw new ArgumentException($"To node: '{to}' not in graph.");
			}

			var newEdges = ImmutableDictionary.CreateBuilder<T, IImmutableSet<T>>();
			newEdges.AddRange(Edges);
			if (!newEdges.TryGetKey(from, out _))
			{
				newEdges.Add(from, ImmutableHashSet<T>.Empty);
			}

			newEdges[from] = newEdges[from].Add(to);

			return new ImmutableGraph<T>(Nodes, newEdges.ToImmutable());
		}

		public IEnumerable<T> GetNeighbors(T node)
		{
			if (Edges.TryGetValue(node, out var edges))
			{
				return edges;
			}
			return Enumerable.Empty<T>();
		}
	}

	// TODO - Make immutable or at least provide an immutable interface.
	[Obsolete("Use ImmutableGraph")]
    internal class Graph<T> where T: class
    {
		private IList<Node<T>> Nodes = new List<Node<T>>();
		private Node<T> Root;

		public IImmutableList<Node<T>> AllNodes => Nodes.ToImmutableList();

		public void AddRootNode(T root)
		{
			Root = new Node<T>(root);
			Nodes.Add(Root);
		}

		public void AddEdge(T from, T to)
		{
			var fromNode = Nodes.SingleOrDefault(n => n.Value == from);
			if (fromNode == null)
			{
				throw new ArgumentException("From value must already be in graph.", nameof(from));
			}


			var toNode = Nodes.SingleOrDefault(n => n.Value == to);
			if (toNode == null)
			{
				toNode = new Node<T>(to);
				Nodes.Add(toNode);
			}

			fromNode.AddNeighbor(toNode);
		}
    }

	[Obsolete("Use ImmutableGraph")]
	// TODO - Make immutable or at least provide an immutable interface.
	internal class Node<T>
	{
		public IList<Node<T>> Neighbors { get; } = new List<Node<T>>();
		public T Value { get; }

		public Node(T value)
		{
			Value = value;
		}

		public void AddNeighbor(Node<T> to)
		{
			if (Neighbors.Contains(to))
			{
				throw new ArgumentException("Node is already neighbor.", nameof(to));
			}

			Neighbors.Add(to);
		}

		public override string ToString()
		{
			return $"N=>{Value.ToString()}";
		}
	}
}
