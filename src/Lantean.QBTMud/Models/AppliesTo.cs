using System.ComponentModel;

namespace Lantean.QBTMud.Models
{
    public enum AppliesTo
    {
        [Description("Filename + Extension")]
        FilenameExtension,

        Filename,
        Extension
    }
}