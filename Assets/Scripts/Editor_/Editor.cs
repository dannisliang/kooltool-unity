﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

using UnityEngine.EventSystems;
using System.Linq;

namespace kooltool.Editor
{
    public class Editor : MonoBehaviour
    {
        public static Editor Instance;

        public LayerMask WorldLayer;

        [SerializeField] protected HighlightGroup Highlights;

        [SerializeField] protected RectTransform Zoomer;

        [Header("UI")]
        [SerializeField] protected Slider ZoomSlider;

        [Header("Cursors")]
        [SerializeField] protected GameObject Cursors;
        [SerializeField] protected PixelCursor PixelCursor;
        [SerializeField] protected TileCursor TileCursor;

        [Header("Settings")]
        [SerializeField] protected AnimationCurve ZoomCurve;

        public RectTransform World;
        public Toolbox Toolbox;

        public Project Project { get; protected set; }

        public MapGenerator generator;

        public Layer Layer;

        public float Zoom { get; protected set; }

        protected Coroutine ZoomCoroutine;

        // poop
        public ITool ActiveTool;
        protected Vector2 LastCursor;
        Vector2 pansite;

        CharacterDrawing dragee;
        Vector2 dragPivot;

        protected bool Panning;
        protected bool Drawing;
        protected bool Dragging;

        public bool ShowCursors
        {
            get
            {
                return !Panning
                    && !Dragging
                    && !Input.GetKey(KeyCode.Tab)
                    && IsPointerOverWorld();
            }
        }

        public bool IsPointerOverWorld()
        {
            var pointer = new PointerEventData(EventSystem.current);
            pointer.position = Input.mousePosition;

            var results = new List<RaycastResult>();

            EventSystem.current.RaycastAll(pointer, results);

            return !(results.Count > 0 && results[0].gameObject.layer != LayerMask.NameToLayer("World"));
        }

        public void SwitchTool()
        {
            PixelCursor.gameObject.SetActive(false);
            TileCursor.gameObject.SetActive(false);
        }
        
        public void SetPixelTool()
        {
            SwitchTool();
            
            ActiveTool = Toolbox.PixelTool;
            
            PixelCursor.gameObject.SetActive(true);
        }
        
        public void SetTileTool()
        {
            SwitchTool();
            
            ActiveTool = Toolbox.TileTool;
            
            TileCursor.gameObject.SetActive(true);
        }

        protected void Awake()
        {
            Instance = this;

            Project = new Project(new Point(32, 32));

            Toolbox.PixelTool = new PixelTool(this);
            Toolbox.TileTool = new TileTool(this);

            Toolbox.PixelTab.SetPixelTool(Toolbox.PixelTool);
            Toolbox.TileTab.SetTileTool(Toolbox.TileTool);

            // poop
            PixelCursor.Tool = Toolbox.PixelTool;
            TileCursor.Tool = Toolbox.TileTool;
            
            ActiveTool = Toolbox.PixelTool;

            ZoomTo(1f);
        }

        protected void Start()
        {
            SetProject(Project);

            generator.Go(Project);

            Toolbox.Hide();
        }

        protected bool GetMouseDown(int button)
        {
            return IsPointerOverWorld() && Input.GetMouseButtonDown(button);
        }

        protected void CancelActions(Vector2 world)
        {
            if (Panning) Panning = false;
            if (Drawing) EndDraw(world);
            if (Dragging) EndDrag(world);
        }

        protected void CheckNavigation()
        {
            ZoomTo(ZoomSlider.value);

            Vector2 cursor = ScreenToWorld(Input.mousePosition);

            // panning
            if (Panning)
            {
                Pan(cursor - pansite);
            }
            
            if (Input.GetMouseButtonUp(1))
            {
                Panning = false;
            }
            
            if (GetMouseDown(1))
            {
                CancelActions(cursor);

                pansite = cursor;
                Panning = true;
            }

            // zoom
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            
            if (Mathf.Abs(scroll) > Mathf.Epsilon)
            {
                if (ZoomCoroutine != null) StopCoroutine(ZoomCoroutine);
                
                ZoomCoroutine = StartCoroutine(SmoothZoomTo(Zoom - scroll * 2, 0.125f));
            }
        }

        private string gistid;

