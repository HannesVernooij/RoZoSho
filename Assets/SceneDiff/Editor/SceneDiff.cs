/*
 *      Contents:
 *      
 *			RunDump variables
 *			Project info variables
 *			Trigger variables
 *			Gui selection variables
 * 
 *          OnMenuItem
 *          OnGUI
 *				ReloadProjectInfo			-	Updates "Project info variables" when settings or active scene change.
 *				AddButtonRow
 *          Update
 *				ProcessTriggers
 *          
 *          RunDump
 *				EnqueueABObjects
 *				EnqueueRoots
 *				EnqueuePrefabRoots
 *				EnqueueObjectsByPath
 *				EnqueueObjectAndChildren
 *				EnqueueUnknownFiles
 *				DeleteUnknownFiles
 *				CreatePendingFolders
 *				WritePending
 *				WriteGameObject
 *				WriteComponent
 *				WriteField
 *				WriteProperty
 *				WritePropertyMaterial
 *				SkipMembers
 *				ContainsFile
 *          
 *          RunSvnCheck
 *          RunDiff
 *          
 *          SceneDiffSettings
 *              LoadSettings
 *              SaveSettings
 *              
 *          Utils
 *              ParseExe
 *              RunExeHidden
 *              RunExeVisible
 *              GetObjPath
 *              GetObjPath2
 *              GetObjPath3
 *              GetValueStr
 *              CreateTexture
 *
 * 
 *      Author:
 *      
 *          HF Games Oy 2013
 *          http://www.hf-games.com/
 *          aare.pikaro@gmail.com
 *          
 *      Usage:
 *      
 *          Utility window can be opened from "Window->Scene Diff" menu.
 *          For step-by-step tutorial see: http://www.hf-games.com/scene-diff/
 *          
 */


using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using StreamWriter = System.IO.StreamWriter;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;
using IntPtr = System.IntPtr;
#pragma warning disable 0618
#pragma warning disable 0162
#pragma warning disable 0219
#pragma warning disable 0642
#pragma warning disable 0649
#pragma warning disable 0414

public class SceneDiff : EditorWindow
{

    // RunDump variables:
	public class PendingWrite
	{
		public GameObject pendingObject;
		public List<Component> pendingComponents;
		public string lineIndent;
		public string targetFile;

		public PendingWrite(GameObject obj, string indent, List<Component> components, string file)
		{
			pendingObject = obj;
			lineIndent = indent;
			pendingComponents = components;
			targetFile = file;
		}
	}
	List<PendingWrite> pendingWrites = new List<PendingWrite>();
	Dictionary<string,bool> writtenMaterials = new Dictionary<string,bool>();
	public delegate void AddLineDelegate(string line);

    
    
    // Project info variables:
    static bool isReady = false;
    string lastScene;
    List<string> allScenes = new List<string>();
    List<string> allPrefabs = new List<string>();
    List<string> visibleScenes = new List<string>();
    int[] textAreaHeights;
    float labelHeight;
    string[][] skipValues;
	string skipCommonShortcuts = 
		"transform,rigidbody,rigidbody2D,camera,"+
		"light,animation,constantForce,renderer,audio,guiText,"+
		"networkView,guiElement,guiTexture,collider,collider2D,"+
		"hingeJoint,particleEmitter,particleSystem,active,gameObject";
	string[] skipCommonShortcutsList = null;

    // Trigger variables:
    Queue<string> doDumpScene = new Queue<string>();
    Queue<string> doDumpObjects = new Queue<string>();
    Queue<string> doDumpTo = new Queue<string>();
    bool doDumpDiff = false;
    string doShowFile;
    string doShowFolder;

	// Gui selection variables:
    UnityEngine.Object customDiffObject1; 
    UnityEngine.Object customDiffObject2;
    Vector2 sceneDiffScroll;
    SceneDiffSettings sceneDiffSettings = new SceneDiffSettings();
    SceneDiffSettings sceneDiffSettingsLast = new SceneDiffSettings();
    





    [MenuItem("Window/Scene Diff")]
    public static void OnMenuItem()
    {
        EditorWindow wnd = EditorWindow.GetWindow(typeof(SceneDiff));
        isReady = false;
		wnd.Show();
        wnd.Focus();
    }









