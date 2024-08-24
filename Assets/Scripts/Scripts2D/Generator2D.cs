using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;
using Graphs;

public class Generator2D : MonoBehaviour {
    enum CellType {
        None,
        Room,
        Hallway
    }

    class Room {
        public RectInt bounds;

        //class constructor for Room; takes a location and a size and adds it to a variable called bounds
        public Room(Vector2Int location, Vector2Int size) {
            bounds = new RectInt(location, size);
        }

        //Check if the rooms are intersecting (essentially for each room check if it's on the right (for x) /bottom (for y) of the other one)
        //why not use !a.bounds.Overlaps(b.bounds) ?
        //I couldn't get it to work either fml
        //In the version of unity this project was made in there seems to be no method with that name so maybe that's why
        public static bool Intersect(Room a, Room b) {
            return !((a.bounds.position.x >= (b.bounds.position.x + b.bounds.size.x)) || ((a.bounds.position.x + a.bounds.size.x) <= b.bounds.position.x)
                || (a.bounds.position.y >= (b.bounds.position.y + b.bounds.size.y)) || ((a.bounds.position.y + a.bounds.size.y) <= b.bounds.position.y));
            // return !a.bounds.Overlaps(b.bounds);
        }
    }

    [SerializeField] Vector2Int size;
    [SerializeField] int roomCount;
    [SerializeField] Vector2Int roomMaxSize;
    [SerializeField] GameObject cubePrefab;
    [SerializeField] Material redMaterial;
    [SerializeField] Material blueMaterial;

    Random random;
    Grid2D<CellType> grid; //Grid2D is a class he constructed himself 
    List<Room> rooms;
    Delaunay2D delaunay;
    HashSet<Prim.Edge> selectedEdges; //ummm... haa?
    //so a hashset is more like a dictionary, doesn't allow duplicate values, is unordered but really fast to use
    //curious why he used it in favour of a list, but will have to wait...
    // i think prim was the algorithm for MST, right?
        //yeh https://www.geeksforgeeks.org/prims-minimum-spanning-tree-mst-greedy-algo-5/

    void Start() {
        Generate();
    }

    void Generate() {
        random = new Random(0);
        grid = new Grid2D<CellType>(size, Vector2Int.zero);
        rooms = new List<Room>();

        //calculates random positions/sizes for rooms
        //adds room positions in the grid and tags them as CellType.Room
        //places a cube at the bounds position and then changes size and color
            //I don't think this is correct; the cube should be placed at (position.x + size.x/2),(position.y + size.y/2)
            //this is because the default cube from unity has the origin in the center
            //he's essentially placing the cube based on top-left corner and then scaling from center
        PlaceRooms();

        //go through the delaunay algorithm
        //we're getting a delaunay object populated with an instance that has relevant data stored in three lists: Vertices, Edges and Triangles
        //I'm assuming the Edges one is the most important since it contains unique values for all the edges that connect the centers of the triangles (kind of; it seems more like it's the approximate centers based on the bounding box surrounding the circle — however it doesn't need to be accurate in this case)
        Triangulate();

        //creates a minimum spanning tree that chooses the path based on the shortest lengths between the rooms, starting at a pseudo-random point (first vertex in the delauney edge list)
        //randomly reintroduces some edges that were not chosen by the mst algorithm
        //all of this is stored in a list of type <Prim.Edge>, which also inherits from Graphs.Edge
        //all edges have two vertices: U, V a GetHashCode() method;
        //Prim.Edge also has a public distance attribute that returns the length of the edge
        CreateHallways();

        
        PathfindHallways();
    }

