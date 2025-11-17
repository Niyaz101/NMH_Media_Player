


using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TorchSharp;
using static TorchSharp.torch;
using static TorchSharp.torch.jit;
using Size = System.Drawing.Size;
using System.Collections.Generic;

namespace NMH.VideoFilter
{
    public static class Filter
    {
        private static string FFmpegPath = string.Empty;
        private static string FramesFolder = string.Empty;
        private static string ModelPath = string.Empty;
        private static ScriptModule? _model;
        private static bool _isAnalyzing = false;
        public static bool IsModelLoaded => _model != null;

        public static void Initialize(string ffmpegPath, string torchScriptModelPath, string framesFolder)
        {
            FFmpegPath = ffmpegPath;
            ModelPath = torchScriptModelPath;
            FramesFolder = framesFolder;
            Directory.CreateDirectory(FramesFolder);

            try
            {
                if (!File.Exists(ModelPath))
                {
                    Log($"❌ TorchScript model not found at: {ModelPath}");
                    return;
                }

                _model = torch.jit.load(ModelPath);
                _model.eval();
                Log($"✅ TorchScript model loaded successfully from: {ModelPath}");
            }
            catch (Exception ex)
            {
                Log($"❌ Model loading failed: {ex.Message}");
            }

            Log($"Filter Initialized\nFFmpeg: {FFmpegPath}\nModel: {ModelPath}\nFrames Folder: {FramesFolder}");
        }

        public static async Task CaptureFramesAsync(string videoPath)
        {
            if (string.IsNullOrEmpty(videoPath) || !File.Exists(videoPath))
            {
                Log($"❌ Invalid video path: {videoPath}");
                return;
            }

            if (string.IsNullOrEmpty(FFmpegPath) || !File.Exists(FFmpegPath))
            {
                Log($"❌ FFmpeg not found at: {FFmpegPath}");
                return;
            }

            Directory.CreateDirectory(FramesFolder);

            // Delete old frames
            foreach (var file in Directory.GetFiles(FramesFolder, "*.jpg"))
            {
                bool deleted = false;
                int attempts = 0;
                while (!deleted && attempts < 5)
                {
                    try
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                        File.Delete(file);
                        deleted = true;
                    }
                    catch (IOException)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        await Task.Delay(100);
                        attempts++;
                    }
                }
                if (!deleted) Log($"⚠️ Could not delete file: {file}");
            }

