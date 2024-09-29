using Exiled.API.Interfaces;

namespace MyPlugin2
{
    public class Config : IConfig
    {
        public bool IsEnabled { get; set; }
        public bool Debug { get; set; }
    }
}