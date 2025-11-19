namespace Lantean.QBTMud
{
    public class LongPressEventArgs : EventArgs
    {
        /// <summary>
        ///
        /// </summary>
        public bool Bubbles { get; set; }

        /// <summary>
        ///
        /// </summary>
        public bool Cancelable { get; set; }

        /// <summary>
        /// The X coordinate of the mouse pointer in global (screen) coordinates.
        /// </summary>
        public double ScreenX { get; set; }

        /// <summary>
        /// The Y coordinate of the mouse pointer in global (screen) coordinates.
        /// </summary>
        public double ScreenY { get; set; }

        /// <summary>
        /// The X coordinate of the mouse pointer in local (DOM content) coordinates.
        /// </summary>
        public double ClientX { get; set; }

        /// <summary>
        /// The Y coordinate of the mouse pointer in local (DOM content) coordinates.
        /// </summary>
        public double ClientY { get; set; }

        /// <summary>
        /// The X coordinate of the mouse pointer in relative (Target Element) coordinates.
        /// </summary>
        public double OffsetX { get; set; }

        /// <summary>
        /// The Y coordinate of the mouse pointer in relative (Target Element) coordinates.
        /// </summary>
        public double OffsetY { get; set; }

        /// <summary>
        /// The X coordinate of the mouse pointer relative to the whole document.
        /// </summary>
        public double PageX { get; set; }

        /// <summary>
        /// The Y coordinate of the mouse pointer relative to the whole document.
        /// </summary>
        public double PageY { get; set; }

        /// <summary>
        ///
        /// </summary>
        public string? SourceElement { get; set; }

        /// <summary>
        ///
        /// </summary>
        public string? TargetElement { get; set; }

        /// <summary>
        ///
        /// </summary>
        public double TimeStamp { get; set; }

        /// <summary>
        ///
        /// </summary>
        public string? Type { get; set; }
    }
}