            Log($"▶ Extracting frames from: {Path.GetFileName(videoPath)}");
            string args = $"-i \"{videoPath}\" -vf fps=1,scale=320:320 \"{Path.Combine(FramesFolder, "frame_%04d.jpg")}\" -y";

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = FFmpegPath,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Log(e.Data); };
            process.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Log(e.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();

            Log($"✅ Frame extraction completed to: {FramesFolder}");
        }

        public static async Task AnalyzeFramesAsync(
    Action onUnsafeDetected,
    Func<Task>? skipForwardAsync = null,
    int checkIntervalMs = 50)
        {
            if (_isAnalyzing)
            {
                Log("⚠️ AnalyzeFramesAsync is already running. Skipping duplicate call.");
                return; // Prevent multiple analyzers
            }

            _isAnalyzing = true;

            try
            {
                if (!IsModelLoaded)
                {
                    Log("❌ TorchScript model is not loaded.");
                    return;
                }

                int lastFrameIndex = 0;

                while (true)
                {
                    var frames = Directory.GetFiles(FramesFolder, "*.jpg")
                                          .OrderBy(f => f)
                                          .ToArray();

                    if (frames.Length == 0)
                    {
                        await Task.Delay(checkIntervalMs);
                        continue;
                    }

                    for (; lastFrameIndex < frames.Length; lastFrameIndex++)
                    {
                        string framePath = frames[lastFrameIndex];

                        try
                        {
                            using var bmp = new Bitmap(framePath);
                            using var tensor = ConvertImageToTensor(bmp);
                            var outputObj = _model!.forward(tensor);

                            if (outputObj is Tensor outputTensor)
                            {
                                var detections = Filter.ParseModelOutput(outputTensor, 0.5f);

                                if (detections.Any())
                                {
                                    Log($"🚨 NSFW detected at frame {lastFrameIndex}: {framePath}");

                                    // Delete unsafe frame safely
                                    bool deleted = false;
                                    for (int i = 0; i < 10 && !deleted; i++)
                                    {
                                        try
                                        {
                                            if (File.Exists(framePath))
                                            {
                                                File.SetAttributes(framePath, FileAttributes.Normal);
                                                File.Delete(framePath);
                                            }
                                            deleted = true;
                                            Log($"🗑️ Deleted unsafe frame: {Path.GetFileName(framePath)}");
                                        }
                                        catch (IOException)
                                        {
                                            await Task.Delay(100);
                                            GC.Collect();
                                            GC.WaitForPendingFinalizers();
                                        }
                                    }

                                    onUnsafeDetected?.Invoke();

                                    if (skipForwardAsync != null)
                                    {
                                        Log("⏩ Skipping forward 10 seconds after NSFW detection...");
                                        await skipForwardAsync();
                                    }

                                    // ✅ Delete all remaining frames after detection
                                    DeleteAllFrames();

                                    return; // stop analysis
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log($"⚠️ Error analyzing frame {lastFrameIndex}: {ex.Message}");
                        }
                    }

                    await Task.Delay(checkIntervalMs);
                }
            }
            finally
            {
                _isAnalyzing = false;

                // ✅ Cleanup after normal completion (no NSFW detected)
                DeleteAllFrames();
            }
        }

        // Utility to delete all frames safely
        private static void DeleteAllFrames()
        {
            try
            {
                var files = Directory.GetFiles(FramesFolder, "*.jpg");
                foreach (var file in files)
                {
                    try
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                        File.Delete(file);
                    }
                    catch (IOException)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                }

                Log($"🧹 All frames deleted from: {FramesFolder}");
            }
            catch (Exception ex)
            {
                Log($"⚠️ Failed to delete frames: {ex.Message}");
            }
        }


        private static Tensor ConvertImageToTensor(Bitmap bmp)
        {
            try
            {
                int modelWidth = 320;
                int modelHeight = 320;

                using Bitmap resized = new Bitmap(bmp, new Size(modelWidth, modelHeight));
                float[,,] data = new float[3, modelHeight, modelWidth];

                for (int y = 0; y < modelHeight; y++)
                {
                    for (int x = 0; x < modelWidth; x++)
                    {
                        var c = resized.GetPixel(x, y);
                        data[0, y, x] = c.R / 255f;
                        data[1, y, x] = c.G / 255f;
                        data[2, y, x] = c.B / 255f;
                    }
                }

                var tensor = torch.tensor(data, dtype: ScalarType.Float32).mul(2).sub(1);
                return tensor.unsqueeze(0);
            }
            catch (Exception ex)
            {
                Log($"⚠️ Failed converting image to tensor: {ex.Message}");
                return null!;
            }
        }

        public class DetectionResult
        {
            public float X, Y, Width, Height, Confidence;
            public int ClassId;
        }

        public static List<DetectionResult> ParseModelOutput(Tensor output, float confThreshold = 0.5f)
        {
            var shape = output.shape;
            int numAttrs = (int)shape[1];
            int numBoxes = (int)shape[2];
            var dets = output.squeeze(0);
            var results = new List<DetectionResult>();
            var data = dets.data<float>().ToArray();

            for (int i = 0; i < numBoxes; i++)
            {
                int offset = i * numAttrs;
                float x = data[offset + 0];
                float y = data[offset + 1];
                float w = data[offset + 2];
                float h = data[offset + 3];
                float conf = data[offset + 4];
                if (conf < confThreshold) continue;

                float maxProb = 0f;
                int classId = -1;
                for (int c = 5; c < numAttrs; c++)
                {
                    float p = data[offset + c];
                    if (p > maxProb)
                    {
                        maxProb = p;
                        classId = c - 5;
                    }
                }

                float finalConf = conf * maxProb;
                if (finalConf < confThreshold) continue;

                results.Add(new DetectionResult
                {
                    X = x,
                    Y = y,
                    Width = w,
                    Height = h,
                    Confidence = finalConf,
                    ClassId = classId
                });
            }

            return results;
        }

        private static void Log(string message)
        {
            try
            {
                string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NMHMediaPlayer");
                Directory.CreateDirectory(logDir);
                string logPath = Path.Combine(logDir, "filter_log.txt");
                string finalMsg = $"[{DateTime.Now:HH:mm:ss}] {message}";
                File.AppendAllText(logPath, finalMsg + "\n");
                Console.WriteLine(finalMsg);
            }
            catch { }
        }
    }
}




