using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace VCSCompiler
{
	// TODO - Make immutable or at least provide an immutable interface.
    internal class Graph<T> where T: class
    {
		private IList<Node<T>> Nodes = new List<Node<T>>();
		private Node<T> Root;

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
