using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using ColoringPixelsMod;
using ColoringPixelsMod.Custom;
using HarmonyLib;


namespace ColoringPixelsPlugin.Transform {
    [HarmonyPatch(typeof(MainMenuBookSpawner))]
    [HarmonyPatch("RefreshMainMenuBooks")]
    [HarmonyPatch(new[] { typeof(string) })]
    public class TransformMainMenuBookSpawner_RefreshMainMenuBooks {
        private static MethodInfo toCall = typeof(BookLoader).GetMethod("AddSection");
        
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            if (TransformMainMenuBookSpawner_RefreshMainMenuBooks.toCall is null) {
                throw new Exception("Failed to find the target method in BookLoader.");
            }
            
            int count = 0;
            bool found = false;
            foreach (var instruction in instructions) {
                if (!found) {
                    if (instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo method) {
                        if (method.Name == "ToArray") {
                            count += 1;
                            if (count == 8) {
                                CodeInstruction insn = new CodeInstruction(OpCodes.Call,
                                    TransformMainMenuBookSpawner_RefreshMainMenuBooks.toCall);
                                CustomImagesPlugin.Log.LogInfo($"Inserting Custom Instruction: {insn.opcode} - {insn.operand}");
                                yield return insn;
                                found = true;
                            }
                        }
                    }
                }

                yield return instruction;
            }

            if (found is false) {
                throw new Exception("Failed to find the target method in MainMenuBookSpawner.");
            }
        }
    }

    [HarmonyPatch(typeof(AllBookDetails))]
    [HarmonyPatch("inst", MethodType.Getter)]
    public class TransformAllBookDetails_get_inst {
        public static void Postfix(ref BookDetails[] __result) {
            __result = BookLoader.AddCustomBook();
        }
    }

    [HarmonyPatch(typeof(ClickTest))]
    [HarmonyPatch("Setup")]
    [HarmonyPatch(new Type[] {  })]
    public class TransformClickTest_CustomBook_Setup {
        private static MethodInfo toCall = typeof(CustomUtil).GetMethod("ExpandTileset");
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            CustomImagesPlugin.Log.LogInfo($"TransformClickTest_CustomBook_Setup");
            if (TransformClickTest_CustomBook_Setup.toCall is null) {
                throw new Exception("Failed to find the target method in CustomUtil.");
            }
            bool first = false;
            bool found = false;
            foreach (CodeInstruction instruction in instructions) {
                CustomImagesPlugin.Log.LogInfo($"Current Instruction: {instruction.opcode} - {instruction.operand}");
                if (!found && instruction.opcode == OpCodes.Blt) {
                    CustomImagesPlugin.Log.LogInfo("Found Blt_S");
                    if (!first) {
                        CustomImagesPlugin.Log.LogInfo("Found First Blt_S");
                        first = true;
                        yield return instruction;
                        continue;
                    }
                    yield return instruction;
                    CustomImagesPlugin.Log.LogInfo("Found Second Blt_S");
                    found = true;
                    var insn = new CodeInstruction(OpCodes.Call, TransformClickTest_CustomBook_Setup.toCall);
                    CustomImagesPlugin.Log.LogInfo($"Inserting Custom Instruction: {insn.opcode} - {insn.operand}");
                    yield return insn;
                    continue;
                }
                yield return instruction;
            }
        }
    }
    
    // todo: zipentry unzip ignore png
    
    // todo: add keyboard support for three number entry
}
