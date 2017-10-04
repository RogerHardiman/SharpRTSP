using System;

namespace Rtsp
{
    public enum RtspStatus { Unknown, Connecting, ConnectFailed, Connected, Disconnected };

    /// <summary>
    /// Event args containing information for status events.
    /// </summary>
    public class RtspStatusEventArgs :EventArgs
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="RtspStatusEventArgs"/> class.
        /// </summary>
        /// <param name="aStatus">A status.</param>
        public RtspStatusEventArgs(RtspStatus aStatus)
        {
            Status = aStatus;
        }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>The status.</value>
        public RtspStatus Status { get; set; }
    }
}
