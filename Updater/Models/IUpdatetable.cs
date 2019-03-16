using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Updater
{
    public interface IUpdatetable
    {
        string ApplicationName { get; }
        string ApplicationID { get; }
        Assembly ApplicationAssemby { get; }
        ImageSource ApplicationIcon { get; }
        Uri UpdateXmlLocation { get; }
        Window Context { get; }
    }
}
