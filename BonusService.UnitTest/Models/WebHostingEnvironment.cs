using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace BonusService.UnitTest.Models
{
    public class WebHostingEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; }

        public string ApplicationName { get; set; }

        public string WebRootPath { get; set; }

        public IFileProvider WebRootFileProvider { get; set; }

        public string ContentRootPath { get; set; }

        public IFileProvider ContentRootFileProvider { get; set; }
        IFileProvider IWebHostEnvironment.WebRootFileProvider { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        IFileProvider IHostEnvironment.ContentRootFileProvider { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}
