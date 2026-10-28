using System;
using System.IO;
using System.Linq;
using Mono.Cecil;

namespace Doorstop
{
    public class Entrypoint
    {
        public static void Start()
        {
            File.WriteAllText("mdj.txt", "thingy works");
            string asmDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OuterWilds_Data", "Managed", "Assembly-CSharp.dll");
            DefaultAssemblyResolver resolver = new DefaultAssemblyResolver();
            ReaderParameters readerParams = new ReaderParameters
            {
                AssemblyResolver = resolver,
                ReadWrite = false
            };
            AssemblyDefinition asm = AssemblyDefinition.ReadAssembly(asmDirectory, readerParams);
            ModuleDefinition module = asm.MainModule;





            asm.Write(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "asm-out.dll"));

            asm.Dispose();
        }
    }
}
