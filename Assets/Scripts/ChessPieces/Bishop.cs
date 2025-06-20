using System.Collections.Generic;
using UnityEngine;

public class Bishop : ChessPiece
{
    List<Vector2Int> excellentSquareWhite = new List<Vector2Int>(){b2, b3, c4, f4, g3, g2};
    List<Vector2Int> goodSquareWhite = new List<Vector2Int>(){a2, a3, a4, b1, b4, b5, c5, f5, g1, g4, g5, h2, h3, h4};
    List<Vector2Int> badSquareWhite = new List<Vector2Int>(){d2, e2, a6, a7, a8, b7,c7, d7, e7, f7, g7, h6, h7, h8};
    List<Vector2Int> horribleSquareWhite = new List<Vector2Int>(){d1, e1, b8, c8, d8, e8, f8, g8};
    List<Vector2Int> excellentSquareBlack = new List<Vector2Int>(){b7, b6, c5, f5, g6, g7};
    List<Vector2Int> goodSquareBlack = new List<Vector2Int>(){a7, a6, a5, b8, b5, b4, c4, f4, g8, g5, g4, h7, h6, h5};
    List<Vector2Int> badSquareBlack = new List<Vector2Int>(){d7, e7, a3, a2, a1, b2, c2, d2, e2, f2, g2, h3, h2, h1};
    List<Vector2Int> horribleSquareBlack = new List<Vector2Int>(){d8, e8, b1, c1, d1, e1, f1, g1};

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

        //Diagonal (+, +)
        for (int x = currentX, y = currentY; x < tileCountX - 1 && y < tileCountY - 1; x++, y++)
        {
            if (board[x + 1, y + 1] == null)
                r.Add(new Vector2Int(x + 1, y + 1));
            
            else if (board[x + 1, y + 1] != null && board[x + 1, y + 1].team != team){
                r.Add(new Vector2Int(x + 1, y + 1));
                break;
            }

            else break;
        }

        //Diagonal (+, -)
        for (int x = currentX, y = currentY; x < tileCountX - 1 && y > 0; x++, y--)
        {
            if (board[x + 1, y - 1] == null)
                r.Add(new Vector2Int(x + 1, y - 1));
            
            else if (board[x + 1, y - 1] != null && board[x + 1, y - 1].team != team){
                r.Add(new Vector2Int(x + 1, y - 1));
                break;
            }

            else break;
        }

        //Diagonal (-, +)
        for (int x = currentX, y = currentY; x > 0 && y < tileCountY - 1; x--, y++)
        {
            if (board[x - 1, y + 1] == null)
                r.Add(new Vector2Int(x - 1, y + 1));
            
            else if (board[x - 1, y + 1] != null && board[x - 1, y + 1].team != team){
                r.Add(new Vector2Int(x - 1, y + 1));
                break;
            }

            else break;
        }

        //Diagonal (-, -)
        for (int x = currentX, y = currentY; x > 0 && y > 0; x--, y--)
        {
            if (board[x - 1, y - 1] == null)
                r.Add(new Vector2Int(x - 1, y - 1));
            
            else if (board[x - 1, y - 1] != null && board[x - 1, y - 1].team != team){
                r.Add(new Vector2Int(x - 1, y - 1));
                break;
            }

            else break;
        }

        return r;
    }
}
