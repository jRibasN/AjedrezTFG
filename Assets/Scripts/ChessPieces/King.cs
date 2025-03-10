using System.Collections.Generic;
using UnityEngine;

public class King : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY){
        List<Vector2Int> r = new List<Vector2Int>();

        List<Vector2Int> kingPotentialMoves = new List<Vector2Int>();

        if(currentX + 1 < tileCountX && currentY + 1 < tileCountY){
            kingPotentialMoves.Add(new Vector2Int(currentX + 1, currentY + 1));
        }

        if(currentX + 1 < tileCountX){
            kingPotentialMoves.Add(new Vector2Int(currentX + 1, currentY));
        }

        if(currentX + 1 < tileCountX && currentY - 1 >= 0){
            kingPotentialMoves.Add(new Vector2Int(currentX + 1, currentY - 1));
        }

        if(currentY - 1 >= 0){
            kingPotentialMoves.Add(new Vector2Int(currentX, currentY - 1));
        }

        if(currentX - 1 >= 0 && currentY - 1 >= 0){
            kingPotentialMoves.Add(new Vector2Int(currentX - 1, currentY - 1));
        }

        if(currentX - 1 >= 0){
            kingPotentialMoves.Add(new Vector2Int(currentX - 1, currentY));
        }

        if(currentX - 1 >= 0 && currentY + 1 < tileCountY){
            kingPotentialMoves.Add(new Vector2Int(currentX - 1, currentY + 1));
        }

        if(currentY + 1 < tileCountY){
            kingPotentialMoves.Add(new Vector2Int(currentX, currentY + 1));
        }

        foreach (Vector2Int move in kingPotentialMoves){
            if (board[move.x, move.y] == null)
                r.Add(move);

            else if (board[move.x, move.y] != null && board[move.x, move.y].team != team)
                r.Add(move);
        }

        return r;
    }

    public override SpecialMove GetSpecialMoves(ref ChessPiece[,] board, ref List<Vector2Int[]> moveList, ref List<Vector2Int> availableMoves)
    {
        SpecialMove r = SpecialMove.None;

        var kingMove = moveList.Find(m => m[0].x == 4 && m[0].y == ((team == 0) ? 0 : 7));
        var leftRook = moveList.Find(m => m[0].x == 0 && m[0].y == ((team == 0) ? 0 : 7));
        var rightRook = moveList.Find(m => m[0].x == 7 && m[0].y == ((team == 0) ? 0 : 7));

        if(kingMove == null && currentX == 4){
            // White team
            if(team == 0){
                // Left Rook
                if (leftRook == null)
                    if(board[0, 0].type == ChessPieceType.Rook)
                        if(board[0, 0].team == 0)
                            if(board[3, 0] == null)
                                if(board[2, 0] == null)
                                    if(board[1, 0] == null){
                                        availableMoves.Add(new Vector2Int(2, 0));
                                        r = SpecialMove.Castling;
                                    }

                // Right Rook
                if (rightRook == null)
                    if(board[7, 0].type == ChessPieceType.Rook)
                        if(board[7, 0].team == 0)
                            if(board[5, 0] == null)
                                if(board[6, 0] == null){
                                    availableMoves.Add(new Vector2Int(6, 0));
                                        r = SpecialMove.Castling;
                                }
            }

            else{
                // Left Rook
                if (leftRook == null)
                    if(board[0, 7].type == ChessPieceType.Rook)
                        if(board[0, 7].team == 1)
                            if(board[3, 7] == null)
                                if(board[2, 7] == null)
                                    if(board[1, 7] == null){
                                        availableMoves.Add(new Vector2Int(2, 7));
                                        r = SpecialMove.Castling;
                                    }

                // Right Rook
                if (rightRook == null)
                    if(board[7, 7].type == ChessPieceType.Rook)
                        if(board[7, 7].team == 1)
                            if(board[5, 7] == null)
                                if(board[6, 7] == null){
                                    availableMoves.Add(new Vector2Int(6, 7));
                                        r = SpecialMove.Castling;
                                }
            }
        }

        return r;
    }
}
