using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid2D<T> {
    T[] data; //i'm guessing the T is standing in for a wildcard variable

    public Vector2Int Size { get; private set; }
    public Vector2Int Offset { get; set; }

    public Grid2D(Vector2Int size, Vector2Int offset) {
        Size = size;
        Offset = offset;

        data = new T[size.x * size.y];
    }

    public int GetIndex(Vector2Int pos) {
        return pos.x + (Size.x * pos.y);
    }

    public bool InBounds(Vector2Int pos) {
        return new RectInt(Vector2Int.zero, Size).Contains(pos + Offset);
    }

    //this seems to be used as a catch-all
        //if you call Grid2D[int x, int y] 
            //it will call itself again for you as Grid2D[new Vector2Int(x, y)]

    public T this[int x, int y] {
        get {
            return this[new Vector2Int(x, y)];
        }
        set {
            this[new Vector2Int(x, y)] = value;
        }
    }

    //using this getter/setter calls the GetIndex function which maps 2D coordinates onto a 1D array
        //you essentially use pos.x as a slider and
            //(size.x * pos.y) as a multiplier that changes what stack you're sliding on
    public T this[Vector2Int pos] {
        get {
            pos += Offset;
            return data[GetIndex(pos)];
        }
        set {
            pos += Offset;
            data[GetIndex(pos)] = value;
        }
    }
}
