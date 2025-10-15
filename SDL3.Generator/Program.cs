
using System.CodeDom.Compiler;
using System.Reflection;
using System.Text;

string outDirectory = "../../../../SDL3/";

StringBuilder sb = new();
IndentedTextWriter writer = new(new StringWriter(sb), "    ");
writer.WriteLine("using System;");
writer.WriteLine("using System.Numerics;");
writer.WriteLine("namespace SDL;");


Assembly assembly = Assembly.LoadFrom("SDL.Interop.dll");

Type sdlType = assembly.GetType("bottlenoselabs.Interop.SDL")!;
foreach (var type in sdlType.GetNestedTypes())
{
    var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
    Console.WriteLine($"{type.Name}");

    if (!type.Name.StartsWith("SDL_"))
    {
        continue;
    }

    if (type.BaseType == typeof(Enum))
    {
        EmitEnum(type);
    }
    else if (fields.Length == 0)
    {
        EmitClass(type);
    }
    else
    {
        EmitStruct(type, fields);
    }
}

File.WriteAllText(outDirectory + "SDL3.g.cs", sb.ToString());

string SnakeToCamel(string snakeCase)
{
    snakeCase = char.ToUpper(snakeCase[0]) + snakeCase[1..];
    while (snakeCase.Contains('_'))
    {
        int index = snakeCase.IndexOf('_');
        snakeCase = snakeCase[..index] + char.ToUpper(snakeCase[index + 1]) + snakeCase[(index + 2)..];
    }

    return snakeCase;
}

int FindCommonPrefix(string[] names)
{
    int minLength = names.Min(n => n.Length);
    int prefixLength = 0;
    for (int i = 0; i < minLength; i++)
    {
        char comp = names[0][i];
        foreach (var name in names.Skip(1))
        {
            if (name[i] != comp)
            {
                return prefixLength;
            }
        }
        prefixLength++;
    }
    return -1;
}

string ProcessTypeName(string typeName)
{
    switch (typeName)
    {
        case "Void": return "void";
        case "Void*": return "void*";
        case "CBool": return "bool";
        case "CString": return "ReadOnlySpan<char>";
        case "CString*": return "ReadOnlySpan<string>";

        case "Int8": return "sbyte";
        case "UInt8": return "byte";

        case "Int16": return "short";
        case "UInt16": return "ushort";

        case "Int32": return "int";
        case "UInt32": return "uint";
        
        case "Int64": return "long";
        case "UInt64": return "ulong";
        
        case "Single": return "float";

        case "Rgba32F": return "Vector4";
        default: break;
    }

    if (typeName.StartsWith("SDL_"))
    {
        typeName = typeName[4..];
    }

    return SnakeToCamel(typeName);
}

void EmitEnum(Type type)
{
    writer.WriteLine($"public enum {type.Name[4..]}");
    writer.WriteLine("{");
    writer.Indent++;

    var fields = type.GetFields(BindingFlags.Static | BindingFlags.Public);
    int prefixLength = FindCommonPrefix(fields.Select(f => f.Name).ToArray());
    foreach (var member in fields)
    {
        string name = member.Name;
        if (prefixLength > 0) 
        { 
            name = name[prefixLength..];
        }

        name = SnakeToCamel(name.ToLower());

        if (char.IsNumber(name[0]))
        {
            name = "_" + name;
        }

        writer.WriteLine(name + ",");
    }

    writer.Indent--;
    writer.WriteLine("}");
}

void EmitStruct(Type type, FieldInfo[] fields)
{
    writer.WriteLine($"public unsafe struct {ProcessTypeName(type.Name)}");
    writer.WriteLine("{");
    writer.Indent++;

    foreach (var field in fields)
    {
        if (field.FieldType.IsFunctionPointer)
        {
            continue;
        }

        string fieldType = field.FieldType.Name;

        if (fieldType.StartsWith("<"))
        {
            Console.WriteLine(string.Join(' ', field.FieldType.GetCustomAttributes().Select(a => a.GetType().ToString())));
            fieldType = "byte[]";
        }

        string fieldName = field.Name;

        writer.WriteLine($"public {ProcessTypeName(fieldType)} {SnakeToCamel(fieldName)};");
    }

    writer.Indent--;
    writer.WriteLine("}");
}

void EmitClass(Type type)
{
    writer.WriteLine($"public sealed unsafe class {ProcessTypeName(type.Name)}");
    writer.WriteLine("{");
    writer.Indent++;

    writer.Indent--;
    writer.WriteLine("}");
}