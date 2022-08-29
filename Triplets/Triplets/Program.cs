/*
 * Данная программа выполняет частотный анализ текста.
 * Выводит 10 самых часто встречающихся в тексте триплетов (3 идущих подряд буквы слова), а также время выполнения программы.
 * 
 * Ввод: путь к файлу с текстом
 * Вывод: 10 наиболее часто встречающихся триплетов и время выполнения программы
 * 
 * Разработано: Петров Д.В.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Triplets
{
    class Program
    {
        // путь к файлу по умолчанию (находится в папке проекта)
        private static string filePath = @"..\..\..\..\Triplets\TextFile.txt";

        static void Main(string[] args)
        {
            // запрос пути к файлу с текстом
            Console.Write("Введите полный путь к текстовому файлу (без кавычек): ");
            filePath = Console.ReadLine();

            // отслеживание времени выполнения программы
            Stopwatch sw = new Stopwatch();
            sw.Start();

            try
            {
                // поток для чтения текста из файла
                StreamReader sr = new StreamReader(filePath);
                string fileText = sr.ReadToEnd();
                sr.Close();

                // поиск оптимальных точек для разбиения текста на 4 примерно равные части
                int[] splitIndex = OptimalIndexes(ref fileText);

                // разбиваем текст на 4 подстроки, исходя из оптимальных индексов
                string[] splitStrings = 
                { 
                    fileText.Substring(0, splitIndex[0]), 
                    fileText.Substring(splitIndex[0] + 1, splitIndex[1] - splitIndex[0]), 
                    fileText.Substring(splitIndex[1] + 1, splitIndex[2] - splitIndex[1]),
                    fileText.Substring(splitIndex[2] + 1, fileText.Length - splitIndex[2] - 1) 
                };

                // словарь для каждого потока, хранит пары "триплет : количество"
                Dictionary<string, int>[] dicts =
                {
                    new Dictionary<string, int>() { },
                    new Dictionary<string, int>() { },
                    new Dictionary<string, int>() { },
                    new Dictionary<string, int>() { }
                };

                // создание потоков
                Thread[] threads =
                {
                    new Thread(() => Triplets(ref splitStrings[0], ref dicts[0])),
                    new Thread(() => Triplets(ref splitStrings[1], ref dicts[1])),
                    new Thread(() => Triplets(ref splitStrings[2], ref dicts[2])),
                    new Thread(() => Triplets(ref splitStrings[3], ref dicts[3]))
                };

                // запуск потоков
                foreach(Thread th in threads)
                {
                    th.Start();
                }

                // ожидание выполнения каждого потока
                foreach(Thread th in threads)
                {
                    th.Join();
                }

                // создание общего словаря для объединения 4 словарей из потоков
                Dictionary<string, int> answer = JoinDictionaries(ref dicts);

                // находим первые 10 наиболее часто встречающихся триплетов
                KeyValuePair<string, int>[] maxPairs = MaxTriplets(ref answer);

                // печатает конечный ответ
                PrintPairs(ref maxPairs);
            }

            // ловим исключение в случае, если неверно введено имя файла или его не существует
            catch(FileNotFoundException e)
            {
                Console.WriteLine(e.Message);
            }

            // ловим исключение в случае, если неверно указана папка в пути или ее не существует
            catch(DirectoryNotFoundException e)
            {
                Console.WriteLine(e.Message);
            }

            // останаваливаем таймер выполнения программы и отображаем затраченное время
            sw.Stop();
            Console.WriteLine($"Прошло {sw.ElapsedMilliseconds} мс");
        }

        /// <summary>
        /// Определяет 10 наиболее часто встречающихся триплетов в переданном словаре
        /// </summary>
        /// <param name="dict"></param>
        /// <returns></returns>
        public static KeyValuePair<string, int>[] MaxTriplets(ref Dictionary<string, int> dict)
        {
            KeyValuePair<string, int>[] res = new KeyValuePair<string, int>[10];
            for (int i = 0; i < res.Length; ++i)
            {
                string name = "";
                int max = 0;
                foreach (KeyValuePair<string, int> pair in dict)
                {
                    if (pair.Value > max && NotRepeated(ref res, pair.Key))
                    {
                        name = pair.Key;
                        max = pair.Value;
                    }
                }
                res[i] = new KeyValuePair<string, int>(name, max);
            }
            return res;
        }

        /// <summary>
        /// Проверяет, что данный ключ уже содержится в переданном массиве
        /// </summary>
        /// <param name="pairs"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool NotRepeated(ref KeyValuePair<string, int>[] pairs, string key)
        {
            foreach(KeyValuePair<string, int> pair in pairs)
            {
                if (pair.Key == key)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Объединяет словари, переданные в массиве. Ключи могут повторяться, в таком случае значения одинаковых ключей складываются
        /// </summary>
        /// <param name="dicts"></param>
        /// <returns></returns>
        public static Dictionary<string, int> JoinDictionaries(ref Dictionary<string, int>[] dicts)
        {
            Dictionary<string, int> res = new Dictionary<string, int>();
            foreach (Dictionary<string, int> dict in dicts)
            {
                foreach (KeyValuePair<string, int> pair in dict)
                {
                    if (!res.ContainsKey(pair.Key))
                    {
                        res.Add(pair.Key, pair.Value);
                    }
                    else
                    {
                        res[pair.Key] += pair.Value;
                    }
                }
            }
            return res;
        }

        /// <summary>
        /// Определяет ближайший возможный индекс для разделения текста
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static int[] OptimalIndexes(ref string text)
        {
            int[] res = { text.Length / 4, text.Length / 2, text.Length * 3 / 4 };

            for (int i = 0; i < res.Length; ++i)
            {
                for (int j = res[i]; j < text.Length; ++j)
                {
                    if (!Char.IsLetter(text[j]))
                    {
                        res[i] = j;
                        break;
                    }
                }
            }

            return res;
        }

        /// <summary>
        /// Определяет сколько раз триплет встречается в переданном тексте, заносит триплеты в переданный словарь
        /// </summary>
        /// <param name="text"></param>
        /// <param name="dict"></param>
        public static void Triplets(ref string text, ref Dictionary<string, int> dict)
        {
            for (int i = 0; i < text.Length - 2; ++i)
            {
                if (Char.IsLetter(text[i]) && Char.IsLetter(text[i + 1]) && Char.IsLetter(text[i + 2]))
                {
                    char[] chs = { Char.ToLower(text[i]), Char.ToLower(text[i + 1]), Char.ToLower(text[i + 2]) };
                    string triplet = new string(chs);

                    if (!dict.ContainsKey(triplet))
                    {
                        dict.Add(triplet, 1);
                    }
                    else
                    {
                        dict[triplet]++;
                    }
                }
            }
        }

        /// <summary>
        /// Печатает триплеты через запятую
        /// </summary>
        /// <param name="pairs"></param>
        public static void PrintPairs(ref KeyValuePair<string, int>[] pairs)
        {
            for(int i = 0; i < pairs.Length; ++i)
            {
                if (i == pairs.Length - 1)
                {
                    Console.WriteLine(pairs[i].Key);
                    break;
                }
                Console.Write(pairs[i].Key + ", ");
            }
        }
    }
}
