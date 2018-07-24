//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AMSReverseProxy.SmoothHelper
{
    using Microsoft.Extensions.Logging;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;
    public enum SmoothAssetStatus
    {
        Init = 0,
        ManifestLoaded,
        SubtitlesAvailable,
        SubtitlesNotAvailable,
        SubtitlesLoaded

    };
    class SmoothSubtitleTrack
    {
        public string Name { get; set; }
        public string Lang { get; set; }
        public int Bitrate { get; set; }
        public int PeriodMs { get; set; }
        public List<SubtitleItem> Subtitles { get; set; }
        public SmoothSubtitleTrack(string name, string lang, int bitrate, int period)
        {
            Name = name;
            Lang = lang;
            Bitrate = bitrate;
            PeriodMs = period;
            Subtitles = new List<SubtitleItem>();
        }
    }
    class SmoothAsset
    {
        public SmoothAssetStatus Status { get; set; }
        public string RootUri { get; set; }

        public Dictionary<string, SmoothSubtitleTrack> SubtitleTrackList { get; set; }
        public bool IsLive()
        {
            if (SmoothManifestManager != null)
                return SmoothManifestManager.IsLive;
            return false;
        }
        SmoothHelper.ManifestManager SmoothManifestManager;
        System.Threading.Tasks.Task SubtitleTask = null;
        public ulong SubtitleLiveOffset { get; set; }
        public int ManifestUpdatePeriod { get; set; }
        bool downloadManifestTaskRunning = false;
        public SmoothAsset(string root, ulong offset = 0, int period = 0)
        {
            RootUri = root;
            SubtitleLiveOffset = offset;
            ManifestUpdatePeriod = period;
            Status = SmoothAssetStatus.Init;
            SubtitleTrackList = new Dictionary<string, SmoothSubtitleTrack>();
        }

        /// <summary>
        /// Defines the TTMLSubtitles element.
        /// </summary>
        private const string TTMLSubtitlesElement = "tt";

        /// <summary>
        /// Defines the TTMLSubtitles head.
        /// </summary>
        private const string TTMLSubtitlesHead = "head";

        /// <summary>
        /// Defines the TTMLSubtitles body.
        /// </summary>
        private const string TTMLSubtitlesBody = "body";

        /// <summary>
        /// Defines the TTMLSubtitles div
        /// </summary>
        private const string TTMLSubtitlesDiv = "div";

        /// <summary>
        /// Defines the TTMLSubtitles p.
        /// </summary>
        private const string TTMLSubtitlesP = "p";
        string GetMultiLineText(XmlReader reader)
        {
            string Text = string.Empty;
            while ((reader.Name == "") && (reader.NodeType == XmlNodeType.Whitespace))
                reader.Read();
            if (reader.NodeType == XmlNodeType.Element)
            {
                if (reader.Name == "span")
                {

                    reader.Read();
                    while ((reader.Name == "") && (reader.NodeType == XmlNodeType.Whitespace))
                        reader.Read();
                    if (reader.NodeType == XmlNodeType.Text)
                    {
                        Text = reader.Value;
                    }
                    reader.Read();
                    while ((reader.Name == "") && (reader.NodeType == XmlNodeType.Whitespace))
                        reader.Read();
                    while (((reader.Name == "br") || (reader.Name == "span")) && ((reader.NodeType == XmlNodeType.Element) || (reader.NodeType == XmlNodeType.EndElement)))
                    {
                        reader.Read();
                        while ((reader.Name == "") && (reader.NodeType == XmlNodeType.Whitespace))
                            reader.Read();
                        if (reader.NodeType == XmlNodeType.Text)
                        {
                            Text += "\n" + reader.Value;
                        }
                        reader.Read();
                        while ((reader.Name == "") && (reader.NodeType == XmlNodeType.Whitespace))
                            reader.Read();
                    }
                }
            }
            else if (reader.NodeType == XmlNodeType.Text)
            {
                Text = reader.Value;
                reader.Read();
                while ((reader.Name == "") && (reader.NodeType == XmlNodeType.Whitespace))
                    reader.Read();
                while ((reader.Name == "br") && (reader.NodeType == XmlNodeType.Element))
                {
                    reader.Read();
                    while ((reader.Name == "") && (reader.NodeType == XmlNodeType.Whitespace))
                        reader.Read();
                    if (reader.NodeType == XmlNodeType.Text)
                    {
                        Text += "\n" + reader.Value;
                    }
                    reader.Read();
                    while ((reader.Name == "") && (reader.NodeType == XmlNodeType.Whitespace))
                        reader.Read();
                }
            }

            return Text;
        }
        /// <summary>
        /// ParseTTMLSubtitles
        /// Parse TTML Subtitles 
        /// </summary>
        /// <param name="manifestBuffer">The buffer of the manifest being parsed.</param>
        public bool ParseAndAddTTMLSubtitles(ulong timeOffset, string name, string lang, int bitrate, int period,  byte[] subtitleBuffer)
        {
            bool bResult = false;
            using (var subtitleStream = new MemoryStream(subtitleBuffer))
            {
                bResult = this.ParseTTMLSubtitles(timeOffset, name, lang, bitrate, period,  subtitleStream);
            }
            return bResult;
        }
        /// <summary>
        /// Parses the TTML stream.
        /// </summary>
        /// <param name="subtitleStream">The manifest stream being parsed.</param>
        public bool ParseTTMLSubtitles(ulong timeOffset, string name, string lang, int bitrate, int period, Stream subtitleStream)
        {
            bool bResult = false;
            try
            {
                using (XmlReader reader = XmlReader.Create(subtitleStream))
                {
                    if (reader.Read() && reader.IsStartElement(TTMLSubtitlesElement))
                    {
                        while (reader.Read())
                        {
                            if (reader.Name == TTMLSubtitlesBody && reader.NodeType == XmlNodeType.Element)
                            {
                                reader.Read();
                                while ((reader.Name == "") && (reader.NodeType == XmlNodeType.Whitespace))
                                    reader.Read();
                                if (reader.Name == TTMLSubtitlesDiv && reader.NodeType == XmlNodeType.Element)
                                {

                                    while (((reader.Name == TTMLSubtitlesP) && (reader.NodeType == XmlNodeType.Element)) || reader.Read())
                                    {
                                        while ((reader.Name == "") && (reader.NodeType == XmlNodeType.Whitespace))
                                            reader.Read();
                                        if (reader.Name == TTMLSubtitlesP && reader.NodeType == XmlNodeType.Element)
                                        {
                                            string Text = string.Empty;
                                            string Id = string.Empty;
                                            string Begin = string.Empty;
                                            string End = string.Empty;
                                            string Dur = string.Empty;
                                            if (reader.HasAttributes)
                                            {
                                                for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
                                                {
                                                    reader.MoveToAttribute(attInd);
                                                    if (reader.NodeType == XmlNodeType.Attribute)
                                                    {
                                                        if (reader.Name == "begin")
                                                            Begin = reader.Value;
                                                        else if (reader.Name == "dur")
                                                            Dur = reader.Value;
                                                        else if (reader.Name == "end")
                                                            End = reader.Value;
                                                        else if (reader.Name == "xml:id")
                                                            Id = reader.Value;
                                                    }
                                                }
                                            }
                                            reader.Read();

                                            Text = GetMultiLineText(reader);

                                            if (!string.IsNullOrEmpty(Text) &&
                                                !string.IsNullOrEmpty(Begin) &&
                                                !string.IsNullOrEmpty(End)
                                                )
                                            {

                                                ulong BeginTime = SubtitleItem.ParseTime(Begin);
                                                ulong EndTime = SubtitleItem.ParseTime(End);

                                                SubtitleItem item = new SubtitleItem((ulong)timeOffset*1000 + BeginTime, (ulong)timeOffset*1000 + EndTime, Text);
                                                if (item != null)
                                                {
                                                    if(SubtitleTrackList == null)
                                                    {
                                                        SubtitleTrackList = new Dictionary<string, SmoothSubtitleTrack>();
                                                    }
                                                    if (SubtitleTrackList != null)
                                                    {
                                                        string key = name + lang;
                                                        if (!SubtitleTrackList.ContainsKey(key))
                                                            SubtitleTrackList.Add(key, new SmoothSubtitleTrack(name, lang, bitrate, period));
                                                        if (SubtitleTrackList[key].Subtitles == null)
                                                            SubtitleTrackList[key].Subtitles = new List<SubtitleItem>();
                                                        SubtitleTrackList[key].Subtitles.Add(item);
                                                        System.Diagnostics.Debug.WriteLine("Subtitle: " + SubtitleItem.TimeToString(item.startTime) + " - " + SubtitleItem.TimeToString(item.endTime) + " Content: \r\n" + Text);

                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                bResult = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while parsing Subtitles file: " + ex.Message);
                bResult = false;
            }
            return bResult;
        }
        public string GetTTMLTextFromMP4Boxes(byte[] data)
        {
            string result = string.Empty;
            if ((data != null) && (data.Length > 0))
            {
                int index = 0;
                while (index < data.Length)
                {
                    AMSReverseProxy.SmoothHelper.Mp4Box box = AMSReverseProxy.SmoothHelper.Mp4Box.CreateMp4Box(data, index);
                    if (box.GetBoxType() == "moof")
                    {
                        index += box.GetBoxLength();
                    }
                    else if (box.GetBoxType() == "mdat")
                    {
                        result = System.Text.Encoding.UTF8.GetString(box.GetBoxData(), 0, box.GetBoxData().Length);
                        index += box.GetBoxLength();
                        break;
                    }
                    else
                    {
                        index += box.GetBoxLength();
                    }
                }
            }
            return result;
        }
        public byte[] GetTTMLBytesFromMP4Boxes(byte[] data)
        {
            byte[] result = null;
            if ((data != null) && (data.Length > 0))
            {
                int index = 0;
                while (index < data.Length)
                {
                    AMSReverseProxy.SmoothHelper.Mp4Box box = AMSReverseProxy.SmoothHelper.Mp4Box.CreateMp4Box(data, index);
                    if (box.GetBoxType() == "moof")
                    {
                        index += box.GetBoxLength();
                    }
                    else if (box.GetBoxType() == "mdat")
                    {
                        result = box.GetBoxData();
                        index += box.GetBoxLength();
                        break;
                    }
                    else
                    {
                        index += box.GetBoxLength();
                    }
                }
            }
            return result;
        }
        public bool StopLoadingSubtitles()
        {
            bool result = false;
            downloadManifestTaskRunning = false;
            System.Threading.Tasks.Task.Delay(1000).Wait();
            if (SubtitleTask!=null)
            {
                int Index = 0;
                while ((!SubtitleTask.IsCompleted)&&(Index++<5))
                {
                    System.Threading.Tasks.Task.Delay(1000).Wait();
                }
                SubtitleTask = null;
            }
            return result;
        }
        public bool StartLoadingSubtitles(ulong offset = 0, int period = 0)
        {
            bool result = false;
            SubtitleLiveOffset = offset;
            ManifestUpdatePeriod = period;
            SubtitleTask = System.Threading.Tasks.Task.Factory.StartNew(async ()
                =>
            {
                try
                {
                    Program.Logger.LogInformation("AMS Reverse Proxy Service: starting to capture and convert TTML subtitles from " + RootUri);

                    downloadManifestTaskRunning = true;
                    SmoothManifestManager = SmoothHelper.ManifestManager.CreateManifestManager(new Uri(RootUri), false, 5000000, 20);
                    if (SmoothManifestManager != null)
                    {

                        bool res = await SmoothManifestManager.LoadAndParseSmoothManifest();
                        if (res == true)
                        {
                            DateTime LatestManifestDownloadTime = DateTime.Now;
                            Status = SmoothAssetStatus.ManifestLoaded;
                            System.Diagnostics.Debug.Write("SmoothStreaming Manifest: " + RootUri + " correctly downloaded and parsed ");

                            // If Live update the list of chunks to downlad
                            while (this.downloadManifestTaskRunning)
                            {

                                // Download Text Chunks
                                if ((SmoothManifestManager.TextChunkListList != null) && (SmoothManifestManager.TextChunkListList.Count > 0))
                                {
                                    if(Status < SmoothAssetStatus.SubtitlesAvailable)
                                        Status = SmoothAssetStatus.SubtitlesAvailable;
                                    // Something to download
                                    if (!SmoothManifestManager.IsDownloadCompleted(SmoothManifestManager.TextChunkListList))
                                    {

                                        System.Diagnostics.Debug.WriteLine(string.Format("{0:d/M/yyyy HH:mm:ss.fff}", DateTime.Now) + " downloadThread downloading text chunks for Manifest Uri: " + SmoothManifestManager.ManifestUri.ToString());
                                        int Index = 0;
                                        foreach (var cl in SmoothManifestManager.TextChunkListList)
                                        {
                                            ulong CaptureSubtitleStartTime = 0;
                                            if ((SubtitleLiveOffset > 0) && (SmoothManifestManager.IsLive))
                                            {
                                                CaptureSubtitleStartTime = cl.LastTimeChunksToRead - (SubtitleLiveOffset * SmoothManifestManager.TimeScale) > 0 ?
                                                                            cl.LastTimeChunksToRead - (SubtitleLiveOffset * SmoothManifestManager.TimeScale) : 0;
                                            }
                                            Index++;
                                            SmoothHelper.ChunkBuffer cb;
                                            while ((cl.ChunksToReadQueue.TryDequeue(out cb)) && (downloadManifestTaskRunning == true))
                                            {


                                                if (cb.Time > CaptureSubtitleStartTime)
                                                {
                                                    string baseUri = SmoothHelper.ManifestManager.GetBaseUri(RootUri);
                                                    string url = baseUri + "/" + cl.TemplateUrl.Replace("{start_time}", cb.Time.ToString());
                                                    System.Diagnostics.Debug.WriteLine(string.Format("{0:d/M/yyyy HH:mm:ss.fff}", DateTime.Now) + " downloadThread downloading text chunks : " + url.ToString());
                                                    cb.chunkBuffer = await SmoothManifestManager.DownloadChunkAsync(new Uri(url));

                                                    if (cb.IsChunkDownloaded())
                                                    {
                                                        ulong l = cb.GetLength();
                                                        if (l > 0)
                                                        {
                                                            double time = SmoothManifestManager.TimescaleToHNS(cb.Time) / (SmoothHelper.ManifestManager.TimeUnit);
                                                            string text = GetTTMLTextFromMP4Boxes(cb.chunkBuffer);
                                                            if (!string.IsNullOrEmpty(text))
                                                            {
                                                                System.Diagnostics.Debug.Write("TTML chunk at : " + time.ToString() + " seconds \r\n" + text);
                                                            }
                                                            byte[] subtitleBuffer = GetTTMLBytesFromMP4Boxes(cb.chunkBuffer);
                                                            if (subtitleBuffer != null)
                                                            {
                                                                string subtitleTrackName = (!string.IsNullOrEmpty(cl.Configuration.TrackName) ? cl.Configuration.TrackName : "sub" + Index.ToString());
                                                                string subtitleTrackLang = (!string.IsNullOrEmpty(cl.Configuration.Language) ? cl.Configuration.Language : "unk");
                                                                int HLSPeriod = 6000;
                                                                ParseAndAddTTMLSubtitles((ulong)time, subtitleTrackName, subtitleTrackLang, cl.Configuration.Bitrate, HLSPeriod, subtitleBuffer);
                                                            }

                                                        }

                                                        cl.InputBytes += l;
                                                        cl.InputChunks++;

                                                        cl.ChunksQueue.Enqueue(cb);
                                                    }
                                                    else
                                                        cl.InputChunks++;

                                                }
                                            }
                                            Status = SmoothAssetStatus.SubtitlesLoaded;

                                        }

                                    }
                                    else
                                    {
                                        if (SmoothManifestManager.IsLive)
                                            result = true;
                                    }
                                }
                                else
                                    Status = SmoothAssetStatus.SubtitlesNotAvailable;

                                if ((SmoothManifestManager.IsLive))
                                {
                                    Program.Logger.LogInformation("AMS Reverse Proxy Service: Updating the live TTML subtitles for " + RootUri);
                                    double delta = (DateTime.Now - LatestManifestDownloadTime).TotalMilliseconds;
                                    if (delta< ManifestUpdatePeriod)
                                        System.Threading.Tasks.Task.Delay(ManifestUpdatePeriod - (int)delta).Wait();
                                    System.Diagnostics.Debug.WriteLine(string.Format("{0:d/M/yyyy HH:mm:ss.fff}", DateTime.Now) + " Update Manifest for Uri: " + RootUri.ToString());
                                    await SmoothManifestManager.ParseAndUpdateSmoothManifest();
                                    System.Diagnostics.Debug.WriteLine(string.Format("{0:d/M/yyyy HH:mm:ss.fff}", DateTime.Now) + " Update Manifest done for Uri: " + RootUri.ToString());
                                    LatestManifestDownloadTime = DateTime.Now;
                                }
                                else
                                {
                                    Program.Logger.LogInformation("AMS Reverse Proxy Service: Capture and conversion of TTML subtitles done for " + RootUri);
                                    this.downloadManifestTaskRunning = false;
                                }

                            }
                        }
                    }
                }
                catch(Exception )
                {
                    result = false;
                }

            }
                );

            return result;
        }




    }
    //class SubtitleTrack
    //{
    //    public SubtitleTrackStatus Status { get; set; }
    //    public string RootUri { get; set; }
    //    public string Lang { get; set; }
    //    public List<TTMLSubtitles> SubtitleList { get; set; }
    //    System.Threading.Tasks.Task SubtitleTask = null;
    //    SmoothHelper.ManifestManager SmoothManifestManager;
    //    public SubtitleTrack(string root)
    //    {
    //        RootUri = root;
    //        Lang = "unk";
    //        Status = SubtitleTrackStatus.Init;
    //        SubtitleList = new List<TTMLSubtitles>();
    //    }
    //    public string ParseTTMLChunk(byte[] data)
    //    {
    //        string result = string.Empty;
    //        if ((data != null) && (data.Length > 0))
    //        {
    //            int index = 0;
    //            while (index < data.Length)
    //            {
    //                AMSReverseProxy.SmoothHelper.Mp4Box box = AMSReverseProxy.SmoothHelper.Mp4Box.CreateMp4Box(data, index);
    //                if (box.GetBoxType() == "moof")
    //                {
    //                    index += box.GetBoxLength();
    //                }
    //                else if (box.GetBoxType() == "mdat")
    //                {
    //                    result = System.Text.Encoding.UTF8.GetString(box.GetBoxData(), 0, box.GetBoxData().Length);
    //                    index += box.GetBoxLength();
    //                    break;
    //                }
    //                else
    //                {
    //                    index += box.GetBoxLength();
    //                }
    //            }
    //        }
    //        return result;
    //    }
    //    public bool LoadSubtitles()
    //    {
    //        bool result = false;
    //        SubtitleTask = System.Threading.Tasks.Task.Factory.StartNew(async ()
    //            =>
    //                {
    //                    SmoothManifestManager = SmoothHelper.ManifestManager.CreateManifestManager(new Uri(RootUri), false, 5000000, 20);
    //                    if (SmoothManifestManager != null)
    //                    {

    //                        bool res = await SmoothManifestManager.LoadAndParseSmoothManifest();
    //                        if (res == true)
    //                        {
    //                            Status = SubtitleTrackStatus.ManifestLoaded;
    //                            System.Diagnostics.Debug.Write("SmoothStreaming Manifest: " + RootUri + " correctly downloaded and parsed ");

    //                            // Download Text Chunks
    //                            if ((SmoothManifestManager.TextChunkListList != null) && (SmoothManifestManager.TextChunkListList.Count > 0))
    //                            {
    //                                Status = SubtitleTrackStatus.SubtitlesAvailable;
    //                                // Something to download
    //                                if (!SmoothManifestManager.IsDownloadCompleted(SmoothManifestManager.TextChunkListList))
    //                                {
    //                                    System.Diagnostics.Debug.WriteLine(string.Format("{0:d/M/yyyy HH:mm:ss.fff}", DateTime.Now) + " downloadThread downloading text chunks for Manifest Uri: " + mm.ManifestUri.ToString());
    //                                    foreach (var cl in SmoothManifestManager.TextChunkListList)
    //                                    {
    //                                        SmoothHelper.ChunkBuffer cb;
    //                                        while (cl.ChunksToReadQueue.TryDequeue(out cb))
    //                                        {
    //                                            string url = (string.IsNullOrEmpty(SmoothManifestManager.RedirectBaseUrl) ? mm.BaseUrl : mm.RedirectBaseUrl) + "/" + cl.TemplateUrl.Replace("{start_time}", cb.Time.ToString());
    //                                            System.Diagnostics.Debug.WriteLine(string.Format("{0:d/M/yyyy HH:mm:ss.fff}", DateTime.Now) + " downloadThread downloading text chunks : " + url.ToString());
    //                                            cb.chunkBuffer = await SmoothManifestManager.DownloadChunkAsync(new Uri(url));

    //                                            if (cb.IsChunkDownloaded())
    //                                            {
    //                                                ulong l = cb.GetLength();
    //                                                string text = ParseTTMLChunk(cb.chunkBuffer);
    //                                                double time = SmoothManifestManager.TimescaleToHNS(cb.Time) / (SmoothHelper.ManifestManager.TimeUnit);

    //                                                System.Diagnostics.Debug.Write("TTML file at : " + time.ToString() + " seconds \r\n" + text);
                                                    
    //                                                SmoothHelper.TTMLSubtitles tTMLSubtitles = new SmoothHelper.TTMLSubtitles(time);
    //                                                if (tTMLSubtitles != null)
    //                                                {
    //                                                    if (tTMLSubtitles.ParseTTMLSubtitles(System.Text.Encoding.UTF8.GetBytes(text)) == true)
    //                                                    {
    //                                                        if (tTMLSubtitles.subtitleList != null)
    //                                                        {
    //                                                            long count = tTMLSubtitles.subtitleList.LongCount();
    //                                                            if (SubtitleList != null)
    //                                                                SubtitleList.Add(tTMLSubtitles);
    //                                                            System.Diagnostics.Debug.WriteLine(string.Format("{0:d/M/yyyy HH:mm:ss.fff}", DateTime.Now) + count.ToString() + " subtitles found in the chunk");
    //                                                        }
    //                                                    }
    //                                                }
    //                                                cl.InputBytes += l;
    //                                                cl.InputChunks++;

    //                                                cl.ChunksQueue.Enqueue(cb);
    //                                            }
    //                                        }
    //                                    }
    //                                    System.Diagnostics.Debug.WriteLine(string.Format("{0:d/M/yyyy HH:mm:ss.fff}", DateTime.Now) + " downloadThread downloading text chunks done for Uri: " + mm.ManifestUri.ToString());
    //                                }
    //                                else
    //                                {
    //                                    if (SmoothManifestManager.IsLive)
    //                                        result = true;
    //                                }
    //                            }
    //                            else
    //                                Status = SubtitleTrackStatus.SubtitlesNotAvailable;
    //                        }
    //                    }
    //                }
    //            );

    //        return result;
    //    }
    //    public SubtitleTrack(string root, string lang)
    //    {
    //        RootUri = root;
    //        Lang = lang;
    //        Status = SubtitleTrackStatus.Init;
    //        SubtitleList = new List<TTMLSubtitles>();
    //    }
    //    public bool AddTTMLSubtitles(TTMLSubtitles ttml)
    //    {
    //        if(SubtitleList == null)
    //            SubtitleList = new List<TTMLSubtitles>();
    //        if (SubtitleList == null)
    //            SubtitleList.Add(ttml);
    //        return true;
    //    }



    //}
    /// <summary>
    /// Parses a Smooth Streaming Manifest.
    /// </summary>
    //class TTMLSubtitles    {


    //    public List<SubtitleItem> subtitleList;
    //    public double TimeOffset =  0;
    //    // tick = hundred nano second = 10^7
    //    public const int TicksPerSecond = 10000000;

    //    /// <summary>
    //    /// Defines the TTMLSubtitles element.
    //    /// </summary>
    //    private const string TTMLSubtitlesElement = "tt";

    //    /// <summary>
    //    /// Defines the TTMLSubtitles head.
    //    /// </summary>
    //    private const string TTMLSubtitlesHead = "head";

    //    /// <summary>
    //    /// Defines the TTMLSubtitles body.
    //    /// </summary>
    //    private const string TTMLSubtitlesBody = "body";

    //    /// <summary>
    //    /// Defines the TTMLSubtitles div.
    //    /// </summary>
    //    private const string TTMLSubtitlesDiv = "div";

    //    /// <summary>
    //    /// Defines the TTMLSubtitles p.
    //    /// </summary>
    //    private const string TTMLSubtitlesP = "p";

    //    /// <summary>
    //    /// Defines the Manifest MajorVersion attribute.
    //    /// </summary>
    //    private const string ManifestMajorVersionAttribute = "MajorVersion";

    //    /// <summary>
    //    /// Defines the  begin attribute.
    //    /// </summary>
    //    private const string BeginAttribute = "begin";

    //    /// <summary>
    //    /// Defines the  end attribute.
    //    /// </summary>
    //    private const string EndAttribute = "end";

    //    /// <summary>
    //    /// Defines the  dur attribute.
    //    /// </summary>
    //    private const string DurationAttribute = "dur";



    //    /// <summary>
    //    /// Initializes a new instance of the <see cref="SmoothStreamingManifestParser"/> class.
    //    /// </summary>
    //    /// <param name="subtitleStream">The stream of the manifest being parsed.</param>
    //    public TTMLSubtitles()
    //    {
    //        TimeOffset = 0;
    //        if (subtitleList == null)
    //            subtitleList = new List<SubtitleItem>();
    //    }
    //    public TTMLSubtitles(double timeOffset)
    //    {
    //        TimeOffset = timeOffset;
    //        if (subtitleList == null)
    //            subtitleList = new List<SubtitleItem>();
    //    }
    //    /// <summary>
    //    /// Initializes a new instance of the <see cref="SmoothStreamingManifestParser"/> class.
    //    /// </summary>
    //    /// <param name="subtitleStream">The buffer of the manifest being parsed.</param>
    //    public bool ParseTTMLSubtitles(byte[] manifestBuffer)
    //    {
    //        bool bResult = false;
    //        using (var subtitleStream = new MemoryStream(manifestBuffer))
    //        {
    //            bResult = this.ParseTTMLSubtitles(subtitleStream);
    //        }
    //        return bResult;
    //    }



    //    /// <summary>
    //    /// Adds attributes to the stream info.
    //    /// </summary>
    //    /// <param name="reader">The xml reader.</param>
    //    /// <param name="streamInfo">The stream info.</param>
    //    private static void AddAttributes(XmlReader reader, StreamInfo streamInfo)
    //    {
    //        if (reader.HasAttributes && reader.MoveToFirstAttribute())
    //        {
    //            do
    //            {
    //                streamInfo.AddAttribute(reader.Name, reader.Value);
    //            }
    //            while (reader.MoveToNextAttribute());
    //            reader.MoveToFirstAttribute();
    //        }
    //    }

    //    /// <summary>
    //    /// Adds attributes to the quality level.
    //    /// </summary>
    //    /// <param name="reader">The xml reader.</param>
    //    /// <param name="qualityLevel">The quality level.</param>
    //    private static void AddAttributes(XmlReader reader, QualityLevel qualityLevel)
    //    {
    //        if (reader.HasAttributes && reader.MoveToFirstAttribute())
    //        {
    //            do
    //            {
    //                qualityLevel.AddAttribute(reader.Name, reader.Value);
    //            }
    //            while (reader.MoveToNextAttribute());
    //            reader.MoveToElement();
    //        }
    //    }

    //    /// <summary>
    //    /// Adds custom attributes to the quality level.
    //    /// </summary>
    //    /// <param name="reader">The xml reader.</param>
    //    /// <param name="qualityLevel">The quality level.</param>
    //    private static void AddCustomAttributes(XmlReader reader, QualityLevel qualityLevel)
    //    {
    //        if (!reader.IsEmptyElement)
    //        {
    //            while (reader.Read())
    //            {
    //                if (reader.Name == "CustomAttributes" && reader.NodeType == XmlNodeType.Element)
    //                {
    //                    while (reader.Read())
    //                    {
    //                        if ((reader.Name == "Attribute") && (reader.NodeType == XmlNodeType.Element))
    //                        {
    //                            string attribute = reader.GetAttribute("Name");

    //                            if (!string.IsNullOrEmpty(attribute))
    //                            {
    //                                qualityLevel.AddCustomAttribute(attribute, reader.GetAttribute("Value"));
    //                            }
    //                        }

    //                        if ((reader.Name == "CustomAttributes") && (reader.NodeType == XmlNodeType.EndElement))
    //                        {
    //                            return;
    //                        }
    //                    }
    //                }

    //                if (reader.Name == "QualityLevel" && reader.NodeType == XmlNodeType.EndElement)
    //                {
    //                    return;
    //                }
    //            }
    //        }
    //    }
    //    string GetMultiLineText(XmlReader reader)
    //    {
    //        string Text = string.Empty;
    //        while ((reader.Name == "") && (reader.NodeType == XmlNodeType.Whitespace))
    //            reader.Read();
    //        if (reader.NodeType == XmlNodeType.Element)
    //        {
    //            if (reader.Name == "span")
    //            {

    //                reader.Read();
    //                while ((reader.Name == "") && (reader.NodeType == XmlNodeType.Whitespace))
    //                    reader.Read();
    //                if (reader.NodeType == XmlNodeType.Text)
    //                {
    //                    Text = reader.Value;
    //                }
    //                reader.Read();
    //                while ((reader.Name == "") && (reader.NodeType == XmlNodeType.Whitespace))
    //                    reader.Read();
    //                while (((reader.Name == "br") || (reader.Name == "span")) && ((reader.NodeType == XmlNodeType.Element)|| (reader.NodeType == XmlNodeType.EndElement)))
    //                {
    //                    reader.Read();
    //                    while ((reader.Name == "") && (reader.NodeType == XmlNodeType.Whitespace))
    //                        reader.Read();
    //                    if (reader.NodeType == XmlNodeType.Text)
    //                    {
    //                        Text += "\n" + reader.Value;
    //                    }
    //                    reader.Read();
    //                    while ((reader.Name == "") && (reader.NodeType == XmlNodeType.Whitespace))
    //                        reader.Read();
    //                }
    //            }
    //        }
    //        else if (reader.NodeType == XmlNodeType.Text)
    //        {
    //            Text = reader.Value;
    //            reader.Read();
    //            while ((reader.Name == "") && (reader.NodeType == XmlNodeType.Whitespace))
    //                reader.Read();
    //            while ((reader.Name == "br") && (reader.NodeType == XmlNodeType.Element))
    //            {
    //                reader.Read();
    //                while ((reader.Name == "") && (reader.NodeType == XmlNodeType.Whitespace))
    //                    reader.Read();
    //                if (reader.NodeType == XmlNodeType.Text)
    //                {
    //                    Text += "\n" + reader.Value;
    //                }
    //                reader.Read();
    //                while ((reader.Name == "") && (reader.NodeType == XmlNodeType.Whitespace))
    //                    reader.Read();
    //            }
    //        }

    //        return Text;
    //    }
    //    /// <summary>
    //    /// Parses the TTML stream.
    //    /// </summary>
    //    /// <param name="subtitleStream">The manifest stream being parsed.</param>
    //    public bool ParseTTMLSubtitles(Stream subtitleStream)
    //    {
    //        bool bResult = false;
    //        try
    //        {
    //            using (XmlReader reader = XmlReader.Create(subtitleStream))
    //            {
    //                if (reader.Read() && reader.IsStartElement(TTMLSubtitlesElement))
    //                {
    //                    while (reader.Read())
    //                    {
    //                        if (reader.Name == TTMLSubtitlesBody && reader.NodeType == XmlNodeType.Element)
    //                        {
    //                            reader.Read();
    //                            while ((reader.Name == "") && (reader.NodeType == XmlNodeType.Whitespace))
    //                                reader.Read();
    //                            if (reader.Name == TTMLSubtitlesDiv && reader.NodeType == XmlNodeType.Element)
    //                            {

    //                                while (((reader.Name == TTMLSubtitlesP) && (reader.NodeType == XmlNodeType.Element)) || reader.Read())
    //                                {
    //                                    while ((reader.Name == "") && (reader.NodeType == XmlNodeType.Whitespace))
    //                                        reader.Read();
    //                                    if (reader.Name == TTMLSubtitlesP && reader.NodeType == XmlNodeType.Element)
    //                                    {
    //                                        string Text = string.Empty;
    //                                        string Id = string.Empty;
    //                                        string Begin = string.Empty;
    //                                        string End = string.Empty;
    //                                        string Dur = string.Empty;
    //                                        if (reader.HasAttributes)
    //                                        {
    //                                            for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
    //                                            {
    //                                                reader.MoveToAttribute(attInd);
    //                                                if (reader.NodeType == XmlNodeType.Attribute)
    //                                                {
    //                                                    if (reader.Name == "begin")
    //                                                        Begin = reader.Value;
    //                                                    else if (reader.Name == "dur")
    //                                                        Dur = reader.Value;
    //                                                    else if (reader.Name == "end")
    //                                                        End = reader.Value;
    //                                                    else if (reader.Name == "xml:id")
    //                                                        Id = reader.Value;
    //                                                }
    //                                            }
    //                                        }
    //                                        reader.Read();

    //                                        Text = GetMultiLineText(reader);

    //                                        if (!string.IsNullOrEmpty(Text) &&
    //                                            !string.IsNullOrEmpty(Begin) &&
    //                                            !string.IsNullOrEmpty(End)
    //                                            )
    //                                        {

    //                                            System.Diagnostics.Debug.WriteLine("Subtitle: " + Id + " Content: \r\n" + Text);
    //                                            ulong BeginTime = SubtitleItem.ParseTime(Begin);
    //                                            ulong EndTime = SubtitleItem.ParseTime(End);

    //                                            SubtitleItem item = new SubtitleItem((ulong)TimeOffset + BeginTime, (ulong)TimeOffset + EndTime, Text);
    //                                            if (item != null)
    //                                                subtitleList.Add(item);
    //                                        }
    //                                    }
    //                                }
    //                            }
    //                        }
    //                    }
    //                }
    //            }
    //            bResult = true;
    //        }
    //        catch(Exception ex)
    //        {
    //            System.Diagnostics.Debug.WriteLine("Exception while parsing Subtitles file: " + ex.Message );
    //            bResult = false;
    //        }
    //        return bResult;
    //    }
    //}
}
