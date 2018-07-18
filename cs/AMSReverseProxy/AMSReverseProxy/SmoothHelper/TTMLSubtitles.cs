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
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;
    class SubtitleTrack
    {
        public string Lang { get; set; }
        public List<TTMLSubtitles> SubtitleList { get; set; }
        public SubtitleTrack()
        {
            Lang = "unk";
            SubtitleList = new List<TTMLSubtitles>();
        }
        public SubtitleTrack(string lang)
        {
            Lang = lang;
            SubtitleList = new List<TTMLSubtitles>();
        }
        public bool AddTTMLSubtitles(TTMLSubtitles ttml)
        {
            if(SubtitleList == null)
                SubtitleList = new List<TTMLSubtitles>();
            if (SubtitleList == null)
                SubtitleList.Add(ttml);
            return true;
        }


    }
    /// <summary>
    /// Parses a Smooth Streaming Manifest.
    /// </summary>
    class TTMLSubtitles    {


        public List<SubtitleItem> subtitleList;
        public double TimeOffset =  0;
        // tick = hundred nano second = 10^7
        public const int TicksPerSecond = 10000000;

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
        /// Defines the TTMLSubtitles div.
        /// </summary>
        private const string TTMLSubtitlesDiv = "div";

        /// <summary>
        /// Defines the TTMLSubtitles p.
        /// </summary>
        private const string TTMLSubtitlesP = "p";

        /// <summary>
        /// Defines the Manifest MajorVersion attribute.
        /// </summary>
        private const string ManifestMajorVersionAttribute = "MajorVersion";

        /// <summary>
        /// Defines the  begin attribute.
        /// </summary>
        private const string BeginAttribute = "begin";

        /// <summary>
        /// Defines the  end attribute.
        /// </summary>
        private const string EndAttribute = "end";

        /// <summary>
        /// Defines the  dur attribute.
        /// </summary>
        private const string DurationAttribute = "dur";



        /// <summary>
        /// Initializes a new instance of the <see cref="SmoothStreamingManifestParser"/> class.
        /// </summary>
        /// <param name="subtitleStream">The stream of the manifest being parsed.</param>
        public TTMLSubtitles()
        {
            TimeOffset = 0;
            if (subtitleList == null)
                subtitleList = new List<SubtitleItem>();
        }
        public TTMLSubtitles(double timeOffset)
        {
            TimeOffset = timeOffset;
            if (subtitleList == null)
                subtitleList = new List<SubtitleItem>();
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="SmoothStreamingManifestParser"/> class.
        /// </summary>
        /// <param name="subtitleStream">The buffer of the manifest being parsed.</param>
        public bool ParseTTMLSubtitles(byte[] manifestBuffer)
        {
            bool bResult = false;
            using (var subtitleStream = new MemoryStream(manifestBuffer))
            {
                bResult = this.ParseTTMLSubtitles(subtitleStream);
            }
            return bResult;
        }



        /// <summary>
        /// Adds attributes to the stream info.
        /// </summary>
        /// <param name="reader">The xml reader.</param>
        /// <param name="streamInfo">The stream info.</param>
        private static void AddAttributes(XmlReader reader, StreamInfo streamInfo)
        {
            if (reader.HasAttributes && reader.MoveToFirstAttribute())
            {
                do
                {
                    streamInfo.AddAttribute(reader.Name, reader.Value);
                }
                while (reader.MoveToNextAttribute());
                reader.MoveToFirstAttribute();
            }
        }

        /// <summary>
        /// Adds attributes to the quality level.
        /// </summary>
        /// <param name="reader">The xml reader.</param>
        /// <param name="qualityLevel">The quality level.</param>
        private static void AddAttributes(XmlReader reader, QualityLevel qualityLevel)
        {
            if (reader.HasAttributes && reader.MoveToFirstAttribute())
            {
                do
                {
                    qualityLevel.AddAttribute(reader.Name, reader.Value);
                }
                while (reader.MoveToNextAttribute());
                reader.MoveToElement();
            }
        }

        /// <summary>
        /// Adds custom attributes to the quality level.
        /// </summary>
        /// <param name="reader">The xml reader.</param>
        /// <param name="qualityLevel">The quality level.</param>
        private static void AddCustomAttributes(XmlReader reader, QualityLevel qualityLevel)
        {
            if (!reader.IsEmptyElement)
            {
                while (reader.Read())
                {
                    if (reader.Name == "CustomAttributes" && reader.NodeType == XmlNodeType.Element)
                    {
                        while (reader.Read())
                        {
                            if ((reader.Name == "Attribute") && (reader.NodeType == XmlNodeType.Element))
                            {
                                string attribute = reader.GetAttribute("Name");

                                if (!string.IsNullOrEmpty(attribute))
                                {
                                    qualityLevel.AddCustomAttribute(attribute, reader.GetAttribute("Value"));
                                }
                            }

                            if ((reader.Name == "CustomAttributes") && (reader.NodeType == XmlNodeType.EndElement))
                            {
                                return;
                            }
                        }
                    }

                    if (reader.Name == "QualityLevel" && reader.NodeType == XmlNodeType.EndElement)
                    {
                        return;
                    }
                }
            }
        }
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
                    while (((reader.Name == "br") || (reader.Name == "span")) && ((reader.NodeType == XmlNodeType.Element)|| (reader.NodeType == XmlNodeType.EndElement)))
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
        /// Parses the manifest stream.
        /// </summary>
        /// <param name="subtitleStream">The manifest stream being parsed.</param>
        public bool ParseTTMLSubtitles(Stream subtitleStream)
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

                                                System.Diagnostics.Debug.WriteLine("Subtitle: " + Id + " Content: \r\n" + Text);
                                                ulong BeginTime = SubtitleItem.ParseTime(Begin);
                                                ulong EndTime = SubtitleItem.ParseTime(End);

                                                SubtitleItem item = new SubtitleItem((ulong)TimeOffset + BeginTime, (ulong)TimeOffset + EndTime, Text);
                                                if (item != null)
                                                    subtitleList.Add(item);
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
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while parsing Subtitles file: " + ex.Message );
                bResult = false;
            }
            return bResult;
        }
    }
}
