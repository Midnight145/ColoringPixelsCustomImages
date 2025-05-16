using System.IO;
using ColoringPixelsMod;
using HarmonyLib;
using UnityEngine;

    public class BookLoader : MonoBehaviour {
        private static readonly string bookName = "Custom";
        private static readonly int hash = -1;
        private static readonly int achievementID = -1;
        private static readonly string achievementName = "";
        private static readonly string discordImageId = "";
        private static readonly BookType bookType = BookType.Free;
        private static readonly string path = Path.Combine(Application.persistentDataPath, "Custom");
        private static readonly bool register = Directory.Exists(BookLoader.path);

        // public static void AddCustomBook(ref BookDetails[] inst) {
        public static BookDetails[] AddCustomBook() {
            R allBookDetails = R.of(typeof(AllBookDetails));
            BookDetails[] inst = allBookDetails.GetField("_inst") as BookDetails[];
            if (!BookLoader.register) {
                return inst;
            }
            
            for (int i = inst.Length - 1; i >= 0; i--) {
                if (inst[i].bookName == BookLoader.bookName) {
                    return inst;
                }
            }
            BookDetails[] array = new BookDetails[inst.Length + 1];
            for (int i = 0; i < inst.Length; i++) {
                array[i] = inst[i];
            }
            BookDetails bookDetails = ScriptableObject.CreateInstance<BookDetails>();
            bookDetails.bookName = BookLoader.bookName;
            bookDetails.bookIndex = inst.Length;
            bookDetails.bookHash = BookLoader.hash;
            bookDetails.mainMenuTitle = BookLoader.bookName;
            bookDetails.steamID = BookLoader.achievementID;
            bookDetails.steamAchievement = BookLoader.achievementName;
            bookDetails.discordImageId = BookLoader.discordImageId;
            bookDetails.bookType = BookLoader.bookType;
            ImageLoader.Init();
            bookDetails.levels = ImageLoader.levels;
            array[array.Length - 1] = bookDetails;
            allBookDetails.SetField("_inst", array);
            return array;
        }

        public static void AddSection() {
            MainMenuBookSpawner mainMenuBookSpawner = BookLoader.FindObjectOfType<MainMenuBookSpawner>();
            if (!BookLoader.register) {
                return;
            }
            R.of(mainMenuBookSpawner).CallMethod("AddSection", new[] {AllBookDetails.GetBookDetailsFromHash(BookLoader.hash)}, "");
        }
    }