        protected void CheckKeyboardShortcuts()
        {
            if (Input.GetKey(KeyCode.Alpha1)) Toolbox.PixelTab.SetSize(1);
            if (Input.GetKey(KeyCode.Alpha2)) Toolbox.PixelTab.SetSize(2);
            if (Input.GetKey(KeyCode.Alpha3)) Toolbox.PixelTab.SetSize(3);
            if (Input.GetKey(KeyCode.Alpha4)) Toolbox.PixelTab.SetSize(4);
            if (Input.GetKey(KeyCode.Alpha5)) Toolbox.PixelTab.SetSize(5);
            if (Input.GetKey(KeyCode.Alpha6)) Toolbox.PixelTab.SetSize(6);
            if (Input.GetKey(KeyCode.Alpha7)) Toolbox.PixelTab.SetSize(7);
            if (Input.GetKey(KeyCode.Alpha8)) Toolbox.PixelTab.SetSize(8);
            if (Input.GetKey(KeyCode.Alpha9)) Toolbox.PixelTab.SetSize(9);

            if (Input.GetKeyDown(KeyCode.T))
            {
                byte[] tileset = Project.Tileset.Texture.EncodeToPNG();
                string encoding = System.Convert.ToBase64String(tileset);

                var files = new Dictionary<string, string>
                {
                    {"some file.txt", "poop"},
                    {"another file.txt", "wee"},
                    {"whatever.txt", "haha"},
                    {"tileset.png", encoding},
                };

                StartCoroutine(MakeGist.Gist.Make("testing stuffff", files, delegate(string id)
                {
                    Debug.Log(id);
                    gistid = id;
                }));
            }

            if (Input.GetKeyDown(KeyCode.U))
            {
                StartCoroutine(MakeGist.Gist.Take(gistid, LoadFiles));
            }
        }

        protected void CheckHighlights()
        {
            var world = new Point(ScreenToWorld(Input.mousePosition));
            CharacterDrawing character;

            if (Input.GetKey(KeyCode.Tab))
            {
                Highlights.Highlights.SetActive(Layer.Characters.Instances.Cast<MonoBehaviour>());
            }
            else if (ShowCursors && Layer.CharacterUnderPoint(world, out character))
            {
                Highlights.Highlights.SetActive(new MonoBehaviour[] { character });
            }
            else
            {
                Highlights.Highlights.Clear();
            }
        }

        protected void LoadFiles(Dictionary<string, string> files)
        {
            string encoding = files["tileset.png"];

            var tileset = System.Convert.FromBase64String(encoding);

            Project.Tileset.Texture.LoadImage(tileset);
        }

        protected void UpdateCursors()
        {
            Vector2 cursor = ScreenToWorld(Input.mousePosition);

            Point grid, dummy;

            Project.Grid.Coords(new Point(cursor), out grid, out dummy);

            var offset = Vector2.one * ((Toolbox.PixelTool.Thickness % 2 == 1) ? 0.5f : 0);

            PixelCursor.end = cursor;
            PixelCursor.GetComponent<RectTransform>().anchoredPosition = cursor.Round() + offset;
            TileCursor.GetComponent<RectTransform>().anchoredPosition = new Vector2((grid.x + 0.5f) * Project.Grid.CellWidth,
                                                                                    (grid.y + 0.5f) * Project.Grid.CellHeight);

            Cursors.SetActive(ShowCursors);

            CheckHighlights();
        }

        protected void UpdateDrag(Vector2 world)
        {
            if (Input.GetKey(KeyCode.Tab))
            {
                if (GetMouseDown(0)) BeginDrag(world);
            }

            if (Dragging && Input.GetMouseButtonUp(0)) EndDrag(world);
            if (Dragging) ContinueDrag(world);
        }

        protected void BeginDrag(Vector2 world, CharacterDrawing character=null)
        {
            if (character != null
             || Layer.CharacterUnderPoint(new Point(world), out character))
            {
                Dragging = true;

                dragPivot = world - (Vector2) character.transform.localPosition;
                dragee = character;
            }
        }

        protected void ContinueDrag(Vector2 world)
        {
            Point grid, offset;

            Project.Grid.Coords(new Point(world - dragPivot), out grid, out offset);

            //dragee.transform.localPosition = (world - dragPivot).Floor();

            dragee.transform.localPosition = grid.Vector2() * 32f + Vector2.one * 16f;
        }