    public void OnGUI()
    {
        ReloadProjectInfo();


        sceneDiffScroll = EditorGUILayout.BeginScrollView(sceneDiffScroll);

        if (Event.current.type == EventType.Repaint)
            labelHeight = GUI.skin.label.CalcSize(new GUIContent("ABC")).y;

        int origLabelSize = GUI.skin.label.fontSize;
        GUI.skin.label.fontStyle = FontStyle.Bold;
        GUI.skin.label.fontSize = 18;
        GUILayout.Label(string.Format("Compare assets:"), GUILayout.Width(250));
        GUI.skin.label.fontSize = origLabelSize;
        GUI.skin.label.fontStyle = FontStyle.Normal;
        GUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(30);
        GUILayout.Label("Commands:", GUILayout.Width(200));
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(60);
        if (GUILayout.Button(new GUIContent("Diff", "Drag&drop items to compare into A/B-fields. Can compare whole scenes, individual scene subtrees, and prefabs. "), GUILayout.Width(200), GUILayout.Height(40)))
        {
            EnqueueABObjects();
        }
        GUILayout.Space(5);
		GUI.enabled = sceneDiffSettings.writeIndividualFiles;
        if (GUILayout.Button(new GUIContent("Diff type: " + (sceneDiffSettings.comparisonTypeAB && sceneDiffSettings.writeIndividualFiles ? "Files" : "Single"), "Controls the way 'Diff'-buttons work. Diff-type 'Files' exports each GameObject to separate txt-files, 'Single' exports all GameObjects to the same txt-file."), GUILayout.Width(200), GUILayout.Height(40)))
        {
            sceneDiffSettings.comparisonTypeAB = !sceneDiffSettings.comparisonTypeAB;
        }
		GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(30);
        GUILayout.Label("Setup:", GUILayout.Width(200));
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(5);
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(60);
        GUILayout.Label(new GUIContent("A: ", "Drag&drop items to compare into A/B-fields. Can compare whole scenes, individual scene subtrees, and prefabs. "), GUILayout.Width(20));
        customDiffObject1 = EditorGUILayout.ObjectField(customDiffObject1, typeof(UnityEngine.Object), GUILayout.Width(200));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(60);
        GUILayout.Label(new GUIContent("B: ", "Drag&drop items to compare into A/B-fields. Can compare whole scenes, individual scene subtrees, and prefabs. "), GUILayout.Width(20));
        customDiffObject2 = EditorGUILayout.ObjectField(customDiffObject2, typeof(UnityEngine.Object), GUILayout.Width(200));
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(20);





        GUI.skin.label.fontStyle = FontStyle.Bold;
        GUI.skin.label.fontSize = 18;
        GUILayout.Label(string.Format("Resolve conflicts:"), GUILayout.Width(200));
        GUI.skin.label.fontSize = origLabelSize;
        GUI.skin.label.fontStyle = FontStyle.Normal;
        GUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(30);
        GUILayout.Label("Commands:", GUILayout.Width(200));
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(60);
        if (GUILayout.Button(new GUIContent("SVN Check Scenes", "Check if there are scenes whose changes conflict with svn-repository. Check if same scene was also modified by other user."), GUILayout.Width(200), GUILayout.Height(40)))
        {
            RunSvnCheck(true);
        }
        GUILayout.Space(5);
        if (GUILayout.Button(new GUIContent("SVN Check Prefabs", "Check if there are prefabs whose changes conflict with svn-repository. Check if same prefab was also modified by other user."), GUILayout.Width(200), GUILayout.Height(40)))
        {
            RunSvnCheck(false);
        }
        GUILayout.Space(5);
        if (GUILayout.Button(new GUIContent("Show Project", "Open unity project in windows explorer."), GUILayout.Width(200), GUILayout.Height(40)))
        {
            doShowFolder = ".";
        }
        GUILayout.Space(5);
		GUI.enabled = sceneDiffSettings.writeIndividualFiles;
        if (GUILayout.Button(
			new GUIContent(
				"Diff type: " + (sceneDiffSettings.comparisonTypeNormal && sceneDiffSettings.writeIndividualFiles ? "Files" : "Single"), 
				"Controls the way 'Diff'-buttons work. Diff-type 'Files' exports each GameObject to separate txt-files, 'Single' exports all GameObjects to the same txt-file."), 
			GUILayout.Width(200), 
			GUILayout.Height(40)))
        {
            sceneDiffSettings.comparisonTypeNormal = !sceneDiffSettings.comparisonTypeNormal;
        }
		GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
        

        int sceneIndex = -1;
        foreach (var sceneFull in visibleScenes)
        {
            sceneIndex++;
            string scene = System.IO.Path.GetFileNameWithoutExtension(sceneFull);
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(30);
            string sceneLabel = string.Format("Scene #{0}: {1}", sceneIndex + 1, sceneFull);
            Vector2 siz = GUI.skin.label.CalcSize(new GUIContent(sceneLabel));
            GUILayout.Label(sceneLabel, GUILayout.Width(siz.x + 10));
            EditorGUILayout.EndHorizontal();
            AddButtonRow(sceneFull, scene, 0, 250);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(60);
            if (GUILayout.Button(string.Format("Show '{0}'", scene), GUILayout.Width(250), GUILayout.Height(50)))
            {
                doShowFile = sceneFull;
            }
            EditorGUILayout.EndHorizontal();
        }

        GUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(30);
        GUILayout.Label(string.Format("Prefabs:"), 
            GUILayout.Width(200));
        EditorGUILayout.EndHorizontal();

        AddButtonRow("", "PREFABS", 1, 250);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(60);
        if (GUILayout.Button(string.Format("Show Assets"), GUILayout.Width(250), GUILayout.Height(50)))
        {
            doShowFolder = "Assets";
        }
        EditorGUILayout.EndHorizontal();


        
        
        
        GUILayout.Space(10);
        GUI.skin.label.fontStyle = FontStyle.Bold;
        GUI.skin.label.fontSize = 18;
        GUILayout.Label(string.Format("Skip settings:"), GUILayout.Width(200));
        GUI.skin.label.fontSize = origLabelSize;
        GUI.skin.label.fontStyle = FontStyle.Normal;
        GUILayout.Space(10);
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(30);
        GUILayout.Label(new GUIContent("Skip components:", "Don't write member values for these components. Comma/newline separated list."), GUILayout.Width(190));
        sceneDiffSettings.skipComponents = EditorGUILayout.TextArea(sceneDiffSettings.skipComponents, GUILayout.Width(400), GUILayout.Height(textAreaHeights[0]));
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(30);
        GUILayout.Label(new GUIContent("Skip components by substring:", "Don't write member values for components whose names contain these strings. Comma/newline separated list."), GUILayout.Width(190));
        sceneDiffSettings.skipComponentsWith = EditorGUILayout.TextArea(sceneDiffSettings.skipComponentsWith, GUILayout.Width(400), GUILayout.Height(textAreaHeights[1]));
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(30);
        GUILayout.Label(new GUIContent("Skip members:", "Don't write values of these member-variables. Comma/newline separated list."), GUILayout.Width(190));
        sceneDiffSettings.skipProperties = EditorGUILayout.TextArea(sceneDiffSettings.skipProperties, GUILayout.Width(400), GUILayout.Height(textAreaHeights[2]));
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(30);
        GUILayout.Label(new GUIContent("Skip members by substring:", "Don't write member values for components whose name contain these strings. Comma/newline separated list."), GUILayout.Width(190));
        sceneDiffSettings.skipPropertiesWith = EditorGUILayout.TextArea(sceneDiffSettings.skipPropertiesWith, GUILayout.Width(400), GUILayout.Height(textAreaHeights[3]));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(30);
        GUILayout.Label(new GUIContent("Skip property if has 'shared*':", "Don't write such members as 'Renderer.material', but write their counterpart such as 'Renderer.sharedMaterial'."), GUILayout.Width(190));
        sceneDiffSettings.skipPropertyIfHasShared = GUILayout.Toggle(sceneDiffSettings.skipPropertyIfHasShared, "", GUILayout.Width(200));
        EditorGUILayout.EndHorizontal();


        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(30);
        GUILayout.Label(new GUIContent("Default settings", "Reset settings."), GUILayout.Width(190));
        if (GUILayout.Button("Reset", GUILayout.Width(50)))
        {
            if (EditorUtility.DisplayDialog("Confirm", "Reset skip-related settings to default?", "Reset", "Cancel"))
            {
                sceneDiffSettings.Reset1();
            }
        }
        EditorGUILayout.EndHorizontal();







        GUILayout.Space(10);
        GUI.skin.label.fontStyle = FontStyle.Bold;
        GUI.skin.label.fontSize = 18;
        GUILayout.Label("Other settings:", GUILayout.Width(200));
        GUI.skin.label.fontSize = origLabelSize;
        GUI.skin.label.fontStyle = FontStyle.Normal;
        GUILayout.Space(10);


        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(30);
        GUILayout.Label(new GUIContent("Prefix properties with:", "Distinguish between normal fields and properties by adding a prefix to property-members."), GUILayout.Width(190));
        sceneDiffSettings.propertiesPrefix = EditorGUILayout.TextField(sceneDiffSettings.propertiesPrefix, GUILayout.Width(400));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(30);
        GUILayout.Label(new GUIContent("Diff tool command:", "Comparer-tool for comparing folders and files. Use 'WinMerge' if in doupt."), GUILayout.Width(190));
        sceneDiffSettings.diffCommand = EditorGUILayout.TextField(sceneDiffSettings.diffCommand, GUILayout.Width(400));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(30);
        GUILayout.Label(new GUIContent("Svn version command 1:", "Command for getting local version numbers. Tested with TortoiseSVN."), GUILayout.Width(190));
        sceneDiffSettings.svnCommand1 = EditorGUILayout.TextField(sceneDiffSettings.svnCommand1, GUILayout.Width(400));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(30);
        GUILayout.Label(new GUIContent("Svn version command 2:", "Command for getting head-version numbers. Tested with TortoiseSVN."), GUILayout.Width(190));
        sceneDiffSettings.svnCommand2 = EditorGUILayout.TextField(sceneDiffSettings.svnCommand2, GUILayout.Width(400));
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(30);
        GUILayout.Label(new GUIContent("Show all scenes:","Show dump/diff-buttons for all available scenes. Otherwise use only scenes configured in build settings."), GUILayout.Width(190));
        sceneDiffSettings.showAllScenes = GUILayout.Toggle(sceneDiffSettings.showAllScenes, "", GUILayout.Width(200));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(30);
        GUILayout.Label(new GUIContent("Write individual files:","Create both a single txt and individual txts for individual GameObjects. Disable this if single txt is enought."), GUILayout.Width(190));
        sceneDiffSettings.writeIndividualFiles = GUILayout.Toggle(sceneDiffSettings.writeIndividualFiles, "", GUILayout.Width(200));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(30);
        GUILayout.Label(new GUIContent("Default settings", "Reset settings."), GUILayout.Width(190));
        if (GUILayout.Button("Reset", GUILayout.Width(50)))
        {
            if (EditorUtility.DisplayDialog("Confirm", "Reset settings to default?", "Reset", "Cancel"))
            {
                sceneDiffSettings.Reset2();
            }
        }
        EditorGUILayout.EndHorizontal();


        GUILayout.Space(10);
        GUI.skin.label.fontStyle = FontStyle.Bold;
        GUI.skin.label.fontSize = 18;
        GUILayout.Label(string.Format("Help:"), GUILayout.Width(200));
        GUI.skin.label.fontSize = origLabelSize;
        GUI.skin.label.fontStyle = FontStyle.Normal;





        GUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(60);
        if (GUILayout.Button(new GUIContent("Help", "Open support website. From there you can make feature requests, or submit bugfixes to the source code."), GUILayout.Width(200), GUILayout.Height(40)))
        {
            System.Diagnostics.Process.Start("http://www.hf-games.com/scene-diff/");
        }
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(10);

        GUILayout.Space(30);



        EditorGUILayout.EndScrollView();

        if (sceneDiffSettings.SaveSettings(ref sceneDiffSettingsLast))
            isReady = false;

    }


