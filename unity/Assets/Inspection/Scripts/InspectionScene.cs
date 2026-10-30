using UnityEngine;

/// <summary>Creates the fixed inspection room and returns its first-person camera.</summary>
public static class InspectionScene
{
    public static Camera Build()
    {
        var root = new GameObject("Inspection Room").transform;
        var floor = MakeMaterial("Floor", new Color(.51f, .56f, .57f));
        var wall = MakeMaterial("Wall", new Color(.79f, .82f, .82f));
        var trim = MakeMaterial("Wall trim", new Color(.43f, .49f, .50f));
        var cabinet = MakeMaterial("Cabinet", new Color(.20f, .29f, .33f));
        var panel = MakeMaterial("Cabinet panels", new Color(.13f, .20f, .23f));
        var metal = MakeMaterial("Metal", new Color(.59f, .65f, .65f));
        var green = MakeMaterial("Indicator green", new Color(.32f, .75f, .49f));
        var amber = MakeMaterial("Safety marking", new Color(.95f, .71f, .19f));
        var electricalBody = MakeMaterial("Electrical box body", new Color(.62f, .66f, .64f));
        var electricalDoor = MakeMaterial("Electrical box door", new Color(.48f, .53f, .51f));
        var electricalSwitch = MakeMaterial("Electrical switch", new Color(.14f, .17f, .16f));

        // Fixed geometry keeps reported positions meaningful across sessions.
        Cube(root, "Ground", new Vector3(0, -.1f, 6), new Vector3(14, .2f, 18), floor);
        Cube(root, "Back wall", new Vector3(0, 2, 14.9f), new Vector3(14, 4, .2f), wall);
        Cube(root, "Left wall", new Vector3(-6.9f, 2, 6), new Vector3(.2f, 4, 18), wall);
        Cube(root, "Right wall", new Vector3(6.9f, 2, 6), new Vector3(.2f, 4, 18), wall);
        var rearBoundary = new GameObject("Rear inspection boundary");
        rearBoundary.transform.SetParent(root, false);
        rearBoundary.transform.localPosition = new Vector3(0, 2, -2.9f);
        rearBoundary.layer = 2; // Unity's built-in Ignore Raycast layer still blocks the character controller.
        rearBoundary.AddComponent<BoxCollider>().size = new Vector3(14, 4, .2f);
        Cube(root, "Back skirting", new Vector3(0, .16f, 14.77f), new Vector3(13.8f, .32f, .05f), trim);
        Cube(root, "Left skirting", new Vector3(-6.77f, .16f, 6), new Vector3(.05f, .32f, 17.7f), trim);
        Cube(root, "Right skirting", new Vector3(6.77f, .16f, 6), new Vector3(.05f, .32f, 17.7f), trim);
        Cube(root, "Passage left line", new Vector3(-1.65f, .012f, 6.2f), new Vector3(.06f, .02f, 14), amber);
        Cube(root, "Passage right line", new Vector3(1.65f, .012f, 6.2f), new Vector3(.06f, .02f, 14), amber);

        Cabinet(root, "A-01", new Vector3(-2.8f, 1.1f, 5.6f), cabinet, panel, metal, green);
        Cabinet(root, "A-02", new Vector3(2.8f, 1.1f, 7.6f), cabinet, panel, metal, green);
        ElectricalBox(root, new Vector3(-5.6f, 1.25f, 11.6f), electricalBody, electricalDoor, electricalSwitch, metal, amber);
        // Pipes retain their primitive colliders and are valid inspection targets.
        Cylinder(root, "Horizontal pipe", new Vector3(3.6f, 2.85f, 14.53f), new Vector3(.18f, 2.3f, .18f), new Vector3(0, 0, 90), metal);
        Cylinder(root, "Vertical pipe", new Vector3(5.85f, 1.55f, 14.53f), new Vector3(.18f, 1.3f, .18f), Vector3.zero, metal);
        Cylinder(root, "Pipe joint", new Vector3(5.85f, 2.85f, 14.53f), new Vector3(.3f, .08f, .3f), new Vector3(0, 0, 90), trim);
        Cube(root, "Room sign", new Vector3(0, 3.1f, 14.76f), new Vector3(2.4f, .52f, .04f), panel);
        Label(root, "Room name", "EQUIPMENT ROOM", new Vector3(-1.03f, 3.13f, 14.72f), .075f);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.55f, .58f, .59f);
        var lightObject = new GameObject("Room directional light");
        lightObject.transform.SetParent(root, false);
        lightObject.transform.rotation = Quaternion.Euler(50, -30, 0);
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;

