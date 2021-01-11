﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading;
using anime_dl.Ext;
using anime_dl.Novels.Models;
using KobeiD.Downloaders;

namespace anime_dl.Novels.Models
{
    class Book
    {
        public MetaData metaData;
        public Chapter[] chapters;
        public string fileLocation;
        public DateTime lastUpdated;
        public Uri url;
        private Site site;
        public string chapterDir;

        public delegate void threadFinished();
        public event threadFinished onThreadFinish;
        public delegate void downloadFinished();
        public event downloadFinished onDownloadFinish;

        private int finishedThreads;
        private int limiter;
        private bool finished;
        Stopwatch sw = new Stopwatch();
        List<Thread> threads = new List<Thread>();
        private int ti;
        Action<int, string> statusUpdate;

        public bool dwnldFinished = false;

        public Book()
        {
            onThreadFinish += Book_onThreadFinish;
        }

        private void Book_onThreadFinish()
        {
            finishedThreads++;
            if (finishedThreads >= limiter)
            {
                sw.Stop();
                statusUpdate(ti, $"Done!, Download of {metaData.name} finished in {sw.Elapsed}");
                dwnldFinished = true;
                onDownloadFinish?.Invoke();
            }
        }

        public Book(string uri, bool parseFromWeb, int taski, Action<int, string> act)
        {
            statusUpdate = act;
            ti = taski;
            if (parseFromWeb)
            {
                onThreadFinish += Book_onThreadFinish;
                url = new Uri(uri);
                this.site = uri.SiteFromString();
                if (parseFromWeb)
                    if (!ParseBookFromWeb(uri))
                    {
                        Console.WriteLine("Can not continue, press enter to exit...");
                        Console.ReadLine();
                        Environment.Exit(-1);
                    }
                this.chapterDir = Directory.GetCurrentDirectory() + "\\Downloaded\\" + metaData.name + "\\Chapters";
            }
            else
            {
                onThreadFinish += Book_onThreadFinish;
                metaData = new MetaData();
                LoadFromADL(uri);
                for (int id = 0; id < chapters.Length; id++)
                    for (int idx = 0; idx < chapters.Length; idx++)
                    {

                        string chr = chapters[idx].name;
                        if (chr.ToArray().FirstLIntegralCount() == 0)
                            chr += 0;
                        string chra = chapters[id].name;
                        if (chra.ToArray().FirstLIntegralCount() == 0)
                            chra += 0;

                        if (chr.ToCharArray().FirstLIntegralCount() > chra.ToCharArray().FirstLIntegralCount())
                        {
                            Chapter a = chapters[id];
                            chapters[id] = chapters[idx];
                            chapters[idx] = a;
                        }
                    }
            }
        }
        public Book(string path)
        {
            if (path.IsValidUri())
            {
                onThreadFinish += Book_onThreadFinish;
                url = new Uri(path);
                this.site = path.SiteFromString();
                if (!ParseBookFromWeb(path))
                    throw new Exception("Unknown Error: e: bp2 | ParseFromWeb returned false");
                this.chapterDir = Directory.GetCurrentDirectory() + "\\Downloaded\\" + metaData.name + "\\Chapters";
            }
            else
            {
                onThreadFinish += Book_onThreadFinish;
                metaData = new MetaData();
                LoadFromADL(path);
                for (int id = 0; id < chapters.Length; id++)
                    for (int idx = 0; idx < chapters.Length; idx++)
                    {

                        string chr = chapters[idx].name;
                        if (chr.ToArray().FirstLIntegralCount() == 0)
                            chr += 0;
                        string chra = chapters[id].name;
                        if (chra.ToArray().FirstLIntegralCount() == 0)
                            chra += 0;

                        if (chr.ToCharArray().FirstLIntegralCount() > chra.ToCharArray().FirstLIntegralCount())
                        {
                            Chapter a = chapters[id];
                            chapters[id] = chapters[idx];
                            chapters[idx] = a;
                        }
                    }
            }
        }

        public bool ParseBookFromWeb(string url)
        {
            switch (site)
            {
                case Site.wuxiaWorldA:
                    FromWuxiaWorldD(url);
                    return true;
                case Site.wuxiaWorldB:
                    FromWuxiaWorldC(url);
                    return true;
                case Site.ScribbleHub:
                    FromScribbleHubC(url);
                    return true;
                case Site.NovelFull:
                    FromNovelFullC(url);
                    return true;
                case Site.Error:
                    Program.WriteToConsole("Error: This site doesn't seem to be supported.");
                    return false;
                default:
                    Program.WriteToConsole("Unknown error");
                    return false;
            }
        }

