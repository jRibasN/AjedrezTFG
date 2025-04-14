using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public enum ChessPieceType{
    None = 0,
    Pawn = 1,
    Rook = 2,
    Knight = 3,
    Bishop = 4,
    Queen = 5,
    King = 6
}

public class ChessPiece : MonoBehaviour
{
    public int team;
    public int currentX;
    public int currentY;
    public float value;
    public ChessPieceType type;

    private Vector3 desiredPosition;
    private Vector3 desiredScale = Vector3.one;

    public static Vector2Int a1 = new Vector2Int(0, 0);
    public static Vector2Int a2 = new Vector2Int(0, 1);
    public static Vector2Int a3 = new Vector2Int(0, 2);
    public static Vector2Int a4 = new Vector2Int(0, 3);
    public static Vector2Int a5 = new Vector2Int(0, 4);
    public static Vector2Int a6 = new Vector2Int(0, 5);
    public static Vector2Int a7 = new Vector2Int(0, 6);
    public static Vector2Int a8 = new Vector2Int(0, 7);
    public static Vector2Int b1 = new Vector2Int(1, 0);
    public static Vector2Int b2 = new Vector2Int(1, 1);
    public static Vector2Int b3 = new Vector2Int(1, 2);
    public static Vector2Int b4 = new Vector2Int(1, 3);
    public static Vector2Int b5 = new Vector2Int(1, 4);
    public static Vector2Int b6 = new Vector2Int(1, 5);
    public static Vector2Int b7 = new Vector2Int(1, 6);
    public static Vector2Int b8 = new Vector2Int(1, 7);
    public static Vector2Int c1 = new Vector2Int(2, 0);
    public static Vector2Int c2 = new Vector2Int(2, 1);
    public static Vector2Int c3 = new Vector2Int(2, 2);
    public static Vector2Int c4 = new Vector2Int(2, 3);
    public static Vector2Int c5 = new Vector2Int(2, 4);
    public static Vector2Int c6 = new Vector2Int(2, 5);
    public static Vector2Int c7 = new Vector2Int(2, 6);
    public static Vector2Int c8 = new Vector2Int(2, 7);
    public static Vector2Int d1 = new Vector2Int(3, 0);
    public static Vector2Int d2 = new Vector2Int(3, 1);
    public static Vector2Int d3 = new Vector2Int(3, 2);
    public static Vector2Int d4 = new Vector2Int(3, 3);
    public static Vector2Int d5 = new Vector2Int(3, 4);
    public static Vector2Int d6 = new Vector2Int(3, 5);
    public static Vector2Int d7 = new Vector2Int(3, 6);
    public static Vector2Int d8 = new Vector2Int(3, 7);
    public static Vector2Int e1 = new Vector2Int(4, 0);
    public static Vector2Int e2 = new Vector2Int(4, 1);
    public static Vector2Int e3 = new Vector2Int(4, 2);
    public static Vector2Int e4 = new Vector2Int(4, 3);
    public static Vector2Int e5 = new Vector2Int(4, 4);
    public static Vector2Int e6 = new Vector2Int(4, 5);
    public static Vector2Int e7 = new Vector2Int(4, 6);
    public static Vector2Int e8 = new Vector2Int(4, 7);
    public static Vector2Int f1 = new Vector2Int(5, 0);
    public static Vector2Int f2 = new Vector2Int(5, 1);
    public static Vector2Int f3 = new Vector2Int(5, 2);
    public static Vector2Int f4 = new Vector2Int(5, 3);
    public static Vector2Int f5 = new Vector2Int(5, 4);
    public static Vector2Int f6 = new Vector2Int(5, 5);
    public static Vector2Int f7 = new Vector2Int(5, 6);
    public static Vector2Int f8 = new Vector2Int(5, 7);
    public static Vector2Int g1 = new Vector2Int(6, 0);
    public static Vector2Int g2 = new Vector2Int(6, 1);
    public static Vector2Int g3 = new Vector2Int(6, 2);
    public static Vector2Int g4 = new Vector2Int(6, 3);
    public static Vector2Int g5 = new Vector2Int(6, 4);
    public static Vector2Int g6 = new Vector2Int(6, 5);
    public static Vector2Int g7 = new Vector2Int(6, 6);
    public static Vector2Int g8 = new Vector2Int(6, 7);
    public static Vector2Int h1 = new Vector2Int(7, 0);
    public static Vector2Int h2 = new Vector2Int(7, 1);
    public static Vector2Int h3 = new Vector2Int(7, 2);
    public static Vector2Int h4 = new Vector2Int(7, 3);
    public static Vector2Int h5 = new Vector2Int(7, 4);
    public static Vector2Int h6 = new Vector2Int(7, 5);
    public static Vector2Int h7 = new Vector2Int(7, 6);
    public static Vector2Int h8 = new Vector2Int(7, 7);

    private void Start() {
        transform.rotation = Quaternion.Euler((type == ChessPieceType.King) ? Vector3.zero : new Vector3(0, 270, 0));
    }

    private void Update(){
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * 10);
        transform.localScale = Vector3.Lerp(transform.localScale, desiredScale, Time.deltaTime * 10);
    }

    public virtual List<Vector2Int> GetAvailableMoves(ChessPiece[,] board, int tileCountX, int tileCountY, List<Vector2Int[]> moveList1){
        List<Vector2Int> r = new List<Vector2Int>();

        return r;
    }

    public virtual float UpdateValue(bool isEndgame){
        return value;
    }

    public virtual void SetPosition(Vector3 position, bool force = false){
        desiredPosition = position;
        if(force) transform.position = desiredPosition;
    }

    public virtual void SetScale(Vector3 scale, bool force = false){
        desiredScale = scale;
        if(force) transform.localScale = desiredScale;
    }
}
