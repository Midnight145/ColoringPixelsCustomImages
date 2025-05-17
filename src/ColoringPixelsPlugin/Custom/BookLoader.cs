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
            R allBookDetails = R.of(typeof(AllBookDetails));
            // we have to use private inst_ here because inst is a property
            // if we use the public inst we recur forever because this method is called from the property getter
            var inst = allBookDetails.GetField("_inst") as BookDetails[];

            foreach (CustomBook book in CustomBook.books) {
                AddBook(ref inst, book);
            }

            allBookDetails.SetField("_inst", inst);
            return inst;
        }

        public static void AddSection() {
            MainMenuBookSpawner mainMenuBookSpawner = BookLoader.FindObjectOfType<MainMenuBookSpawner>();
            R.of(mainMenuBookSpawner).CallMethod("AddSection",
                CustomBook.books.Select(book => book.GetBookDetails()).ToArray(), "Custom");
        }
    }
}