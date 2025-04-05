using System.Collections.Generic;
using UnityEngine;

public class King : ChessPiece
{
    List<Vector2Int> excellentSquareWhiteMidgame = new List<Vector2Int>(){b1, g1};
    List<Vector2Int> goodSquareWhiteMidgame = new List<Vector2Int>(){a1, h1};
    List<Vector2Int> averageSquareWhiteMidgame = new List<Vector2Int>(){c1, d1, e1, f1};
    List<Vector2Int> badSquareWhiteMidgame = new List<Vector2Int>(){a2, b2, c2, d2, e2, f2, g2, h2};
    List<Vector2Int> excellentSquareBlackMidgame = new List<Vector2Int>(){b8, g8};
    List<Vector2Int> goodSquareBlackMidgame = new List<Vector2Int>(){a8, h8};
    List<Vector2Int> averageSquareBlackMidgame = new List<Vector2Int>(){c8, d8, e8, f8};
    List<Vector2Int> badSquareBlackMidgame = new List<Vector2Int>(){a7, b7, c7, d7, e7, f7, g7, h7};

    List<Vector2Int> excellentSquareWhiteEndgame = new List<Vector2Int>(){d4, d5, e4, e5};
    List<Vector2Int> goodSquareWhiteEndgame = new List<Vector2Int>(){c3, c4, c5 ,c6, d3, d6, e3, e6, f3, f4, f5, f6};
    List<Vector2Int> averageSquareWhiteEndgame = new List<Vector2Int>(){b3, b4, b5, b6, b7, c7, d7, e7, f7, g3, g4, g5, g6, g7};
    List<Vector2Int> badSquareWhiteEndgame = new List<Vector2Int>(){b2, c2, d2, e2, f2, g2, b8, c8, d8, e8, f8, g8};
    List<Vector2Int> excellentSquareBlackEndgame = new List<Vector2Int>(){d4, d5, e4, e5};
    List<Vector2Int> goodSquareBlackEndgame = new List<Vector2Int>(){c3, c4, c5 ,c6, d3, d6, e3, e6, f3, f4, f5, f6};
    List<Vector2Int> averageSquareBlackEndgame = new List<Vector2Int>(){b3, b4, b5, b6, b2, c2, d2, e2, f2, g3, g4, g5, g6, g2};
    List<Vector2Int> badSquareBlackEndgame = new List<Vector2Int>(){b7, c7, d7, e7, f7, g7, b1, c1, d1, e1, f1, g1};

    public override float UpdateValue(bool isEndgame)
    {
        Vector2Int square = new Vector2Int(currentX, currentY);

        if(isEndgame){
            if(team == 0){
            if (excellentSquareWhiteEndgame.Contains(square))
                value = 1001f;
            else if (goodSquareWhiteEndgame.Contains(square))
                value = 1000.75f;
            else if (averageSquareWhiteEndgame.Contains(square))
                value = 1000.5f;
            else if (badSquareWhiteEndgame.Contains(square))
                value = 1000.25f;
            }
            else {
                if (excellentSquareBlackEndgame.Contains(square))
                    value = 1001f;
                else if (goodSquareBlackEndgame.Contains(square))
                    value = 1000.75f;
                else if (averageSquareBlackEndgame.Contains(square))
                    value = 1000.5f;
                else if (badSquareBlackEndgame.Contains(square))
                    value = 1000.25f;
            }        
        }
        else{
            if(team == 0){
            if (excellentSquareWhiteMidgame.Contains(square))
                value = 1001f;
            else if (goodSquareWhiteMidgame.Contains(square))
                value = 1000.75f;
            else if (averageSquareWhiteMidgame.Contains(square))
                value = 1000.5f;
            else if (badSquareWhiteMidgame.Contains(square))
                value = 1000.25f;
            }
            else {
                if (excellentSquareBlackMidgame.Contains(square))
                    value = 1001f;
                else if (goodSquareBlackMidgame.Contains(square))
                    value = 1000.75f;
                else if (averageSquareBlackMidgame.Contains(square))
                    value = 1000.5f;
                else if (badSquareBlackMidgame.Contains(square))
                    value = 1000.25f;
            }        
        }

        

        return value;
    }
    public override List<Vector2Int> GetAvailableMoves(ChessPiece[,] board, int tileCountX, int tileCountY, List<Vector2Int[]> moveList){
        var kingMove = moveList.Find(m => m[0].x == 4 && m[0].y == ((team == 0) ? 0 : 7));
        var leftRook = moveList.Find(m => m[0].x == 0 && m[0].y == ((team == 0) ? 0 : 7));
        var rightRook = moveList.Find(m => m[0].x == 7 && m[0].y == ((team == 0) ? 0 : 7));

        ChessBoard chessBoard = FindObjectOfType<ChessBoard>();

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

        if(kingMove == null && currentX == 4){
            // White team
            if(team == 0){
                // Left Rook
                if (leftRook == null && board[0, 0] != null)
                    if(board[0, 0].type == ChessPieceType.Rook)
                        if(board[0, 0].team == 0)
                            if(board[3, 0] == null)
                                if(board[2, 0] == null)
                                    if(board[1, 0] == null){
                                        if (!chessBoard.IsSquareThreatened(new Vector2Int(2, 0), team) &&
                                            !chessBoard.IsSquareThreatened(new Vector2Int(3, 0), team) &&
                                            !chessBoard.IsSquareThreatened(new Vector2Int(4, 0), team)){
                                            r.Add(new Vector2Int(2, 0));
                                        }
                                    }

                // Right Rook
                if (rightRook == null && board[7, 0] != null)
                    if(board[7, 0].type == ChessPieceType.Rook)
                        if(board[7, 0].team == 0)
                            if(board[5, 0] == null)
                                if(board[6, 0] == null){
                                    if (!chessBoard.IsSquareThreatened(new Vector2Int(6, 0), team) &&
                                            !chessBoard.IsSquareThreatened(new Vector2Int(5, 0), team) &&
                                            !chessBoard.IsSquareThreatened(new Vector2Int(4, 0), team)){
                                            r.Add(new Vector2Int(6, 0));
                                        }
                                }
            }

            else{
                // Left Rook
                if (leftRook == null && board[0, 7] != null)
                    if(board[0, 7].type == ChessPieceType.Rook)
                        if(board[0, 7].team == 1)
                            if(board[3, 7] == null)
                                if(board[2, 7] == null)
                                    if(board[1, 7] == null){
                                        if (!chessBoard.IsSquareThreatened(new Vector2Int(2, 7), team) &&
                                            !chessBoard.IsSquareThreatened(new Vector2Int(3, 7), team) &&
                                            !chessBoard.IsSquareThreatened(new Vector2Int(4, 7), team)){
                                            r.Add(new Vector2Int(2, 7));
                                        }
                                    }

                // Right Rook
                if (rightRook == null && board[7, 7] != null)
                    if(board[7, 7].type == ChessPieceType.Rook)
                        if(board[7, 7].team == 1)
                            if(board[5, 7] == null)
                                if(board[6, 7] == null){
                                    if (!chessBoard.IsSquareThreatened(new Vector2Int(6, 7), team) &&
                                            !chessBoard.IsSquareThreatened(new Vector2Int(5, 7), team) &&
                                            !chessBoard.IsSquareThreatened(new Vector2Int(4, 7), team)){
                                            r.Add(new Vector2Int(6, 7));
                                        }
                                }
            }
        }

        return r;
    }
}
