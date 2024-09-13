using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;
using KModkit;
using System.Diagnostics;

public class OddWallOutScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;

    public KMSelectable[] ButtonSels;
    public GameObject[] ButtonObjs;
    public GameObject[] WedgeObjs;
    public Material[] WedgeMats;
    public Material[] ButtonMats;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    private const int _size = 4;
    private const int _sttpo = 2 * _size + 1;
    private const int _numColors = 4;
    private MazeGenerator _mazeGenerator;
    private string _maze;

    private int? _previouslyPressedButton = null;
    private int[] _displayOrder;
    private int _submissionIx;
    private int[] _dummyColors;
    private int _cycleIx;
    private int[] _innerWallIxs;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;

        for (int i = 0; i < ButtonSels.Length; i++)
            ButtonSels[i].OnInteract += ButtonPress(i);
        for (int i = 0; i < WedgeObjs.Length; i++)
            WedgeObjs[i].GetComponent<MeshRenderer>().material = WedgeMats[4];
    }

    private void Generate()
    {
        var rnd = new MonoRandom(Rnd.Range(int.MinValue, int.MaxValue));
        var gen = new MazeGenerator(_size, rnd);
        var maze = gen.GenerateMaze();
        var walls = Enumerable.Range(0, _sttpo * _sttpo / 2).Select(ix => 2 * ix + 1).Where(ix => ix % _sttpo != _sttpo - 1 && ix / _sttpo != _sttpo - 1 && maze[ix] == '█').ToArray().Shuffle();
        var firstNonEdgeWall = walls.IndexOf(ix => ix % _sttpo != 0 && ix % _sttpo != _sttpo - 1 && ix / _sttpo != 0 && ix / _sttpo != _sttpo - 1);
        if (firstNonEdgeWall != 0)
        {
            var temp = walls[0];
            walls[0] = walls[firstNonEdgeWall];
            walls[firstNonEdgeWall] = temp;
        }

        var wallColors = walls.Select(w => rnd.Next(0, _numColors)).ToArray();
        var specialWallColors = Enumerable.Range(0, _numColors).ToArray().Shuffle().Take(2).ToArray();
        var tiles = Enumerable.Range(0, _size * _size).Select(pos =>
        {
            var cell = transformPosition(pos);
            return new Tile(
                getColor(cell - _sttpo, false, walls, wallColors, specialWallColors),
                getColor(cell + 1, false, walls, wallColors, specialWallColors),
                getColor(cell + _sttpo, true, walls, wallColors, specialWallColors),
                getColor(cell - 1, true, walls, wallColors, specialWallColors)
            );
        }).ToArray();

        // log the tiles??

    }

    IEnumerable<Maze> ConstructMaze(Tile[] sofar, Tile[] remaining, int[][] disconnectedPieces, int mismatchedWallAlready)
    {
        if (sofar.Length == _size * _size)
        {
            if (remaining.Length != 0)
                Debugger.Break();
            if (disconnectedPieces.Length == 1)
                yield return VisualizeMaze(sofar);
            yield break;
        }

        var cell = sofar.Length;
        for (var i = 0; i < remaining.Length; i++)
        {
            var newTile = remaining[i];

            // Make sure there is a wall around the edge of the maze
            if ((cell % _size == 0 && newTile.left == -1) ||
                (cell % _size == _size - 1 && newTile.right == -1) ||
                (cell / _size == 0 && newTile.top == -1) ||
                (cell / _size == _size - 1 && newTile.bottom == -1))
                continue;

            // Make sure that walls/non-walls join up with each other ABOVE and LEFT
            if (cell / _size != 0 && (sofar[above(cell)].bottom != -1) != (newTile.top != -1) || cell % _size != 0 && (sofar[left(cell)].right != -1) != (newTile.left != -1))
                continue;
            // If toroidal, make sure they join up BELOW and RIGHT
            if ((cell / _size == _size - 1 && (sofar[below(cell)].top != -1) != (newTile.bottom != -1) || cell % _size == _size - 1 && (sofar[right(cell)].left != -1) != (newTile.right != -1)))
                continue;

            var conflictsWithAbove = cell / _size != 0 && sofar[above(cell)].bottom != newTile.top;
            var conflictsWithLeft = cell % _size != 0 && sofar[left(cell)].right != newTile.left;
            var conflictsWithBelow = cell / _size == _size - 1 && sofar[below(cell)].top != newTile.bottom;
            var conflictsWithRight = cell % _size == _size - 1 && sofar[right(cell)].left != newTile.right;

            var newMismatches = mismatchedWallAlready + (conflictsWithAbove ? 1 : 0) + (conflictsWithLeft ? 1 : 0) + (conflictsWithBelow ? 1 : 0) + (conflictsWithRight ? 1 : 0);
            if (newMismatches > 1)
                continue;

            var pieceAbove = newTile.top != -1 ? -1 : disconnectedPieces.IndexOf(dp => dp.Contains(above(cell)));
            var pieceLeft = newTile.left != -1 ? -1 : disconnectedPieces.IndexOf(dp => dp.Contains(left(cell)));
            var newDisconnectedPieces =
                (pieceAbove != -1 && pieceLeft == pieceAbove) ? disconnectedPieces.Replace(pieceAbove, disconnectedPieces[pieceAbove].Append(cell)) :
                (pieceAbove != -1 && pieceLeft != -1) ? disconnectedPieces.Remove(Math.Max(pieceLeft, pieceAbove), 1).Remove(Math.Min(pieceLeft, pieceAbove), 1)
                    .Append(disconnectedPieces[pieceAbove].Concat(disconnectedPieces[pieceLeft]).Append(cell)) :
                (pieceAbove != -1) ? disconnectedPieces.Replace(pieceAbove, disconnectedPieces[pieceAbove].Append(cell)) :
                (pieceLeft != -1) ? disconnectedPieces.Replace(pieceLeft, disconnectedPieces[pieceLeft].Append(cell)) :
                disconnectedPieces.Append([cell]);

            foreach (var solution in constructMaze(sofar.Append(newTile), remaining.Remove(i, 1), newDisconnectedPieces, newMismatches))
                yield return solution;
        }
    }

    private static int right(int cell)
    {
        return (cell % _size + 1) % _size + _size * (cell / _size);
    }

    private static int below(int cell)
    {
        return (cell + _size) % (_size * _size);
    }

    private static int left(int cell)
    {
        return (cell % _size + _size - 1) % _size + _size * (cell / _size);
    }

    private static int above(int cell)
    {
        return (cell - _size + _size * _size) % (_size * _size);
    }

    private int getColor(int cl, bool sec, int[] walls, int[] wallColors, int[] specialWallColors)
    {
        if (cl == walls[0])
        {
            return specialWallColors[sec ? 1 : 0];
        }
        else
        {
            var p = Array.IndexOf(walls, cl);
            var p2 = Array.IndexOf(walls, toroidalled(cl));
            return p != -1 ? wallColors[p] : p2 != -1 ? wallColors[p2] : -1;
        }
    }

    private int transformPosition(int pos)
    {
        return (pos / _size * (_size * 2 + 1) * 2) + (pos % _size * 2) + _size * 2 + 2;
    }

    private int toroidalled(int cl)
    {
        return (cl % _sttpo) % (_sttpo - 1) + _sttpo * ((cl / _sttpo) % (_sttpo - 1));
    }

    private KMSelectable.OnInteractHandler ButtonPress(int btn)
    {
        return delegate ()
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, ButtonSels[btn].transform);
            ButtonSels[btn].AddInteractionPunch(0.3f);

            if (_moduleSolved)
                return false;

            return false;
        };
    }
}