//using System;
//using System.Diagnostics;
//using System.Drawing;
//using System.Drawing.Imaging;
//using System.IO;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using TorchSharp;
//using static TorchSharp.torch;
//using static TorchSharp.torch.jit;
//using Size = System.Drawing.Size;
//using System.Collections.Generic;

//namespace NMH.VideoFilter
//{
//    public static class Filter
//    {
//        private static string FFmpegPath = string.Empty;
//        private static string FramesFolder = string.Empty;
//        private static string ModelPath = string.Empty;
//        private static ScriptModule? _model;
//        private static bool _isAnalyzing = false;

//        public static bool IsModelLoaded => _model != null;

//        // ------------------------- Initialize -------------------------
//        public static void Initialize(string ffmpegPath, string torchScriptModelPath, string framesFolder)
//        {
//            FFmpegPath = ffmpegPath;
//            ModelPath = torchScriptModelPath;
//            FramesFolder = framesFolder;

//            Directory.CreateDirectory(FramesFolder);

//            try
//            {
//                if (!File.Exists(ModelPath))
//                {
//                    Log($"❌ TorchScript model not found at: {ModelPath}");
//                    return;
//                }

//                _model = torch.jit.load(ModelPath);
//                _model.eval();
//                Log($"✅ TorchScript model loaded successfully from: {ModelPath}");
//            }
//            catch (Exception ex)
//            {
//                Log($"❌ Model loading failed: {ex.Message}");
//            }

//            Log($"Filter Initialized\nFFmpeg: {FFmpegPath}\nModel: {ModelPath}\nFrames Folder: {FramesFolder}");
//        }

//        // ------------------------- Capture Frames -------------------------
//        public static async Task CaptureFramesAsync(string videoPath, CancellationToken token)
//        {
//            if (string.IsNullOrEmpty(videoPath) || !File.Exists(videoPath))
//            {
//                Log($"❌ Invalid video path: {videoPath}");
//                return;
//            }

//            if (string.IsNullOrEmpty(FFmpegPath) || !File.Exists(FFmpegPath))
//            {
//                Log($"❌ FFmpeg not found at: {FFmpegPath}");
//                return;
//            }

//            Directory.CreateDirectory(FramesFolder);

//            // Delete old frames
//            foreach (var file in Directory.GetFiles(FramesFolder, "*.jpg"))
//            {
//                token.ThrowIfCancellationRequested();

//                bool deleted = false;
//                int attempts = 0;
//                while (!deleted && attempts < 5)
//                {
//                    try
//                    {
//                        File.SetAttributes(file, FileAttributes.Normal);
//                        File.Delete(file);
//                        deleted = true;
//                    }
//                    catch (IOException)
//                    {
//                        GC.Collect();
//                        GC.WaitForPendingFinalizers();
//                        await Task.Delay(100, token);
//                        attempts++;
//                    }
//                }
//                if (!deleted) Log($"⚠️ Could not delete file: {file}");
//            }

//            Log($"▶ Extracting frames from: {Path.GetFileName(videoPath)}");
//            string args = $"-i \"{videoPath}\" -vf fps=1,scale=320:320 \"{Path.Combine(FramesFolder, "frame_%04d.jpg")}\" -y";

//            using var process = new Process
//            {
//                StartInfo = new ProcessStartInfo
//                {
//                    FileName = FFmpegPath,
//                    Arguments = args,
//                    RedirectStandardOutput = true,
//                    RedirectStandardError = true,
//                    UseShellExecute = false,
//                    CreateNoWindow = true
//                }
//            };

//            process.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Log(e.Data); };
//            process.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Log(e.Data); };

//            process.Start();
//            process.BeginOutputReadLine();
//            process.BeginErrorReadLine();

//            // Wait until process exits or cancellation
//            while (!process.HasExited)
//            {
//                token.ThrowIfCancellationRequested();
//                await Task.Delay(100, token);
//            }

//            Log($"✅ Frame extraction completed to: {FramesFolder}");
//            var createdFrames = Directory.GetFiles(FramesFolder, "*.jpg").Length;
//            Log($"✅ Frame extraction completed to: {FramesFolder} | {createdFrames} frames created by FFmpeg");

//        }

