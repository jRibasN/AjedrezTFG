using System.Collections.Generic;
using UnityEngine;

public class Knight : ChessPiece
{
    List<Vector2Int> excellentSquareWhite = new List<Vector2Int>(){c3, c4, c5, c6, d4, d5, d6, e4, e5, e6, f3, f4, f5, f6};
    List<Vector2Int> goodSquareWhite = new List<Vector2Int>(){b5, b6, d3, e3, g5, g6};
    List<Vector2Int> badSquareWhite = new List<Vector2Int>(){a8, b8, c8, d8, e8, f8, g8, h8, a7, h7, a3, h3, a2, b2, g2, h2};
    List<Vector2Int> horribleSquareWhite = new List<Vector2Int>(){a1, h1};
    List<Vector2Int> excellentSquareBlack = new List<Vector2Int>(){c3, c4, c5, c6, d4, d5, d3, e4, e5, e3, f3, f4, f5, f6};
    List<Vector2Int> goodSquareBlack = new List<Vector2Int>(){b3, b4, d6, e6, g4, g3};
    List<Vector2Int> badSquareBlack = new List<Vector2Int>(){a1, b1, c1, d1, e1, f1, g1, h1, a2, h2, a6, h6, a7, b7, g7, h7};
    List<Vector2Int> horribleSquareBlack = new List<Vector2Int>(){a8, h8};

    public override float UpdateValue(bool isEndgame)
    {
        Vector2Int square = new Vector2Int(currentX, currentY);

        if(team == 0){
            if (excellentSquareWhite.Contains(square))
                value = 3.5f;
            else if (goodSquareWhite.Contains(square))
                value = 3.25f;
            else if (badSquareWhite.Contains(square))
                value = 2.75f;
            else if (horribleSquareWhite.Contains(square))
                value = 2.5f;
        }
        else {
            if (excellentSquareBlack.Contains(square))
                value = 3.5f;
            else if (goodSquareBlack.Contains(square))
                value = 3.25f;
            else if (badSquareBlack.Contains(square))
                value = 2.75f;
            else if (horribleSquareBlack.Contains(square))
                value = 2.5f;
        }          

        return value;
    }

    public override List<Vector2Int> GetAvailableMoves(ChessPiece[,] board, int tileCountX, int tileCountY, List<Vector2Int[]> moveList){
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
