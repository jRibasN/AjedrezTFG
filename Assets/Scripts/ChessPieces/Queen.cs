using System.Collections.Generic;
using UnityEngine;

public class Queen : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY){
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
