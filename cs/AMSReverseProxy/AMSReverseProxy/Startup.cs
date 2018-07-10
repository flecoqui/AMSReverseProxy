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
                    if (url.EndsWith(".m3u8", StringComparison.CurrentCultureIgnoreCase))
                        return true;
                    if (url.EndsWith("/manifest(format=m3u8-aapl)", StringComparison.CurrentCultureIgnoreCase))
                        return true;
                    if (url.EndsWith("manifest(format=m3u8-aapl-v3)", StringComparison.CurrentCultureIgnoreCase))
                        return true;
                }
            }
            return false;
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
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.Run(async (context) =>
            {
                string Path = context.Request.Path;
                if (string.IsNullOrEmpty(Path))
                    await context.Response.WriteAsync("Hello World!");
                else
                {
                    string Host = "http://localhost/";
                 //   string Host = "http://srgtest-accountdigitalinnovation-euwe.channel.media.azure.net/";
                    //string Host = "http://amssamples.streaming.mediaservices.windows.net/";
                    System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
                    var response = await client.GetAsync(new Uri(Host + Path));
                    if (response != null)
                    {

                        context.Response.ContentType = response.Content.Headers.ContentType.MediaType;
                        context.Response.ContentLength = response.Content.Headers.ContentLength.Value;
                        if(IsSmoothStreamingManifest(Host + Path, context.Response.ContentType))
                        {
                            System.Diagnostics.Debug.Write("SmoothStreaming Manifest: " + Host + Path + " ContentType: " + context.Response.ContentType);

                            SmoothHelper.ManifestManager mm = SmoothHelper.ManifestManager.CreateManifestCache(new Uri(Host + Path), false, 5000000, 20);
                            if(mm != null)
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
                                if(result == true)
                                {
                                    System.Diagnostics.Debug.Write("SmoothStreaming Manifest: " + Host + Path + " correctly downloaded and parsed ");
                                    
                                    // Download Text Chunks
                                    if ((mm.TextChunkListList != null) && (mm.TextChunkListList.Count > 0))
                                    {
                                        // Something to download
                                        if (!mm.IsDownloadCompleted(mm.TextChunkListList))
                                        {
                                            System.Diagnostics.Debug.WriteLine(string.Format("{0:d/M/yyyy HH:mm:ss.fff}", DateTime.Now) + " downloadThread downloading text chunks for Manifest Uri: " + mm.ManifestUri.ToString());
                                            foreach (var cl in mm.TextChunkListList)
                                            {
                                                SmoothHelper.ChunkBuffer cb;
                                                while (cl.ChunksToReadQueue.TryDequeue(out cb))
                                                {
                                                    string url = (string.IsNullOrEmpty(mm.RedirectBaseUrl) ? mm.BaseUrl : mm.RedirectBaseUrl) + "/" + cl.TemplateUrl.Replace("{start_time}", cb.Time.ToString());
                                                    System.Diagnostics.Debug.WriteLine(string.Format("{0:d/M/yyyy HH:mm:ss.fff}", DateTime.Now) + " downloadThread downloading text chunks : " + url.ToString());
                                                    cb.chunkBuffer = await mm.DownloadChunkAsync(new Uri(url));

                                                    if (cb.IsChunkDownloaded())
                                                    {
                                                        ulong l = cb.GetLength();
                                                        string text = ParseTTMLChunk(cb.chunkBuffer);
                                                        double time = mm.TimescaleToHNS(cb.Time) / (SmoothHelper.ManifestManager.TimeUnit);
                                                        
                                                        System.Diagnostics.Debug.Write("TTML file at : " + time.ToString() + " seconds \r\n" + text );
                                                        System.Text.Encoding.UTF8.GetBytes(text);
                                                        SmoothHelper.TTMLSubtitles tTMLSubtitles = new SmoothHelper.TTMLSubtitles(time);
                                                        if(tTMLSubtitles!=null)
                                                        {
                                                            if(tTMLSubtitles.ParseTTMLSubtitles(System.Text.Encoding.UTF8.GetBytes(text))==true)
                                                            {
                                                                if (tTMLSubtitles.subtitleList != null)
                                                                {
                                                                    long count = tTMLSubtitles.subtitleList.LongCount();
                                                                    System.Diagnostics.Debug.WriteLine(string.Format("{0:d/M/yyyy HH:mm:ss.fff}", DateTime.Now) + count.ToString() + " subtitles found in the chunk");
                                                                }
                                                            }
                                                        }
                                                        cl.InputBytes += l;
                                                        cl.InputChunks++;

                                                        cl.ChunksQueue.Enqueue(cb);
                                                    }
                                                }
                                            }                                       
                                            System.Diagnostics.Debug.WriteLine(string.Format("{0:d/M/yyyy HH:mm:ss.fff}", DateTime.Now) + " downloadThread downloading text chunks done for Uri: " + mm.ManifestUri.ToString());
                                        }
                                        else
                                        {
                                            if (mm.IsLive)
                                                result = true;
                                        }
                                    }
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
                            await response.Content.CopyToAsync(context.Response.Body);
                        }
                        else if (IsDASHManifest(Host + Path, context.Response.ContentType))
                        {
                            System.Diagnostics.Debug.Write("DASH Manifest: " + Host + Path + " ContentType: " + context.Response.ContentType);
                            await response.Content.CopyToAsync(context.Response.Body);
                        }

                    }

                }
            });
        }
    }
}
