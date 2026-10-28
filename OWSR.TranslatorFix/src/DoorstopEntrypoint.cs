using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

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


            TypeDefinition translatorType = module.GetType("NomaiTranslatorProp");
            FieldDefinition numTranslatedWordsField = translatorType.Fields.First(field => field.FullName.Contains("NomaiTranslatorProp::_numTranslatedWords"));
            File.WriteAllText("fieldname.txt", numTranslatedWordsField.FullName); // TEMP
            MethodDefinition displayNodeMethod = translatorType.Methods.First(method => method.Name == "DisplayTextNode");

            ILProcessor ilProcessor = displayNodeMethod.Body.GetILProcessor();
            Instruction lastIfBodyInstruction = ilProcessor.Body.Instructions.First(instruction =>
                instruction.OpCode == OpCodes.Stfld &&
                (FieldDefinition)instruction.Operand == numTranslatedWordsField
            );
            // branch -87 steps (to the start of the "if" condition
            ilProcessor.InsertAfter(lastIfBodyInstruction, ilProcessor.Create(OpCodes.Br_S, (byte)(-87 + 0x100)));


            asm.Write(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "asm-out.dll"));

            asm.Dispose();
        }
    }
}