//        // ------------------------- Analyze Frames -------------------------
//        public static async Task AnalyzeFramesAsync(
//    Action onUnsafeDetected,          // NSFW detected handler (pause video + show message)
//    Func<Task>? skipForwardAsync,     // optional skip forward
//    CancellationToken token,
//    int checkIntervalMs = 50)
//        {
//            if (_isAnalyzing)
//            {
//                Log("⚠️ AnalyzeFramesAsync is already running. Skipping duplicate call.");
//                return;
//            }

//            _isAnalyzing = true;
//            bool nsfwFound = false;

//            try
//            {
//                if (!IsModelLoaded)
//                {
//                    Log("❌ TorchScript model is not loaded.");
//                    return;
//                }

//                int lastFrameIndex = 0;

//                while (!token.IsCancellationRequested)
//                {
//                    var frames = Directory.GetFiles(FramesFolder, "*.jpg")
//                                          .OrderBy(f => f)
//                                          .ToArray();

//                    if (frames.Length == 0)
//                    {
//                        await Task.Delay(checkIntervalMs, token);
//                        continue;
//                    }

//                    for (; lastFrameIndex < frames.Length; lastFrameIndex++)
//                    {
//                        token.ThrowIfCancellationRequested();

//                        string framePath = frames[lastFrameIndex];
//                        try
//                        {
//                            using var bmp = new Bitmap(framePath);
//                            var tensor = await ConvertImageToTensorAsync(bmp);

//                            var outputObj = _model!.forward(tensor);
//                            if (outputObj is Tensor outputTensor)
//                            {
//                                var detections = ParseModelOutput(outputTensor, 0.5f);

//                                if (detections.Any())
//                                {
//                                    // NSFW detected
//                                    nsfwFound = true;
//                                    Log($"🚨 NSFW detected at frame {lastFrameIndex}: {framePath}");

//                                    DeleteFileSafe(framePath);

//                                    // Stop or pause video
//                                    onUnsafeDetected?.Invoke();

//                                    if (skipForwardAsync != null)
//                                        await skipForwardAsync();

//                                    // Delete remaining frames
//                                    await DeleteAllFramesAsync(token);
//                                    return; // stop analysis immediately
//                                }
//                            }
//                        }
//                        catch (OperationCanceledException)
//                        {
//                            Log("⚠️ Analysis cancelled by user.");
//                            return;
//                        }
//                        catch (Exception ex)
//                        {
//                            Log($"⚠️ Error analyzing frame {lastFrameIndex}: {ex.Message}");
//                        }

//                        await Task.Delay(5, token);
//                    }

//                    await Task.Delay(checkIntervalMs, token);

//                    // If all frames analyzed and no NSFW found
//                    if (!nsfwFound && lastFrameIndex >= frames.Length)
//                    {
//                        Log("✅ Video is safe, continue playing.");
//                        return;
//                    }
//                }
//            }
//            finally
//            {
//                _isAnalyzing = false;
//                if (!nsfwFound)
//                    Log("✅ Video analysis completed: safe video.");
//                await DeleteAllFramesAsync(token);
//            }
//        }


//        // ------------------------- Utilities -------------------------
//        private static void DeleteFileSafe(string path)
//        {
//            try
//            {
//                if (File.Exists(path))
//                {
//                    File.SetAttributes(path, FileAttributes.Normal);
//                    File.Delete(path);
//                    Log($"🗑️ Deleted frame: {Path.GetFileName(path)}");
//                }
//            }
//            catch (IOException)
//            {
//                GC.Collect();
//                GC.WaitForPendingFinalizers();
//            }
//        }

//        public static Task DeleteAllFramesAsync(CancellationToken token = default)
//        {
//            return Task.Run(async () =>
//            {
//                string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NMHMediaPlayer", "Frames");

//                try
//                {
//                    var files = Directory.GetFiles(folderPath, "*.jpg");
//                    int deletedCount = 0;
//                    int batchSize = 500;

//                    for (int i = 0; i < files.Length; i += batchSize)
//                    {
//                        token.ThrowIfCancellationRequested();

