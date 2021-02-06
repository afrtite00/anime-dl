﻿using ADLCore.Ext;
using ADLCore.Video.Constructs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ADLCore.Video.Extractors
{
    class TwistMoe : ExtractorBase
    {
        private WebHeaderCollection whc;
        private HttpWebRequest wRequest;
        private WebResponse response;
        private TwistMoeAnimeInfo info;
        private int[] rRange;
        //episodes to download: 0-12, 1-12, 5-6 etc.
        //TODO: Implement download ranges for GoGoStream and TwistMoe (and novel downloaders)
        private int[] downloadRange;

        //key  MjY3MDQxZGY1NWNhMmIzNmYyZTMyMmQwNWVlMmM5Y2Y= -> search for atob(e) and floating-player
        public TwistMoe(ArgumentObject args, int ti = -1, Action<int, string> u = null) : base(ti, u)
        {
            GenerateHeaders();
        }

        public override void Begin()
        {
            videoInfo = new Constructs.Video();



        }

        //TODO: Implement dual threaded downloading for multithreading.
        public override bool Download(string path, bool mt, bool continuos)
        {
            wRequest = (HttpWebRequest)WebRequest.Create(path);
            return true;
        }

        public override void GenerateHeaders()
        {
            whc = new WebHeaderCollection();
            whc.Add("DNT", "1");
            whc.Add("Sec-Fetch-Dest", "video");
            whc.Add("Sec-Fetch-Site", "same-site");

            //Get anime slug to use for api
            string k = ao.term.TrimToSlash().SkipCharSequence("https://twist.moe/a/".ToCharArray());
            wRequest = (HttpWebRequest)WebRequest.Create($"https://api.twist.moe/api/anime/{k}");
            wRequestSet();
            WebResponse wb = wRequest.GetResponse();
            using (StreamReader str = new StreamReader(wb.GetResponseStream()))
                info = JsonSerializer.Deserialize<TwistMoeAnimeInfo>(str.ReadToEnd());

            wRequest = (HttpWebRequest)WebRequest.Create($"https://api.twist.moe/api/anime/{k}/sources");
            wb = wRequest.GetResponse();
            using (StreamReader str = new StreamReader(wb.GetResponseStream()))
                info.episodes = JsonSerializer.Deserialize<List<Episode>>(str.ReadToEnd());
        }

        private void wRequestSet()
        {
            wRequest.Headers = whc;
            wRequest.Host = "cdn.twist.moe";
        }

        public override dynamic Get(HentaiVideo obj, bool dwnld)
        {
            throw new NotImplementedException();
        }

        public override string GetDownloadUri(string path)
        {
            throw new NotImplementedException();
        }

        public override string GetDownloadUri(HentaiVideo path)
        {
            throw new NotImplementedException();
        }

        public override string Search(string name, bool d = false)
        {
            throw new NotImplementedException();
        }

        private static void DeriveKeyAndIV(byte[] p, byte[] salt, out byte[] key, out byte[] iv)
        {
            // http://www.openssl.org/docs/crypto/EVP_BytesToKey.html#KEY_DERIVATION_ALGORITHM @#%@#$^#$&@^#$%!!#$^!
            //https://stackoverflow.com/questions/8008253/c-sharp-version-of-openssl-evp-bytestokey-method
            List<byte> concatenatedHashes = new List<byte>(48);
            byte[] currentHash = new byte[0];
            MD5 md5 = MD5.Create();
            bool enoughBytesForKey = false;

            while (!enoughBytesForKey)
            {
                int preHashLength = currentHash.Length + p.Length + salt.Length;

                byte[] preHash = new byte[preHashLength];


                Buffer.BlockCopy(currentHash, 0, preHash, 0, currentHash.Length);
                Buffer.BlockCopy(p, 0, preHash, currentHash.Length, p.Length);
                Buffer.BlockCopy(salt, 0, preHash, currentHash.Length + p.Length, salt.Length);

                currentHash = md5.ComputeHash(preHash);
                concatenatedHashes.AddRange(currentHash);

                if (concatenatedHashes.Count >= 48)
                    enoughBytesForKey = true;
            }
            key = new byte[32];
            iv = new byte[16];
            concatenatedHashes.CopyTo(0, key, 0, 32);
            concatenatedHashes.CopyTo(32, iv, 0, 16);
            md5.Clear();
            md5 = null;
        }
    }
}