    void ReloadProjectInfo()
    {
        if (EditorApplication.currentScene != lastScene)
        {
            isReady = false;
            lastScene = EditorApplication.currentScene;
        }

        if (EditorPrefs.GetString("SceneDiff_settingsStore", "") == "")
        {
            EditorPrefs.SetString("SceneDiff_settingsStore", "true");
            sceneDiffSettings.Reset1();
            sceneDiffSettings.Reset2();
            sceneDiffSettings.SaveSettings(ref sceneDiffSettingsLast);
            isReady = false;
        }

        if (!isReady)
        {
            isReady = true;
            sceneDiffSettings.LoadSettings();
            sceneDiffSettingsLast = sceneDiffSettings;
            if (sceneDiffSettings.SaveSettings(ref sceneDiffSettingsLast))
                Debug.LogError("SceneDiff: Internal error 1");

            labelHeight = (int)GUI.skin.label.CalcSize(new GUIContent("ABC")).y;
            int labelHeight2 = (int)labelHeight;
            textAreaHeights = new int[]
            {
                labelHeight2 * sceneDiffSettings.skipComponents.Split(new char[]{'\r','\n'}).Length,
                labelHeight2 * sceneDiffSettings.skipComponentsWith.Split(new char[]{'\r','\n'}).Length,
                labelHeight2 * sceneDiffSettings.skipProperties.Split(new char[]{'\r','\n'}).Length,
                labelHeight2 * sceneDiffSettings.skipPropertiesWith.Split(new char[]{'\r','\n'}).Length,
            };


            string[] allPaths = AssetDatabase.GetAllAssetPaths();
            allScenes.Clear();
            allPrefabs.Clear();
            visibleScenes.Clear();
            
            foreach (var pth in allPaths)
            {
                if (!string.IsNullOrEmpty(pth))
                    if (pth.EndsWith(".unity"))
                    {
                        allScenes.Add(pth);
                    }
                    else if (pth.EndsWith(".prefab"))
                    {
                        allPrefabs.Add(pth);
                    }
            }
            allPrefabs.Sort();

            //Debug.Log("SceneDiff: found scenes: " + allScenes.Count + ", " + allScenes[0]);
            if (sceneDiffSettings.showAllScenes)
                visibleScenes.AddRange(allScenes);
            else
            {
                foreach (var sceneFull in EditorBuildSettings.scenes)
                    visibleScenes.Add(sceneFull.path);
                if (!visibleScenes.Contains(EditorApplication.currentScene))
                    if (EditorApplication.currentScene != "")
                        visibleScenes.Add(EditorApplication.currentScene);
            }

			skipValues = new string[][]
			{
				sceneDiffSettings.skipProperties.Split(new char[]{',',';',' ','\r','\n'}, System.StringSplitOptions.RemoveEmptyEntries),
				sceneDiffSettings.skipPropertiesWith.Split(new char[]{',',';',' ','\r','\n'}, System.StringSplitOptions.RemoveEmptyEntries),
				sceneDiffSettings.skipComponents.Split(new char[]{',',';',' ','\r','\n'}, System.StringSplitOptions.RemoveEmptyEntries),
				sceneDiffSettings.skipComponentsWith.Split(new char[]{',',';',' ','\r','\n'}, System.StringSplitOptions.RemoveEmptyEntries)
			};

			skipCommonShortcutsList  = skipCommonShortcuts.Split(',');
        }

    }


    void AddButtonRow(string sceneFull, string scene, int type, int bigButtonWidth)
    {
        int totalH = 100;
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(60);
        GUILayout.BeginVertical(GUILayout.Width(bigButtonWidth));
        GUILayout.Space(totalH / 2 - (50 / 2));
        if (GUILayout.Button(string.Format("Dump '{0}-LOCAL'", scene), GUILayout.Width(bigButtonWidth), GUILayout.Height(totalH / 2)))
        {
            doDumpScene.Enqueue(sceneFull);
            doDumpTo.Enqueue(scene + "-LOCAL");
            doDumpObjects.Enqueue("");
        }
        GUILayout.EndVertical();


        Rect part1 = GUILayoutUtility.GetLastRect();

        GUILayout.Space(10);

        GUILayout.BeginVertical(GUILayout.Width(50));
        GUILayout.Space(totalH / 4 - 25 / 2);
        if (GUILayout.Button(new GUIContent("Diff", "Compare LOCAL with LATEST."), GUILayout.Width(50), GUILayout.Height(25)))
        {
            RunDiff(scene, "-LOCAL", "-LATEST", type);
        }

        GUILayout.Space(-25 / 2 + totalH / 2 - 25 / 2 - 4);
        if (GUILayout.Button(new GUIContent("Diff", "Compare LOCAL with OTHER."), GUILayout.Width(50), GUILayout.Height(25)))
        {
            RunDiff(scene, "-LOCAL", "-OTHER", type);
        }
        GUILayout.EndVertical();

        Rect part2 = GUILayoutUtility.GetLastRect();
        GUILayout.Space(10);




        GUILayout.BeginVertical(GUILayout.Width(bigButtonWidth));
        if (GUILayout.Button(string.Format("Dump '{0}-LATEST'", scene), GUILayout.Width(bigButtonWidth), GUILayout.Height(totalH / 2)))
        {
            doDumpScene.Enqueue(sceneFull);
            doDumpTo.Enqueue(scene + "-LATEST");
            doDumpObjects.Enqueue("");
        }
        if (GUILayout.Button(string.Format("Dump '{0}-OTHER'", scene), GUILayout.Width(bigButtonWidth), GUILayout.Height(totalH / 2)))
        {
            doDumpScene.Enqueue(sceneFull);
            doDumpTo.Enqueue(scene + "-OTHER");
            doDumpObjects.Enqueue("");
        }
        GUILayout.EndVertical();

        Rect part3 = GUILayoutUtility.GetLastRect();
        GUILayout.Space(10);
        GUILayout.BeginVertical(GUILayout.Width(50));
        GUILayout.Space(totalH / 2 - 25 / 2);
        if (GUILayout.Button(new GUIContent("Diff", "Compare LATEST with OTHER."), GUILayout.Width(50), GUILayout.Height(25)))
        {
            RunDiff(scene, "-LATEST", "-OTHER", type);
        }
        GUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();

        if (Event.current.type == EventType.Repaint)
        {
            GUI.skin.label.fontSize = 16;
            float h2 = labelHeight / 2;
            GUI.Label(new Rect(part1.xMax, part1.y + totalH / 4 - h2 + h2 / 2, 40, 40), "/");
            GUI.Label(new Rect(part1.xMax, part1.y + totalH - totalH / 4 - h2 - h2 / 2 - 4, 40, 40), "\\");
            GUI.Label(new Rect(part2.xMax, part1.y + totalH / 4 - h2, 40, 40), "-");
            GUI.Label(new Rect(part2.xMax, part1.y + totalH - totalH / 4 - h2, 40, 40), "-");
            GUI.Label(new Rect(part3.xMax, part1.y + totalH / 4 - h2 + h2 / 2, 40, 40), "\\");
            GUI.Label(new Rect(part3.xMax, part1.y + totalH - totalH / 4 - h2 - h2 / 2 - 4, 40, 40), "/");
            GUI.skin.label.fontSize = 0;
        }
    }

    void Update()
    {
		ProcessTriggers();
    }

	void ProcessTriggers()
	{
		while (doDumpTo.Count != 0)
		{
			RunDump();
		}
		if (doDumpDiff)
		{
			doDumpDiff = false;
			RunDiff("", "CUSTOM-A", "CUSTOM-B", 2);
		}

		if (!string.IsNullOrEmpty(doShowFile))
		{
			string file = doShowFile;
			doShowFile = "";
			Utils.ShowFile(file);
		}

		if (!string.IsNullOrEmpty(doShowFolder))
		{
			string file = doShowFolder;
			doShowFolder = "";
			System.Diagnostics.Process.Start(file);
		}
	}
    