//                        var batch = files.Skip(i).Take(batchSize);
//                        foreach (var file in batch)
//                        {
//                            int retry = 3;
//                            while (retry > 0)
//                            {
//                                try
//                                {
//                                    if (File.Exists(file))
//                                    {
//                                        File.SetAttributes(file, FileAttributes.Normal);
//                                        File.Delete(file);
//                                        deletedCount++;
//                                        Log($"🗑️ Deleted frame: {Path.GetFileName(file)}");
//                                        break; // success, exit retry loop
//                                    }
//                                    else break; // file not found
//                                }
//                                catch (IOException)
//                                {
//                                    retry--;
//                                    await Task.Delay(100, token); // wait and retry
//                                }
//                                catch (UnauthorizedAccessException)
//                                {
//                                    retry--;
//                                    await Task.Delay(100, token);
//                                }
//                            }
//                        }

//                        await Task.Delay(50, token); // small pause between batches
//                    }

//                    Log($"🧹 All frames deleted from: {folderPath} | {deletedCount} frames deleted");
//                }
//                catch (OperationCanceledException)
//                {
//                    Log("⚠️ Frame deletion cancelled by user.");
//                }
//                catch (Exception ex)
//                {
//                    Log($"⚠️ Failed to delete frames: {ex.Message}");
//                }
//            }, token);
//        }



//        // ------------------------- Image to Tensor (Windows only) -------------------------
//        private static async Task<Tensor> ConvertImageToTensorAsync(Bitmap bmp)
//        {
//            return await Task.Run(() =>
//            {
//                int modelWidth = 320;
//                int modelHeight = 320;
//                using Bitmap resized = new Bitmap(bmp, new Size(modelWidth, modelHeight));
//                float[,,] data = new float[3, modelHeight, modelWidth];

//                var bmpData = resized.LockBits(
//                    new Rectangle(0, 0, modelWidth, modelHeight),
//                    System.Drawing.Imaging.ImageLockMode.ReadOnly,
//                    System.Drawing.Imaging.PixelFormat.Format24bppRgb
//                );

//                unsafe
//                {
//                    byte* ptr = (byte*)bmpData.Scan0;
//                    int stride = bmpData.Stride;
//                    for (int y = 0; y < modelHeight; y++)
//                    {
//                        for (int x = 0; x < modelWidth; x++)
//                        {
//                            int index = y * stride + x * 3;
//                            data[0, y, x] = ptr[index + 2] / 255f; // R
//                            data[1, y, x] = ptr[index + 1] / 255f; // G
//                            data[2, y, x] = ptr[index + 0] / 255f; // B
//                        }
//                    }
//                }

//                resized.UnlockBits(bmpData);

//                var tensor = torch.tensor(data, dtype: ScalarType.Float32).mul(2).sub(1);
//                return tensor.unsqueeze(0);
//            });
//        }


//        public class DetectionResult
//        {
//            public float X, Y, Width, Height, Confidence;
//            public int ClassId;
//        }

//        public static List<DetectionResult> ParseModelOutput(Tensor output, float confThreshold = 0.5f)
//        {
//            var shape = output.shape;
//            int numAttrs = (int)shape[1];
//            int numBoxes = (int)shape[2];
//            var dets = output.squeeze(0);
//            var results = new List<DetectionResult>();
//            var data = dets.data<float>().ToArray();

//            for (int i = 0; i < numBoxes; i++)
//            {
//                int offset = i * numAttrs;
//                float x = data[offset + 0];
//                float y = data[offset + 1];
//                float w = data[offset + 2];
//                float h = data[offset + 3];
//                float conf = data[offset + 4];
//                if (conf < confThreshold) continue;

//                float maxProb = 0f;
//                int classId = -1;
//                for (int c = 5; c < numAttrs; c++)
//                {
//                    float p = data[offset + c];
//                    if (p > maxProb)
//                    {
//                        maxProb = p;
//                        classId = c - 5;
//                    }
//                }

//                float finalConf = conf * maxProb;
//                if (finalConf < confThreshold) continue;

//                results.Add(new DetectionResult
//                {
//                    X = x,
//                    Y = y,
//                    Width = w,
//                    Height = h,
//                    Confidence = finalConf,
//                    ClassId = classId
//                });
//            }

//            return results;
//        }

//        private static void Log(string message)
//        {
//            try
//            {
//                string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NMHMediaPlayer");
//                Directory.CreateDirectory(logDir);
//                string logPath = Path.Combine(logDir, "filter_log.txt");
//                string finalMsg = $"[{DateTime.Now:HH:mm:ss}] {message}";
//                File.AppendAllText(logPath, finalMsg + "\n");
//                Console.WriteLine(finalMsg);
//            }
//            catch { }
//        }
//    }
//}
