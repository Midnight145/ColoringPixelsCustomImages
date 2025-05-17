using System.Collections.Generic;
using System.IO;
using ColoringPixelsMod;
using UnityEngine;

namespace ColoringPixelsPlugin.Custom {
    public class CustomBook {
        internal static List<CustomBook> books = new List<CustomBook>();

        internal readonly string bookName;
        internal readonly int hash;
        internal readonly bool register;

        private const int achievementID = -1;
        private const string achievementName = "";
        private const string discordImageId = "";
        private const BookType bookType = BookType.Free;
        private int index = -1;


        public CustomBook(string path, string name) {
            this.bookName = name;
            string fullPath = Path.Combine(path, name);
            this.register = Directory.Exists(fullPath);
            CustomBook.books.Add(this);
            hash = -CustomBook.books.Count;
            if (CustomBook.books == null) {
                CustomBook.books = new List<CustomBook>();
            }
        }

        public BookDetails GetBookDetails(int index) {
            this.index = index;
            CustomImagesPlugin.Log.LogInfo("GetBookDetails: Loading Book: " + this.bookName);
            CustomImagesPlugin.Log.LogInfo("Contents of ImageLoader.books: " + ImageLoader.books.Count);
            foreach (var book in ImageLoader.books) {
                CustomImagesPlugin.Log.LogInfo("Book: " + book.Key);
            }

            BookDetails bookDetails = ScriptableObject.CreateInstance<BookDetails>();
            bookDetails.bookName = this.bookName;
            bookDetails.bookIndex = index;
            bookDetails.bookHash = this.hash;
            bookDetails.mainMenuTitle = this.bookName;
            bookDetails.steamID = CustomBook.achievementID;
            bookDetails.steamAchievement = CustomBook.achievementName;
            bookDetails.discordImageId = CustomBook.discordImageId;
            bookDetails.bookType = bookType;
            bookDetails.levels = ImageLoader.books[this.bookName];
            return bookDetails;
        }

        public BookDetails GetBookDetails() {
            return this.GetBookDetails(this.index);
        }
    }
}