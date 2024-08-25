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
        
        public Room(Vector2Int location, Vector2Int size) {
            bounds = new RectInt(location, size);
        }
        
        public static bool Intersect(Room a, Room b) {
            return !((a.bounds.position.x >= (b.bounds.position.x + b.bounds.size.x)) || ((a.bounds.position.x + a.bounds.size.x) <= b.bounds.position.x)
                || (a.bounds.position.y >= (b.bounds.position.y + b.bounds.size.y)) || ((a.bounds.position.y + a.bounds.size.y) <= b.bounds.position.y));
        }
    }

    [SerializeField] Vector2Int size;
    [SerializeField] int roomCount;
    [SerializeField] Vector2Int roomMaxSize;
    [SerializeField] GameObject cubePrefab;
    [SerializeField] Material redMaterial;
    [SerializeField] Material blueMaterial;

    Random random;
    Grid2D<CellType> grid; 
    List<Room> rooms;
    Delaunay2D delaunay;
    HashSet<Prim.Edge> selectedEdges; 

    //variables for room placement
    public int scale = 6;

    public GameObject straight;
    public GameObject corner;
    public GameObject deadEnd;
    public GameObject crossRoads;
    public GameObject tJunction;

    //Prefabs for room generation
    public GameObject wallPiece;
    public GameObject floorPiece;
    public GameObject ceilingPiece;
    public GameObject doorPiece;

    public GameObject pillar;
    public GameObject door;
    public List<Vector2Int> pillarLocations = new List<Vector2Int>();
    
        
    void Start() {
        Generate();
    }

    void Generate() {
        random = new Random(0);
        grid = new Grid2D<CellType>(size, Vector2Int.zero);
        rooms = new List<Room>();

        PlaceRooms();

        Triangulate();
        
        CreateHallways();
        
        PathfindHallways();

        DrawMap();
    }

    void PlaceRooms() {
        for (int i = 0, loops = 0; i < roomCount && loops < 50000; loops++) {
            Vector2Int location = new Vector2Int(
                random.Next(0, size.x - roomMaxSize.x),
                random.Next(0, size.y - roomMaxSize.y)
            );

            Vector2Int roomSize = new Vector2Int(
                random.Next(1, roomMaxSize.x + 1),
                random.Next(1, roomMaxSize.y + 1)
            );


            bool add = true;
            Room newRoom = new Room(location, roomSize);
            
            Room buffer = new Room(location + new Vector2Int(-1, -1), roomSize + new Vector2Int(2, 2));
            
            foreach (var room in rooms) {
                if (Room.Intersect(room, buffer)) {
                    add = false;
                    break;
                }
            }
            
            if (newRoom.bounds.xMin < 0 || newRoom.bounds.xMax >= size.x
                || newRoom.bounds.yMin < 0 || newRoom.bounds.yMax >= size.y) {
                add = false;
            }

            if (add)
            {
                i++;
                rooms.Add(newRoom);

                foreach (var pos in newRoom.bounds.allPositionsWithin) {
                    grid[pos] = CellType.Room;
                }
            }
        }
    }

    void Triangulate() {
        List<Vertex> vertices = new List<Vertex>();


        foreach (var room in rooms) {
            vertices.Add(new Vertex<Room>((Vector2)room.bounds.position + ((Vector2)room.bounds.size) / 2, room));
        }

        delaunay = Delaunay2D.Triangulate(vertices);
    }

    void CreateHallways() {
        List<Prim.Edge> edges = new List<Prim.Edge>(); 

        foreach (var edge in delaunay.Edges) {
            edges.Add(new Prim.Edge(edge.U, edge.V));
        }
        
        List<Prim.Edge> mst = Prim.MinimumSpanningTree(edges, edges[0].U);
        
        selectedEdges = new HashSet<Prim.Edge>(mst);
        
        var remainingEdges = new HashSet<Prim.Edge>(edges);
        remainingEdges.ExceptWith(selectedEdges);

        foreach (var edge in remainingEdges) {
            if (random.NextDouble() < 0.125) {
                selectedEdges.Add(edge);
            }
        }
    }

    void PathfindHallways() {
        
        DungeonPathfinder2D aStar = new DungeonPathfinder2D(size);

        foreach (var edge in selectedEdges) {
            var startRoom = (edge.U as Vertex<Room>).Item;
            var endRoom = (edge.V as Vertex<Room>).Item;
            
            var startPosf = startRoom.bounds.center;
            var endPosf = endRoom.bounds.center;

            var startPos = new Vector2Int((int)startPosf.x, (int)startPosf.y);
            var endPos = new Vector2Int((int)endPosf.x, (int)endPosf.y);
            
            var path = aStar.FindPath(startPos, endPos, (DungeonPathfinder2D.Node a, DungeonPathfinder2D.Node b) => {
                var pathCost = new DungeonPathfinder2D.PathCost();
                    
                pathCost.cost = Vector2Int.Distance(b.Position, endPos);

                if (grid[b.Position] == CellType.Room) {
                    pathCost.cost += 10;
                } else if (grid[b.Position] == CellType.None) {
                    pathCost.cost += 5;
                } else if (grid[b.Position] == CellType.Hallway) {
                    pathCost.cost += 1;
                }

                pathCost.traversable = true;

                return pathCost;
            });
            
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
                        // PlaceHallway(pos);
                    }
                }
            }
        }
    }

    void PlaceCube(Vector2Int location, Vector2Int size, Material material) {
        GameObject go = Instantiate(cubePrefab, new Vector3(location.x + size.x / 2.0f, 0, location.y + size.y / 2.0f), Quaternion.identity);
        go.GetComponent<Transform>().localScale = new Vector3(size.x, 1, size.y);
        go.GetComponent<MeshRenderer>().material = material;
    }

    void PlaceRoom(Vector2Int location, Vector2Int size) {
        PlaceCube(location, size, redMaterial);
    }

    void PlaceHallway(Vector2Int location) {
        PlaceCube(location, new Vector2Int(1, 1), blueMaterial);
    }

    public struct BuildingBlock
    {
        public int[] Pattern;
        public GameObject Prefab;
        public Vector3 Orientation;

        public BuildingBlock(int[] _pattern, GameObject _prefab, Vector3 _orientation)
        {
            Pattern = _pattern;
            Prefab = _prefab;
            Orientation = _orientation;
        }
    }
    
    void DrawMap()
    {
        BuildingBlock straightVertical =
            new BuildingBlock(new int[] { 5, 2, 5, 
                                                 5, 2, 5, 
                                                 5, 2, 5 }, straight, new Vector3(0, 90, 0));
        BuildingBlock straightHorizontal =
            new BuildingBlock(new int[] { 5, 5, 5, 
                                                 2, 2, 2, 
                                                 5, 5, 5 }, straight, new Vector3(0, 0, 0)); //default
        BuildingBlock cornerTopLeft =
            new BuildingBlock(new int[] { 5, 2, 5, 
                                                 2, 2, 5, 
                                                 5, 5, 5 }, corner, new Vector3(0, 270, 0));
        BuildingBlock cornerBottomLeft =
            new BuildingBlock(new int[] { 5, 5, 5, 
                                                 2, 2, 5, 
                                                 5, 2, 5 }, corner, new Vector3(0, 180, 0));
        BuildingBlock cornerTopRight =
            new BuildingBlock(new int[] { 5, 2, 5, 
                                                 5, 2, 2, 
                                                 5, 5, 5 }, corner, new Vector3(0, 0, 0)); //default
        BuildingBlock cornerBottomRight =
            new BuildingBlock(new int[] { 5, 5, 5, 
                                                 5, 2, 2, 
                                                 5, 2, 5 }, corner, new Vector3(0, 90, 0));
        BuildingBlock junctionTop =
            new BuildingBlock(new int[] { 5, 2, 5, 
                                                 2, 2, 2, 
                                                 5, 5, 5 }, tJunction, new Vector3(0, 270, 0));
        BuildingBlock junctionBottom =
            new BuildingBlock(new int[] { 5, 5, 5, 
                                                 2, 2, 2, 
                                                 5, 2, 5 }, tJunction, new Vector3(0, 90, 0));
        BuildingBlock junctionLeft =
            new BuildingBlock(new int[] { 5, 2, 5, 
                                                 2, 2, 5, 
                                                 5, 2, 5 }, tJunction, new Vector3(0, 180, 0));
        BuildingBlock junctionRight =
            new BuildingBlock(new int[] { 5, 2, 5, 
                                                 5, 2, 2, 
                                                 5, 2, 5 }, tJunction, new Vector3(0, 0, 0)); //default
        BuildingBlock crossroads =
            new BuildingBlock(new int[] { 5, 2, 5, 
                                                 2, 2, 2, 
                                                 5, 2, 5 }, crossRoads, new Vector3(0, 90, 0));

        BuildingBlock[] buildingBlocks = new BuildingBlock[]
        {
            crossroads,
            junctionTop,
            junctionBottom,
            junctionLeft,
            junctionRight,
            cornerTopLeft, 
            cornerBottomLeft, 
            cornerTopRight, 
            cornerBottomRight,
            straightVertical, 
            straightHorizontal, 
        };
        
        for(int z = 0; z < size.y; z++)
        {
            for (int x = 0; x < size.x; x++)
            {
                bool placedBlock = false;
                foreach (var block in buildingBlocks)
                {
                    //three nested for loops... gotta love that time complexity
                    if (Search2D(x, z, block.Pattern) && !placedBlock && grid[new Vector2Int(x, z)] != CellType.Room)
                    {
                       Instantiate(block.Prefab, new Vector3(x, 0, z), Quaternion.Euler(block.Orientation));
                       placedBlock = true;
                    }
                }

                #region DeleteThisShit
                    //// STRAIGHT CORRIDORS ////
                    // if (Search2D(x, z, straightVertical.Pattern))
                    //     Instantiate(straightVertical.Prefab, new Vector3(x, 0, z), Quaternion.Euler(straightVertical.Orientation));
                    // else if (Search2D(x, z, straightHorizontal.Pattern))
                    //     Instantiate(straightHorizontal.Prefab, new Vector3(x, 0, z), Quaternion.Euler(straightHorizontal.Orientation));
                    // //// CORNERS ////
                    // else if (Search2D(x, z, cornerTopLeft.Pattern))
                    //     Instantiate(cornerTopLeft.Prefab, new Vector3(x, 0, z), Quaternion.Euler(cornerTopLeft.Orientation));
                    // else if (Search2D(x, z, cornerBottomLeft.Pattern))
                    //     Instantiate(cornerBottomLeft.Prefab, new Vector3(x, 0, z), Quaternion.Euler(cornerBottomLeft.Orientation));
                    // else if (Search2D(x, z, cornerTopRight.Pattern))
                    //     Instantiate(cornerTopRight.Prefab, new Vector3(x, 0, z), Quaternion.Euler(cornerTopRight.Orientation));
                    // else if (Search2D(x, z, cornerBottomRight.Pattern))
                    //     Instantiate(cornerBottomRight.Prefab, new Vector3(x, 0, z), Quaternion.Euler(cornerBottomRight.Orientation));
                    
                    //// POPULATE ALL MAP AREAS ////
                    // else
                #endregion

                if (grid[new Vector2Int(x, z)] == CellType.Room)
                {
                    GameObject roomInst = Instantiate(GameObject.CreatePrimitive(PrimitiveType.Sphere), new Vector3(x, 0, z), Quaternion.identity);
                    roomInst.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
                }

                // if (grid[new Vector2Int(x, z)] == CellType.Room &&
                //     (CountSquareNeighbours(x, z) > 1 && CountDiagonalNeighbours(x, z) >= 1 ||
                //      CountSquareNeighbours(x, z) >= 1 && CountDiagonalNeighbours(x, z) > 1))
                // {
                //     Instantiate(floorPiece, new Vector3(x, 0, z), Quaternion.identity);
                // }
                
                if (grid[new Vector2Int(x, z)] == CellType.Room)
                {
                    Instantiate(floorPiece, new Vector3(x, 0, z), Quaternion.identity);
                    Instantiate(ceilingPiece, new Vector3(x, 0, z), Quaternion.identity);
                    
                    LocateWalls(x, z);
                    if(top)
                    {
                        GameObject wall = Instantiate(wallPiece);
                        wall.transform.position = new Vector3(x, 0, z);
                        wall.transform.Rotate(0, 0, 0);
                        wall.name = "Top Wall";

                        if(grid[new Vector2Int(x + 1, z)] == CellType.Room && grid[ new Vector2Int(x + 1, z + 1)] == CellType.Hallway && !pillarLocations.Contains(new Vector2Int(x, z)))
                        {
                            GameObject pillarCorner = Instantiate(pillar);
                            pillarCorner.transform.position = new Vector3(x, 0, z);
                            pillarCorner.transform.Translate(0,0,-0.1f);
                            pillarCorner.name = "Top Right Pillar";
                            pillarLocations.Add(new Vector2Int(x, z));
                        }
                        
                        if(grid[new Vector2Int(x - 1, z)] == CellType.Room && grid[new Vector2Int(x - 1, z + 1)] == CellType.Hallway && !pillarLocations.Contains(new Vector2Int(x - 1, z)))
                        {
                            GameObject pillarCorner = Instantiate(pillar);
                            pillarCorner.transform.position = new Vector3((x - 1), 0, z);
                            pillarCorner.transform.Translate(0,0,-0.1f);
                            pillarCorner.name = "Top Left Pillar";
                            pillarLocations.Add(new Vector2Int(x - 1, z));
                        }
                    }
                    if (bottom)
                    {
                        GameObject wall = Instantiate(wallPiece);
                        wall.transform.position = new Vector3(x, 0, z);
                        wall.transform.Rotate(0, 180, 0);
                        wall.name = "Bottom Wall";
                        
                        if(grid[ new Vector2Int(x + 1, z)] == CellType.Room && grid[ new Vector2Int(x + 1, z - 1)] == CellType.Hallway && !pillarLocations.Contains(new Vector2Int(x, z - 1)))
                        {
                            GameObject pillarCorner = Instantiate(pillar);
                            pillarCorner.transform.position = new Vector3(x, 0, (z - 1));
                            pillarCorner.transform.Translate(0,0,0.1f);
                            pillarCorner.name = "Bottom Right Pillar";
                            pillarLocations.Add(new Vector2Int(x, z - 1));
                        }
                        
                        if(grid[ new Vector2Int(x - 1, z)] == CellType.Room && grid[ new Vector2Int(x - 1, z - 1)] == CellType.Hallway && !pillarLocations.Contains(new Vector2Int(x - 1, z - 1)))
                        {
                            GameObject pillarCorner = Instantiate(pillar);
                            pillarCorner.transform.position = new Vector3((x - 1), 0, (z - 1));
                            pillarCorner.transform.Translate(0,0,0.1f);
                            pillarCorner.name = "Bottom Left Pillar";
                            pillarLocations.Add(new Vector2Int(x - 1, z - 1));
                        }
                    }
                    if (left)
                    {
                        GameObject wall = Instantiate(wallPiece);
                        wall.transform.position = new Vector3(x, 0, z);
                        wall.transform.Rotate(0, 270, 0);
                        wall.name = "Left Wall";
                        
                        if(grid[ new Vector2Int(x - 1, z + 1)] == CellType.Hallway && grid[ new Vector2Int(x, z + 1)] == CellType.Room && !pillarLocations.Contains(new Vector2Int(x - 1, z)))
                        {
                            GameObject pillarCorner = Instantiate(pillar);
                            pillarCorner.transform.position = new Vector3((x - 1), 0, z);
                            pillarCorner.transform.Translate(0.1f,0,0);
                            pillarCorner.name = "Left Top Pillar";
                            pillarLocations.Add(new Vector2Int(x - 1, z));
                        }
                        
                        if(grid[ new Vector2Int(x - 1, z - 1)] == CellType.Hallway && grid[ new Vector2Int(x, z - 1)] == CellType.Room && !pillarLocations.Contains(new Vector2Int(x - 1, z - 1)))
                        {
                            GameObject pillarCorner = Instantiate(pillar);
                            pillarCorner.transform.position = new Vector3((x - 1), 0, (z - 1));
                            pillarCorner.transform.Translate(0.1f,0,0);
                            pillarCorner.name = "Left Bottom Pillar";
                            pillarLocations.Add(new Vector2Int(x - 1, z - 1));
                        }
                    }
                    if (right)
                    {
                        GameObject wall = Instantiate(wallPiece);
                        wall.transform.position = new Vector3(x, 0, z);
                        wall.transform.Rotate(0, 90, 0);
                        wall.name = "Right Wall";
                        
                        if(grid[new Vector2Int(x + 1, z + 1)] == CellType.Hallway && grid[new Vector2Int(x, z + 1)] == CellType.Room && !pillarLocations.Contains(new Vector2Int(x, z - 1)))
                        {
                            GameObject pillarCorner = Instantiate(pillar);
                            pillarCorner.transform.position = new Vector3(x, 0, z);
                            pillarCorner.transform.Translate(-0.1f,0,0);
                            pillarCorner.name = "Right Top Pillar";
                            pillarLocations.Add(new Vector2Int(x, z));
                        }
                        
                        if(grid[new Vector2Int(x, z - 1)] == CellType.Room && grid[new Vector2Int(x + 1, z - 1)] == CellType.Hallway && !pillarLocations.Contains(new Vector2Int(x + 1, z - 1)))
                        {
                            GameObject pillarCorner = Instantiate(pillar);
                            pillarCorner.transform.position = new Vector3(x, 0, (z - 1));
                            pillarCorner.transform.Translate(-0.1f,0,0);
                            pillarCorner.name = "Right Bottom Pillar";
                            pillarLocations.Add(new Vector2Int(x, z - 1));
                        }
                    }
                }
            }
        }

        for (int z = 0; z < size.y; z++)
        {
            for (int x = 0; x < size.x; x++)
            {
                if(grid[new Vector2Int(x,z)] != CellType.Room) continue;
                GameObject doorway;
                LocateDoors(x,z);
                if(top)
                {
                    doorway = Instantiate(door);
                    doorway.transform.position = new Vector3(x, 0, z);
                    doorway.transform.Rotate(0, 180, 0);
                    doorway.transform.Translate(0, 0, 0.1f);
                }
                if(bottom)
                {
                    doorway = Instantiate(door);
                    doorway.transform.position = new Vector3(x, 0, z);
                    doorway.transform.Rotate(0, 0, 0);
                    doorway.transform.Translate(0, 0, 0.1f);
                }
                if(left)
                {
                    doorway = Instantiate(door);
                    doorway.transform.position = new Vector3(x, 0, z);
                    doorway.transform.Rotate(0, 90, 0);
                    doorway.transform.Translate(0, 0, 0.1f);
                }
                if(right)
                {
                    doorway = Instantiate(door);
                    doorway.transform.position = new Vector3(x, 0, z);
                    doorway.transform.Rotate(0, -90, 0);
                    doorway.transform.Translate(0, 0, 0.1f);
                }
            }
        }
    }
    
    bool top;
    bool bottom;
    bool right;
    bool left;
    public void LocateWalls(int x, int z)
    {
        top = false;
        bottom = false;
        right = false;
        left = false;

        if (x <= 0 || x >= size.x - 1 || z <= 0 || z >= size.y - 1) return;
        if(grid[new Vector2Int(x, z + 1)] == CellType.None) top = true;
        if(grid[new Vector2Int(x, z - 1)] == CellType.None) bottom = true;
        if(grid[new Vector2Int(x + 1, z)] == CellType.None) right = true;
        if(grid[new Vector2Int(x - 1, z)] == CellType.None) left = true;
    }
    
    public void LocateDoors(int x, int z)
    {
        top = false;
        bottom = false;
        right = false;
        left = false;

        if (x <= 0 || x >= size.x - 1 || z <= 0 || z >= size.y - 1) return;
        // if(map[x, z + 1] == 0 && map[x-1, z+1] == 1 && map[x+1, z+1] == 1) top = true;
        // if(map[x, z - 1] == 0 && map[x-1, z-1] == 1 && map[x+1, z-1] == 1) bottom = true;
        // if(map[x + 1, z] == 0 && map[x+1, z+1] == 1 && map[x+1, z-1] == 1) right = true;
        // if(map[x - 1, z] == 0 && map[x-1, z+1] == 1 && map[x-1, z-1] == 1) left = true;
        
        if(grid[ new Vector2Int(x, z + 1)] != CellType.Room && grid[ new Vector2Int(x, z + 1) ] == CellType.Hallway) top = true;
        if(grid[ new Vector2Int(x, z - 1)] != CellType.Room && grid[ new Vector2Int(x, z - 1) ] == CellType.Hallway) bottom = true;
        if(grid[ new Vector2Int(x - 1, z)] != CellType.Room && grid[ new Vector2Int(x - 1, z) ] == CellType.Hallway) left = true;
        if(grid[ new Vector2Int(x + 1, z)] != CellType.Room && grid[ new Vector2Int(x + 1, z) ] == CellType.Hallway) right = true;

    }
    
    public int CountSquareNeighbours(int x, int z)
    {
        int count = 0;

        if( x <= 0 || x >= size.x - 1 || z <= 0 || z >= size.y - 1) return 5;
        if(grid[ new Vector2Int(x - 1, z)] == CellType.Room) count++;
        if(grid[ new Vector2Int(x, z + 1)] == CellType.Room) count++;
        if(grid[ new Vector2Int(x + 1, z)] == CellType.Room) count++;
        if(grid[ new Vector2Int(x, z - 1)] == CellType.Room) count++;
        
        return count;
    }
    
    public int CountDiagonalNeighbours(int x, int z)
    {
        int count = 0;
        if( x <= 0 || x >= size.x - 1 || z <= 0 || z >= size.y - 1) return 5;
        if(grid[ new Vector2Int(x + 1, z + 1)] == CellType.Room) count++;
        if(grid[ new Vector2Int(x - 1, z + 1)] == CellType.Room) count++;
        if(grid[ new Vector2Int(x - 1, z - 1)] == CellType.Room) count++;
        if(grid[ new Vector2Int(x + 1, z - 1)] == CellType.Room) count++;


        return count;
    }
    
    bool Search2D(int col, int row, int[] pattern)
    {
        int count = 0;
        int pos = 0;
        for(int z = 1; z > -2; z--)
        {
            for(int x = -1; x < 2; x++)
            {
                int equivalent = 0;
                if (col + x < 0 || col + x >= size.x || row + z < 0 || row + z >= size.y)
                {
                    //set this variable to 0 if we're out of bounds, as if we would have a CellType.None
                    equivalent = 0;
                    if(pattern[pos] == equivalent || pattern[pos] == 5)
                        count++;
                    pos++;
                    continue;
                }
                
                CellType type = grid[new Vector2Int(col + x, row + z)];

                switch (type)
                {
                    case CellType.Room:
                        equivalent = 2;
                        break;
                    case CellType.Hallway:
                        equivalent = 2;
                        break;
                    case CellType.None:
                        equivalent = 0;
                        break;
                    default:
                        equivalent = 5;
                        break;
                }
                
                if(pattern[pos] == equivalent || pattern[pos] == 5)
                    count++;
                pos++;
            }
        }
        return (count == 9);
    }
}
