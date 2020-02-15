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

            // Assert
            Assert.That(() => folderOperations.GetMp3FileNames(path, artist), Throws.Exception.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void GetFileNames_Should_Throw_When_Artist_Null()
        {
            //Arrange
            string path = "c:/../..";
            string artist = null;

            // Assert
            Assert.That(() => folderOperations.GetMp3FileNames(path, artist), Throws.Exception.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void GetFileNamesCount()
        {
            //Arrange
            string path = @"E:\Yeni Müzik Arþivi\Yabancý\Bob Sinclaire";
            string artist = "Bob Sinclaire";

            //Act
            var fileNames = folderOperations.GetMp3FileNames(path, artist);

            // Assert
            Assert.That(fileNames.Count(), Is.EqualTo(2));
        }

    }
}