        private void FromWuxiaWorldC(string url)
        {
            statusUpdate(ti, $"{metaData?.name} Creating Novel Object");
            cWuxiaWorld wuxiaWorld = new cWuxiaWorld(url, ti, statusUpdate);
            statusUpdate(ti, $"{metaData?.name} Getting MetaData");
            metaData = wuxiaWorld.GetMetaData();
            statusUpdate(ti, $"{metaData?.name} Getting Chapter links");
            chapters = wuxiaWorld.GetChapterLinks();
            fileLocation = $"{Directory.GetCurrentDirectory()}\\{metaData.name}";
            Program.WriteToConsole($"Downloading Chapters for {metaData.name}");
        }
        private void FromWuxiaWorldD(string url)
        {
            statusUpdate(ti, $"{metaData?.name} Creating Novel Object");
            dWuxiaWorld wuxiaWorld = new dWuxiaWorld(url, ti, statusUpdate);
            statusUpdate(ti, $"{metaData?.name} Getting MetaData");
            metaData = wuxiaWorld.GetMetaData();
            statusUpdate(ti, $"{metaData?.name} Getting Chapter links");
            chapters = wuxiaWorld.GetChapterLinks();
            fileLocation = $"{Directory.GetCurrentDirectory()}\\{metaData.name}";
            Program.WriteToConsole($"Downloading Chapters for {metaData.name}");
        }
        private void FromScribbleHubC(string url)
        {
            statusUpdate(ti, $"{metaData?.name} Creating Novel Object");
            cScribbleHub scribbleHub = new cScribbleHub(url, ti, statusUpdate);
            statusUpdate(ti, $"{metaData?.name} Getting MetaData");
            metaData = scribbleHub.GetMetaData();
            statusUpdate(ti, $"{metaData?.name} Getting Chapter links");
            chapters = scribbleHub.GetChapterLinks(true);
            fileLocation = $"{Directory.GetCurrentDirectory()}\\{metaData.name}";
            Program.WriteToConsole($"Downloading Chapters for {metaData.name}");
        }

        private void FromNovelFullC(string url)
        {
            statusUpdate(ti, $"{metaData?.name} Creating Novel Object");
            cNovelFull novelFull = new cNovelFull(url, ti, statusUpdate);
            statusUpdate(ti, $"{metaData?.name} Getting MetaData");
            metaData = novelFull.GetMetaData();
            statusUpdate(ti, $"{metaData?.name} Getting Chapter links");
            chapters = novelFull.GetChapterLinks();
            fileLocation = $"{Directory.GetCurrentDirectory()}\\{metaData.name}";
            Program.WriteToConsole($"Downloading Chapters for {metaData.name}");
        }

        private void sU(int a, string b)
        {
            b = $"{metaData.name} {b}";
            statusUpdate(a, b);
        }

        public void DownloadChapters()
            => chapters = Chapter.BatchChapterGet(chapters, chapterDir, site, ti, sU);

        public void DownloadChapters(bool multithreaded)
        {
            if (!multithreaded)
            {
                DownloadChapters();
                onDownloadFinish?.Invoke();
                return;
            }
            sw.Start();
            int[] a = chapters.Length.GCFS();
            this.limiter = a[0];
            int limiter = 0;
            Chapter[][] chaps = new Chapter[a[0]][];
            for (int i = a[0] - 1; i > -1; i--)
            {
                chaps[i] = chapters.Skip(limiter).Take(a[1]).ToArray();
                limiter += a[1];
            }

            for (int idx = 0; idx < a[0]; idx++)
            {
                Chapter[] chpa = chaps[idx];
                int i = idx;
                Thread ab = new Thread(() => { chpa = Chapter.BatchChapterGet(chpa, chapterDir, site, ti, sU); onThreadFinish?.Invoke(); }) { Name = i.ToString() };
                ab.Start();
                threads.Add(ab);
            }
        }

        public void ExportToADL()
        {
            Directory.CreateDirectory(chapterDir);
            TextWriter tw = new StreamWriter(new FileStream($"{Directory.GetCurrentDirectory()}\\Downloaded\\{metaData.name}\\main.adl", FileMode.OpenOrCreate));
            foreach (FieldInfo pie in typeof(MetaData).GetFields())
            {
                if (pie.Name != "cover")
                    tw.WriteLine($"{pie.Name}|{pie.GetValue(metaData)}");
                else
                    using (BinaryWriter bw = new BinaryWriter(new FileStream($"{Directory.GetCurrentDirectory()}\\Downloaded\\{metaData.name}\\cover.jpeg", FileMode.OpenOrCreate)))
                        bw.Write(metaData.cover, 0, metaData.cover.Length);
            }
            tw.Close();
        }

        public void LoadFromADL(string pathToDir)
        {
            string[] adl = File.ReadAllLines(pathToDir + "\\main.adl");
            FieldInfo[] fi = typeof(MetaData).GetFields();
            foreach (string str in adl)
                if (str != "")
                    fi.First(x => x.Name == str.Split('|')[0]).SetValue(metaData, str.Split('|')[1]);
            metaData.cover = File.ReadAllBytes(pathToDir + "\\cover.jpeg");

            adl = Directory.GetFiles(pathToDir + "\\Chapters", "*.txt");

            List<Chapter> chaps = new List<Chapter>();

            foreach (string str in adl)
                chaps.Add(new Chapter() { name = str.GetFileName().Replace('_', ' '), text = File.ReadAllText(str) });

            chapters = chaps.ToArray();
            chaps.Clear();

            return;
        }

        public void ExportToEPUB()
        {
            statusUpdate(ti, $"{metaData?.name} Exporting to EPUB");
            Epub e = new Epub(metaData.name, metaData.author, new Image() { bytes = metaData.cover }, new Uri(metaData.url));
            foreach (Chapter chp in chapters)
            {
                statusUpdate(ti, $"{metaData?.name} Generating page for {chp.name.Replace('_', ' ')}");
                e.AddPage(Page.AutoGenerate(chp.text, chp.name.Replace('_', ' ')));
            }
            e.CreateEpub();
            statusUpdate(ti, $"{metaData?.name} EPUB Created!");
        }

        public void ParseBookFromFile()
        {

        }

        public void UpdateBook()
        {

        }

        public void MergeChapters()
        {

        }
    }
}
