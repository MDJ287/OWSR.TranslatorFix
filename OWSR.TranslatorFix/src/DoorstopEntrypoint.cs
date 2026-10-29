using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace Doorstop
{
    public class Entrypoint
    {
        public static void Start()
        {
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
            MethodDefinition displayNodeMethod = translatorType.Methods.First(method => method.Name == "DisplayTextNode");

            ILProcessor ilProcessor = displayNodeMethod.Body.GetILProcessor();
            Collection<Instruction> instructions = ilProcessor.Body.Instructions;

            // first instance of stfld _numTranslatedWords is the end of the if statement
            Instruction? lastIfBodyInstruction = null;
            int lastIfBodyInstructionIndex = 0;
            for (int i = 0; i < instructions.Count; i++) {
                Instruction instruction = instructions[i];
                if (
                    instruction.OpCode == OpCodes.Stfld &&
                    ((FieldDefinition)instruction.Operand).FullName.Contains("NomaiTranslatorProp::_numTranslatedWords")
                )
                {
                    lastIfBodyInstruction = instruction;
                    lastIfBodyInstructionIndex = i;
                    break;
                }
            }

            if (lastIfBodyInstruction == null) return;

            // instruction before previous instance of ldfld _translationTimeElapsed is the start of the if condition
            Instruction? firstIfConditionInstruction = null;
            for (int i = lastIfBodyInstructionIndex; i >= 0; i--)
            {
                Instruction instruction = instructions[i];
                if (
                    instruction.OpCode == OpCodes.Ldfld &&
                    ((FieldDefinition)instruction.Operand).FullName.Contains("NomaiTranslatorProp::_translationTimeElapsed")
                )
                {
                    firstIfConditionInstruction = instruction.Previous;
                    break;
                }
            }

            if (firstIfConditionInstruction == null) return;

            // branch to the start of the "if" condition
            ilProcessor.InsertAfter(lastIfBodyInstruction, ilProcessor.Create(OpCodes.Br_S, firstIfConditionInstruction));

            // for debugging: write the updated assembly to a file
            // (to then decompile and verify it was updated correctly)

            //asm.Write(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "asm-out.dll"));

            asm.Dispose();
        }
    }
}
