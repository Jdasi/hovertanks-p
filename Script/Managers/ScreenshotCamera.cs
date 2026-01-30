using HoverTanks.UI;
using System.IO;
using System.Collections;
using System.Diagnostics;
using UnityEngine;

public class ScreenshotCamera : MonoBehaviour
{
#if UNITY_EDITOR
    private bool _snapScheduled;

    public void Snap()
    {
        if (_snapScheduled)
        {
            return;
        }

        _snapScheduled = true;
        gameObject.SetActive(true);
    }

    private void OnEnable()
    {
        StartCoroutine(SnapRoutine());
    }

    private IEnumerator SnapRoutine()
    {
        // disable game cam
        var mainCamera = GameObject.Find("Main Camera");
        mainCamera.SetActive(false);

        // turn off gameplay UI
        var gameplayUI = GameObject.FindFirstObjectByType<GameplayUI>();
        if (gameplayUI != null)
        {
            gameplayUI.gameObject.SetActive(false);
        }

        // wait for camera switch
        yield return new WaitForSecondsRealtime(0.25f);

        string screenshotPath;
        string screenshotName;
        string exportPath;
        int snapId = 0;

        // find a unique filename
        do
        {
            screenshotName = $"Snap_{snapId++}";
            screenshotPath = $"Snaps/{screenshotName}.png";
            exportPath = $"Snaps/{screenshotName}_export.png";
        } while (File.Exists(exportPath));

        // capture the screenshot
        ScreenCapture.CaptureScreenshot(screenshotPath);

        // wait for export
        while (IsFileUnavailable(screenshotPath))
        {
            yield return new WaitForSeconds(0.05f);
        }

        // run conversion tool
        var process = Process.Start(new ProcessStartInfo()
        {
            FileName = Path.GetFullPath(Application.dataPath + "/../Tools/TextureTool/texture_tool.exe"),
            Arguments = $"220 20 230 {screenshotName}.png",
            WorkingDirectory = Path.GetFullPath(Application.dataPath + "/../Snaps"),
            CreateNoWindow = true,
            UseShellExecute = false
        });

        process.WaitForExit();

        // check for error
        if (process.ExitCode != 0)
        {
            Log.Info(LogChannel.Default, $"[ScreenshotCamera] TextureTool failed to export screenshot! Exited with: {process.ExitCode}");
        }

        // clean up old file
        File.Delete(screenshotPath);

        // switch back to game cam
        gameObject.SetActive(false);
        mainCamera.SetActive(true);

        // turn on gameplay UI
        if (gameplayUI != null)
        {
            gameplayUI.gameObject.SetActive(true);
        }

        // clear snap state
        _snapScheduled = false;
    }

    protected virtual bool IsFileUnavailable(string path)
    {
        // abort if file doesn't exist
        if (!File.Exists(path))
        {
            return true;
        }

        FileInfo file = new FileInfo(path);
        FileStream stream = null;

        try
        {
            stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
        }
        catch (IOException)
        {
            /*
              the file is unavailable because it:
              - is still being written to
              - is being processed by another thread
              - does not exist (has already been processed)
            */
            return true;
        }
        finally
        {
            if (stream != null)
            {
                stream.Close();
            }
        }

        // file is not locked
        return false;
    }
#endif
}
