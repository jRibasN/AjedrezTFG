using System.Collections.Generic;
using UnityEngine;

public class Queen : ChessPiece
{
    List<Vector2Int> excellentSquare = new List<Vector2Int>(){d4, d5, e4, e5};
    List<Vector2Int> goodSquare = new List<Vector2Int>(){c3, c4, c5, c6, d3, d6, e3, e6, f3, f4, f5, f6};
    List<Vector2Int> badSquare = new List<Vector2Int>(){a2, a3, a4, a5, a6, a7, b1, b2, b7, b8, c1, c8, f1, f8, g1, g2, g7, g8, h2, h3, h4, h5, h6, h7, d1, e1, d8, e8};
    List<Vector2Int> horribleSquare = new List<Vector2Int>(){a1, a8, h1, h8};

    public override float UpdateValue(bool isEndgame)
    {
        Vector2Int square = new Vector2Int(currentX, currentY);

        if (excellentSquare.Contains(square))
            value = 9.5f;
        else if (goodSquare.Contains(square))
            value = 9.25f;
        else if (badSquare.Contains(square))
            value = 8.75f;
        else if (horribleSquare.Contains(square))
            value = 8.5f;     

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

        // Diagonal (+, +)
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

        // Diagonal (+, -)
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

        // Diagonal (-, +)
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

        // Diagonal (-, -)
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