    public void RunDump()
    {
        isReady = false;
        string doDumpScene_ = doDumpScene.Dequeue();
        string doDumpTo_ = doDumpTo.Dequeue();
        string doDumpObjects_ = doDumpObjects.Dequeue();
        if (doDumpObjects_ != "")
        { 
        
        }
        else if (doDumpScene_ == "")
        { 
        
        }
        else if (EditorApplication.currentScene != doDumpScene_)
        {
            Debug.Log("SceneDiff: Loading scene: " + doDumpScene_ + ", " + EditorApplication.currentScene);
            EditorApplication.SaveCurrentSceneIfUserWantsTo();
            EditorApplication.OpenScene(doDumpScene_);
        }

        string projectpath = System.IO.Path.GetDirectoryName(Application.dataPath).Replace('\\', '/');
        string topfolder = projectpath + "/Dump";
        string mainfolder = projectpath + "/Dump/" + doDumpTo_;
        string scenefull = projectpath + "/Dump/" + doDumpTo_ + ".txt";
        
        if (!System.IO.Directory.Exists(topfolder))
            System.IO.Directory.CreateDirectory(topfolder);
		if (sceneDiffSettings.writeIndividualFiles)
		{
			if (!System.IO.Directory.Exists(mainfolder))
				System.IO.Directory.CreateDirectory(mainfolder);
		}
		else
		{
			if (System.IO.Directory.Exists(mainfolder))
				System.IO.Directory.Delete(mainfolder);
		}

		pendingWrites.Clear();
        writtenMaterials.Clear();
        
        pendingWrites.Add(new PendingWrite(null, null, null, mainfolder));

        if (doDumpObjects_ != "")
            EnqueueObjectsByPath(doDumpObjects_, mainfolder, scenefull);
        else if (doDumpScene_ == "")
            EnqueuePrefabRoots(allPrefabs, mainfolder, scenefull);
        else
            EnqueueRoots(mainfolder, scenefull);
        
        List<string> unknownFiles = new List<string>();
		List<string> unknownFolders = new List<string>();
		if (sceneDiffSettings.writeIndividualFiles)
			EnqueueUnknownFiles(mainfolder, unknownFiles, unknownFolders);

		if (sceneDiffSettings.writeIndividualFiles)
			CreatePendingFolders();
        
        using (StreamWriter fullWriter = new StreamWriter(scenefull, false, System.Text.Encoding.UTF8))
        {
			foreach (PendingWrite pending in pendingWrites)
				if (pending.pendingObject != null)
					WritePending(a => fullWriter.WriteLine(a), pending);
        }

		if (sceneDiffSettings.writeIndividualFiles)
			DeleteUnknownFiles(unknownFiles, unknownFolders);

	
		if (sceneDiffSettings.writeIndividualFiles)
		{
			int numBroken = 0;
			foreach (PendingWrite pending in pendingWrites)
				if (pending.targetFile.Length >= (pending.pendingObject != null ? 260 : 245))
					numBroken++;
			if (numBroken != 0)
				Debug.LogWarning("SceneDiff: Failed to write " + numBroken + " files/folders, because path was too long!");
		}

        Debug.Log("SceneDiff: " + (doDumpObjects_ != "" ? "Object" : doDumpScene_ == "" ? "Prefab" : "Scene") + " dump complete! Full file: " + doDumpTo_ + ".txt, Folder: " + mainfolder);
    }






    void EnqueueABObjects()
    {
        string path = AssetDatabase.GetAssetPath(customDiffObject1);
        string path2 = AssetDatabase.GetAssetPath(customDiffObject2);
        string pathb = Utils.GetObjPath(customDiffObject1);
        string pathb2 = Utils.GetObjPath(customDiffObject2);
        //Debug.Log("SceneDiff: Scheduling update " + path + ", " + path2 + ", " + pathb + ", " + pathb2);
        doDumpDiff = true;

        if (path != null && path.EndsWith(".unity"))
        {
            doDumpScene.Enqueue(path);
            doDumpTo.Enqueue("CUSTOM-A");
            doDumpObjects.Enqueue("");
        }
        else if (!string.IsNullOrEmpty(pathb))
        {
            doDumpScene.Enqueue("");
            doDumpObjects.Enqueue(pathb);
            doDumpTo.Enqueue("CUSTOM-A");
        }
        else if (customDiffObject1 == null)
        {
            doDumpDiff = false;
            Debug.LogError("SceneDiff: No object A specified.");
        }
        else if (string.IsNullOrEmpty(pathb))
        {
            doDumpDiff = false;
            Debug.LogError("SceneDiff: Failed to get path of object A: " + customDiffObject1);
        }

        if (path2 != null && path2.EndsWith(".unity"))
        {
            doDumpScene.Enqueue(path2);
            doDumpTo.Enqueue("CUSTOM-B");
            doDumpObjects.Enqueue("");
        }
        else if (!string.IsNullOrEmpty(pathb2))
        {
            doDumpScene.Enqueue("");
            doDumpObjects.Enqueue(pathb2);
            doDumpTo.Enqueue("CUSTOM-B");
        }
        else if (customDiffObject2 == null)
        {
            doDumpDiff = false;
            Debug.LogError("SceneDiff: No object B specified.");
        }
        else if (string.IsNullOrEmpty(pathb2))
        {
            doDumpDiff = false;
            Debug.LogError("SceneDiff: Failed to get path of object B: " + customDiffObject2);
        }
    }

    void EnqueueRoots(string mainfolder, string scenefull)
    {
        UnityEngine.Object[] all1 =
            GameObject.FindSceneObjectsOfType(typeof(GameObject));
        List<GameObject> all2 = new List<GameObject>();
        foreach (Object obj in all1)
        {
            if ((obj as GameObject).transform.parent == null) 
                all2.Add(obj as GameObject);
        }

        all2.Sort((a, b) => a.name.CompareTo(b.name));

        foreach (GameObject obj in all2)
        {
            EnqueueObjectAndChildren(obj as GameObject, mainfolder, "");
        }
    }

    void EnqueuePrefabRoots(List<string> paths, string mainfolder, string scenefull)
    {
        foreach (string path in paths)
        {
            GameObject obj = 
                AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject;
            string path1 = path;
            if (path1.StartsWith("Assets/"))
                path1 = path1.Substring("Assets/".Length);
            if (path1.StartsWith("/"))
                path1 = path1.Substring("/".Length);
            //if (path1.StartsWith("Prefabs/Resources/Blocks/Candyland"))
              //  continue;
            if (obj == null)
                continue;
            // Note: path2-directory is created for root prefab-objects. Then additional folders are created for children.
            string path2 = System.IO.Path.GetDirectoryName(mainfolder + "/" + path1);
            // Example: 
            //      - for prefab "c:\projectx\dump\prefabs\other\test1.prefab"
            //      - add path "c:\projectx\dump\prefabs\other"
            //      - add path "c:\projectx\dump\prefabs"
            //      - break on path "c:\projectx\dump" (was added before)
            // 
            string path3 = path2;
            while (!ContainsFile(path3))
            {
				pendingWrites.Add(new PendingWrite(null, null, null, path3));
                path3 = System.IO.Path.GetDirectoryName(path3);
            }
            EnqueueObjectAndChildren(obj, path2, "");
        }
    }

    void EnqueueObjectsByPath(string path, string mainfolder, string scenefull)
    {
        GameObject obj =
            path.StartsWith("/") ?
                GameObject.Find(path) :
            AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject;
        EnqueueObjectAndChildren(obj, mainfolder, "");
    }

    /*
	List<KeyValuePair<string, Transform>> prefabLinks = new List<KeyValuePair<string, Transform>>();
	void EnqueuePrefabLink(Component component, object val3)
    {
        if (val3 is Transform &&
            !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(val3 as Object)))
            prefabLinks.Add(new KeyValuePair<string, Transform>((Utils.GetPath(component.gameObject).Substring(1).Replace("\\", ".").Replace("/", ".") + "." + component.GetType().Name), val3 as Transform));
        if (val3 is GameObject &&
            !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(val3 as Object)))
            prefabLinks.Add(new KeyValuePair<string, Transform>((Utils.GetPath(component.gameObject).Substring(1).Replace("\\", ".").Replace("/", ".") + "." + component.GetType().Name), (val3 as GameObject).transform));
    }*/

    /*
    void EnqueueUsedPrefabs(string prefabfolder)
    {
        foreach (KeyValuePair<string, Transform> pref in prefabLinks)
        {
            string fold0 = prefabfolder + "/" + pref.Key + ".#1";
            string fold = prefabfolder + "/" + pref.Key + ".#1." + pref.Value.gameObject;
            int extraCount = 2;
            while (ContainsFile(fold0))
            {
                fold0 = prefabfolder + "/" + pref.Key + ".#" + (extraCount);
                fold = prefabfolder + "/" + pref.Key + ".#" + (extraCount) + "." + pref.Value.gameObject;
                extraCount++;
            }

			pendingWrites.Add(new PendingWrite(null, null, null, fold0));
            pendingWrites.Add(new PendingWrite(null, null, null, fold));

            EnqueueObjectAndChildren(pref.Value.gameObject, fold, "  ");
            //DumpGameObject(pref.Value.gameObject, fullWriter2, fold, "", true);
        }
    }
    */

