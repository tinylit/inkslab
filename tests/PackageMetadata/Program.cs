using System;
using System.IO;
using System.IO.Compression;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace PackageMetadata
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            int inspected = 0;
            foreach (string path in args)
            {
                using var package = ZipFile.OpenRead(path);
                foreach (var entry in package.Entries)
                {
                    if (!entry.FullName.StartsWith("lib/", StringComparison.Ordinal)
                        || !entry.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    using var input = entry.Open();
                    using var bytes = new MemoryStream();
                    input.CopyTo(bytes);
                    bytes.Position = 0;
                    using var pe = new PEReader(bytes);
                    var metadata = pe.GetMetadataReader();
                    foreach (var handle in metadata.GetAssemblyDefinition().GetCustomAttributes())
                    {
                        var attribute = metadata.GetCustomAttribute(handle);
                        EntityHandle type;
                        if (attribute.Constructor.Kind == HandleKind.MemberReference)
                        {
                            type = metadata.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent;
                        }
                        else if (attribute.Constructor.Kind == HandleKind.MethodDefinition)
                        {
                            type = metadata.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor).GetDeclaringType();
                        }
                        else { continue; }

                        string typeName;
                        string typeNamespace;
                        if (type.Kind == HandleKind.TypeReference)
                        {
                            var reference = metadata.GetTypeReference((TypeReferenceHandle)type);
                            typeName = metadata.GetString(reference.Name);
                            typeNamespace = metadata.GetString(reference.Namespace);
                        }
                        else if (type.Kind == HandleKind.TypeDefinition)
                        {
                            var definition = metadata.GetTypeDefinition((TypeDefinitionHandle)type);
                            typeName = metadata.GetString(definition.Name);
                            typeNamespace = metadata.GetString(definition.Namespace);
                        }
                        else { continue; }

                        if (typeName == "InternalsVisibleToAttribute"
                            && typeNamespace == "System.Runtime.CompilerServices")
                        {
                            Console.Error.WriteLine(path + ": " + entry.FullName + " contains InternalsVisibleToAttribute.");
                            return 1;
                        }
                    }
                    inspected++;
                }
            }
            if (inspected == 0) { throw new InvalidOperationException("No packaged DLLs were inspected."); }
            Console.WriteLine("No InternalsVisibleToAttribute in " + inspected + " packaged DLLs.");
            return 0;
        }
    }
}
