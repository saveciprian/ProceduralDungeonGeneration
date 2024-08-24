using System;
using System.Collections.Generic;
using UnityEngine;
using BlueRaja;

public class DungeonPathfinder2D {
    public class Node {
        public Vector2Int Position { get; private set; }
        public Node Previous { get; set; }
        public float Cost { get; set; }

        public Node(Vector2Int position) {
            Position = position;
        }
    }

    public struct PathCost {
        public bool traversable;
        public float cost;
    }

    //directions in which a node can move
    static readonly Vector2Int[] neighbors = {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1),
    };

    Grid2D<Node> grid;
    SimplePriorityQueue<Node, float> queue; // Represents a collection of items that have a value and a priority. On dequeue, the item with the lowest priority value is removed.
    HashSet<Node> closed;
    Stack<Vector2Int> stack;

    public DungeonPathfinder2D(Vector2Int size) {
        grid = new Grid2D<Node>(size, Vector2Int.zero);

        queue = new SimplePriorityQueue<Node, float>();
        closed = new HashSet<Node>();
        stack = new Stack<Vector2Int>();

        for (int x = 0; x < size.x; x++) {
            for (int y = 0; y < size.y; y++) {
                    grid[x, y] = new Node(new Vector2Int(x, y));
            }
        }
    }

    void ResetNodes() {
        var size = grid.Size;
        
        for (int x = 0; x < size.x; x++) {
            for (int y = 0; y < size.y; y++) {
                var node = grid[x, y];
                node.Previous = null;
                node.Cost = float.PositiveInfinity;
            }
        }
    }

    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, Func<Node, Node, PathCost> costFunction) {
        //clean previous operation data from nodes
        ResetNodes();
        queue.Clear(); //simple priority queue
        closed.Clear(); //hashset

        queue = new SimplePriorityQueue<Node, float>();
        closed = new HashSet<Node>();

        //set the initial starting location's cost to 0
        grid[start].Cost = 0;
        queue.Enqueue(grid[start], 0); //add the first node to the queue

        while (queue.Count > 0) { 
            //while there is still something in the queue keep running
            //first loop: dequeue the starting node
            Node node = queue.Dequeue();
            //add it to the closed hashset
                //what is this used for? it only appreas in this loop
            closed.Add(node);

            //reconstruct the path only if we reached the end (sorry for all the obvious comments)
            if (node.Position == end) {
                return ReconstructPath(node);
            }

            //check each offset position for a given node
            foreach (var offset in neighbors) {
                //if the position is outside the bounds, move on                
                if (!grid.InBounds(node.Position + offset)) continue;
                var neighbor = grid[node.Position + offset];
                //if we've already been at this position, move on
                if (closed.Contains(neighbor)) continue;

                //use the lambda function to get a cost for moving in this direction
                var pathCost = costFunction(node, neighbor);
                //if the path isn't traversable, move on
                if (!pathCost.traversable) continue;

                //calculate the total cost
                float newCost = node.Cost + pathCost.cost;

                //if the new cost is lower than the previous one, change the previous node to the current one (aka move here) and change the total cost to the current one
                if (newCost < neighbor.Cost) {
                    neighbor.Previous = node;
                    neighbor.Cost = newCost;

                    
                    if (queue.TryGetPriority(node, out float existingPriority)) {
                        queue.UpdatePriority(node, newCost);
                    } else {
                        //add the new position and cost to the queue
                        queue.Enqueue(neighbor, neighbor.Cost);
                    }
                }
            }
        }

        return null;
    }

    List<Vector2Int> ReconstructPath(Node node) {
        List<Vector2Int> result = new List<Vector2Int>();

        while (node != null) {
            stack.Push(node.Position); //push all nodes into a stack in order to reverse their indices
            node = node.Previous;
        }

        while (stack.Count > 0) {
            result.Add(stack.Pop()); //add them all back to a list, the direction is now startPos -> endPos
            //doesn't necessarily matter in our case, but it's good practice i think
        }

        return result;
    }
}
