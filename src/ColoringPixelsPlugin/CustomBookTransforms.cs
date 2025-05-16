using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using ColoringPixelsMod;
using HarmonyLib;


namespace ColoringPixelsPlugin.Transform {
    [HarmonyPatch(typeof(MainMenuBookSpawner))]
    [HarmonyPatch("RefreshMainMenuBooks")]
    [HarmonyPatch(new[] { typeof(string) })]
    public class TransformMainMenuBookSpawner_RefreshMainMenuBooks {
        private static MethodInfo toCall = typeof(BookLoader).GetMethod("AddSection");
        
        /*
         * Injects a call to BookLoader.AddSection() into MainMenuBookSpawner.RefreshMainMenuBooks()
         */
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            var targetMethod = TransformMainMenuBookSpawner_RefreshMainMenuBooks.toCall;

            if (targetMethod is null) {
                throw new Exception("Failed to find the target method in BookLoader.");
            }

            int toArrayCallCount = 0;
            bool injected = false;

            foreach (var instruction in instructions) {
                if (!injected && instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo method && method.Name == "ToArray") {
                    toArrayCallCount++;
                    // 8th call to ToArray() is the one we want to inject after
                    // Located right before the try block (currently line 71)
                    if (toArrayCallCount == 8) {
                        CodeInstruction callInsn = new CodeInstruction(OpCodes.Call, targetMethod);
                        yield return callInsn;
                        CustomImagesPlugin.Log.LogInfo($"Injected call to: {targetMethod} ({callInsn.opcode}) {callInsn.operand}");
                        injected = true;
                    }
                }

                yield return instruction;
            }

            if (!injected) {
                throw new Exception("Failed to insert custom method call in MainMenuBookSpawner.");
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


    /*
     * Injects a call to CustomUtil.ExpandTileset() into ClickTest.Setup()
     */
    [HarmonyPatch(typeof(ClickTest))]
    [HarmonyPatch("Setup")]
    [HarmonyPatch(new Type[] {  })]
    public class TransformClickTest_Setup {
        private static MethodInfo toCall = typeof(CustomSprites).GetMethod("ExpandTileset");
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            var targetMethod = TransformClickTest_Setup.toCall;

            if (targetMethod is null) {
                throw new Exception("Failed to find the target method in CustomUtil.");
            }
            int bltCount = 0;
            foreach (var instruction in instructions) {

                if (instruction.opcode == OpCodes.Blt) {
                    bltCount++;
                    yield return instruction;
                    // 2nd blt is the one we want to inject after
                    // Located right before the for loop containing SetSingleCell
                    // Currently line 390
                    if (bltCount == 2) {
                        var callInsn = new CodeInstruction(OpCodes.Call,targetMethod);
                        CustomImagesPlugin.Log.LogInfo($"Injected call to: {targetMethod.Name} ({callInsn.opcode}) {callInsn.operand}");
                        yield return callInsn;
                    }

                    continue;
                }

                yield return instruction;
            }
        }
    }
    
    // todo: zipentry unzip ignore png
    
    // todo: add keyboard support for three number entry
}
