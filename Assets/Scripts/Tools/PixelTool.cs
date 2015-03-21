﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

using PixelDraw;

public class PixelTool : ITool
{
    public enum ToolMode
    {
        Pencil,
        Fill,
        Eraser,
        Picker,
    }

    public ToolMode Tool;
    public ToolMode LastTool;

    public int Thickness = 1;
    public Color Color = Color.red;
    public Tilemap Tilemap;
    public InfiniteDrawing Drawing;

    public IDrawing Target;
    public MonoBehaviour CoroutineTarget;

    public PixelTool(Tilemap tilemap, InfiniteDrawing drawing)
    {
        Tilemap = tilemap;
        Drawing = drawing;
    }

    public void BeginStroke(Vector2 start)
    {
        var cell = TileTool.Vector2Cell(start);
        Tileset.Tile tile;

        if (Tilemap.Get(cell, out tile))
        {
            Target = Tilemap;
        }
        else
        {
            Target = Drawing;
        }

        if (Tool == ToolMode.Fill)
        {
            CoroutineTarget.StartCoroutine(Target.Fill(new Point(start), Color, 61 * 64));
            Target.Apply();
        }
        else if (Tool == ToolMode.Picker)
        {
            Color sampled;
            
            if (Target.Sample(new Point(start), out sampled))
            {
                Color = sampled;
            }
        }
    }

    public void ContinueStroke(Vector2 start, Vector2 end)
    {
        if (Tool == ToolMode.Pencil
         || Tool == ToolMode.Eraser)
        {
            var tl = new Vector2(Mathf.Min(start.x, end.x),
                                 Mathf.Min(start.y, end.y));
            
            var sprite = Brush.Line(new Point(start - tl), 
                                    new Point(end - tl), 
                                    Color.a > 0 ? Color : Color.white, 
                                    Thickness);          

            Target.Blit(new Point(start), sprite, Color.a == 0);
            Target.Apply();
        }
    }

    public void EndStroke(Vector2 end)
    {
    }
}