    public void EnqueueObjectAndChildren(GameObject gameObject, string folder, string indent)
    {
        if (folder.EndsWith(" "))
            Debug.LogError("SceneDiff: Please remove space at the end of: '" + folder + "'", gameObject);
        string subfolder = folder + "/" + gameObject.name;
        int extraCount = 2;

        while (ContainsFile(subfolder + ".txt"))
            subfolder = folder + "/" + gameObject.name + "_#" + (extraCount++);

        pendingWrites.Add(new PendingWrite(gameObject, indent, new List<Component>(), subfolder + ".txt"));

        Component[] comp = gameObject.GetComponents<Component>();
        if (comp != null)
            System.Array.Sort(comp, (a, b) => a == null ? 1 : b == null ? -1 : a.GetType().Name.CompareTo(b.GetType().Name));

        pendingWrites[pendingWrites.Count - 1].pendingComponents.AddRange(comp);

        List<GameObject> children = new List<GameObject>();
        foreach (Transform child in gameObject.transform)
            children.Add(child.gameObject);
        children.Sort((a, b) => a.name.CompareTo(b.name));

        if (children.Count != 0)
        {
			pendingWrites.Add(new PendingWrite(null, null, null, subfolder));

            foreach (GameObject obj in children)
            {
                EnqueueObjectAndChildren(obj.gameObject, subfolder, indent + "  ");
            }
        }
    }

    void EnqueueUnknownFiles(string folder, List<string> unknownFiles, List<string> unknownFolders)
    {
        string[] allFiles = System.IO.Directory.GetFiles(folder);
        foreach (string file in allFiles)
        {
            string file2 = file.Replace('\\', '/');
            if (!ContainsFile(file2))
            {
                unknownFiles.Add(file2);
            }
        }

        string[] allFolders = System.IO.Directory.GetDirectories(folder);
        foreach (string dir in allFolders)
        {
            string dir2 = dir.Replace('\\', '/');
            if (!ContainsFile(dir2))
                unknownFolders.Add(dir2);

            EnqueueUnknownFiles(dir2, unknownFiles, unknownFolders);
        }
    }

	static void DeleteUnknownFiles(List<string> unknownFiles, List<string> unknownFolders)
	{
		foreach (string f in unknownFiles)
		{
			//Debug.Log("SceneDiff: Deleting old file: " + f);
			System.IO.File.Delete(f);
		}
		unknownFolders.Reverse();
		foreach (string d in unknownFolders)
		{
			//Debug.Log("SceneDiff: Deleting old folder: " + d);
			new System.IO.DirectoryInfo(d).Delete();
		}
	}

    public void CreatePendingFolders()
    {
		foreach (PendingWrite pendingWrite in pendingWrites)
			if (pendingWrite.pendingObject == null)
				if (pendingWrite.targetFile.Length < 245)
					if (!System.IO.Directory.Exists(pendingWrite.targetFile))
						System.IO.Directory.CreateDirectory(pendingWrite.targetFile);
	}

    public void WritePending(AddLineDelegate fullWriter, PendingWrite pendingWrite)
    {
		using (StreamWriter objectWriter_ = sceneDiffSettings.writeIndividualFiles && pendingWrite.targetFile.Length < 260 ? new StreamWriter(pendingWrite.targetFile, false, System.Text.Encoding.UTF8) : null)
        {
			AddLineDelegate objectWriter = line => { if (objectWriter_ != null) objectWriter_.WriteLine(line); };
			string componentNameIndent = pendingWrite.lineIndent + "  ";
			string componentMemberIndent = pendingWrite.lineIndent + "    ";
			string gameObjectMemberIndent = pendingWrite.lineIndent + "  ";
				
			WriteGameObject(fullWriter, objectWriter, pendingWrite, gameObjectMemberIndent);
				
            foreach (Component comp in pendingWrite.pendingComponents)
            {
                WriteComponent(fullWriter, objectWriter, pendingWrite, comp, componentNameIndent, componentMemberIndent);
            }
        }
    }

    void WriteGameObject(AddLineDelegate fullWriter, AddLineDelegate objectWriter, PendingWrite pendingWrite, string gameObjectMemberIndent)
    {
		GameObject obj = pendingWrite.pendingObject;
		fullWriter(string.Format("{0}GameObject: \"{1}\"", pendingWrite.lineIndent, Utils.GetObjPath(obj)));
		objectWriter(string.Format("GameObject: \"{1}\"", 0, Utils.GetObjPath(obj)));
	        
        FieldInfo[] fields = obj.GetType().GetFields();
        PropertyInfo[] props = obj.GetType().GetProperties();

        foreach (FieldInfo member in fields)
			WriteField(fullWriter, objectWriter, gameObjectMemberIndent, obj, member, props, true);

        foreach (PropertyInfo member in props)
			WriteProperty(fullWriter, objectWriter, gameObjectMemberIndent, obj, member, props, true);

		fullWriter(string.Format("{0}", pendingWrite.lineIndent));
		objectWriter("");
    }

	void WriteComponent(AddLineDelegate fullWriter, AddLineDelegate objectWriter, PendingWrite pendingWrite, Component component, string componentNameIndent, string componentMemberIndent)
	{
		fullWriter(string.Format("{0}Component: \"{1}\" -> {2}",
			componentNameIndent,
			Utils.GetObjPath(pendingWrite.pendingObject),
			(component == null ? "<null-component>" : component.GetType().Name)));
		objectWriter(string.Format("{1}", 0, (component == null ? "<null-component>" : component.GetType().Name)));

		FieldInfo[] fields = component != null ? component.GetType().GetFields() : new FieldInfo[0];
		PropertyInfo[] props = component != null ? component.GetType().GetProperties() : new PropertyInfo[0];

		foreach (FieldInfo member in fields)
			WriteField(fullWriter, objectWriter, componentMemberIndent, component, member, props, true);

		foreach (PropertyInfo member in props)
			WriteProperty(fullWriter, objectWriter, componentMemberIndent, component, member, props, true);


		fullWriter(string.Format("{0}", pendingWrite.lineIndent));
		objectWriter("");
	}