    void PlaceRooms() {
        for (int i = 0, loops = 0; i < roomCount && loops < 50000; loops++) {
            Vector2Int location = new Vector2Int(
                random.Next(0, size.x - roomMaxSize.x),
                random.Next(0, size.y - roomMaxSize.y)
            ); 
            //generate random position for next room
            //could use (size.x - roomMaxSize.x) to keep inside the alotted space 

            Vector2Int roomSize = new Vector2Int(
                random.Next(1, roomMaxSize.x + 1), //have to increase the second number as the limit doesn't include it
                random.Next(1, roomMaxSize.y + 1)
            );

            //generate random size for room

            bool add = true;
            Room newRoom = new Room(location, roomSize); //create new room here

            //add a 1x1 border around the room so that the rooms aren't touching
            //this border is used only when figuring out where to place the room
            Room buffer = new Room(location + new Vector2Int(-1, -1), roomSize + new Vector2Int(2, 2)); 

            foreach (var room in rooms) {
                //check for each room that has already been placed
                if (Room.Intersect(room, buffer)) {
                    //if the already placed room intersects with the new one (+ buffer) then trigger bool to discard it
                    add = false;
                    break;
                }
            }

            //what is the point of this? 
            //when would the room size be less than 0 on either axis?
            //when generating could tweak the random parameters to keep it inside bounds
            //anyway set trigger to discard it if it's outside bounds
            if (newRoom.bounds.xMin < 0 || newRoom.bounds.xMax >= size.x
                || newRoom.bounds.yMin < 0 || newRoom.bounds.yMax >= size.y) {
                add = false;
            }

            //I think he is discarding rooms but not accounting for it
            //it would be better if the increment of the loop variable would be here
            //if the rooms get discarded you just get left with less rooms instead of having to go through more loops
            //it may also be the case that you just can't place the rooms inside the alotted space so could have a variable to break in case of an infinite loop
            /*
                for (int i = 0, int loops = 0; i < roomCount || loops < 50000; loops++)
                    [...]
                    if (add) {
                        i++;
                }

                OR 

                while (rooms.Count < roomCount || loops < 50000) {
                    loops++;
                }
            */

            if (add)
            {
                i++;
                rooms.Add(newRoom); //add new room to the list of rooms
                PlaceRoom(newRoom.bounds.position, newRoom.bounds.size); //take care of the visual representation of room

                foreach (var pos in newRoom.bounds.allPositionsWithin) {                    
                    //go through all spaces inside a room and tag them accordingly
                        //I'm guessing this is for setting the material (and model) when placing stuff in
                    grid[pos] = CellType.Room; 

                    //I understand what is happening but I don't understand how it works
                    //He's making an instance of Grid2D which is of type <CellType>
                    //when initializing the class, he's passing in the size of the grid, as well as an offset for it
                    //in the class constructor, the "data" variable, which is implemented as an array of wildcard variables
                    //gets initialized as an array of a size based on the map dimensions size.x * size.y
                    /*
                            public T this[int x, int y] {
                                get {
                                    return this[new Vector2Int(x, y)];
                                }
                                set {
                                    this[new Vector2Int(x, y)] = value;
                                }
                            }

                        So I guess the array has enough slots to store all spaces in the grid, but then how does the computer know at what indices to store the variables? 

                        Nevermind, I was looking at the wrong getter/setter; I've added the comments in Grid2D
                    */
                }
            }
        }
    }

    void Triangulate() {
        //create a list of vertices
        //Vertex seems to be a class he created himself (as well as Edge) that is inside the Graphs library
        List<Vertex> vertices = new List<Vertex>();


        foreach (var room in rooms) {
            //for each room, create a vertex object that contains the center of the room as well as a reference to the room
            vertices.Add(new Vertex<Room>((Vector2)room.bounds.position + ((Vector2)room.bounds.size) / 2, room));
        }


        delaunay = Delaunay2D.Triangulate(vertices);
        //this is so damn confusing
        //inside the delaunay variable, store a delaunay type object that is generated when running the static Delaunay Triangulate function
            //this function in turn is calling a triangulate function that doesn't return anything, so I assume that the triangulation is changing a list by reference 
        //can this be called like
            //delaunay = delaunay.Triangulate(vertices);
            //well no, the Triangulate function is a static one so can't call it from a non-static context
    }

