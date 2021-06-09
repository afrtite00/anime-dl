﻿using ADLCore.Alert;
using ADLCore.Ext;
using ADLCore.Novels.Models;
using ADLCore.Video;
using ADLCore.Video.Constructs;
using ADLCore.Video.Extractors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace ADLCore.Interfaces
{
    /// <summary>
    /// Used for "automatic" usage of this library. Pass the arguments in upon creation, and it will automatically execute it.
    /// </summary>
    public class Main
    {
        public IAppBase _base;

        public Main(ArgumentObject args, int ti = -1, Action<int, string> u = null)
        {
            Restart:;
            if (args.arguments.mn == "nvl")
                NovelDownload(args.arguments, ti, u);
            else if(args.arguments.mn == "ani")
                AnimeDownload(args.arguments, ti, u);
            else
            {
                if (!searchMN(ref args))
                {
                    u?.Invoke(ti, "Error: could not parse command (Failure to parse website to ani/nvl flag.. you can retry with ani/nvl flag)");
                    ADLUpdates.CallError(new Exception("Error: Could not parse command (mn selector)"));
                    return;
                }
                else
                    goto Restart;
            }
        }

        private bool searchMN(ref ArgumentObject args)
        {
            switch (args.arguments.term.SiteFromString())
            {
                case Site.HAnime: args[0] = "ani";  return true;
                case Site.Vidstreaming: args[0] = "ani"; return true;
                case Site.ScribbleHub: args[0] = "nvl"; return true;
                case Site.wuxiaWorldA: args[0] = "nvl"; return true;
                case Site.wuxiaWorldB: args[0] = "nvl"; return true;
                case Site.NovelFull: args[0] = "nvl"; return true;
                case Site.MangaKakalot: args[0] = "mng"; return true;
                default:
                    return false;
            }
        }

        public Main(string[] arguments, int ti = -1, Action<int, string> u = null)
        {
            ArgumentObject args = ArgumentObject.Parse(arguments);
        Restart:;
            if (args.arguments.mn == "nvl")
                NovelDownload(args.arguments, ti, u);
            else if (args.arguments.mn == "ani")
                AnimeDownload(args.arguments, ti, u);
            else
            {
                if (!searchMN(ref args))
                {
                    u?.Invoke(ti, "Error: could not parse command (Failure to parse website to ani/nvl flag.. you can retry with ani/nvl flag)");
                    ADLUpdates.CallError(new Exception("Error: Could not parse command (mn selector)"));
                    return;
                }
                else
                    goto Restart;

            }
        }

        private void NovelDownload(argumentList args, int ti, Action<int, string> u)
        {
            if (args.s)
                throw new Exception("Novel Downloader does not support searching at this time.");
            if (args.cc)
                throw new Exception("Novel Downloader does not support continuos downloads at this time.");
            Book bk;
            if (args.term.IsValidUri())
            {
                bk = new Book(args.term, true, ti, new Action<int, string>(u), args.l == false ? null : args.export);
                bk.ExportToADL();
            }
            else
            {
                bk = new Book(args.term, false, ti, new Action<int, string>(u), args.l == false ? null : args.export);
                bk.dwnldFinished = true;
            }

            if (args.d)
            {
                bk.DownloadChapters(args.mt);
                while (!bk.dwnldFinished)
                    Thread.Sleep(200);
            }

            if (args.e)
            {
                bk.ExportToEPUB(args.android ? args.export + "/" + bk.metaData.name : args.l ? args.export : Path.Join(Directory.GetCurrentDirectory(), "Epubs", bk.metaData.name));
                u.Invoke(ti, $"{bk.metaData.name} exported to epub successfully!");
            }
        }

        private void AnimeDownload(argumentList args, int ti, Action<int, string> u)
        {
            VideoBase e = new VideoBase(args, ti, u);
            _base = e;
            e.BeginExecution();
            return;
        }
    }
}
