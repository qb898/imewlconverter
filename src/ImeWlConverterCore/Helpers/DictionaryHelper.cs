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
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Studyzy.IMEWLConverter.Helpers;

public class DictionaryHelper
{
    private static readonly Lazy<Dictionary<char, ChineseCode>> lazyDict =
        new Lazy<Dictionary<char, ChineseCode>>(BuildDictionary);

    private static Dictionary<char, ChineseCode> BuildDictionary()
    {
        var dict = new Dictionary<char, ChineseCode>();
        var allPinYin = GetResourceContent("ChineseCode.txt");
        var pyList = allPinYin.Split(
            new[] { "\r", "\n" },
            StringSplitOptions.RemoveEmptyEntries
        );
        for (var i = 0; i < pyList.Length; i++)
        {
            var hzpy = pyList[i].Split('\t');
            var hz = Convert.ToChar(hzpy[1]);

            dict.Add(
                hz,
                new ChineseCode
                {
                    Code = hzpy[0],
                    Word = hzpy[1][0],
                    Wubi86 = hzpy[2],
                    Wubi98 = hzpy[3],
                    WubiNewAge = hzpy[4],
                    Pinyins = hzpy[5],
                    Freq = Convert.ToDouble(hzpy[6])
                }
            );
        }
        return dict;
    }

    private static Dictionary<char, ChineseCode> Dict => lazyDict.Value;

    public static ChineseCode GetCode(char c)
    {
        if (Dict.TryGetValue(c, out var code))
            return code;
        throw new Exception("给定关键字不在字典中，【" + c + "】");
    }

    public static List<ChineseCode> GetAll()
    {
        return new List<ChineseCode>(Dict.Values);
    }

    public static string GetResourceContent(string fileName)
    {
        string file;
        var assembly = typeof(DictionaryHelper).GetTypeInfo().Assembly;

        var resourceName = "ImeWlConverterCore.Resources." + fileName;
        using (var stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream == null)
                throw new InvalidOperationException($"Embedded resource not found: {resourceName}");

            using (var reader = new StreamReader(stream, true))
            {
                file = reader.ReadToEnd();
            }
        }

        return file;
    }
}

public struct ChineseCode
{
    public string Code { get; set; }
    public char Word { get; set; }
    public string Wubi86 { get; set; }
    public string Wubi98 { get; set; }
    public string WubiNewAge { get; set; }
    public string Pinyins { get; set; }
    public double Freq { get; set; }
}
