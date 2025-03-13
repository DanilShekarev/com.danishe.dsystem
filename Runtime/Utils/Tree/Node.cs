using System.Collections.Generic;

namespace DSystem.Utils.Tree
{
    public class Tree<T>
    {
        public readonly Node<T> RootNode = new (null, default);
        
        private Node<T> _currentNode;

        public Tree()
        {
            _currentNode = RootNode;
        }

        public void Push(T data)
        {
            var node = new Node<T>(_currentNode, data);
            _currentNode.Push(node);
            _currentNode = node;
        }

        public void Back()
        {
            _currentNode = _currentNode.Parent;
        }
    }
    
    public class Node<T>
    {
        public readonly T Data;
        public readonly Node<T> Parent;
        public IReadOnlyCollection<Node<T>> Children => _children;
        
        private readonly List<Node<T>> _children = new();
        
        public Node(Node<T> parent, T data)
        {
            Parent = parent;
            Data = data;
        }
        
        public void Push(Node<T> node)
        {
            _children.Add(node);
        }
    }
}