    void CreateHallways() {
        List<Prim.Edge> edges = new List<Prim.Edge>(); //create a list that will be containing edges, but in the structure of the prim object

        foreach (var edge in delaunay.Edges) {
            edges.Add(new Prim.Edge(edge.U, edge.V));
            //transfer the edges from delaunay to Prim
            //i'm guessing this is to have multiple structures for how edges are defined/ operated on
            //in the Delaunay object there's a function to check if edges are aproximately similar
            //in the Prim one it seems he's added overloads for == and != as well as GetHashCode()
                //not sure if the GetHashCode function gets used though
        }

        //create a minimum spanning tree here
        List<Prim.Edge> mst = Prim.MinimumSpanningTree(edges, edges[0].U);

        //popuplate the selectedEdges variable with the mst
        selectedEdges = new HashSet<Prim.Edge>(mst);
        
        //create a HashSet of type Prim.Edge that holds all possible edges and then remove the ones that are present in the mst
        var remainingEdges = new HashSet<Prim.Edge>(edges);
        remainingEdges.ExceptWith(selectedEdges);

        //for each discarded edge, set a 12.5 chance to add it back into the selection
        foreach (var edge in remainingEdges) {
            if (random.NextDouble() < 0.125) {
                selectedEdges.Add(edge);
            }
        }
    }

    void PathfindHallways() {
        
        DungeonPathfinder2D aStar = new DungeonPathfinder2D(size);

        //we're running the pathfinding algorithm for each of the selected edges in our graph
        foreach (var edge in selectedEdges) {
            //set the starting and ending rooms (each edge has two vertices, each vertex stores a reference to the room object — this was set in the Triangulate() function above)
            var startRoom = (edge.U as Vertex<Room>).Item;
            var endRoom = (edge.V as Vertex<Room>).Item;

            //get the start and end positions based on the rooms Rect object attributes
            var startPosf = startRoom.bounds.center;
            var endPosf = endRoom.bounds.center;
            
            //truncate those values to integers to fit within the grid
            var startPos = new Vector2Int((int)startPosf.x, (int)startPosf.y);
            var endPos = new Vector2Int((int)endPosf.x, (int)endPosf.y);

            //where did DungeonPathfinder2D.Node a and node b get created??
            //NEVERMIND
                //we're only defining the lambda cost function here, and it gets called inside the FindPath function multiple times 
            
            //path is a list of positions, we'll use these to instantiate the objects
            var path = aStar.FindPath(startPos, endPos, (DungeonPathfinder2D.Node a, DungeonPathfinder2D.Node b) => { 
                //create a new PathCost struct
                    //stores a bool called traversable and a float for cost
                    var pathCost = new DungeonPathfinder2D.PathCost();
                
                //get the distance between various node positions to the end position we're trying to get to
                pathCost.cost = Vector2Int.Distance(b.Position, endPos);    //heuristic

                //change the cost based on what cell type the node is on top of
                if (grid[b.Position] == CellType.Room) {
                    pathCost.cost += 10;
                } else if (grid[b.Position] == CellType.None) {
                    pathCost.cost += 5;
                } else if (grid[b.Position] == CellType.Hallway) {
                    pathCost.cost += 1;
                }
                
                //i guess we're not using the traversable modifier since it applies to all nodes;
                //could add it to one of the checks above if we don't want it to go through rooms at all for example
                pathCost.traversable = true;

                return pathCost;
            });

            //if the path got populated, check each position and assign the Hallway cell type if None
            //upon each step, set the previous position as well as the difference to the current one
                //what the heck is this for? maybe for choosing what type of prefab to instantiate?
            if (path != null) {
                for (int i = 0; i < path.Count; i++) {
                    var current = path[i];

                    if (grid[current] == CellType.None) {
                        grid[current] = CellType.Hallway;
                    }

                    if (i > 0) {
                        var prev = path[i - 1];

                        var delta = current - prev;
                    }
                }

                foreach (var pos in path) {
                    if (grid[pos] == CellType.Hallway) {
                        PlaceHallway(pos);
                    }
                }
            }
        }
    }

    void PlaceCube(Vector2Int location, Vector2Int size, Material material) {
        GameObject go = Instantiate(cubePrefab, new Vector3(location.x, 0, location.y), Quaternion.identity);
        go.GetComponent<Transform>().localScale = new Vector3(size.x, 1, size.y);
        go.GetComponent<MeshRenderer>().material = material;
    }

    void PlaceRoom(Vector2Int location, Vector2Int size) {
        PlaceCube(location, size, redMaterial);
    }

    void PlaceHallway(Vector2Int location) {
        PlaceCube(location, new Vector2Int(1, 1), blueMaterial);
    }
}