	void WriteField(AddLineDelegate fullWriter, AddLineDelegate objectWriter, string memberIndent, UnityEngine.Object obj, FieldInfo member, PropertyInfo[] props, bool tryToSkip)
	{
		int doSkip = tryToSkip ? SkipMembers(member, obj, props) : 0;

		try
		{
            System.Collections.IList listMember = doSkip == 0 && member.FieldType.IsArray ? member.GetValue(obj) as System.Collections.IList : null;
						
			if (doSkip < 0)
			{
				return;
			}
			//
			// Example: "  material = <skipped>"
			//
			else if (doSkip != 0)
			{
				fullWriter(string.Format("{0}{1} = <skipped/{2}>", memberIndent, member.Name, doSkip));
				objectWriter(string.Format("  {1} = <skipped/{2}>", 0, member.Name, doSkip));
			}
			//
			// Example: "  intList1 = Array(int, <null>)"
			//
			else if (member.FieldType.IsArray &&
				listMember == null)
			{
				fullWriter(string.Format("{0}{1} = Array({2}, <null>)", memberIndent, member.Name, member.FieldType));
				objectWriter(string.Format("  {1} = Array({2}, <null>)", 0, member.Name, member.FieldType));
			}
			//
			// Example1: "  intList1 = Array(int, 12)"
			//           "      0 = 123"
			//           "      1 = 123"
			//           ...
			//
			// Example2: "  objectList1 = Array(MyType, 12)"
			//           "    0 = MyType()"
			//           "      myTypeMember1 = 123
			//           "      myTypeMember2 = test
			//           "    1 = MyType()"
			//           "      myTypeMember1 = 123
			//           "      myTypeMember2 = test
			//           ...
			//
			else if (member.FieldType.IsArray &&
					 listMember != null)
			{
				fullWriter(string.Format("{0}{1} = Array({2}, {3})", memberIndent, member.Name, member.FieldType, listMember.Count));
				objectWriter(string.Format("  {1} = Array({2}, {3})", 0, member.Name, member.FieldType, listMember.Count));

				int jj = 0;
				foreach (var item in listMember)
				{
					fullWriter(string.Format("{0}    {1} => {2}", memberIndent, jj, item != null ? item.ToString() : "<null>"));
					objectWriter(string.Format("    {1} => {2}", 0, jj, item != null ? item.ToString() : "<null>"));

					if (item != null && item.GetType().IsSerializable)
					{
						FieldInfo[] mem3 = item.GetType().GetFields();
						foreach (FieldInfo m3 in mem3)
						{
							string val3 = Utils.GetValueStr(m3.GetValue(item));
							fullWriter(string.Format("{0}      {1} = {2}", memberIndent, m3.Name, val3));
							objectWriter(string.Format("      {1} = {2}", 0, m3.Name, val3));
							//EnqueuePrefabLink(item, val3);
						}
					}

					jj++;
				}
			}
			// Example: "  myMember1 = 123"
			else
			{
				string val = Utils.GetValueStr(member.GetValue(obj));
				fullWriter(string.Format("{0}{1} = {2}", memberIndent, member.Name, val));
				objectWriter(string.Format("  {1} = {2}", 0, member.Name, val));
				//EnqueuePrefabLink(obj, val);
			}
		}
		catch(System.Exception ex)
		{
			// WARNING: some properties throw exceptions. You will have to add them to "Skip properties"-list.
			//
			Debug.LogWarning("SceneDiff: Failed to write field: " + obj.GetType().Name + "." + member.Name + ", " + doSkip + ", " + Utils.GetObjPath(obj) + ", " + ex.Message);
		}
	}

	
	void WriteProperty(AddLineDelegate fullWriter, AddLineDelegate objectWriter, string memberIndent, UnityEngine.Object obj, PropertyInfo member, PropertyInfo[] props, bool tryToSkip)
	{
        int doSkip = tryToSkip ? SkipMembers(member, obj, props) : 0;

		try
		{
            System.Array listMember = doSkip == 0 && member.PropertyType.IsArray ? member.GetValue(obj, new object[0]) as System.Array : null;

			if (doSkip < 0)
			{
				return;
			}
			else if (member.GetGetMethod() == null)
			{

			}
			//
			// Example: "  material = <skipped>"
			//
			else if (doSkip != 0)
			{
				fullWriter(string.Format("{0}{1}{2} = <skipped/{3}>", memberIndent, sceneDiffSettings.propertiesPrefix, member.Name, doSkip));
				objectWriter(string.Format("  {1}{2} = <skipped/{3}>", 0, sceneDiffSettings.propertiesPrefix, member.Name, doSkip));
			}
			//
			// Example: "  intList1 = Array(int, <null>)"
			//
			else if (member.PropertyType.IsArray &&
					listMember == null)
			{
				fullWriter(string.Format("{0}{1}{2} = Array({3}, <null>)", memberIndent, sceneDiffSettings.propertiesPrefix, member.Name, member.PropertyType));
				objectWriter(string.Format("  {1}{2} = Array({3}, <null>)", 0, sceneDiffSettings.propertiesPrefix, member.Name, member.PropertyType));
			}
			//
			// Example: "  intList1 = Array(int, 12)"
			//          "      0 = 123"
			//          "      1 = 123"
			//          ...
			//
			else if (member.PropertyType.IsArray)
			{
				fullWriter(string.Format("{0}{1}{2} = Array({3}, {4})", memberIndent, sceneDiffSettings.propertiesPrefix, member.Name, member.PropertyType, listMember.Length));
				objectWriter(string.Format("  {1}{2} = Array({3}, {4})", 0, sceneDiffSettings.propertiesPrefix, member.Name, member.PropertyType, listMember.Length));
				int jj = 0;
				for (int kk = 0; kk < listMember.Length; kk++)
				{
					System.Object val3_ = listMember.GetValue(kk);
					string val3 = Utils.GetValueStr(val3_);
					fullWriter(string.Format("{0}    {1} => {2}", memberIndent, jj, val3));
					objectWriter(string.Format("      {1} => {2}", 0, jj, val3));
					
					if (val3_ is UnityEngine.Material)
						WritePropertyMaterial(fullWriter, objectWriter, memberIndent, val3_ as Material);

					jj++;
				}
			}
			//
			// Example: "  layer = Default"
			//
			else if (
				obj.GetType().Name == "GameObject" && member.Name == "layer" ? true :
				obj.GetType().Name == "MeshRenderer" && member.Name == "sortingLayerID" ? true :
				false)
			{
				string val = Utils.GetValueStr(member.GetValue(obj, new object[0]));
				val = LayerMask.LayerToName(int.Parse(val));
				fullWriter(string.Format("{0}{1}{2} = {3}", memberIndent, sceneDiffSettings.propertiesPrefix, member.Name, val));
				objectWriter(string.Format("  {1}{2} = {3}", 0, sceneDiffSettings.propertiesPrefix, member.Name, val));
			}
			//
			// Example: "  sharedMaterial = diffuse1"
			//
			else
			{
				string val = Utils.GetValueStr(member.GetValue(obj, new object[0]));
				fullWriter(string.Format("{0}{1}{2} = {3}", memberIndent, sceneDiffSettings.propertiesPrefix, member.Name, val));
				objectWriter(string.Format("  {1}{2} = {3}", 0, sceneDiffSettings.propertiesPrefix, member.Name, val));
			}
		}
		catch(System.Exception ex)
		{
			// WARNING: some properties throw exceptions. You will have to add them to "Skip properties"-list.
			//
			Debug.LogWarning("SceneDiff: Failed to write field: " + obj.GetType().Name + "." + member.Name + ", " + doSkip + ", " + Utils.GetObjPath(obj) + ", " + ex.Message);
		}
	}

	void WritePropertyMaterial(AddLineDelegate fullWriter, AddLineDelegate objectWriter, string memberIndent, Material mat)
	{
		bool alreadyWritten = writtenMaterials.ContainsKey(mat.name);
		if (!alreadyWritten)
			writtenMaterials.Add(mat.name, true);
		AddLineDelegate fullWriter2 = line => { if (!alreadyWritten) fullWriter(line); };
		AddLineDelegate objectWriter2 = line => objectWriter("      " + line);
		PropertyInfo[] props2 = mat.GetType().GetProperties();
		objectWriter("--test " + props2.Length);
		foreach (PropertyInfo member2 in props2)
		{
			if (member2.Name == "color" && !mat.HasProperty("_Color"))
				continue;
			WriteProperty(fullWriter2, objectWriter2, memberIndent + "      ", mat, member2, props2, false);
		}

		if (alreadyWritten)
			fullWriter(string.Format("{0}      (see above)", memberIndent));
	}

    int SkipMembers(MemberInfo member, UnityEngine.Object obj, PropertyInfo[] props)
    {
		// Skip shortcut references (like gameObject.transform)
		if (obj is GameObject ||
			obj is Component)
		{
			if (System.Array.IndexOf(skipCommonShortcutsList, member.Name) != -1)
				return -1;
		}
  

        // Skip obsolite properties
        System.Object[] attr = member.GetCustomAttributes(typeof(System.ObsoleteAttribute), true);
        if (attr != null && attr.Length != 0)
            return -2;

        // Skip indexers
        ParameterInfo[] ind = member is PropertyInfo ? (member as PropertyInfo).GetIndexParameters() : null;
        if (ind != null && ind.Length != 0)
            return 2;

        // Skip "mesh"/"material" properties if there is "sharedMesh"/"sharedMaterial" properties
        if (sceneDiffSettings.skipPropertyIfHasShared)
        {
            string sharedName = "shared" + member.Name.Substring(0, 1).ToUpper() + member.Name.Substring(1);
            //if (member.Name == "material")
            //  Debug.Log("CHECKING " + member.Name + ", " + sharedName);
            foreach (PropertyInfo prop in props)
                if (prop.Name == sharedName)
                    return 3;

            // hack: handle error when accessing GUIText.material
            if (obj.GetType().Name == "GUIText" &&
                member.Name == "material")
                return 9;
        }

        // Skip by property name, property name substrings, obj name, obj name substring
        for (int k = 0; k < skipValues[0].Length; k++)
            if (skipValues[0][k].Contains("."))
            {
                if (obj.GetType().Name + "." + member.Name == skipValues[0][k])
                    return 4;
            }
            else
            {
                if (member.Name == skipValues[0][k])
                    return 5;
            }
        for (int k = 0; k < skipValues[1].Length; k++)
            if (member.Name.Contains(skipValues[1][k]))
                return 6;
        for (int k = 0; k < skipValues[2].Length; k++)
            if (obj.GetType().Name == skipValues[2][k])
                return 7;
        for (int k = 0; k < skipValues[3].Length; k++)
            if (obj.GetType().Name.Contains(skipValues[3][k]))
                return 8;

        return 0;
    }

