using System.Collections.Generic;
using UnityEngine;

public class Knight : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY){
        List<Vector2Int> r = new List<Vector2Int>();

        List<Vector2Int> knightPotentialMoves = new List<Vector2Int>();

        if(currentX + 1 < tileCountX && currentY + 2 < tileCountY){
            knightPotentialMoves.Add(new Vector2Int(currentX + 1, currentY + 2));
        }

        if(currentX + 2 < tileCountX && currentY + 1 < tileCountY){
            knightPotentialMoves.Add(new Vector2Int(currentX + 2, currentY + 1));
        }

        if(currentX + 2 < tileCountX && currentY - 1 >= 0){
            knightPotentialMoves.Add(new Vector2Int(currentX + 2, currentY - 1));
        }

        if(currentX + 1 < tileCountX && currentY - 2 >= 0){
            knightPotentialMoves.Add(new Vector2Int(currentX + 1, currentY - 2));
        }

        if(currentX - 1 >= 0 && currentY - 2 >= 0){
            knightPotentialMoves.Add(new Vector2Int(currentX - 1, currentY - 2));
        }

        if(currentX - 2 >= 0 && currentY - 1 >= 0){
            knightPotentialMoves.Add(new Vector2Int(currentX - 2, currentY - 1));
        }

        if(currentX - 2 >= 0 && currentY + 1 < tileCountY){
            knightPotentialMoves.Add(new Vector2Int(currentX - 2, currentY + 1));
        }

        if(currentX - 1 >= 0 && currentY + 2 < tileCountY){
            knightPotentialMoves.Add(new Vector2Int(currentX - 1, currentY + 2));
        }

        foreach (Vector2Int move in knightPotentialMoves){
            if (board[move.x, move.y] == null)
                r.Add(move);

            else if (board[move.x, move.y] != null && board[move.x, move.y].team != team)
                r.Add(move);
        }

        return r;
    }
}
