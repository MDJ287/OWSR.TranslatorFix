using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace Doorstop
{
    public class Entrypoint
    {
        private static string patchVersion = "0.1.1";

        public static void Start()
        {
            string asmDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OuterWilds_Data", "Managed", "Assembly-CSharp.dll");
            string thisDirectory = typeof(Entrypoint).Assembly.Location;

            DefaultAssemblyResolver resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(Path.GetDirectoryName(asmDirectory));
            resolver.AddSearchDirectory(Path.GetDirectoryName(thisDirectory));
            resolver.AddSearchDirectory(Path.GetDirectoryName(typeof(object).Assembly.Location));
            ReaderParameters readerParams = new ReaderParameters
            {
                AssemblyResolver = resolver,
                ReadWrite = false
            };

            AssemblyDefinition asm = AssemblyDefinition.ReadAssembly(asmDirectory, readerParams);
            ModuleDefinition module = asm.MainModule;

            /** PATCH DISPLAY */

            string patchText = $"Speedrun Patch v{patchVersion}: TranslatorFix";

            TypeDefinition titleScreenType = module.GetType("TitleScreenManager");
            MethodDefinition fadeInTitleMethod = titleScreenType.Methods.First(method => method.Name == "FadeInTitleLogo");

            ILProcessor titleScreenIl = fadeInTitleMethod.Body.GetILProcessor();
            Collection<Instruction> titleScreenInstructions = titleScreenIl.Body.Instructions;

            // before first ldfld _gameVersionTextDisplay is when we want to update the string
            Instruction? loadVersionDisplayInstruction = null;
            for (int i=0; i <  titleScreenInstructions.Count; i++)
            {
                Instruction instruction = titleScreenInstructions[i];
                if (
                    instruction.OpCode == OpCodes.Ldfld &&
                    ((FieldDefinition)instruction.Operand).FullName.Contains("TitleScreenManager::_gameVersionTextDisplay")
                )
                {
                    loadVersionDisplayInstruction = instruction.Previous;
                    break;
                }
            }
            if (loadVersionDisplayInstruction == null) return;

            MethodReference concatReference = module.ImportReference(typeof(string).GetMethod(
                "Concat",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string), typeof(string) },
                null
            ));

            // append text to ow version number
            Instruction[] instructionsToAdd = new Instruction[] {
                // text += (string)
                titleScreenIl.Create(OpCodes.Ldloc_0),
                titleScreenIl.Create(OpCodes.Ldstr, $"\n{patchText}"),
                titleScreenIl.Create(OpCodes.Call, concatReference),
                titleScreenIl.Create(OpCodes.Stloc_0),
                // re-add the instruction we removed
                // (we can't re-add the original because old branch references will point to it)
                titleScreenIl.Create(OpCodes.Ldarg_0)
            };

            titleScreenIl.Replace(loadVersionDisplayInstruction, instructionsToAdd[0]);
            
            for (int i=1; i<instructionsToAdd.Length; i++)
            {
                titleScreenIl.InsertAfter(instructionsToAdd[i-1], instructionsToAdd[i]);
            }

            /** TRANSLATOR FIX (todo: move this into another class) */

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

            // branch to the start of the "if" condition at the end of its body (turn it into a while loop)
            ilProcessor.InsertAfter(lastIfBodyInstruction, ilProcessor.Create(OpCodes.Br_S, firstIfConditionInstruction));

            // write the modified assembly to a file in the mod's folder
            // this will be read by the game in place of the original file
            // due to unity doorstop's dll_search_path_override
            asm.Write(Path.Combine(Path.GetDirectoryName(thisDirectory), "Assembly-CSharp.dll"));

            // cleanup
            asm.Dispose();
        }
    }
}
