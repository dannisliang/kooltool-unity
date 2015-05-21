﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

using UnityEngine.EventSystems;

using PixelDraw;

namespace kooltool.Editor
{
    public class CharacterDrawing : MonoDrawing,
                                    IPointerEnterHandler,
                                    IPointerExitHandler
    {
        [SerializeField] protected Image Image;

        public Character Character { get; protected set; }

        public void SetCharacter(Character character)
        {
            Character = character;

            Drawing = new SpriteDrawing(character.Costume.Sprite);

            Image.sprite = character.Costume.Sprite;
            Image.SetNativeSize();
        }

        public void OnPointerEnter(PointerEventData data)
        {
            Editor.Instance.CharacterHover(this, true);
        }

        public void OnPointerExit(PointerEventData data)
        {
            Editor.Instance.CharacterHover(this, false);
        }
    }
}
