using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Security;
using Mono.Cecil;

namespace CsSandboxing;

internal class Program
{
    private static void MyMethod()
    {
        Console.WriteLine("WRONG");
    }

    public static void Main(string[] args)
    {
        Injection.Swap(
            typeof(FileOld).GetMethod("WriteAllText", BindingFlags.Static | BindingFlags.Public, [typeof(string), typeof(string)]),
            typeof(System.IO.File).GetMethod("WriteAllText", BindingFlags.Static | BindingFlags.Public, [typeof(string), typeof(string)])
        );
        Injection.Swap(
            typeof(System.IO.File).GetMethod("WriteAllText", BindingFlags.Static | BindingFlags.Public, [typeof(string), typeof(string)]),
            typeof(File).GetMethod("WriteAllText", BindingFlags.Static | BindingFlags.Public, [typeof(string), typeof(string)])
        );

        var pluginBytes = System.IO.File.ReadAllBytes(@"CsPlugin.dll");
        //var pluginBytes = System.IO.File.ReadAllBytes(@"C:\Users\Douglas\.nuget\packages\newtonsoft.json\13.0.3\lib\net6.0\Newtonsoft.Json.dll");

        var pluginContext = new IsolatedLoadContext(GetSharedAssemblies());
        var assy = pluginContext.LoadFromStream(new MemoryStream(pluginBytes));

        foreach (var ty in assy.GetTypes())
        {
            if (typeof(ICallable).IsAssignableFrom(ty))
            {
                var constructor = ty.GetConstructor(new Type[] { })!;
                var obj = (ICallable)constructor.Invoke(null);
                obj.CallMe("LMAO! ");
            }
        }

        Console.WriteLine("ok");
        Console.ReadKey();
    }

    private static AssemblyName[] GetSharedAssemblies()
    {
        string[] names = [
            "CsSandboxing",
            "System.Console",
            "System.Runtime",
            "Unbreakable.Runtime",
        ];
        return AppDomain.CurrentDomain.GetAssemblies().Select(x => x.GetName()).Where(x => names.Contains(x.Name)).ToArray();
    }

    class IsolatedLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyName[] _sharedAssemblies;

        public IsolatedLoadContext(AssemblyName[] sharedAssemblies) : base(true)
        {
            _sharedAssemblies = sharedAssemblies.ToArray();
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            // One way to deal with permissions is to shim or disable all RT methods 
            Console.WriteLine($"LOAD {assemblyName.Name}");
            //check for shared assemblies, return null because they'll be loaded by default AssemblyLoadContext 
            if (_sharedAssemblies.FirstOrDefault(x => x.FullName == assemblyName.FullName) != null)
            {
                return null;
            }

            throw new NotSupportedException($"Unable to load managed assembly: {assemblyName.Name}");
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            throw new NotSupportedException($"Unable to load unmanaged assembly: {unmanagedDllName}");
        }
    }

    public static class File
    {
        public static byte[] ReadAllBytes(string path)
        {
            Console.WriteLine("Check calling assembly has permissions.");
            return FileOld.ReadAllBytes(path);
        }

        public static void WriteAllText(string path, string text)
        {
            Console.WriteLine("Check calling assembly has permissions.");
            FileOld.WriteAllText(path, text);
        }
    }

    public static class FileOld
    {
        public static void WriteAllText(string path, string text) { }
        public static byte[] ReadAllBytes(string path) { throw new NotImplementedException(); }
    }

    public class Injection
    {
        public static void Swap(MethodInfo methodToReplace, MethodInfo methodToInject)
        {
            RuntimeHelpers.PrepareMethod(methodToReplace.MethodHandle);
            RuntimeHelpers.PrepareMethod(methodToInject.MethodHandle);
            if (methodToReplace.IsVirtual)
            {
                ReplaceVirtualInner(methodToReplace, methodToInject);
            }
            else
            {
                ReplaceInner(methodToReplace, methodToInject);
            }
        }

        static void ReplaceVirtualInner(MethodInfo methodToReplace, MethodInfo methodToInject)
        {
            unsafe
            {
                UInt64* methodDesc = (UInt64*)(methodToReplace.MethodHandle.Value.ToPointer());
                int index = (int)(((*methodDesc) >> 32) & 0xFF);
                if (IntPtr.Size == 4)
                {
                    uint* classStart = (uint*)methodToReplace.DeclaringType!.TypeHandle.Value.ToPointer();
                    classStart += 10;
                    classStart = (uint*)*classStart;
                    uint* tar = classStart + index;

                    uint* inj = (uint*)methodToInject.MethodHandle.Value.ToPointer() + 2;
                    *tar = *inj;
                }
                else
                {
                    ulong* classStart = (ulong*)methodToReplace.DeclaringType!.TypeHandle.Value.ToPointer();
                    classStart += 8;
                    classStart = (ulong*)*classStart;
                    ulong* tar = classStart + index;

                    ulong* inj = (ulong*)methodToInject.MethodHandle.Value.ToPointer() + 1;
                    *tar = *inj;
                }
            }
        }

        public delegate void Callback22();

        static void ReplaceInner(MethodInfo methodToReplace, MethodInfo methodToInject)
        {
            unsafe
            {
                if (IntPtr.Size == 4)
                {
                    int* inj = (int*)methodToInject.MethodHandle.Value.ToPointer() + 2;
                    int* tar = (int*)methodToReplace.MethodHandle.Value.ToPointer() + 2;
                    *tar = *inj;
                }
                else
                {
                    ulong* inj = (ulong*)methodToInject.MethodHandle.Value.ToPointer() + 1;
                    ulong* tar = (ulong*)methodToReplace.MethodHandle.Value.ToPointer() + 1;
                    *tar = *inj;
                }
            }
        }
    }
}

