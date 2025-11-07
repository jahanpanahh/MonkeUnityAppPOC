using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

public class iOSBuildPostProcessor
{
    [PostProcessBuild(1)]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string path)
    {
        if (buildTarget != BuildTarget.iOS)
            return;

        Debug.Log("iOS Post-Process: Starting...");

        // Get plist path
        string plistPath = Path.Combine(path, "Info.plist");
        PlistDocument plist = new PlistDocument();
        plist.ReadFromFile(plistPath);

        PlistElementDict rootDict = plist.root;

        // Add Speech Recognition permission
        rootDict.SetString("NSSpeechRecognitionUsageDescription", "Monke uses speech recognition to understand what you say");

        // Add Microphone permission (should already exist but make sure)
        rootDict.SetString("NSMicrophoneUsageDescription", "Monke listens to your voice");

        // Add Local Network permission
        rootDict.SetString("NSLocalNetworkUsageDescription", "Monke needs to connect to local server for AI responses");

        // Add Bonjour services
        PlistElementArray bonjourServices = rootDict.CreateArray("NSBonjourServices");
        bonjourServices.AddString("_http._tcp");

        // Allow arbitrary loads for HTTP connections
        PlistElementDict atsDict = rootDict.CreateDict("NSAppTransportSecurity");
        atsDict.SetBoolean("NSAllowsArbitraryLoads", true);

        // Write back to Info.plist
        plist.WriteToFile(plistPath);
        Debug.Log("iOS Post-Process: Info.plist updated successfully");

        // Add Speech framework to Xcode project
        string projectPath = PBXProject.GetPBXProjectPath(path);
        PBXProject project = new PBXProject();
        project.ReadFromFile(projectPath);

        // Get specific targets
        string unityFrameworkTarget = project.GetUnityFrameworkTargetGuid();
        string mainTarget = project.GetUnityMainTargetGuid();

        // Add frameworks to UnityFramework target
        if (!string.IsNullOrEmpty(unityFrameworkTarget))
        {
            project.AddFrameworkToProject(unityFrameworkTarget, "Speech.framework", false);
            project.AddFrameworkToProject(unityFrameworkTarget, "AVFoundation.framework", false);
            Debug.Log($"iOS Post-Process: Added frameworks to UnityFramework target");
        }

        // Add frameworks to Unity-iPhone (main) target
        if (!string.IsNullOrEmpty(mainTarget))
        {
            project.AddFrameworkToProject(mainTarget, "Speech.framework", false);
            project.AddFrameworkToProject(mainTarget, "AVFoundation.framework", false);
            Debug.Log($"iOS Post-Process: Added frameworks to Unity-iPhone target");
        }
        else
        {
            Debug.LogWarning("iOS Post-Process: Could not find Unity-iPhone target, trying alternative method");

            // Try finding by name
            string iPhoneTarget = project.TargetGuidByName("Unity-iPhone");
            if (!string.IsNullOrEmpty(iPhoneTarget))
            {
                project.AddFrameworkToProject(iPhoneTarget, "Speech.framework", false);
                project.AddFrameworkToProject(iPhoneTarget, "AVFoundation.framework", false);
                Debug.Log($"iOS Post-Process: Added frameworks to Unity-iPhone target (via name)");
            }
        }

        // Write back to project
        project.WriteToFile(projectPath);

        Debug.Log("iOS Post-Process: Complete!");
    }
}