        var cameraObject = new GameObject("Inspection Camera");
        cameraObject.transform.SetParent(root, false);
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0, 1.6f, 0);
        cameraObject.transform.rotation = Quaternion.identity;
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.65f, .74f, .76f);
        return camera;
    }

    private static Material MakeMaterial(string name, Color color)
    {
        var shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        return new Material(shader) { name = name, color = color };
    }

    private static void Cube(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static void Cylinder(Transform parent, string name, Vector3 position, Vector3 scale, Vector3 rotation, Material material)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position;
        obj.transform.localScale = scale;
        obj.transform.localRotation = Quaternion.Euler(rotation);
        obj.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static void Cabinet(Transform parent, string id, Vector3 position, Material body, Material panel, Material metal, Material green)
    {
        parent = DeviceRoot(parent, id, "机柜 " + id);
        Cube(parent, "Cabinet " + id, position, new Vector3(1.55f, 2.2f, 1.1f), body);
        var front = position.z - .57f;
        Cube(parent, id + " door", new Vector3(position.x, position.y, front), new Vector3(1.38f, 2.03f, .04f), panel);
        Cube(parent, id + " handle", new Vector3(position.x + .52f, position.y, front - .04f), new Vector3(.04f, .55f, .04f), metal);
        for (var i = 0; i < 4; i++)
            Cube(parent, id + " vent " + (i + 1), new Vector3(position.x - .12f, position.y - .16f - i * .14f, front - .035f), new Vector3(.75f, .035f, .025f), metal);
        Cube(parent, id + " indicator", new Vector3(position.x - .47f, position.y + .66f, front - .035f), new Vector3(.08f, .08f, .025f), green);
        Label(parent, id + " label", id, new Vector3(position.x - .30f, position.y + .69f, front - .055f), .095f);
    }

    private static void ElectricalBox(Transform parent, Vector3 position, Material body, Material door, Material dark, Material metal, Material amber)
    {
        parent = DeviceRoot(parent, "POWER-01", "配电箱 POWER-01");
        // Retain the original centre while making the raised electrical box clearly smaller than the floor-standing racks.
        Cube(parent, "Electrical cabinet", position, new Vector3(1.20f, 1.35f, .62f), body);
        var front = position.z - .33f;
        Cube(parent, "Electrical door", new Vector3(position.x, position.y, front), new Vector3(1.08f, 1.23f, .05f), door);
        Cube(parent, "Electrical mounting bracket", new Vector3(-6.28f, position.y, position.z), new Vector3(1.18f, .14f, .35f), metal);
        Cube(parent, "Electrical handle", new Vector3(position.x + .43f, position.y, front - .04f), new Vector3(.045f, .28f, .035f), dark);

        for (var i = 0; i < 3; i++)
        {
            var x = position.x - .30f + i * .30f;
            Cube(parent, "Electrical switch " + (i + 1), new Vector3(x, position.y - .33f, front - .04f), new Vector3(.18f, .10f, .04f), dark);
            Cylinder(parent, "Electrical conduit " + (i + 1), new Vector3(x, position.y + .91f, position.z), new Vector3(.10f, .24f, .10f), Vector3.zero, metal);
        }

        var warning = new Vector3(position.x, position.y + .27f, front - .035f);
        WarningMesh(parent, "Electrical warning triangle", warning, amber,
            new[] { new Vector3(-.24f, -.18f, 0), new Vector3(0, .21f, 0), new Vector3(.24f, -.18f, 0) },
            new[] { 0, 1, 2 });
        WarningMesh(parent, "Electrical warning bolt", warning + new Vector3(0, 0, -.005f), dark,
            new[] { new Vector3(-.01f, .105f, 0), new Vector3(-.09f, -.015f, 0), new Vector3(-.015f, -.015f, 0), new Vector3(-.045f, -.11f, 0), new Vector3(.085f, .025f, 0), new Vector3(.02f, .025f, 0) },
            new[] { 0, 2, 1, 0, 5, 2, 2, 4, 3, 2, 5, 4 });
    }

    private static Transform DeviceRoot(Transform parent, string id, string displayName)
    {
        var device = new GameObject("Device " + id);
        device.transform.SetParent(parent, false);
        device.AddComponent<InspectionDevice>().Initialize(id, displayName);
        return device.transform;
    }

    private static void WarningMesh(Transform parent, string name, Vector3 position, Material material, Vector3[] vertices, int[] triangles)
    {
        var obj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position;
        var mesh = new Mesh { name = name, vertices = vertices, triangles = triangles };
        mesh.RecalculateNormals();
        obj.GetComponent<MeshFilter>().sharedMesh = mesh;
        obj.GetComponent<MeshRenderer>().sharedMaterial = material;
        // No collider on the symbol: clicks pass through to the door's real surface.
    }

    private static void Label(Transform parent, string name, string text, Vector3 position, float characterSize)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position;
        var label = obj.AddComponent<TextMesh>();
        label.text = text;
        label.fontSize = 32;
        label.characterSize = characterSize;
        label.anchor = TextAnchor.MiddleLeft;
        label.color = Color.white;
    }
}
