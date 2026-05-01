using System.ComponentModel;

namespace Lantean.QBTMud.Core.Models
{
    public enum AppliesTo
    {
        [Description("Filename + Extension")]
        FilenameExtension,

        Filename,
        Extension
    }
}
