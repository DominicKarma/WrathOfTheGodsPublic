using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Shaders.Screen;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics.Shaders
{
    public class ShaderRecompilationMonitor : ModSystem
    {
        public static Queue<string> CompilingFiles
        {
            get;
            private set;
        }

        public FileSystemWatcher ShaderWatcher
        {
            get;
            private set;
        }

        public static string EffectsPath
        {
            get;
            private set;
        }

        public override void OnModLoad()
        {
            // Don't do anything development features are disabled.
            if (!NoxusBoss.DebugFeaturesEnabled)
                return;

            CompilingFiles = new();
            if (Main.netMode != NetmodeID.SinglePlayer)
                return;

            // Check to see if the user has a folder that corresponds to the shaders for this mod.
            // If this folder is not present, that means that they are not a developer and thusly this system would be irrelevant.
            string modSourcesPath = $"{Path.Combine(Program.SavePathShared, "ModSources")}\\{Mod.Name}".Replace("\\..\\tModLoader", string.Empty);
            if (!Directory.Exists(modSourcesPath))
                return;

            // Verify that the Assets/Effects directory exists.
            EffectsPath = $"{modSourcesPath}\\Assets\\Effects";
            if (!Directory.Exists(EffectsPath))
                return;

            // If the Assets/Effects directory exists, watch over it.
            ShaderWatcher = new(EffectsPath)
            {
                Filter = "*.fx",
                IncludeSubdirectories = true,
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.Attributes | NotifyFilters.CreationTime | NotifyFilters.FileName | NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.Security
            };
            ShaderWatcher.Changed += RecompileShader;
        }

        public override void PostUpdateEverything()
        {
            // Don't do anything development features are disabled.
            if (!NoxusBoss.DebugFeaturesEnabled)
                return;

            bool shaderIsCompiling = false;
            List<string> compiledFiles = new();
            string compilerDirectory = EffectsPath + "\\Compiler\\";
            while (CompilingFiles.TryDequeue(out string shaderPath))
            {
                // Take the contents of the new shader and copy them over to the compiler folder so that the XNB can be regenerated.
                string shaderPathInCompilerDirectory = compilerDirectory + Path.GetFileName(shaderPath);
                File.Delete(shaderPathInCompilerDirectory);
                File.WriteAllText(shaderPathInCompilerDirectory, File.ReadAllText(shaderPath));
                shaderIsCompiling = true;
                compiledFiles.Add(shaderPath);
            }

            if (shaderIsCompiling)
            {
                // Execute EasyXNB.
                Process easyXnb = new()
                {
                    StartInfo = new()
                    {
                        FileName = EffectsPath + "\\Compiler\\EasyXnb.exe",
                        WorkingDirectory = compilerDirectory,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };
                easyXnb.Start();
                if (!easyXnb.WaitForExit(3000))
                {
                    Main.NewText("Shader compiler timed out. Likely error.");
                    easyXnb.Kill();
                    return;
                }

                easyXnb.Kill();
            }

            for (int i = 0; i < compiledFiles.Count; i++)
            {
                // Copy over the XNB from the compiler, and delete the copy in the Compiler folder.
                string shaderPath = compiledFiles[i];
                string compiledXnbPath = EffectsPath + "\\Compiler\\" + Path.GetFileNameWithoutExtension(shaderPath) + ".xnb";
                string originalXnbPath = shaderPath.Replace(".fx", ".xnb");
                File.Delete(originalXnbPath);
                File.Copy(compiledXnbPath, originalXnbPath);
                File.Delete(compiledXnbPath);

                // Finally, load the new XNB's shader data into the game's managed wrappers that reference it.
                string shaderPathInCompilerDirectory = compilerDirectory + Path.GetFileName(shaderPath);
                File.Delete(shaderPathInCompilerDirectory);
                Main.QueueMainThreadAction(() =>
                {
                    ContentManager tempManager = new(Main.instance.Content.ServiceProvider, Path.GetDirectoryName(originalXnbPath));
                    string assetName = Path.GetFileNameWithoutExtension(originalXnbPath);
                    Effect recompiledEffect = tempManager.Load<Effect>(assetName);
                    Ref<Effect> refEffect = new(recompiledEffect);
                    ShaderManager.SetShader(Path.GetFileNameWithoutExtension(compiledXnbPath), refEffect);

                    if (assetName.Contains("LightSlashes"))
                    {
                        Filters.Scene[LightSlashesOverlayShaderData.ShaderKey].Deactivate();
                        Filters.Scene[LightSlashesOverlayShaderData.ShaderKey] = new Filter(new LightSlashesOverlayShaderData(refEffect, ManagedShader.DefaultPassName), EffectPriority.VeryHigh);
                    }

                    Main.NewText($"Shader with the file name '{Path.GetFileName(shaderPath)}' has been successfully recompiled.");
                });
            }
        }

        public override void OnModUnload()
        {
            ShaderWatcher?.Dispose();
        }

        private void RecompileShader(object sender, FileSystemEventArgs e)
        {
            // Prevent the shader watcher from looking in the compiler folder.
            if (e.FullPath.Contains("\\Compiler"))
                return;

            // Prevent compiling files from being listed twice.
            if (CompilingFiles.Contains(e.FullPath))
                return;

            CompilingFiles.Enqueue(e.FullPath);
        }
    }
}
