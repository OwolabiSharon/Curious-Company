#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Creates editable Unity scenes and original furniture prefabs. Existing prototype scenes are retained.
public static class CampaignBuilder
{
    const string Art = "Assets/CampaignArt";
    const string Scenes = "Assets/Scenes/Campaign";
    static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
    static Transform environment;
    static readonly List<Transform> points = new List<Transform>();
    static readonly List<Light> lamps = new List<Light>();

    [MenuItem("Curious Company/Build Campaign")]
    public static void Build()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(Scenes);
        Directory.CreateDirectory(Art + "/Materials");
        Directory.CreateDirectory(Art + "/Prefabs");
        AssetDatabase.Refresh();
        MakeMaterials();
        MakeFurniture();
        MakeActors();
        for (int i = 0; i < CampaignSession.MissionCount; i++) BuildMission(i);
        var buildScenes = EditorBuildSettings.scenes.Where(s => !s.path.StartsWith(Scenes + "/")).ToList();
        for (int i = 0; i < CampaignSession.MissionCount; i++)
            buildScenes.Add(new EditorBuildSettingsScene($"{Scenes}/{CampaignSession.SceneName(i)}.unity", true));
        EditorBuildSettings.scenes = buildScenes.ToArray();
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene($"{Scenes}/Campaign_01.unity");
        Debug.Log("CAMPAIGN_BUILD_COMPLETE: 10 missions, navigation and population paths validated.");
    }

    static Material Material(string name, Color color, float metallic = 0, float smoothness = 0.25f, string texture = null, float tiling = 1)
    {
        string path = $"{Art}/Materials/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Smoothness", smoothness);
        if (texture != null) mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texture));
        mat.SetTextureScale("_BaseMap", Vector2.one * tiling);
        materials[name] = mat;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void MakeMaterials()
    {
        Material("Enamel", new Color(.64f,.65f,.56f));
        Material("Metal", new Color(.19f,.23f,.23f), .65f, .48f);
        Material("Fabric", new Color(.19f,.32f,.28f));
        Material("Linen", new Color(.72f,.73f,.64f));
        Material("Wood", new Color(.27f,.18f,.105f));
        Material("Screen", new Color(.045f,.16f,.18f));
        Material("Paper", new Color(.81f,.77f,.62f));
        Material("Leaf", new Color(.13f,.24f,.12f));
        Material("Pot", new Color(.29f,.27f,.23f));
        Material("Black", new Color(.035f,.045f,.045f));
        Material("Plaster", new Color(.69f,.66f,.56f));
        Material("HospitalPaint", new Color(.22f,.34f,.30f));
        Material("OfficePaint", new Color(.22f,.28f,.32f));
        Material("WallCap", new Color(.075f,.09f,.085f));
        Material("Brass", new Color(.65f,.47f,.21f), .5f);
        Material("Floor", new Color(.78f,.73f,.59f), 0, .30f, "Assets/visuals/textures/cheap_old_linoleum1_albedo.png");
        Material("Tile", new Color(.70f,.73f,.69f), 0, .24f, "Assets/visuals/textures/base-white-tile_albedo.png");
        Material("OfficeFloor", new Color(.63f,.60f,.52f), 0, .20f, "Assets/visuals/textures/laminate-flooring-brown_albedo.png");
        materials["Floor"].SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/visuals/textures/cheap_old_linoleum1_Normal-ogl.png"));
        materials["Floor"].SetFloat("_BumpScale", .4f);
        materials["Floor"].EnableKeyword("_NORMALMAP");
        materials["Tile"].SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/visuals/textures/base-white-tile_normal-ogl.png"));
        materials["Tile"].SetFloat("_BumpScale", .3f);
        materials["Tile"].EnableKeyword("_NORMALMAP");
        Material("Extract", new Color(.16f,.48f,.35f));
        Material("Light", new Color(1,.78f,.39f));
        materials["Light"].EnableKeyword("_EMISSION");
        materials["Light"].SetColor("_EmissionColor", new Color(1,.69f,.3f) * 2);
        materials["Screen"].EnableKeyword("_EMISSION");
        materials["Screen"].SetColor("_EmissionColor", new Color(.05f,.23f,.20f));
    }

    static void MakeFurniture()
    {
        foreach (string path in Directory.GetFiles(Art + "/Models", "*.fbx"))
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!source) throw new InvalidOperationException("Missing furniture model: " + path);
            var obj = (GameObject)PrefabUtility.InstantiatePrefab(source);
            foreach (Renderer renderer in obj.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(m =>
                {
                    string name = m.name.Split('.')[0];
                    return materials.TryGetValue(name, out Material mapped) ? mapped : materials["Enamel"];
                }).ToArray();
            }
            PrefabUtility.SaveAsPrefabAsset(obj, $"{Art}/Prefabs/{Path.GetFileNameWithoutExtension(path)}.prefab");
            UnityEngine.Object.DestroyImmediate(obj);
        }
    }

    static void MakeActors()
    {
        foreach (var pair in new[] {
            ("Player", "Assets/assets/scene prefabs/Player.prefab"),
            ("Survivor", "Assets/assets/scene prefabs/rescue.prefab"),
            ("Enemy", "Assets/assets/enemy (4).prefab") })
        {
            var obj = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(pair.Item2));
            obj.name = pair.Item1;
            obj.transform.position = Vector3.up;
            // The source player contains disabled legacy components on its model child.
            // Remove those only in the new campaign variant, preserving the source prefab.
            foreach (var c in obj.GetComponentsInChildren<CharacterController>(true))
                if (c.gameObject != obj) UnityEngine.Object.DestroyImmediate(c);
            foreach (var c in obj.GetComponentsInChildren<TakeDamage>(true))
                if (c.gameObject != obj) UnityEngine.Object.DestroyImmediate(c);
            foreach (var c in obj.GetComponentsInChildren<Collider>(true))
                if (c.gameObject != obj) c.enabled = false;
            foreach (var c in obj.GetComponentsInChildren<Light>(true)) c.enabled = false;
            foreach (var particle in obj.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = particle.main;
                main.playOnAwake = false;
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            var body = obj.GetComponent<Rigidbody>();
            if (body)
            {
                body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
                body.useGravity = false;
                body.isKinematic = pair.Item1 != "Player";
                if (pair.Item1 == "Player")
                {
                    body.linearDamping = 7;
                    body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                }
            }
            if (pair.Item1 == "Player")
            {
                var player = obj.GetComponent<CharacterController>();
                player.moveSpeed = 4;
                var muzzle = new GameObject("Campaign muzzle").transform;
                muzzle.SetParent(obj.transform, false);
                muzzle.localPosition = new Vector3(.18f, .15f, .8f);
                player.bulletSpawn = muzzle;
                var light = new GameObject("Flashlight").AddComponent<Light>();
                light.transform.SetParent(obj.transform, false);
                light.transform.localPosition = new Vector3(.15f,.30f,.4f);
                light.transform.localRotation = Quaternion.Euler(14,0,0);
                light.type = LightType.Spot;
                light.spotAngle = 62;
                light.innerSpotAngle = 30;
                light.range = 15;
                light.intensity = 6;
                light.color = new Color(.88f,.94f,1);
                light.shadows = LightShadows.Soft;
            }
            PrefabUtility.SaveAsPrefabAsset(obj, $"{Art}/Prefabs/Campaign{pair.Item1}.prefab");
            UnityEngine.Object.DestroyImmediate(obj);
        }
    }

    static void BuildMission(int mission)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        points.Clear(); lamps.Clear();
        bool hospital = mission % 2 == 0;
        int tier = mission / 2;
        int rows = 3 + tier / 2;
        float end = rows * 8 + 2;
        environment = new GameObject("Architecture and furnishings").transform;
        var gm = new GameObject("GameManager").AddComponent<GameManager>();
        gm.gameObject.AddComponent<InputReader>();
        var director = gm.gameObject.AddComponent<CampaignDirector>();
        director.mission = mission;
        director.manager = gm;
        gm.gameObject.AddComponent<CampaignHUD>().director = director;
        director.enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Art + "/Prefabs/CampaignEnemy.prefab");
        director.survivorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Art + "/Prefabs/CampaignSurvivor.prefab");
        gm.totalRescues = CampaignSession.SurvivorCount(mission);
        // Tile modules keep consistent texture scale across differently sized floors.
        for (int z = -4; z < end + 2; z += 2)
            for (int x = -11; x < 11; x += 2)
                Cube("Floor", new Vector3(x+1,-.15f,z+1),new Vector3(2,.3f,2),
                    Mathf.Abs(x+1)<3 ? "Floor" : hospital ? "Tile" : "OfficeFloor");
        Wall(new Vector3(-11,0,(end-2)/2),new Vector3(.28f,2.6f,end+6),hospital);
        Wall(new Vector3(11,0,(end-2)/2),new Vector3(.28f,2.6f,end+6),hospital);
        Wall(new Vector3(0,0,end+2),new Vector3(22,2.6f,.28f),hospital);
        Wall(new Vector3(0,0,-4),new Vector3(22,1.5f,.28f),hospital);
        for (int row = 0; row < rows; row++)
        {
            float z = row * 8 + 4;
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 7;
                // Doorway in the corridor wall, with a clearly visible brass threshold.
                Wall(new Vector3(side*3,0,z-2.65f),new Vector3(.23f,2.6f,2.7f),hospital);
                Wall(new Vector3(side*3,0,z+2.65f),new Vector3(.23f,2.6f,2.7f),hospital);
                Cube("Door threshold",new Vector3(side*3,.02f,z),new Vector3(.6f,.035f,2.6f),"Brass",false);
                // Room-to-room doorway creates alternate escort routes.
                Wall(new Vector3(side*4.35f,0,z+4),new Vector3(2.7f,2.6f,.23f),hospital);
                Wall(new Vector3(side*9.3f,0,z+4),new Vector3(3.4f,2.6f,.23f),hospital);
                if (row == 0) Wall(new Vector3(x,0,z-4),new Vector3(8,1.5f,.23f),hospital);
                Room(hospital, row, side, tier, x, z);
                Lamp(new Vector3(side*7,2.2f,z+3.6f));
                // Keep candidates in open room lanes, away from furniture and door edges.
                foreach (float dx in new[] {-1.6f,0f,1.6f})
                    foreach (float dz in new[] {-.4f,1.2f})
                        Spawn(new Vector3(x+dx,.1f,z+dz));
                FloorLabel((hospital ? "WARD " : "OFFICE ") + (row+1) + (side<0 ? "A" : "B"),new Vector3(side*4.6f,.035f,z-1.8f),.11f);
            }
            Lamp(new Vector3(0,2.8f,z+2));
            if (tier >= 2 && row == 1)
                Prop("MedicalTrolley", new Vector3(hospital ? 2.25f : -9.7f,0,z+2.8f),25, new Vector3(.85f,1.9f,.65f));
        }
        // Extraction is wide enough to accommodate the whole escort without blocking the route.
        var zone = Cube("Extraction • Safe",new Vector3(0,.012f,-1.5f),new Vector3(5.2f,.025f,3.8f),"Extract",false);
        zone.tag = "Safe";
        zone.layer = 0;
        var trigger = zone.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(1,100,1);
        trigger.center = new Vector3(0,50,0);
        director.extractionPosition = new Vector3(0,0,-1.5f);
        FloorLabel("EXTRACTION",new Vector3(0,.04f,-2.8f),.18f);
        FloorLabel(hospital ? "ST. BRIGID  /  HOSPITAL" : "NORTHLINE  /  OFFICES",new Vector3(0,.035f,1.8f),.12f);
        var surface = environment.gameObject.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = 1 << 6;
        surface.BuildNavMesh();
        string navPath = $"{Scenes}/{CampaignSession.SceneName(mission)}_Navigation.asset";
        var existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(navPath);
        if (existing)
        {
            EditorUtility.CopySerialized(surface.navMeshData, existing);
            surface.RemoveData(); surface.navMeshData = existing; surface.AddData();
        }
        else AssetDatabase.CreateAsset(surface.navMeshData,navPath);
        var playerObj = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Art + "/Prefabs/CampaignPlayer.prefab"));
        playerObj.name = "Player";
        playerObj.transform.position = new Vector3(0,1,0);
        director.player = playerObj.GetComponent<CharacterController>();
        director.player.gm = gm;
        var camera = new GameObject("Main Camera").AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.transform.position = playerObj.transform.position + new Vector3(0,16,-7);
        camera.transform.rotation = Quaternion.Euler(55,0,0);
        camera.orthographic = true;
        camera.orthographicSize = 8.5f;
        camera.nearClipPlane = .1f;
        camera.farClipPlane = 90;
        camera.backgroundColor = new Color(.025f,.034f,.035f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.AdditionalCameraData().renderPostProcessing = true;
        camera.gameObject.AddComponent<AudioListener>();
        var follow = camera.gameObject.AddComponent<CameraController>();
        follow.player = playerObj.transform;
        follow.offset = new Vector3(0,16,-7);
        follow.gm = gm;
        follow.isPlaying = true;
        follow.followSpeed = 8;
        Lighting();
        director.spawnPoints = points.ToArray();
        director.roomLights = lamps.ToArray();
        foreach (var renderer in environment.GetComponentsInChildren<MeshRenderer>())
            GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, StaticEditorFlags.BatchingStatic);
        ValidateNavigation(director);
        EditorSceneManager.SaveScene(scene,$"{Scenes}/{CampaignSession.SceneName(mission)}.unity");
    }

    static UniversalAdditionalCameraData AdditionalCameraData(this Camera camera) => camera.GetUniversalAdditionalCameraData();

    static void Room(bool hospital, int row, int side, int tier, float x, float z)
    {
        if (hospital && row > 0)
        {
            Prop("HospitalBed", new Vector3(x-1.8f,0,z-2.1f),0,new Vector3(1.25f,1.15f,2.35f));
            Prop("HospitalBed", new Vector3(x+1.65f,0,z-2.1f),0,new Vector3(1.25f,1.15f,2.35f));
            Prop("PrivacyCurtain",new Vector3(x-.1f,0,z-2.2f),90,new Vector3(.18f,2,2.2f));
            Prop("MedicalTrolley",new Vector3(x+2.7f,0,z+2.9f),0,new Vector3(.85f,1.9f,.65f));
        }
        else
        {
            if (row == 0)
                Prop("ReceptionCounter",new Vector3(x-1.5f,0,z-2.5f),0,new Vector3(2.7f,1.75f,1.9f));
            else Prop("OfficeDesk",new Vector3(x-1.9f,0,z-2.5f),0,new Vector3(1.85f,1.4f,.95f));
            Prop("OfficeChair",new Vector3(x-1.9f,0,z-1.5f),180,new Vector3(.8f,1.3f,.8f));
            if (hospital || (row+tier)%2==0)
                Prop("WaitingBench",new Vector3(x+1.6f,0,z-2.8f),0,new Vector3(1.9f,1.2f,.8f));
            else
            {
                Prop("OfficeDesk",new Vector3(x+1.5f,0,z-2.5f),0,new Vector3(1.85f,1.4f,.95f));
                Prop("OfficeChair",new Vector3(x+1.5f,0,z-1.5f),180,new Vector3(.8f,1.3f,.8f));
            }
        }
        if (hospital && row > 0)
            Prop("Washbasin",new Vector3(x+.8f,0,z+3.5f),0,new Vector3(.75f,1.2f,.6f));
        Prop("Cabinet",new Vector3(x-2.6f,0,z+3.4f),0,new Vector3(1,1.4f,.6f));
        Prop("Plant",new Vector3(x+3.0f,0,z-3.3f),0,new Vector3(.65f,1.5f,.65f));
        // Noticeboard, individual papers, and small restrained floor litter.
        Cube("Noticeboard",new Vector3(x-1.1f,1.6f,z+3.83f),new Vector3(1.25f,.8f,.045f),"Wood",false);
        for (int i=0;i<3;i++)
            Cube("Notice",new Vector3(x-1.45f+i*.34f,1.6f,z+3.79f),new Vector3(.26f,.53f,.012f),"Paper",false);
        for (int i=0;i<3;i++)
        {
            var paper = Cube("Dropped paperwork",new Vector3(x+2.3f+i*.19f,.012f,z+2.8f-i*.36f),new Vector3(.23f,.015f,.32f),"Paper",false);
            paper.transform.rotation = Quaternion.Euler(0,23+i*41,0);
        }
    }

    static GameObject Cube(string name, Vector3 position, Vector3 size, string material, bool solid = true)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(environment);
        obj.transform.position = position;
        obj.transform.localScale = size;
        obj.layer = 6;
        obj.GetComponent<Renderer>().sharedMaterial = materials[material];
        if (!solid) UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
        else obj.tag = "wall";
        return obj;
    }

    static void Wall(Vector3 center, Vector3 size, bool hospital)
    {
        float lower = Mathf.Min(1.05f,size.y);
        Cube("Painted wall",center+Vector3.up*lower*.5f,new Vector3(size.x,lower,size.z),hospital ? "HospitalPaint":"OfficePaint");
        Cube("Plaster wall",center+Vector3.up*(lower+(size.y-lower)*.5f),new Vector3(size.x,size.y-lower,size.z),"Plaster");
        Cube("Wall cap",center+Vector3.up*(size.y+.035f),new Vector3(size.x+.055f,.07f,size.z+.055f),"WallCap",false);
        Cube("Skirting",center+Vector3.up*.08f,new Vector3(size.x+.035f,.16f,size.z+.035f),"WallCap",false);
    }

    static void Prop(string name, Vector3 position, float rotation, Vector3 bounds)
    {
        var obj = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"{Art}/Prefabs/{name}.prefab"));
        obj.transform.SetParent(environment);
        obj.transform.position = position;
        obj.transform.rotation = Quaternion.Euler(0,rotation,0);
        obj.name = name;
        obj.layer = 6;
        obj.tag = "wall";
        var collider = obj.AddComponent<BoxCollider>();
        collider.center = Vector3.up * bounds.y*.5f;
        collider.size = bounds;
    }

    static void Spawn(Vector3 p)
    {
        var point = new GameObject("Population spawn").transform;
        point.position = p;
        point.SetParent(environment);
        points.Add(point);
    }

    static void Lamp(Vector3 position)
    {
        Cube("Warm light fixture",position,new Vector3(.65f,.16f,.18f),"Light",false);
        var lamp = new GameObject("Room light").AddComponent<Light>();
        lamp.transform.SetParent(environment);
        lamp.transform.position = position + new Vector3(0,-.22f,-.25f);
        lamp.type = LightType.Point;
        lamp.range = 7;
        lamp.intensity = 18f;
        lamp.color = new Color(1,.75f,.42f);
        lamp.shadows = LightShadows.None;
        lamps.Add(lamp);
    }

    static void FloorLabel(string text, Vector3 p, float size)
    {
        var label = new GameObject(text).AddComponent<TextMesh>();
        label.transform.SetParent(environment);
        label.transform.position = p;
        label.transform.rotation = Quaternion.Euler(90,0,0);
        label.text = text;
        label.fontSize = 64;
        label.characterSize = size * .48f;
        label.anchor = TextAnchor.MiddleCenter;
        label.color = new Color(.84f,.81f,.64f);
    }

    static void Lighting()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.35f,.38f,.42f);
        RenderSettings.ambientEquatorColor = new Color(.23f,.24f,.23f);
        RenderSettings.ambientGroundColor = new Color(.055f,.055f,.05f);
        var sun = new GameObject("Soft overhead fill").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.transform.rotation = Quaternion.Euler(62,-30,0);
        sun.color = new Color(.70f,.78f,.85f);
        sun.intensity = 1.2f;
        sun.shadows = LightShadows.Soft;
        var volume = new GameObject("Atmosphere").AddComponent<Volume>();
        volume.isGlobal = true;
        string path = Art + "/CampaignAtmosphere.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (!profile)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile,path);
            var tonemap = profile.Add<Tonemapping>(); tonemap.mode.Override(TonemappingMode.ACES);
            var color = profile.Add<ColorAdjustments>(); color.postExposure.Override(.35f); color.saturation.Override(-12); color.contrast.Override(12);
            var vignette = profile.Add<Vignette>(); vignette.intensity.Override(.23f); vignette.smoothness.Override(.5f);
            var bloom = profile.Add<Bloom>(); bloom.intensity.Override(.12f); bloom.threshold.Override(1.2f);
            foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component,profile);
        }
        volume.sharedProfile = profile;
    }

    static void ValidateNavigation(CampaignDirector director)
    {
        if (!NavMesh.SamplePosition(Vector3.zero,out NavMeshHit start,2,NavMesh.AllAreas))
            throw new InvalidOperationException("No navigation at the player entrance.");
        foreach (Transform point in points)
        {
            var path = new NavMeshPath();
            if (!NavMesh.SamplePosition(point.position,out NavMeshHit target,1.5f,NavMesh.AllAreas)
                || !NavMesh.CalculatePath(start.position,target.position,NavMesh.AllAreas,path)
                || path.status != NavMeshPathStatus.PathComplete)
                throw new InvalidOperationException($"Mission {director.mission+1}: unreachable spawn {point.position}");
        }
        if (points.Count < CampaignSession.EnemyCount(9) + CampaignSession.SurvivorCount(9))
            throw new InvalidOperationException("Insufficient population points.");
    }
}
#endif
