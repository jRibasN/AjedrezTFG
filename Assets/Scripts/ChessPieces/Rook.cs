using System.Collections.Generic;
using UnityEngine;

public class Rook : ChessPiece
{
    List<Vector2Int> excellentSquareWhite = new List<Vector2Int>(){a7, b7, c7, d7, e7, f7, g7, h7};
    List<Vector2Int> goodSquareWhite = new List<Vector2Int>(){c1, d1, e1, f1, d8, e8};
    List<Vector2Int> excellentSquareBlack = new List<Vector2Int>(){a2, b2, c2, d2, e2, f2, g2, h2};
    List<Vector2Int> goodSquareBlack = new List<Vector2Int>(){c8, d8, e8, f8, d1, e1};

    public override float UpdateValue(bool isEndgame)
    {
        Vector2Int square = new Vector2Int(currentX, currentY);

        if(team == 0){
            if (excellentSquareWhite.Contains(square))
                value = 5.5f;
            else if (goodSquareWhite.Contains(square))
                value = 5.25f;
        }
        else {
            if (excellentSquareBlack.Contains(square))
                value = 5.5f;
            else if (goodSquareBlack.Contains(square))
                value = 5.25f;
        }        

        return value;
    }

    public override List<Vector2Int> GetAvailableMoves(ChessPiece[,] board, int tileCountX, int tileCountY, List<Vector2Int[]> moveList){
        List<Vector2Int> r = new List<Vector2Int>();

        // Move up
        for (int i = currentY; i < tileCountY - 1; i++)
        {
            if (board[currentX, i + 1] == null)
                r.Add(new Vector2Int(currentX, i + 1));

            else if (board[currentX, i + 1] != null && board[currentX, i + 1].team != team){
                r.Add(new Vector2Int(currentX, i + 1));
                break;
            }

            else break;
        }

        // Move down
        for (int i = currentY; i > 0; i--)
        {
            if (board[currentX, i - 1] == null)
                r.Add(new Vector2Int(currentX, i - 1));

            else if (board[currentX, i - 1] != null && board[currentX, i - 1].team != team){
                r.Add(new Vector2Int(currentX, i - 1));
                break;
            }

            else break;
        }

        // Move right
        for (int i = currentX; i < tileCountX - 1; i++)
        {
            if (board[i + 1, currentY] == null)
                r.Add(new Vector2Int(i + 1, currentY));

            else if (board[i + 1, currentY] != null && board[i + 1, currentY].team != team){
                r.Add(new Vector2Int(i + 1, currentY));
                break;
            }

            else break;
        }

        // Move left
        for (int i = currentX; i > 0; i--)
        {
            if (board[i - 1, currentY] == null)
                r.Add(new Vector2Int(i - 1, currentY));

            else if (board[i - 1, currentY] != null && board[i - 1, currentY].team != team){
                r.Add(new Vector2Int(i - 1, currentY));
                break;
            }

            else break;
        }

        return r;
    }
}
