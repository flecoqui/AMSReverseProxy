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
                    string Host = "http://amssamples.streaming.mediaservices.windows.net/";
                    System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
                    var response = await client.GetAsync(new Uri(Host + Path));
                    if (response != null)
                    {
                        context.Response.ContentType = response.Content.Headers.ContentType.MediaType;
                        context.Response.ContentLength = response.Content.Headers.ContentLength.Value;
                        if(IsSmoothStreamingManifest(Host + Path, context.Response.ContentType))
                        {
                            System.Diagnostics.Debug.Write("SmoothStreaming Manifest: " + Host + Path + " ContentType: " + context.Response.ContentType);
                        }
                        else if (IsHLSManifest(Host + Path, context.Response.ContentType))
                        {
                            System.Diagnostics.Debug.Write("HLS Manifest: " + Host + Path + " ContentType: " + context.Response.ContentType);

                        }
                        else if (IsDASHManifest(Host + Path, context.Response.ContentType))
                        {
                            System.Diagnostics.Debug.Write("DASH Manifest: " + Host + Path + " ContentType: " + context.Response.ContentType);

                        }
                        await response.Content.CopyToAsync(context.Response.Body);
                    }

                }
            });
        }
    }
}
