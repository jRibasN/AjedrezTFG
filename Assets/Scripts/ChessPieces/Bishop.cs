using System.Collections.Generic;
using UnityEngine;

public class Bishop : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY){
        List<Vector2Int> r = new List<Vector2Int>();

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