/*using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Reflection;
using System.Runtime.Loader;

namespace CsSandboxing;

internal class Program
{
    public static void Main(string[] args)
    {
        var pluginBytes = System.IO.File.ReadAllBytes(@"C:\Users\Douglas\Documents\GitHub\CsSandboxing\CsPlugin\bin\Debug\net8.0\CsPlugin.dll");
        //var pluginBytes = System.IO.File.ReadAllBytes(@"C:\Users\Douglas\.nuget\packages\newtonsoft.json\13.0.3\lib\net6.0\Newtonsoft.Json.dll");

        var pluginAssy = AssemblyDefinition.ReadAssembly(new MemoryStream(pluginBytes));

        foreach (var refAssy in pluginAssy.MainModule.AssemblyReferences)
        {
            Console.WriteLine($"{refAssy.FullName}");
        }

        foreach (var module in pluginAssy.Modules)
        {
            foreach (var type in module.Types)
            {
                foreach (var method in type.Methods)
                {
                    //Console.WriteLine($"f {type.Name}::{method.Name}");

                    if (method.HasBody)
                    {
                        foreach (var local in method.Body.Variables)
                        {
                            if (local.VariableType.IsPointer || local.VariableType.IsFunctionPointer)
                            {
                                Console.WriteLine($"USE OF UNSAFE IN {pluginAssy.Name} {type.Name}::{method.FullName} : {local.ToString()}");
                            }
                        }

                        foreach (var op in method.Body.Instructions)
                        {
                            if (op.OpCode.Code == Code.Jmp
                                || op.OpCode.Code == Code.Ldloca
                                || op.OpCode.Code == Code.Ldarga)
                            {
                                Console.WriteLine($"USE OF UNSAFE IN {pluginAssy.Name} {type.Name}::{method.FullName} : {op.OpCode.Code}");
                            }

                            if (op.OpCode.Code == Code.Call || op.OpCode.Code == Code.Calli)
                            {
                                if (op.Operand is MethodReference target)
                                {
                                    Console.WriteLine($"have call {target.Name} in {target.DeclaringType}");
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /*
        var pluginContext = new IsolatedLoadContext(GetSharedAssemblies());
        var assy = pluginContext.LoadFromStream(new MemoryStream(pluginBytes));

        foreach (var ty in assy.GetTypes())
        {
            if (typeof(ICallable).IsAssignableFrom(ty))
            {
                var constructor = ty.GetConstructor(new Type[] { })!;
                var obj = (ICallable)constructor.Invoke(null);
                obj.CallMe("LMAO! ");
                Console.WriteLine($"Has {obj.TestType() == typeof(File)} || " + ty.Name);
            }
        }

        Console.WriteLine("ok");
        Console.ReadKey();
    }

    private static AssemblyName[] GetSharedAssemblies()
    {
        string[] names = [
            "CsSandboxing",
            "System.Console",
            "System.Runtime"
        ];
        return AppDomain.CurrentDomain.GetAssemblies().Select(x => x.GetName()).Where(x => names.Contains(x.Name)).ToArray();
    }

    class IsolatedLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyName[] _sharedAssemblies;

        public IsolatedLoadContext(AssemblyName[] sharedAssemblies) : base(true)
        {
            _sharedAssemblies = sharedAssemblies.ToArray();
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            // One way to deal with permissions is to shim or disable all RT methods 
            Console.WriteLine($"LOAD {assemblyName.Name}");
            //check for shared assemblies, return null because they'll be loaded by default AssemblyLoadContext 
            if (_sharedAssemblies.FirstOrDefault(x => x.FullName == assemblyName.FullName) != null)
            {
                return null;
            }

            throw new NotSupportedException($"Unable to load managed assembly: {assemblyName.Name}");
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            throw new NotSupportedException($"Unable to load unmanaged assembly: {unmanagedDllName}");
        }
    }
}
*/