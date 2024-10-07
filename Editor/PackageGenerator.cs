using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HexTecGames.PackageGenerator
{
    public class PackageGenerator : EditorWindow
    {
        public TextAsset gitIgnore;

        static string author;
        static string displayName;

        static bool documentationDirectory;
        static bool editorDirectory = true;
        static bool testDirectory;
        static bool addGitIgnoreFile;
        static int examples;

        private string authorNoSpace;
        private string displayNameNoSpace;
        private string infoText;
        private bool overwrite;

        [MenuItem("Tools/Package Generator")]
        public static void ShowWindow()
        {
            GetWindow(typeof(PackageGenerator));
        }

        private void OnGUI()
        {
            if (string.IsNullOrEmpty(author))
            {
                author = Application.companyName;
            }
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = Application.productName;
            }

            GUILayout.Label("Generate Package", EditorStyles.boldLabel);
            author = EditorGUILayout.TextField("Author", author);
            displayName = EditorGUILayout.TextField("Package Name", displayName);

            GUILayout.Label("Include Folders", EditorStyles.boldLabel);
            editorDirectory = EditorGUILayout.Toggle("Editor", editorDirectory);
            testDirectory = EditorGUILayout.Toggle("Tests", testDirectory);
            documentationDirectory = EditorGUILayout.Toggle("Documentation", documentationDirectory);
            addGitIgnoreFile = EditorGUILayout.Toggle("Add GitIgnore", addGitIgnoreFile);
            examples = EditorGUILayout.IntField("Examples: ", examples);

            if (string.IsNullOrEmpty(infoText) == false)
            {
                EditorGUILayout.HelpBox(infoText, MessageType.Info);
            }
            if (GUILayout.Button(overwrite ? "Overwrite" : "Generate"))
            {
                infoText = GenerateProjectFiles();
                AssetDatabase.Refresh();
            }
        }

        private string GenerateProjectFiles()
        {
            authorNoSpace = author.Replace(" ", string.Empty);
            displayNameNoSpace = displayName.Replace(" ", string.Empty);

            if (overwrite == false && Directory.Exists(Path.Combine(Application.dataPath, displayNameNoSpace)))
            {
                overwrite = true;
                return "Package with name '" + displayNameNoSpace + "' already exists, click again to override.";
            }
            else overwrite = false;
            Directory.CreateDirectory(Path.Combine(Application.dataPath, displayNameNoSpace));

            File.CreateText(Path.Combine(Application.dataPath, displayNameNoSpace, "README.md")).Close();
            File.CreateText(Path.Combine(Application.dataPath, displayNameNoSpace, "LICENSE.md")).Close();
            File.CreateText(Path.Combine(Application.dataPath, displayNameNoSpace, "CHANGELOG.md")).Close();

            GeneratePackageJSON();

            Directory.CreateDirectory(Path.Combine(Application.dataPath, displayNameNoSpace, "Runtime"));
            GenerateASMDEF(Path.Combine("Runtime"), "", false);

            if (editorDirectory)
            {
                Directory.CreateDirectory(Path.Combine(Application.dataPath, displayNameNoSpace, "Editor"));
                GenerateASMDEF(Path.Combine("Editor"), ".Editor", true);
            }

            if (testDirectory)
            {
                Directory.CreateDirectory(Path.Combine(Application.dataPath, displayNameNoSpace, "Tests"));
                Directory.CreateDirectory(Path.Combine(Application.dataPath, displayNameNoSpace, "Tests", "Runtime"));
                GenerateASMDEF(Path.Combine("Tests", "Runtime"), ".Tests.Runtime", true);
                if (editorDirectory)
                {
                    Directory.CreateDirectory(Path.Combine(Application.dataPath, displayNameNoSpace, "Tests", "Editor"));
                    GenerateASMDEF(Path.Combine("Tests", "Editor"), ".Tests.Editor", true);
                }
            }
            if (documentationDirectory)
            {
                Directory.CreateDirectory(Path.Combine(Application.dataPath, displayNameNoSpace, "Documentation"));
            }
            if (addGitIgnoreFile)
            {
                if (gitIgnore != null)
                {
                    using (StreamWriter sw = File.CreateText(Path.Combine(Application.dataPath, displayNameNoSpace, ".gitIgnore")))
                    {
                        sw.Write(gitIgnore.text);
                    }
                }
                else Debug.Log("No gitIgnore file selected");
            }
            if (examples > 0)
            {
                Directory.CreateDirectory(Path.Combine(Application.dataPath, displayNameNoSpace, "Samples"));
                for (int i = 1; i <= examples; i++)
                {
                    Directory.CreateDirectory(Path.Combine(Application.dataPath, displayNameNoSpace, "Samples", "Example " + i));
                }
            }
            return "Success";
        }
        private void GeneratePackageJSON()
        {
            using (StreamWriter sw = File.CreateText(Path.Combine(Application.dataPath, displayNameNoSpace, "package.json")))
            {
                sw.WriteLine("{");
                sw.WriteLine("  \"name\": \"com.{0}.{1}\",", authorNoSpace.ToLower(), displayNameNoSpace.ToLower());
                sw.WriteLine("  \"version\": \"1.0.0\",");
                sw.WriteLine("  \"displayName\": \"{0}\",", displayName);
                sw.WriteLine("  \"description\": \"My Package\",");
                sw.WriteLine("  \"unity\": \"2019.1\",");
                sw.WriteLine("  \"dependencies\": {},");
                sw.WriteLine("  \"author\": {");
                sw.WriteLine("    \"name\": \"{0}\"", author);
                sw.WriteLine("  },");
                if (examples > 0)
                {
                    sw.WriteLine("  \"hideInEditor\": \"false\",");
                }
                else sw.WriteLine("  \"hideInEditor\": \"false\"");
                if (examples > 0)
                {
                    sw.WriteLine("  \"samples\": [");

                    for (int i = 1; i <= examples; i++)
                    {
                        sw.WriteLine("    {");
                        sw.WriteLine("      \"displayName\": \"Example {0}\",", i);
                        sw.WriteLine("      \"description\": \"Example {0} description\",", i);
                        sw.WriteLine("      \"path\": \"Samples/Example {0}\"", i);
                        if (i < examples)
                        {
                            sw.WriteLine("    },");
                        }
                        else sw.WriteLine("    }");
                    }
                    sw.WriteLine("  ]");
                }
                sw.WriteLine("}");
            }
        }
        private void GenerateASMDEF(string dir, string suffix, bool editor)
        {
            using (StreamWriter sw = File.CreateText(Path.Combine(Application.dataPath, displayNameNoSpace, dir, authorNoSpace + "." + displayNameNoSpace + suffix + ".asmdef")))
            {
                sw.WriteLine("{");
                sw.WriteLine("    \"name\": \"{0}.{1}{2}\",", authorNoSpace, displayNameNoSpace, suffix);
                sw.WriteLine("    \"references\": [");
                sw.WriteLine("    ],");
                sw.WriteLine("    \"includePlatforms\": [");
                if (editor)
                {
                    sw.WriteLine("        \"Editor\"");
                }
                sw.WriteLine("    ],");
                sw.WriteLine("    \"excludePlatforms\": [],");
                sw.WriteLine("    \"allowUnsafeCode\": false,");
                sw.WriteLine("    \"overrideReferences\": false,");
                sw.WriteLine("    \"precompiledReferences\": [],");
                sw.WriteLine("    \"autoReferenced\": true,");
                sw.WriteLine("    \"defineConstraints\": [],");
                sw.WriteLine("    \"versionDefines\": [],");
                sw.WriteLine("    \"noEngineReferences\": false");
                sw.WriteLine("}");
            }
        }
    }
}