        protected void EndDrag(Vector2 world)
        {
            Dragging = false;
        }

        protected void UpdateDraw()
        {
            Vector2 cursor = ScreenToWorld(Input.mousePosition);

            if (GetMouseDown(0)) BeginDraw(cursor);

            if (Drawing && Input.GetMouseButtonUp(0)) EndDraw(cursor);
            if (Drawing) ContinueDraw(cursor);
        }

        protected void BeginDraw(Vector2 cursor)
        {
            CancelActions(cursor);

            Drawing = true;

            ActiveTool.BeginStroke(cursor);
        }

        protected void ContinueDraw(Vector2 world)
        {
            ActiveTool.ContinueStroke(LastCursor, world);
        }

        protected void EndDraw(Vector2 world)
        {
            ContinueDraw(world);

            Drawing = false;

            PixelCursor.end = world;
            PixelCursor.Update();
            ActiveTool.EndStroke(world);
        }

        protected void Update()
        {
            if (Project == null) return;

            Vector2 world = ScreenToWorld(Input.mousePosition);

            CheckKeyboardShortcuts();

            if (Input.GetKeyDown(KeyCode.Tab)
             || Input.GetKeyUp(KeyCode.Tab))
            {
                CancelActions(world);
            }

            if (!Toolbox.gameObject.activeSelf)
            {
                CheckNavigation();
                UpdateDrag(world);

                if (!Dragging && !Input.GetKey(KeyCode.Tab))
                {
                    UpdateDraw();
                }
            }

            UpdateCursors();

            if (Input.GetKeyDown(KeyCode.Space)) Toolbox.Show();
            if (Input.GetKeyUp(KeyCode.Space)) Toolbox.Hide();

            if (Input.GetKeyDown(KeyCode.LeftAlt)
             || Input.GetKeyDown(KeyCode.LeftShift))
            {
                Toolbox.TileTool.Tool = TileTool.ToolMode.Picker;
            }

            LastCursor = ScreenToWorld(Input.mousePosition);
        }

        public void SetProject(Project project)
        {
            Project = project;

            Toolbox.SetProject(project);
        }

        public Vector2 ScreenToWorld(Vector2 screen)
        {
            Vector2 world;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(World, 
                                                                    screen,
                                                                    null,
                                                                    out world);

            return world;                                                   
        }

        public IEnumerator SmoothZoomTo(float zoom, float duration)
        {
            float start = Zoom;
            float end = zoom;
            float timer = 0;

            Vector2 focus = new Vector2(Camera.main.pixelWidth  * 0.5f,
                                        Camera.main.pixelHeight * 0.5f);


            if (start > end)
            {
                focus = Input.mousePosition;
            }

            Vector2 worlda = ScreenToWorld(focus);

            while (timer < duration)
            {
                yield return new WaitForEndOfFrame();

                timer += Time.deltaTime;

                ZoomTo(start + timer / duration * (end - start), focus);
            }

            ZoomTo(end, focus);
        }

        public void ZoomTo(float zoom, Vector2? focus = null)
        {
            Zoom = Mathf.Clamp01(zoom);

            Vector2 center = new Vector2(Camera.main.pixelWidth  * 0.5f,
                                         Camera.main.pixelHeight * 0.5f);

            Vector2 screen = focus ?? center;

            Vector2 worlda = ScreenToWorld(screen);
            Zoomer.localScale = (Vector3) (ZoomCurve.Evaluate(Zoom) * Vector2.one);
            Vector2 worldb = ScreenToWorld(screen);
            
            Pan(worldb - worlda);

            ZoomSlider.value = Zoom;
        }

        public void Pan(Vector2 delta)
        {
            World.localPosition += (Vector3) delta;
        }

        public void MakeCharacter(Costume costume)
        {
            var character = new Character(Point.Zero, costume);

            CharacterDrawing drawing = Layer.Characters.Get(character);

            (drawing.transform as RectTransform).anchoredPosition = ScreenToWorld(Input.mousePosition);

            StartCoroutine(Delay(delegate
            {
                Toolbox.Hide();
                BeginDrag(ScreenToWorld(Input.mousePosition), drawing);
            }));
        }

        protected IEnumerator Delay(System.Action action)
        {
            yield return new WaitForEndOfFrame();

            action();
        }
    }
}
