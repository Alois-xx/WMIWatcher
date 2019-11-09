using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using WMIWatcher;

namespace WMIWatcher_uTest
{
    [TestFixture]
    public class Tests
    {

        [Test]
        public void Ensure_That_Oldest_Files_Are_Deleted()
        {
            using (var tmp = TempDir.Create())
            {
                var dir = tmp.Name;

                using var logger = new FileLogger(dir, 30, 3);
                var deleted = new List<string>();
                logger.Delete = (path) =>
                {
                    deleted.Add(path);
                    Console.WriteLine($"Delete file {path}");
                };

                string baseName = Path.GetFileName(FileLogger.myFileBaseName);
                string ext = Path.GetExtension(FileLogger.myFileBaseName);
                string[] files = new string[] { ".1", ".2", ".3", ".4" };
                List<string> fullNames = new List<string>();
                for (int i = 0; i < files.Length; i++)
                {
                    string fullName = Path.Combine(dir, $"{baseName}{files[i]}{ext}");
                    fullNames.Add(fullName);
                    File.WriteAllText(fullName, "Hi file");
                    new FileInfo(fullName).LastWriteTime = DateTime.Now - TimeSpan.FromDays(i);
                }

                logger.RollOver();

                Assert.AreEqual(2, deleted.Count);
                Assert.AreEqual(fullNames[2], deleted[0]);
                Assert.AreEqual(fullNames[3], deleted[1]);
            }
        }

        [Test]
        public void Ensure_Generations_Are_Created()
        {
            using (var tmp = TempDir.Create())
            {
                var dir = tmp.Name;

                using var logger = new FileLogger(dir, maxFileSizeInMB:1, maxGenerationsToKeep: 3);

                var deleted = new List<string>();
                logger.Delete = (path) =>
                {
                    deleted.Add(path);
                    Console.WriteLine($"Delete file {path}");
                    File.Delete(path);
                };

                var bigString = new string('x', 600_000);

                // 3 generations + 1 base file 
                // 20 writes where after 2 writes a new file is created 
                // 20/2 = 10 - 4 = 6 files are deleted

                for (int i = 0; i < 20; i++)
                {
                    logger.Log(bigString);
                }


                Assert.AreEqual(6, deleted.Count);
            }
        }
    }
}