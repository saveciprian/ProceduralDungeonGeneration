using System;
using System.Collections.Generic;
using UnityEngine;
using Graphs;

public static class Prim {
    public class Edge : Graphs.Edge {
        public float Distance { get; private set; }

        public Edge(Vertex u, Vertex v) : base(u, v) {
            Distance = Vector3.Distance(u.Position, v.Position);
        }

        public static bool operator ==(Edge left, Edge right) {
            return (left.U == right.U && left.V == right.V)
                || (left.U == right.V && left.V == right.U);
        }

        public static bool operator !=(Edge left, Edge right) {
            return !(left == right);
        }

        public override bool Equals(object obj) {
            if (obj is Edge e) {
                return this == e;
            }

            return false;
        }

        public bool Equals(Edge e) {
            return this == e;
        }

        public override int GetHashCode() {
            return U.GetHashCode() ^ V.GetHashCode();
        }
    }

    public static List<Edge> MinimumSpanningTree(List<Edge> edges, Vertex start) {
        /*
            These are the steps for Prim's MST algorithm:  
                Step 1: Determine an arbitrary vertex as the starting vertex of the MST.
                Step 2: Follow steps 3 to 5 till there are vertices that are not included in the MST (known as fringe vertex).
                Step 3: Find edges connecting any tree vertex with the fringe vertices.
                Step 4: Find the minimum among these edges.
                Step 5: Add the chosen edge to the MST if it does not form any cycle.
                Step 6: Return the MST and exit 
         */
        
        //so the open set is the set of vertices that hasn't been processed yet
        //the closed set is the set containing the vertices for the MST
        //the edge weight is its length in this case
        HashSet<Vertex> openSet = new HashSet<Vertex>();
        HashSet<Vertex> closedSet = new HashSet<Vertex>();

        foreach (var edge in edges) {
            //populate the openSet with all the vertices present in the graph
            //these are hashsets so the values are unique
            openSet.Add(edge.U);
            openSet.Add(edge.V);
        }

        //shouldn't we also remove the starting vertex from the openSet then?
        closedSet.Add(start);

        //have to create a list because we need an ordered structure to store the edges in, right?
        List<Edge> results = new List<Edge>();

#region POPULATE MST
        while (openSet.Count > 0) {
            bool chosen = false;
            Edge chosenEdge = null;
            float minWeight = float.PositiveInfinity; //equate the length of the edge with positive infinity in order for any value to be lower

            //check each edge from Delauney
            foreach (var edge in edges) {
                int closedVertices = 0;
                //if the mst hashset contains the vertex, don't increment closedVertices
                //following the naming conventions used prior, wouldn't it be more logical to call it openVertices?
                if (!closedSet.Contains(edge.U)) closedVertices++;
                if (!closedSet.Contains(edge.V)) closedVertices++;
                
                //only get past this if a single vertex is present in the MST set
                if (closedVertices != 1) continue;

                //if the length of the edge is smaller than what we have stored already, then set this length as the smallest, mark the edge ast the chosen edge and set chosen to true
                if (edge.Distance < minWeight) {
                    chosenEdge = edge;
                    chosen = true;
                    minWeight = edge.Distance;
                }
            }

            //if there are no more chosen edges, then break out of the while loop
            //i guess this is a safeguard in case openSet doesn't completely empty out
            if (!chosen) break;
            results.Add(chosenEdge); //add the chosen edge into the list with results
            openSet.Remove(chosenEdge.U); //remove the vertices from the available pool
            openSet.Remove(chosenEdge.V);
            closedSet.Add(chosenEdge.U); //add the vertices to the mst hashset
            closedSet.Add(chosenEdge.V);
        }
#endregion

        return results;
    }
}