	public bool ContainsFile(string file)
	{
		foreach (PendingWrite pending in pendingWrites)
			if (pending.targetFile == file)
				return true;
		return false;
	}





    void RunSvnCheck(bool checkScenes)
    {
        if (!checkScenes)
            AssetDatabase.SaveAssets();

        isReady = false;
        string svnExe1,svnExe2;
        string svnArgs1,svnArgs2;
        Utils.ParseExe(sceneDiffSettings.svnCommand1, out svnExe1, out svnArgs1);
        Utils.ParseExe(sceneDiffSettings.svnCommand2, out svnExe2, out svnArgs2);
        
        if (!System.IO.File.Exists(svnExe1))
        {
            Debug.LogError(string.Format("SceneDiff: Failed to find svn-utility '{0}'. If you are using TortoiseSVN please check that 'command line client tools' are checked when installing.", svnExe1));
            return;
        }
        if (!System.IO.File.Exists(svnExe2))
        {
            Debug.LogError(string.Format("SceneDiff: Failed to find svn-utility '{0}'. If you are using TortoiseSVN please check that 'command line client tools' are checked when installing.", svnExe2));
            return;
        }

        int modifiedCount = 0;
        int conflictedCount = 0;
        int jj = -1;
        List<string> conflictedList = new List<string>();
        foreach (string filePath in (checkScenes ? allScenes : allPrefabs))
        {
            jj++;
            string sceneFull = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath), filePath);
            if (!System.IO.File.Exists(sceneFull))
            {
                Debug.LogWarning("SceneDiff: Failed to find scene file: " + sceneFull);
                continue;
            }
            string svnResult1 = Utils.RunExeHidden(svnExe1, string.Format(svnArgs1, "\"" + sceneFull + "\""));
            //Debug.Log("svnResult1: " + jj + ", '" + svnResult1 + "', " + scene + ", " + svnExe1 + ", " + string.Format(svnArgs1, "\"" + sceneFull + "\""));

            if (svnResult1.EndsWith("M"))
            {
                modifiedCount++;
                int origRevision = 0;
                if (!int.TryParse(svnResult1.TrimEnd('M'), out origRevision))
                {
                    Debug.LogError("SceneDiff: Failed to parse revision-number from: " + svnResult1);
                    return;
                }
                string svnResult2 = Utils.RunExeHidden(svnExe2, string.Format(svnArgs2, "\"" + sceneFull + "\""));
                string[] svnParts2 = svnResult2.Split(new char[] { ':', '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
                int lastRevIndex = System.Array.IndexOf(svnParts2, "Last Changed Rev");
                if (lastRevIndex == -1)
                {
                    if (svnResult2.Contains("E180001"))
                        Debug.LogError("SceneDiff: Failed to connect to svn-repository. " + System.Environment.NewLine + "\"" + System.Environment.NewLine + svnResult2 + System.Environment.NewLine + "\"" + System.Environment.NewLine + "Check your svn-server connection!");
                    else
                        Debug.LogError("SceneDiff: Failed to parse 'Last Changed Rev'-number from: " + System.Environment.NewLine + "\"" + System.Environment.NewLine + svnResult2 + System.Environment.NewLine + "\"" + System.Environment.NewLine + "Check your svn-server connection!");
                    return;
                }
                else
                {
                    int finalRevision = int.Parse(svnParts2[lastRevIndex + 1]);
                    if (origRevision < finalRevision)
                    {
                        conflictedCount++;
                        conflictedList.Add("****" + filePath + " (latest " + finalRevision + ", last updated " + origRevision + ")");
                        //Debug.Log(string.Format("SceneDiff: Found conflicting scene: '{0}'. Last committed version {1}. Your last update {2}.", scene, finalRevision, origRevision));
                    }
                }
            }
            
        }
        if (conflictedCount == 0)
            Debug.Log(string.Format("SceneDiff: All ok! Found {0} modified {1}, no conflicts.", modifiedCount, checkScenes ? (modifiedCount == 1 ? "scene" : "scenes") : (modifiedCount == 1 ? "prefab" : "prefabs")));
        else
        {
            Debug.Log(string.Format("SceneDiff: Conflicting {0}: ", (checkScenes ? "scenes" : "prefabs")));
            foreach (string str in conflictedList)
                Debug.Log(str);
        }
    }


    void RunDiff(string scene, string postfix1, string postfix2, int type)
    {
        string projectpath = System.IO.Path.GetDirectoryName(Application.dataPath).Replace('\\', '/');
        string topfolder = projectpath + "/Dump";
        string diffExe;
        string args;
        Utils.ParseExe(sceneDiffSettings.diffCommand, out diffExe, out args);

        if (!System.IO.File.Exists(diffExe))
        {
            Debug.LogError(string.Format("SceneDiff: Failed to find diff-utility '{0}'. Please install some diff-tool (for example WinMerge is free and works great)", diffExe));
            return;
        }
        string fullArgs =
            (
                type == 2 ? sceneDiffSettings.comparisonTypeAB && sceneDiffSettings.writeIndividualFiles :
                sceneDiffSettings.comparisonTypeNormal && sceneDiffSettings.writeIndividualFiles
            ) ? 
                string.Format(args, "\"" + topfolder + "/" + scene + postfix1 + "\"", "\"" + topfolder + "/" + scene + postfix2 + "\"") :
            string.Format(args, "\"" + topfolder + "/" + scene + postfix1 + ".txt\"", "\"" + topfolder + "/" + scene + postfix2 + ".txt\"");
        Utils.RunExeVisible(diffExe, fullArgs);
        Debug.Log("SceneDiff: Comparison started: " + topfolder + "/" + scene + postfix1 + " <-> " + topfolder + "/" + scene + postfix2);
        
    }





    public struct SceneDiffSettings
    {
        public string skipComponents;
        public string skipComponentsWith;
        public string skipProperties;
        public string skipPropertiesWith;
        public bool skipPropertyIfHasShared;
        public string propertiesPrefix;
        public string diffCommand;
        public string svnCommand1;
        public string svnCommand2;
        public bool showAllScenes;
        public bool comparisonTypeNormal;
        public bool comparisonTypeAB;
        public bool writeIndividualFiles;

        public void LoadSettings()
        {
            skipProperties = EditorPrefs.GetString("SceneDiff_skipProperties", "");
            skipPropertiesWith = EditorPrefs.GetString("SceneDiff_skipPropertiesWith", "");
            skipComponents = EditorPrefs.GetString("SceneDiff_skipComponents", "");
            skipComponentsWith = EditorPrefs.GetString("SceneDiff_skipComponentsWith", "");
            skipPropertyIfHasShared = EditorPrefs.GetBool("SceneDiff_skipPropertyIfHasShared", true);
            propertiesPrefix = EditorPrefs.GetString("SceneDiff_settingsPropertiesPrefix", "");
            diffCommand = EditorPrefs.GetString("SceneDiff_settingsDiffCommand", "");
            svnCommand1 = EditorPrefs.GetString("SceneDiff_settingsSvnVerCommand", "");
            svnCommand2 = EditorPrefs.GetString("SceneDiff_settingsSvnCommand", "");
            showAllScenes = EditorPrefs.GetBool("SceneDiff_settingsShowAllScenes", false);
            comparisonTypeNormal = EditorPrefs.GetBool("SceneDiff_comparisonType", true);
            comparisonTypeAB = EditorPrefs.GetBool("SceneDiff_comparisonTypeCustom", true);
            writeIndividualFiles = EditorPrefs.GetBool("SceneDiff_writeIndividualFiles", true);

        }

