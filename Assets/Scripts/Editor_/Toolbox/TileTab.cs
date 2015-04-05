﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace kooltool.Editor
{
    public class TileTab : MonoBehaviour
    {
        [Header("Tools")]
        [SerializeField] protected Button NewButton;

        [Header("Tiles")]
        [SerializeField] protected ToggleGroup TileToggleGroup;
        [SerializeField] protected RectTransform TileContainer;
        [SerializeField] protected TileIndicator TilePrefab;

        protected TileTool Tool;

        protected ChildElements<TileIndicator> Tiles;

        private void Awake()
        {
            NewButton.onClick.AddListener(OnClickedNew);

            Tiles = new ChildElements<TileIndicator>(TileContainer, TilePrefab);

            Refresh();
        }

        public void SetProject(Project project)
        {
            //Project = project;
        }

        public void SetTileTool(TileTool tool)
        {
            Tool = tool;
        }

        public void Refresh()
        {
            Tiles.Clear();

            foreach (Tileset.Tile tile in Tool.Tileset.Tiles)
            {
                TileIndicator element = Tiles.Add();

                element.SetTile(tile);

                element.Toggle.group = TileToggleGroup;
                if (tile == Tool.PaintTile) element.Toggle.isOn = true;

                var set = tile;
               
                element.Toggle.onValueChanged.RemoveAllListeners();
                element.Toggle.onValueChanged.AddListener(delegate (bool active) 
                {
                    if (active) Tool.PaintTile = set;
                });
            }
        }

        public void OnClickedNew()
        {
            Tool.PaintTile = Tool.Tileset.AddTile();

            Refresh();
        }
    }
}
