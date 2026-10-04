using UnityEngine;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine.UIElements;

// Static zoom manager to handle the zooming logic
public static class ZoomManager
{
    private static bool isZoomingIn = false;
    private static bool isZoomingOut = false;
    private static double lastTime = 0;
    private static float zoomSpeed = 3f;

    static ZoomManager()
    {
        EditorApplication.update += Update;
    }

    public static void StartZoomIn()
    {
        if (!isZoomingIn)
        {
            isZoomingIn = true;
            isZoomingOut = false;
            lastTime = EditorApplication.timeSinceStartup;
        }
    }

    public static void StartZoomOut()
    {
        if (!isZoomingOut)
        {
            isZoomingOut = true;
            isZoomingIn = false;
            lastTime = EditorApplication.timeSinceStartup;
        }
    }

    public static void StopZoom()
    {
        isZoomingIn = false;
        isZoomingOut = false;
    }

    private static void Update()
    {
        if (!isZoomingIn && !isZoomingOut)
            return;

        double currentTime = EditorApplication.timeSinceStartup;
        float deltaTime = (float)(currentTime - lastTime);
        lastTime = currentTime;

        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null)
        {
            StopZoom();
            return;
        }

        if (isZoomingIn)
        {
            // Zoom in - move camera closer (same logic as original)
            float zoomFactor = 1f - (zoomSpeed * deltaTime);
            sceneView.size *= zoomFactor;
            sceneView.Repaint();
        }
        else if (isZoomingOut)
        {
            // Zoom out - move camera farther (same logic as original)
            float zoomFactor = 1f + (zoomSpeed * deltaTime);
            sceneView.size *= zoomFactor;
            sceneView.Repaint();
        }
    }
}

// Custom button that properly handles hold-to-zoom
public class ZoomButton : VisualElement
{
    private bool isPressed = false;
    private bool isZoomIn;

    public ZoomButton(bool zoomIn)
    {
        isZoomIn = zoomIn;

        // Style the button to look like Unity's toolbar buttons
        style.minWidth = 30;
        style.minHeight = 30;
        style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        style.borderTopWidth = 1;
        style.borderBottomWidth = 1;
        style.borderLeftWidth = 1;
        style.borderRightWidth = 1;
        style.borderTopColor = Color.gray;
        style.borderBottomColor = Color.gray;
        style.borderLeftColor = Color.gray;
        style.borderRightColor = Color.gray;
        style.borderTopLeftRadius = 3;
        style.borderTopRightRadius = 3;
        style.borderBottomLeftRadius = 3;
        style.borderBottomRightRadius = 3;
        style.alignItems = Align.Center;
        style.justifyContent = Justify.Center;

        // Add text label
        Label label = new Label(zoomIn ? "+" : "-");
        label.style.fontSize = 16;
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        label.style.color = Color.white;
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        Add(label);

        // Register mouse events
        RegisterCallback<MouseDownEvent>(OnMouseDown);
        RegisterCallback<MouseUpEvent>(OnMouseUp);
        RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
        RegisterCallback<MouseEnterEvent>(OnMouseEnter);
    }

    private void OnMouseDown(MouseDownEvent evt)
    {
        if (evt.button == 0) // Left mouse button
        {
            isPressed = true;
            style.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 1f); // Pressed appearance

            if (isZoomIn)
                ZoomManager.StartZoomIn();
            else
                ZoomManager.StartZoomOut();

            this.CaptureMouse();
            evt.StopPropagation();
        }
    }

    private void OnMouseUp(MouseUpEvent evt)
    {
        if (evt.button == 0)
        {
            isPressed = false;
            style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f); // Normal appearance
            ZoomManager.StopZoom();
            this.ReleaseMouse();
        }
    }

    private void OnMouseLeave(MouseLeaveEvent evt)
    {
        if (isPressed)
        {
            ZoomManager.StopZoom();
            style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f); // Normal appearance
            this.ReleaseMouse();
            isPressed = false;
        }
    }

    private void OnMouseEnter(MouseEnterEvent evt)
    {
        if (!isPressed)
        {
            style.backgroundColor = new Color(0.4f, 0.4f, 0.4f, 1f); // Hover appearance
        }
    }
}

// Zoom Controls Overlay
[Overlay(typeof(SceneView), "Zoom")]
public class ZoomControlsOverlay : Overlay
{
    public override VisualElement CreatePanelContent()
    {
        var root = new VisualElement();
        root.style.flexDirection = FlexDirection.Row;
        root.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        root.style.borderTopWidth = 1;
        root.style.borderBottomWidth = 1;
        root.style.borderLeftWidth = 1;
        root.style.borderRightWidth = 1;
        root.style.borderTopColor = Color.gray;
        root.style.borderBottomColor = Color.gray;
        root.style.borderLeftColor = Color.gray;
        root.style.borderRightColor = Color.gray;
        root.style.borderTopLeftRadius = 5;
        root.style.borderTopRightRadius = 5;
        root.style.borderBottomLeftRadius = 5;
        root.style.borderBottomRightRadius = 5;
        root.style.paddingTop = 2;
        root.style.paddingBottom = 2;
        root.style.paddingLeft = 2;
        root.style.paddingRight = 2;

        // Add zoom buttons
        var zoomInButton = new ZoomButton(true);
        var zoomOutButton = new ZoomButton(false);

        root.Add(zoomInButton);
        root.Add(zoomOutButton);

        return root;
    }
}