        public bool SaveSettings(ref SceneDiffSettings sceneDiffSettingsLast)
        {
            if (sceneDiffSettingsLast.skipProperties != skipProperties ||
                sceneDiffSettingsLast.skipPropertiesWith != skipPropertiesWith ||
                sceneDiffSettingsLast.skipComponents != skipComponents ||
                sceneDiffSettingsLast.skipComponentsWith != skipComponentsWith ||
                sceneDiffSettingsLast.skipPropertyIfHasShared != skipPropertyIfHasShared ||
                sceneDiffSettingsLast.propertiesPrefix != propertiesPrefix ||
                sceneDiffSettingsLast.diffCommand != diffCommand ||
                sceneDiffSettingsLast.svnCommand1 != svnCommand1 ||
                sceneDiffSettingsLast.svnCommand2 != svnCommand2 ||
                sceneDiffSettingsLast.showAllScenes != showAllScenes ||
                sceneDiffSettingsLast.comparisonTypeNormal != comparisonTypeNormal ||
                sceneDiffSettingsLast.comparisonTypeAB != comparisonTypeAB ||
                sceneDiffSettingsLast.writeIndividualFiles != writeIndividualFiles
                )
            {
                sceneDiffSettingsLast = this;
                //Debug.Log("Saving settings...");
                EditorPrefs.SetString("SceneDiff_skipProperties", skipProperties);
                EditorPrefs.SetString("SceneDiff_skipPropertiesWith", skipPropertiesWith);
                EditorPrefs.SetString("SceneDiff_skipComponents", skipComponents);
                EditorPrefs.SetString("SceneDiff_skipComponentsWith", skipComponentsWith);
                EditorPrefs.SetBool("SceneDiff_skipPropertyIfHasShared", skipPropertyIfHasShared);
                EditorPrefs.SetString("SceneDiff_settingsPropertiesPrefix", propertiesPrefix);
                EditorPrefs.SetString("SceneDiff_settingsDiffCommand", diffCommand);
                EditorPrefs.SetString("SceneDiff_settingsSvnVerCommand", svnCommand1);
                EditorPrefs.SetString("SceneDiff_settingsSvnCommand", svnCommand2);
                EditorPrefs.SetBool("SceneDiff_settingsShowAllScenes", showAllScenes);
                EditorPrefs.SetBool("SceneDiff_comparisonType", comparisonTypeNormal);
                EditorPrefs.SetBool("SceneDiff_comparisonTypeCustom", comparisonTypeAB);
                EditorPrefs.SetBool("SceneDiff_writeIndividualFiles", writeIndividualFiles);
                return true;
            }
            return false;
        }

        public void Reset1()
        {
            skipComponents = "";
            skipComponentsWith = "";
            skipProperties = "worldToLocalMatrix,localToWorldMatrix," + System.Environment.NewLine + "cameraToWorldMatrix,worldToCameraMatrix";
            skipPropertiesWith = "";
            skipPropertyIfHasShared = true;
        }

        public void Reset2()
        {
            string[] fileFolders = new string[] { @"C:\Program Files (x86)", @"C:\Program Files" };
            string diff =
                System.IO.File.Exists(@"C:\Program Files\WinMerge\WinMerge.exe") ?
                    @"C:\Program Files\WinMerge\WinMerge.exe" :
                @"C:\Program Files (x86)\WinMerge\WinMerge.exe";
            string svn1 =
                System.IO.File.Exists(@"C:\Program Files (x86)\TortoiseSVN\bin\svnversion.exe") ?
                    @"C:\Program Files (x86)\TortoiseSVN\bin\svnversion.exe" :
                @"C:\Program Files\TortoiseSVN\bin\svnversion.exe";
            string svn2 =
                System.IO.File.Exists(@"C:\Program Files (x86)\TortoiseSVN\bin\svn.exe") ?
                    @"C:\Program Files (x86)\TortoiseSVN\bin\svn.exe" :
                @"C:\Program Files\TortoiseSVN\bin\svn.exe";
            diffCommand = "\"" + diff + "\" /r {0} {1}";
            svnCommand1 = "\"" + svn1 + "\" {0}";
            svnCommand2 = "\"" + svn2 + "\" info -r HEAD {0}";
            showAllScenes = false;
            propertiesPrefix = "";
			writeIndividualFiles = true;
        }
    }





    public class Utils
    {
        public static void ParseExe(string fileArgs, out string file, out string args)
        {
            file = fileArgs.TrimStart();
            args = "{0}";
            if (file.StartsWith("\"") && file.IndexOf('\"', 1) != -1)
            {
                args = file.Substring(file.IndexOf('\"', 1) + 1);
                file = file.Substring(1, file.IndexOf('\"', 1) - 1);
            }
            else if (file.IndexOf(' ') != -1)
            {
                args = file.Substring(file.IndexOf(' ') + 1);
                file = file.Substring(1, file.IndexOf(' ') - 1);
            }
        }

        public static string RunExeHidden(string file, string args3)
        {
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = file;
            info.Arguments = args3;
            info.UseShellExecute = false;
            info.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.CreateNoWindow = true;
            System.Diagnostics.Process proc = System.Diagnostics.Process.Start(info);
            proc.WaitForExit();
            string res = proc.StandardOutput.ReadToEnd();
            if (string.IsNullOrEmpty(res))
                res = proc.StandardError.ReadToEnd();
            proc.Close();
            return res != null ? res.TrimEnd(new char[] { '\r', '\n' }) : "";
        }

        public static string RunExeVisible(string file, string args3)
        {
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = file;
            info.Arguments = args3;
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            System.Diagnostics.Process proc = System.Diagnostics.Process.Start(info);
            return "";
        }

        public static string GetObjPath(UnityEngine.Object obj)
        {
            string assetPath = obj != null ? AssetDatabase.GetAssetPath(obj) : null;
			return string.IsNullOrEmpty(assetPath) ? GetObjPath2(obj) : GetObjPath3(obj, assetPath);
        }

        public static string GetObjPath2(UnityEngine.Object obj)
        {
            if (obj == null)
                return "";
            string path = "/" + obj.name;
			Transform trans = obj is GameObject ? (obj as GameObject).transform : (obj as Component).transform;
            while (trans != null && trans.parent != null)
            {
                trans = trans.parent;
                path = "/" + trans.gameObject.name + path;
            }
            return path;
        }
        public static string GetObjPath3(UnityEngine.Object obj, string assetPath)
        {
            if (obj == null)
                return assetPath;
            string path = 
				obj is Component ? "/" + (obj as Component).GetType().Name :
				obj is GameObject ? "/" + (obj as GameObject).name :
				"";
			Transform trans = obj is GameObject ? (obj as GameObject).transform : (obj as Component).transform;
            while (trans != null && trans.parent != null)
            {
                trans = trans.parent;
                path = "/" + trans.gameObject.name + path;
            }
            return assetPath + path;
        }

        public static string GetValueStr(System.Object val_)
        {
            string val = 
				val_ == null ? "<null>" : 
				val_ is GameObject ? Utils.GetObjPath(val_ as GameObject) :
				val_ is Component ? Utils.GetObjPath(val_ as Component) :
				val_ is UnityEngine.Object && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(val_ as UnityEngine.Object)) ? 
					AssetDatabase.GetAssetPath(val_ as UnityEngine.Object) + " -> " + val_.ToString() :
				val_.ToString();
            if (val.Contains("\n"))
                val = val.Replace("\n", System.Environment.NewLine);
            return val;
        }

        public static Texture2D CreateTexture(int width, int height, Color col)
        {

            Color[] pix = new Color[width * height];

            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        public static void ShowFile(string filePath)
        {
            string projectpath = System.IO.Path.GetDirectoryName(Application.dataPath);
            string fil = (projectpath + "\\" + filePath).Replace('/', '\\');
            if (!System.IO.File.Exists(fil))
                Debug.LogError("SceneDiff: Failed to find file: " + fil);
			
			#if UNITY_WINDOWS
				IntPtr pidl = ILCreateFromPathW(fil);
				SHOpenFolderAndSelectItems(pidl, 0, IntPtr.Zero, 0);
				ILFree(pidl);
			#else
				Debug.LogError("SceneDiff: not supported platform: " + Application.platform);
			#endif
        }

		#if UNITY_WINDOWS
			[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
			private static extern IntPtr ILCreateFromPathW(string pszPath);

			[DllImport("shell32.dll")]
			private static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, int cild, IntPtr apidl, int dwFlags);

			[DllImport("shell32.dll")]
			private static extern void ILFree(IntPtr pidl);
		#endif
    }
}
#endif