internal struct Tile
{
    public int top;
    public int right;
    public int bottom;
    public int left;

    public Tile(int top, int right, int bottom, int left)
    {
        this.top = top;
        this.right = right;
        this.bottom = bottom;
        this.left = left;
    }

    public override bool Equals(object obj)
    {
        if (!(obj is Tile))
        {
            return false;
        }

        Tile other = (Tile)obj;
        return top == other.top &&
               right == other.right &&
               bottom == other.bottom &&
               left == other.left;
    }

    public override int GetHashCode()
    {
        int hashCode = -402104261;
        hashCode = hashCode * -1521134295 + top.GetHashCode();
        hashCode = hashCode * -1521134295 + right.GetHashCode();
        hashCode = hashCode * -1521134295 + bottom.GetHashCode();
        hashCode = hashCode * -1521134295 + left.GetHashCode();
        return hashCode;
    }

    public void Deconstruct(out int top, out int right, out int bottom, out int left)
    {
        top = this.top;
        right = this.right;
        bottom = this.bottom;
        left = this.left;
    }

    public static implicit operator (int top, int right, int bottom, int left)(Tile value)
    {
        return (value.top, value.right, value.bottom, value.left);
    }

    public static implicit operator Tile((int top, int right, int bottom, int left) value)
    {
        return new NewStruct(value.top, value.right, value.bottom, value.left);
    }
}
internal struct Maze
{
    public string str;
    public int[] colors;

    public Maze(string str, int[] colors)
    {
        this.str = str;
        this.colors = colors;
    }

    public override bool Equals(object obj)
    {
        if (!(obj is Maze))
        {
            return false;
        }

        Maze other = (Maze)obj;
        return str == other.str &&
               EqualityComparer<int[]>.Default.Equals(colors, other.colors);
    }

    public override int GetHashCode()
    {
        int hashCode = -1468143167;
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(str);
        hashCode = hashCode * -1521134295 + EqualityComparer<int[]>.Default.GetHashCode(colors);
        return hashCode;
    }

    public void Deconstruct(out string str, out int[] colors)
    {
        str = this.str;
        colors = this.colors;
    }

    public static implicit operator (string str, int[] colors)(Maze value)
    {
        return (value.str, value.colors);
    }

    public static implicit operator Maze((string str, int[] colors) value)
    {
        return new NewStruct(value.str, value.colors);
    }
}