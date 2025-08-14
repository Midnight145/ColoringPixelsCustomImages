using System.Linq;
using ColoringPixelsMod;
using UnityEngine;

namespace ColoringPixelsPlugin.Custom {
    public class BookLoader : MonoBehaviour {
        public static void AddBook(ref BookDetails[] inst, CustomBook book) {
            if (!book.register || inst.Any(item => item.bookName == book.bookName)) {
                return;
            }

            BookDetails[] array = new BookDetails[inst.Length + 1];
            for (int i = 0; i < inst.Length; i++) {
                array[i] = inst[i];
            }

            BookDetails bookDetails = book.GetBookDetails(inst.Length);
            // bookDetails.levels = ImageLoader.
            array[array.Length - 1] = bookDetails;
            inst = array;
        }


        public static BookDetails[] AddCustomBook() {
            ImageLoader.Initialize();
            foreach (CustomBook book in CustomBook.books) {
                AddBook(ref AllBookDetails._inst, book);

            }
            return AllBookDetails._inst;
        }

        // ReSharper disable once UnusedMember.Global (called via transpiler)
        public static void AddSection() {
            MainMenuBookSpawner mainMenuBookSpawner = BookLoader.FindObjectOfType<MainMenuBookSpawner>();
            mainMenuBookSpawner.AddSection(CustomBook.books.Select(book => book.GetBookDetails()).ToArray(), "Custom");
        }
    }
}