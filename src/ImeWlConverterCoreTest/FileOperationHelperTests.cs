/*
 *   Copyright © 2009-2020 studyzy(深蓝,曾毅)

 *   This program "IME WL Converter(深蓝词库转换)" is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.

 *   This program is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details.

 *   You should have received a copy of the GNU General Public License
 *   along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using NUnit.Framework;
using Studyzy.IMEWLConverter.Helpers;

namespace Studyzy.IMEWLConverter.Test;

internal class FileOperationHelperTests
{
    [Test]
    public void WriteReadFile_Roundtrip()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt");
        try
        {
            const string content = "Hello, world!";
            Assert.That(FileOperationHelper.WriteFile(path, Encoding.UTF8, content), Is.True);
            Assert.That(File.Exists(path), Is.True);
            var read = FileOperationHelper.ReadFile(path);
            Assert.That(read, Does.Contain("Hello"));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Test]
    public void ZipFile_Unzip_PreservesContent()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            // Build a zip that contains a file nested inside a subdirectory.
            // The entry name uses a forward slash, which is the standard zip path separator.
            // On non-Windows platforms the old code turned '/' into '\\' (a literal backslash
            // character), so the subdirectory was never created and File.Exists returned false.
            var zipPath = Path.Combine(tempDir, "test.zip");
            const string entryName = "subdir/content.txt";
            const string fileContent = "hello from zip";
            using (var zipStream = new ZipOutputStream(File.Create(zipPath)))
            {
                zipStream.PutNextEntry(new ZipEntry(entryName));
                var bytes = Encoding.UTF8.GetBytes(fileContent);
                zipStream.Write(bytes, 0, bytes.Length);
                zipStream.Finish();
            }

            var outDir = Path.Combine(tempDir, "output");
            var success = FileOperationHelper.UnZip(zipPath, outDir);
            Assert.That(success, Is.True, "UnZip should return true");

            var extracted = Path.Combine(outDir, "subdir", "content.txt");
            Assert.That(File.Exists(extracted), Is.True, "Extracted file should exist");

            var extractedContent = File.ReadAllText(extracted, Encoding.UTF8);
            Assert.That(extractedContent, Is.EqualTo(fileContent), "Extracted content should match original");
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { /* best-effort cleanup */ }
        }
    }
}
