using NUnit.Framework;
using System;
using System.Linq;
using winamptospotifyforms;

namespace winamptospotifyTest
{
    public class FolderOperationsTest
    {
        private readonly FolderOperations folderOperations = new FolderOperations();

        [SetUp]
        public void Setup()
        {

        }

        [Test]
        public void GetFileNames_Should_Throw_When_Path_Null()
        {
            //Arrange
            string path = null;
            string artist = "Sean Paul";
            bool isArtistExists = true;

            // Assert
            Assert.That(() => folderOperations.GetMp3FileNames(path, artist, ref isArtistExists), Throws.Exception.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void GetFileNames_Should_Throw_When_Artist_Null()
        {
            //Arrange
            string path = "c:/../..";
            string artist = null;
            bool isArtistExists = false;

            // Assert
            Assert.That(() => folderOperations.GetMp3FileNames(path, artist, ref isArtistExists), Throws.Exception.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void GetFileNamesCount()
        {
            //Arrange
            string path = @"N:\..\...\";
            string artist = "Bob Sinclaire";
            bool isArtistExists = true;

            //Act
            var fileNames = folderOperations.GetMp3FileNames(path, artist, ref isArtistExists);

            // Assert
            Assert.That(fileNames.Count(), Is.Not.EqualTo(2));
        }

    }
}