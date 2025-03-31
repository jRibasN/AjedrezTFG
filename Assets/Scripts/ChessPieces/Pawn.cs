using System.Collections.Generic;
using UnityEngine;

public class Pawn : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ChessPiece[,] board, int tileCountX, int tileCountY, List<Vector2Int[]> moveList1){
        List<Vector2Int> r = new List<Vector2Int>();

        int direction = (team == 0) ? 1 : -1;

        if (currentY == 0 || currentY == tileCountY - 1) // If the pawn is on the first or last row, it can't move
            return r;
            
        // One in front
        if (board[currentX, currentY + direction] == null){
                r.Add(new Vector2Int(currentX, currentY + direction));
        }

        // Two in front
        if (board[currentX, currentY + direction] == null){
            // White team
            if(team == 0 && currentY == 1 && board[currentX, currentY +(direction * 2)] == null)
                r.Add(new Vector2Int(currentX, currentY +(direction * 2)));

            // Black team
            if(team == 1 && currentY == 6 && board[currentX, currentY +(direction * 2)] == null)
                r.Add(new Vector2Int(currentX, currentY +(direction * 2)));
        }

        // Capture move
        if (currentX != tileCountX - 1)
            if (board[currentX + 1, currentY + direction] != null &&  board[currentX + 1, currentY + direction].team != team)
                r.Add(new Vector2Int(currentX + 1, currentY + direction));

        if (currentX != 0)
            if (board[currentX - 1, currentY + direction] != null &&  board[currentX - 1, currentY + direction].team != team)
                r.Add(new Vector2Int(currentX - 1, currentY + direction));
        
        // En Passant
        if (moveList1.Count > 0){
            Vector2Int[] lastMove = moveList1[moveList1.Count - 1];
            if (board[lastMove[1].x, lastMove[1].y] != null){
                if (board[lastMove[1].x, lastMove[1].y].type == ChessPieceType.Pawn){ // If the last piece moved was a pawn
                    if (Mathf.Abs(lastMove[0].y - lastMove[1].y) == 2){ // If the last move was a +2 in either direction
                        if (board[lastMove[1].x, lastMove[1].y].team != team){ // If the move was from the other team
                            if (lastMove[1].y == currentY){ // If both pawns are on the same Y
                                if(lastMove[1].x == currentX - 1){ // Landed left
                                    r.Add(new Vector2Int(currentX - 1, currentY + direction));
                                }

                                if(lastMove[1].x == currentX + 1){ // Landed right
                                    r.Add(new Vector2Int(currentX + 1, currentY + direction));
                                }
                            }
                        }
                    }
                }
            }
        }

        // Promotion handled in ProcessSpecialMove() in ChessBoard.cs

        return r;
    }
}
