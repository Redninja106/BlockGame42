using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace BlockGame42.Assets.Pipeline;
internal class ShaderPipeline : AssetPipeline
{
    
    public override string PrimaryInputExtension => ".slang";
    public override string PrimaryOutputExtension => ".cso";

    public override void Build(AssetBuildContext context)
    {
        string source = File.ReadAllText(context.InputFile);

        bool isComputeShader = false;
        string? shaderModel = null;
        if (source.Contains("""[shader("vertex")]"""))
        {
            shaderModel = "vs_6_5";
        }
        else if (source.Contains("""[shader("fragment")]"""))
        {
            shaderModel = "ps_6_5";
        }
        else if (source.Contains("""[shader("compute")]"""))
        {
            shaderModel = "cs_6_5";
            isComputeShader = true;
        }
        else
        {
            Console.WriteLine($"shader {context.InputFile} has no entrypoint!");
            return; // no entrypoint
        }

        string reflectionJsonPath = context.GetOutputFilePath(".reflection.json");
        string hlslPath = context.GetOutputFilePath(".hlsl");
        string debugPath = context.GetOutputFilePath(".pdb");

        SlangCompiler(context.InputFile, 
            target: "hlsl",
            profile: shaderModel,
            output: hlslPath,
            reflectionJson: reflectionJsonPath
            );

        DxCompiler(hlslPath, shaderModel, context.GetOutputFilePath(".cso"), debugPath);

        JsonDocument reflectionDocument = JsonDocument.Parse(File.ReadAllText(reflectionJsonPath));
        JsonElement reflection = reflectionDocument.RootElement;
        
        JsonObject metadata = new();

        JsonElement entryPoint = reflection.GetProperty("entryPoints").EnumerateArray().Single();
        metadata.Add("entryPoint", entryPoint.GetProperty("name").GetString());
        metadata.Add("stage", entryPoint.GetProperty("stage").GetString());

        int numSamplers = 0, 
            numStorageBuffers = 0, 
            numStorageTextures = 0, 
            numUniformBuffers = 0, 
            numRWStorageBuffers = 0, 
            numRWStorageTextures = 0;

        void ProcessParameter(JsonElement binding, JsonElement type)
        {
            switch (binding.GetProperty("kind").GetString())
            {
                case "constantBuffer":
                    numUniformBuffers++;
                    break;
                case "samplerState":
                    numSamplers++;
                    break;
                case "shaderResource":
                    switch (type.GetProperty("baseShape").GetString())
                    {
                        case "texture1D":
                        case "texture2D":
                        case "texture3D":
                            numStorageTextures++;
                            break;

                        case "byteAddressBuffer":
                        case "structuredBuffer":
                            numStorageBuffers++;
                            break;
                    }
                    break;
                case "unorderedAccess":
                    switch (type.GetProperty("baseShape").GetString())
                    {
                        case "texture1D":
                        case "texture2D":
                        case "texture3D":
                            numRWStorageTextures++;
                            break;

                        case "byteAddressBuffer":
                        case "structuredBuffer":
                            numRWStorageBuffers++;
                            break;
                    }
                    break;
            }
        }

        foreach (JsonElement parameter in reflection.GetProperty("parameters").EnumerateArray())
        {
            JsonElement type = parameter.GetProperty("type");
            if (parameter.TryGetProperty("binding", out JsonElement binding))
            {
                ProcessParameter(binding, type);
            }

            if (parameter.TryGetProperty("bindings", out JsonElement bindings))
            {
                foreach (var b in bindings.EnumerateArray())
                {
                    ProcessParameter(b, type);
                }
            }
        }
        
        metadata.Add("numSamplers", numSamplers);
        metadata.Add("numStorageBuffers", numStorageBuffers);
        metadata.Add("numStorageTextures", numStorageTextures - numSamplers);
        metadata.Add("numUniformBuffers", numUniformBuffers);
        if (isComputeShader)
        {
            metadata.Add("numRWStorageBuffers", numRWStorageBuffers);
            metadata.Add("numRWStorageTextures", numRWStorageTextures);

            var threadGroupSize = entryPoint.GetProperty("threadGroupSize").EnumerateArray();
            _ = threadGroupSize.MoveNext();
            metadata.Add("threadCountX", threadGroupSize.Current.GetUInt32());
            _ = threadGroupSize.MoveNext();
            metadata.Add("threadCountY", threadGroupSize.Current.GetUInt32());
            _ = threadGroupSize.MoveNext();
            metadata.Add("threadCountZ", threadGroupSize.Current.GetUInt32());
        }

        string metadataJsonPath = context.GetOutputFilePath(".metadata.json");
        File.WriteAllText(metadataJsonPath, metadata.ToJsonString());
    }

    private void SlangCompiler(string inputFile, string? target = null, string? profile = null, string? stage = null, string? output = null, string? reflectionJson = null)
    {
        string SlangCompilerPath = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule!.FileName)!, "../../../slang-2025.17-windows-x86_64/bin/slangc.exe");

        List<string> args = [inputFile];
        
        if (target != null)
        {
            args.Add("-target");
            args.Add(target);
        }

        if (profile != null)
        {
            args.Add("-profile");
            args.Add(profile);
        }

        if (output != null)
        {
            args.Add("-o");
            args.Add(output);
        }

        if (reflectionJson != null)
        {
            args.Add("-reflection-json");
            args.Add(reflectionJson);
        }

        Console.WriteLine($"{SlangCompilerPath} {string.Join(" ", args)}");

        Process process = Process.Start(SlangCompilerPath, args);
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new("slangc failed!");
        }
    }

    private void DxCompiler(string inputFile, string? target = null, string? output = null, string? debugInfo = null)
    {
        string DxCompilerPath = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule!.FileName)!, "../../../slang-2025.17-windows-x86_64/bin/dxc.exe");
        
        List<string> args = [inputFile];

        if (target != null)
        {
            args.Add("-T");
            args.Add(target);
        }

        if (output != null)
        {
            args.Add("-Fo");
            args.Add(output);
        }

        if (debugInfo != null)
        {
            args.AddRange(["-Od", "-Gfp", "-Zi", "-Fd", debugInfo]);
        }

        Console.WriteLine($"{DxCompilerPath} {string.Join(" ", args)}");

        Process process = Process.Start(DxCompilerPath, args);
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new("dxc failed!");
        }
    }
}
