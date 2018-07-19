using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AMSReverseProxy
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }
        bool IsSmoothStreamingManifestContentType(string contentType)
        {
            if (!string.IsNullOrEmpty(contentType))
            {
                if (contentType.EndsWith("application/vnd.ms-sstr+xml", StringComparison.CurrentCultureIgnoreCase))
                    return true;
                if (contentType.EndsWith("text/xml", StringComparison.CurrentCultureIgnoreCase))
                    return true;
            }
            return false;
        }
        bool IsSmoothStreamingManifest(string url, string contentType)
        {
            if (!string.IsNullOrEmpty(url))
            {
                if (IsSmoothStreamingManifestContentType(contentType))
                {

                    if (url.EndsWith("/manifest", StringComparison.CurrentCultureIgnoreCase))
                        return true;
                }
            }
            return false;
        }
        bool IsHLSManifestContentType(string contentType)
        {
            if (!string.IsNullOrEmpty(contentType))
            {
                if (contentType.EndsWith("application/vnd.apple.mpegurl", StringComparison.CurrentCultureIgnoreCase))
                    return true;

            }
            return false;
        }
        bool IsHLSManifest(string url, string contentType)
        {
            if (!string.IsNullOrEmpty(url))
            {
                if (IsHLSManifestContentType(contentType))
                {

                    if (url.EndsWith("/manifest(format=m3u8-aapl)", StringComparison.CurrentCultureIgnoreCase))
                        return true;
                    if (url.EndsWith("manifest(format=m3u8-aapl-v3)", StringComparison.CurrentCultureIgnoreCase))
                        return true;
                }
            }
            return false;
        }
        bool IsHLSSubManifest(string url, string contentType)
        {
            if (!string.IsNullOrEmpty(url))
            {
                if (IsHLSManifestContentType(contentType))
                {
                    if (url.Contains(")/Manifest(")&&
                         url.Contains(".ism/QualityLevels(") &&
                         url.Contains(",format=m3u8-aapl"))
                        return true;
                }
            }
            return false;
        }
        bool IsHLSVideoManifest(string url)
        {
            if (!string.IsNullOrEmpty(url))
            {
                if (url.Contains(")/Manifest(video") &&
                     url.Contains("QualityLevels(") &&
                     url.Contains(",format=m3u8-aapl"))
                    return true;
            }
            return false;
        }
        bool IsHLSSubtitleManifest(string url)
        {
            if (!string.IsNullOrEmpty(url))
            {
                if (url.Contains(")/Manifest(text") &&
                     url.Contains("QualityLevels(") &&
                     url.Contains(",format=m3u8-aapl"))
                    return true;
            }
            return false;
        }

        bool IsHLSSubtitle(string url)
        {
            if (!string.IsNullOrEmpty(url))
            {
                if (url.Contains(")/Fragments(text") &&
                     url.Contains("QualityLevels(") &&
                     url.Contains(",format=m3u8-aapl"))
                    return true;
            }
            return false;
        }
        ulong GetHLSTimeFromUrl(string url)
        {
            ulong result = 0;
            if (!string.IsNullOrEmpty(url))
            {
                try
                {

                    int pos = url.IndexOf(")/Fragments(text");
                    int lastpos = url.IndexOf(",format=m3u8-aapl");
                    if ((pos > 0) && (lastpos > 0))
                    {
                        string s = url.Substring(pos, lastpos - pos);
                        if (!string.IsNullOrEmpty(s))
                        {
                            char sep = '=';
                            string[] array = s.Split(sep);
                            if (array.Length == 2)
                                ulong.TryParse(array[1], out result);

                        }
                    }
                }
                catch(Exception)
                {
                    result = 0;
                }
            }
            return result;
        }
        bool IsDASHManifestContentType(string contentType)
        {
            if (!string.IsNullOrEmpty(contentType))
            {
                if (contentType.EndsWith("application/dash+xml", StringComparison.CurrentCultureIgnoreCase))
                    return true;
            }
            return false;
        }
        bool IsDASHManifest(string url, string contentType)
        {
            if (!string.IsNullOrEmpty(url))
            {
                if (IsDASHManifestContentType(contentType))
                {
                    if (url.EndsWith(".mpd", StringComparison.CurrentCultureIgnoreCase))
                        return true;
                    if (url.EndsWith("/manifest(format=mpd-time-csf)", StringComparison.CurrentCultureIgnoreCase))
                        return true;
                }
            }
            return false;
        }
        public string ParseTTMLChunk(byte[] data)
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
                    }
                    else 
                    {
                        index += box.GetBoxLength();
                    }
                }
            }
            return result;
        }
        public string GetAMSRootPath(string Path)
        {
            string result = string.Empty;
            const string amsSuffix = "ism/manifest";
            if (!string.IsNullOrEmpty(Path))
            {
                int pos = Path.IndexOf(amsSuffix);
                if(pos>0)
                {
                    result = Path.Substring(0, pos + amsSuffix.Length);
                }
            }
            return result;
        }
        public Uri GetAMSRootUri(Uri inputUri)
        {
            const string hlsSuffix = "(format=m3u8-aapl)";
            const string dashSuffix = "(format=mpd-time-csf)";

            Uri result = null;

            if(inputUri!=null)
            {
                try
                {
                    if ((inputUri.ToString().EndsWith("ism/manifest", StringComparison.InvariantCultureIgnoreCase)) ||
                       (inputUri.ToString().EndsWith("isml/manifest", StringComparison.InvariantCultureIgnoreCase)))
                    {
                        result = inputUri;
                    }
                    else if ((inputUri.ToString().EndsWith("ism/manifest" + hlsSuffix, StringComparison.InvariantCultureIgnoreCase)) ||
                       (inputUri.ToString().EndsWith("isml/manifest" + hlsSuffix, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        result = new Uri(inputUri.ToString().Substring(0, inputUri.ToString().Length - hlsSuffix.Length));
                    }
                    if ((inputUri.ToString().EndsWith("ism/manifest" + dashSuffix, StringComparison.InvariantCultureIgnoreCase)) ||
                       (inputUri.ToString().EndsWith("isml/manifest" + dashSuffix, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        result = new Uri(inputUri.ToString().Substring(0, inputUri.ToString().Length - dashSuffix.Length));
                    }
                }
                catch(Exception)
                {
                    result = null;
                }

            }
            return result;
        }
        //
        bool IsHLSManifestWithSubtitle(string m3u8)
        {
            bool result = false;
            if(!string.IsNullOrEmpty(m3u8))
            {
                if (m3u8.IndexOf("#EXT-X-MEDIA:TYPE=SUBTITLES") > 0)
                    result = true; 
            }
            return result;
        }
        int GetNumberOfSequences(string manifest)
        {
            int result = 0;
            if(!string.IsNullOrEmpty(manifest))
            {
                string key = "#EXT-X-MEDIA-SEQUENCE:";
                int pos = manifest.IndexOf(key);
                if(pos>0)
                {
                    int lastPos = manifest.IndexOf("\r", pos);
                    if (lastPos < 0)
                        lastPos = manifest.IndexOf("\n", pos);
                    if (lastPos > 0)
                    {
                        string s = manifest.Substring(pos + key.Length, lastPos - pos - key.Length);
                        if (!string.IsNullOrEmpty(s))
                        {
                            int.TryParse(s, out result);
                        }
                    }
                }
            }
            return result;
        }
        int GetNumberOfItems(string manifest)
        {
            int result = 0;
            if (!string.IsNullOrEmpty(manifest))
            {
                
                string key = "#EXTINF:";

                int pos = 0;
                while (pos >= 0)
                {
                    pos = manifest.IndexOf(key, pos);
                    if (pos > 0)
                    { 
                    result++;
                    pos += 8;
                    }
                }
            }
            return result;
        }
        int GetSequenceDuration(string manifest)
        {
            int result = 0;
            if (!string.IsNullOrEmpty(manifest))
            {
                string key = "#EXT-X-TARGETDURATION:";
                int pos = manifest.IndexOf(key);
                if (pos > 0)
                {
                    int lastPos = manifest.IndexOf("\r", pos);
                    if (lastPos < 0)
                        lastPos = manifest.IndexOf("\n", pos);
                    if (lastPos > 0)
                    {
                        string s = manifest.Substring(pos + key.Length, lastPos - pos - key.Length);
                        if (!string.IsNullOrEmpty(s))
                        {
                            int.TryParse(s, out result);
                        }
                    }
                }
            }
            return result;
        }
        ulong GetSequenceStartTick(string manifest)
        {
            ulong result = 0;
            if (!string.IsNullOrEmpty(manifest))
            {
                string key = "Fragments(video=";
                int pos = manifest.IndexOf(key);
                if (pos > 0)
                {
                    int lastPos = manifest.IndexOf(",", pos);
                    if (lastPos > 0)
                    {
                        string s = manifest.Substring(pos + key.Length, lastPos - pos - key.Length);
                        if (!string.IsNullOrEmpty(s))
                        {
                            ulong.TryParse(s, out result);
                        }
                    }
                }
            }
            return result;
        }
        string AddSubtitlesinHLSManifest(string inputManifest, string groupID, string language, string name, string uri)
        {
            string result = string.Empty;

            if(!string.IsNullOrEmpty(inputManifest))
            {
                int pos = -1;
                pos = inputManifest.LastIndexOf("#EXT-X-MEDIA:TYPE=");
                if(pos>0)
                {
                    int lastPos = inputManifest.IndexOf("\r", pos);
                    if(lastPos <0)
                        lastPos = inputManifest.IndexOf("\n", pos);
                    if (lastPos > 0)
                    {
                        string newLine = "\r\n#EXT-X-MEDIA:TYPE=SUBTITLES,GROUP-ID=\"" + groupID + "\",NAME=\"" + name + "\",DEFAULT=YES,FORCED=NO,LANGUAGE=\"" + language + "\",URI=\"" + uri + "\"\r\n";

                        string LastMediaTypeLine = inputManifest.Substring(pos, lastPos - pos);
                        result = inputManifest.Replace(LastMediaTypeLine, LastMediaTypeLine + newLine);
                        int startPos = 0;
                        pos = 0;
                        while (pos >= 0)
                        {
                            pos = result.IndexOf("#EXT-X-STREAM-INF:", startPos);
                            if(pos>0)
                            {
                                startPos = pos + 1;
                                lastPos = result.IndexOf("\r", pos);
                                if (lastPos < 0)
                                    lastPos = result.IndexOf("\n", pos);
                                if (lastPos > 0)
                                {
                                    string newAttribute = ",SUBTITLES=\"" + groupID + "\"";
                                    string streamInf = result.Substring(pos, lastPos - pos);
                                    result = result.Replace(streamInf, streamInf + newAttribute);
                                }
                            }
                        }
                    }
                }
            }
            return result;
        }
        public static string TimeToString(ulong d)
        {
            ulong hours = d / (3600 * 1000);
            ulong minutes = (d - hours * 3600 * 1000) / (60 * 1000);
            ulong seconds = (d - hours * 3600 * 1000 - minutes * 60 * 1000) / 1000;
            ulong milliseconds = (d - hours * 3600 * 1000 - minutes * 60 * 1000 - seconds * 1000);
            //if (hours == 0)
            //    return string.Format("{0:00}:{1:00}.{2:000}", minutes, seconds, milliseconds);
            //else 
            if (hours < 100)
                return string.Format("{0:00}:{1:00}:{2:00}.{3:000}", hours, minutes, seconds, milliseconds);
            else
                return string.Format("{0}:{1:00}:{2:00}.{3:000}", hours, minutes, seconds, milliseconds);
        }
        static int numberOfSequences ;
        static int numberOfItems;
        static int sequenceDuration;
        static ulong sequenceStartTick;
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            string localDNSName = Program.Configuration["localDNSName"];
            string remoteDNSName = Program.Configuration["remoteDNSName"];
            Dictionary<Uri, SmoothHelper.SmoothAsset> subTitleAssets = new Dictionary<Uri, SmoothHelper.SmoothAsset>();
            app.Run(async (context) =>
            {
                
                string Path = context.Request.Path;

                if (string.IsNullOrEmpty(Path))
                    await context.Response.WriteAsync("AMS Reverse Proxy");
                else
                {
                    //string Host = "http://localhost/";
                    string Host = string.Empty;

                    if(context.Request.IsHttps)
                        Host = "https://" + remoteDNSName + "/";
                    else
                        Host = "http://" + remoteDNSName + "/";
                    //string Host = "http://testamsmedia.streaming.mediaservices.windows.net/";

                    // 7155b94b-f47e-47e5-8265-622d3fcf6e9d/5890621e-c209-4abe-a524-7bd6723e7851.ism/manifest(format=m3u8-aapl)
                    //                  https://testamsmedia.streaming.mediaservices.windows.net/7155b94b-f47e-47e5-8265-622d3fcf6e9d/5890621e-c209-4abe-a524-7bd6723e7851.ism/manifest(format=m3u8-aapl)


                    //                    string Host = "http://testsmoothlive-testamsmedia.channel.mediaservices.windows.net/";
                    //                    string Host = "http://testsmoothlive-testamsmedia.channel.mediaservices.windows.net/preview.isml/manifest";
                    //    string Host = "http://srgtest-accountdigitalinnovation-euwe.channel.media.azure.net/";
                    //string Host = "http://amssamples.streaming.mediaservices.windows.net/";
                    Uri requestUri = null;

                    Uri rootUri = null;
                    string rootPath = null;
                    try
                    {
                        requestUri = new Uri(Host + Path);
                    }
                    catch(Exception)
                    {

                    }
                    rootUri = GetAMSRootUri(requestUri);
                    rootPath = GetAMSRootPath(Path);
                   // if (rootUri != null)
                    {
                        System.Diagnostics.Debug.WriteLine("Request uri: " + Host + Path);
                        if (IsHLSSubtitleManifest(Host + Path))
                        {
                            ulong startTick = sequenceStartTick ;
                            DateTime d = new DateTime (1970,1,1) + new TimeSpan((long)sequenceStartTick);
                            string hlsSubtitleManifestPrefix = string.Format("#EXTM3U\r\n#EXT-X-VERSION:4\r\n#EXT-X-ALLOW-CACHE:NO\r\n#EXT-X-MEDIA-SEQUENCE:{0}\r\n#EXT-X-TARGETDURATION:{1}\r\n#EXT-X-PROGRAM-DATE-TIME:{2}\r\n",
                                numberOfSequences.ToString(),
                                sequenceDuration.ToString(),
                                d.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                            string hlsSubtitleSequencePrefix = string.Format("#EXTINF:{0}.000000,no-desc\r\n", sequenceDuration.ToString());
                            string hlsSubtitleSequenceMask = "Fragments(text1={0},format=m3u8-aapl)\r\n";



                            System.Text.StringBuilder sb = new System.Text.StringBuilder();
                            sb.Append(hlsSubtitleManifestPrefix);
                            for (int i = 0; i < numberOfItems; i++)
                            {
                                sb.Append(hlsSubtitleSequencePrefix);
                                sb.Append(string.Format(hlsSubtitleSequenceMask, startTick.ToString()));
                                startTick += ((ulong)sequenceDuration * 10000000);
                            }
                            System.Net.Http.HttpContent content = new System.Net.Http.StringContent(sb.ToString());
                            context.Response.ContentLength = content.Headers.ContentLength;
                            context.Response.ContentType = "application/vnd.apple.mpegurl";
                            await content.CopyToAsync(context.Response.Body);

                        }
                        else if (IsHLSSubtitle(Host + Path))
                        {
                            ulong time = GetHLSTimeFromUrl(Host + Path)/10000;
                            string hlsSubTitleMask = "WEBVTT\nX-TIMESTAMP-MAP=MPEGTS:0,LOCAL:00:00:00.000\n\n{0} --> {1}\n je peux pas\n\n{2} --> {3}\npas impossible,\n\n";
                            System.Net.Http.HttpContent content = new System.Net.Http.StringContent(string.Format(hlsSubTitleMask, /*(time*90).ToString()*/ TimeToString(time+1504), TimeToString(time + 2504), TimeToString(time + 3504), TimeToString(time + 5504)));
                            context.Response.ContentLength = content.Headers.ContentLength;
                            context.Response.ContentType = "binary/octet-stream";
                            await content.CopyToAsync(context.Response.Body);
                        }
                        else
                        {
                            // Azure Media Services request on manifest
                            System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
                            var response = await client.GetAsync(new Uri(Host + Path));
                            if (response != null)
                            {

                                context.Response.ContentType = response.Content.Headers.ContentType.MediaType;
                                context.Response.ContentLength = response.Content.Headers.ContentLength.Value;
                                if (IsSmoothStreamingManifest(Host + Path, context.Response.ContentType))
                                {
                                    System.Diagnostics.Debug.Write("SmoothStreaming Manifest: " + Host + Path + " ContentType: " + context.Response.ContentType);

                                    SmoothHelper.ManifestManager mm = SmoothHelper.ManifestManager.CreateManifestManager(new Uri(Host + Path), false, 5000000, 20);
                                    if (mm != null)
                                    {
                                        if ((response.Headers.Location != null) && (response.Headers.Location != new Uri(Host + Path)))
                                        {
                                            mm.RedirectUri = response.Headers.Location;
                                            mm.RedirectBaseUrl = SmoothHelper.ManifestManager.GetBaseUri(mm.RedirectUri.AbsoluteUri);
                                        }
                                        else
                                        {
                                            mm.RedirectBaseUrl = string.Empty;
                                            mm.RedirectUri = null;
                                        }
                                        mm.BaseUrl = SmoothHelper.ManifestManager.GetBaseUri(new Uri(Host + Path).AbsoluteUri);
                                        bool result = mm.ParseManifest(await response.Content.ReadAsByteArrayAsync());
                                        if (result == true)
                                        {
                                            System.Diagnostics.Debug.Write("SmoothStreaming Manifest: " + Host + Path + " correctly downloaded and parsed ");

                                            // Download Text Chunks
                                            //if ((mm.TextChunkListList != null) && (mm.TextChunkListList.Count > 0))
                                            //{
                                            //    // Something to download
                                            //    if (!mm.IsDownloadCompleted(mm.TextChunkListList))
                                            //    {
                                            //        System.Diagnostics.Debug.WriteLine(string.Format("{0:d/M/yyyy HH:mm:ss.fff}", DateTime.Now) + " downloadThread downloading text chunks for Manifest Uri: " + mm.ManifestUri.ToString());
                                            //        foreach (var cl in mm.TextChunkListList)
                                            //        {
                                            //            SmoothHelper.ChunkBuffer cb;
                                            //            while (cl.ChunksToReadQueue.TryDequeue(out cb))
                                            //            {
                                            //                string url = (string.IsNullOrEmpty(mm.RedirectBaseUrl) ? mm.BaseUrl : mm.RedirectBaseUrl) + "/" + cl.TemplateUrl.Replace("{start_time}", cb.Time.ToString());
                                            //                System.Diagnostics.Debug.WriteLine(string.Format("{0:d/M/yyyy HH:mm:ss.fff}", DateTime.Now) + " downloadThread downloading text chunks : " + url.ToString());
                                            //                cb.chunkBuffer = await mm.DownloadChunkAsync(new Uri(url));

                                            //                if (cb.IsChunkDownloaded())
                                            //                {
                                            //                    ulong l = cb.GetLength();
                                            //                    string text = ParseTTMLChunk(cb.chunkBuffer);
                                            //                    double time = mm.TimescaleToHNS(cb.Time) / (SmoothHelper.ManifestManager.TimeUnit);

                                            //                    System.Diagnostics.Debug.Write("TTML file at : " + time.ToString() + " seconds \r\n" + text);
                                            //                    System.Text.Encoding.UTF8.GetBytes(text);
                                            //                    SmoothHelper.TTMLSubtitles tTMLSubtitles = new SmoothHelper.TTMLSubtitles(time);
                                            //                    if (tTMLSubtitles != null)
                                            //                    {
                                            //                        if (tTMLSubtitles.ParseTTMLSubtitles(System.Text.Encoding.UTF8.GetBytes(text)) == true)
                                            //                        {
                                            //                            if (tTMLSubtitles.subtitleList != null)
                                            //                            {
                                            //                                long count = tTMLSubtitles.subtitleList.LongCount();
                                            //                                System.Diagnostics.Debug.WriteLine(string.Format("{0:d/M/yyyy HH:mm:ss.fff}", DateTime.Now) + count.ToString() + " subtitles found in the chunk");
                                            //                            }
                                            //                        }
                                            //                    }
                                            //                    cl.InputBytes += l;
                                            //                    cl.InputChunks++;

                                            //                    cl.ChunksQueue.Enqueue(cb);
                                            //                }
                                            //            }
                                            //        }
                                            //        System.Diagnostics.Debug.WriteLine(string.Format("{0:d/M/yyyy HH:mm:ss.fff}", DateTime.Now) + " downloadThread downloading text chunks done for Uri: " + mm.ManifestUri.ToString());
                                            //    }
                                            //    else
                                            //    {
                                            //        if (mm.IsLive)
                                            //            result = true;
                                            //    }
                                            //}
                                        }
                                        context.Response.Redirect(Host + Path);
                                        await response.Content.CopyToAsync(context.Response.Body);
                                        //var buffer = mm.UpdateSmoothManifestWithAbsoluteUri(await response.Content.ReadAsByteArrayAsync());

                                        //using (var manifestStream = new System.IO.MemoryStream(buffer))
                                        //{
                                        //    context.Response.ContentLength = buffer.LongLength;
                                        //    await manifestStream.CopyToAsync(context.Response.Body);
                                        //}
                                    }
                                }
                                else if (IsHLSManifest(Host + Path, context.Response.ContentType))
                                {
                                    System.Diagnostics.Debug.Write("HLS Manifest: " + Host + Path + " ContentType: " + context.Response.ContentType);

                                    string hlsManifest = await response.Content.ReadAsStringAsync();
                                    bool result = IsHLSManifestWithSubtitle(hlsManifest);
                                    if (result == true)
                                    {
                                        // Subtitles already present nothing to do
                                        context.Response.Redirect(Host + Path);
                                        await response.Content.CopyToAsync(context.Response.Body);
                                    }
                                    else
                                    {
                                        string lang = "en";
                                        string name = "english";
                                        string groupid = "sub1";
                                        //string subtitleUri = "http://" + localDNSName + rootPath + "/subtitles/" + lang + "_index.m3u8";
                                        //string subtitleUri = lang + "_index.m3u8";
                                        int subtitleBitrate = 10000;
                                        string subtitleUri = string.Format("QualityLevels({0})/Manifest({1},format=m3u8-aapl)", subtitleBitrate.ToString(),"text1");
                                        hlsManifest = AddSubtitlesinHLSManifest(hlsManifest, groupid, lang, name, subtitleUri);
                                        if (!string.IsNullOrEmpty(hlsManifest))
                                        {
                                            System.Net.Http.HttpContent content = new System.Net.Http.StringContent(hlsManifest);
                                            //context.Response.Redirect(Host + Path);
                                            context.Response.ContentLength = content.Headers.ContentLength;
                                            context.Response.ContentType = response.Content.Headers.ContentType.ToString();
                                            await content.CopyToAsync(context.Response.Body);
                                        }
                                        else
                                        {
                                            await response.Content.CopyToAsync(context.Response.Body);
                                        }

                                    }
                                }
                                else if (IsHLSSubManifest(Host + Path, context.Response.ContentType))
                                {
                                    if(IsHLSVideoManifest(Host + Path))
                                    {
                                        string videoPlayList = await response.Content.ReadAsStringAsync();
                                        if(!string.IsNullOrEmpty(videoPlayList))
                                        {
                                            numberOfSequences = GetNumberOfSequences(videoPlayList);
                                            numberOfItems = GetNumberOfItems(videoPlayList);
                                            sequenceDuration = GetSequenceDuration(videoPlayList);
                                            sequenceStartTick = GetSequenceStartTick(videoPlayList);
                                        }

                                    }
                                    System.Diagnostics.Debug.Write("HLS Sub Manifest: " + Host + Path + " ContentType: " + context.Response.ContentType);
                                   // context.Response.Redirect(Host + Path);
                                    await response.Content.CopyToAsync(context.Response.Body);
                                }
                                else if (IsDASHManifest(Host + Path, context.Response.ContentType))
                                {
                                    System.Diagnostics.Debug.Write("DASH Manifest: " + Host + Path + " ContentType: " + context.Response.ContentType);
                                    context.Response.Redirect(Host + Path);
                                    await response.Content.CopyToAsync(context.Response.Body);
                                }
                                else
                                {
                                    System.Diagnostics.Debug.Write("No asset associated with the incoming request: " + Host + Path + " ContentType: " + context.Response.ContentType);
                                    context.Response.ContentLength = response.Content.Headers.ContentLength;
                                    context.Response.ContentType = response.Content.Headers.ContentType.ToString();
                                    await response.Content.CopyToAsync(context.Response.Body);
                                }


                            }
                        }
                    }

                }
            });
        }
    }
}
