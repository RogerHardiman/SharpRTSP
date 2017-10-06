using System;
using System.Threading;
using System.Threading.Tasks;
using Rtsp;

namespace RtspClientExample
{
    class Program
    {
        enum StreamMode { Play, Pause,  Stop };

        static void Main(string[] args)
        {
            //String url = "rtsp://192.168.1.128/ch1.h264";    // IPS
            //String url = "rtsp://192.168.1.125/onvif-media/media.amp?profile=quality_h264"; // Axis
            //String url = "rtsp://user:password@192.168.1.102/onvif-media/media.amp?profile=quality_h264"; // Axis
            //String url = "rtsp://192.168.1.124/rtsp_tunnel?h26x=4&line=1&inst=1"; // Bosch

            //String url = "rtsp://192.168.1.121:8554/h264";  // Raspberry Pi RPOS using Live555
            //String url = "rtsp://192.168.1.121:8554/h264m";  // Raspberry Pi RPOS using Live555 in Multicast mode

            //String url = "rtsp://127.0.0.1:8554/h264ESVideoTest"; // Live555 Cygwin
            //String url = "rtsp://192.168.1.160:8554/h264ESVideoTest"; // Live555 Cygwin
            //String url = "rtsp://127.0.0.1:8554/h264ESVideoTest"; // Live555 Cygwin
             String url = "rtsp://wowzaec2demo.streamlock.net/vod/mp4:BigBuckBunny_115k.mov";

            // MJPEG Tests (Payload 26)
            //String url = "rtsp://192.168.1.125/onvif-media/media.amp?profile=mobile_jpeg";


            DateTime time_check = DateTime.Now; // Holds the time 'Play' started or the time of the last NAL
            StreamMode current_mode = StreamMode.Play;
            StreamMode requested_mode = StreamMode.Play;

            // Create a RTSP Client. This is created in a Task so that it can connect (and re-connect)
            // in parallel with the User Interface (readline)
            Task rtspTask = new Task(delegate {
                // Keep looping until want_video is false
                while (requested_mode != StreamMode.Stop) {
                    RTSPClient c = new RTSPClient(url, RTSPClient.RTP_TRANSPORT.TCP);
                    c.Received_SPS32_PPS32 += (byte[] sps32, byte[] pps32) => {
                        Console.WriteLine("Received SPS and PPS from RTSP Client");
                        // Pass this data into a H264 decoder
                    };
                    c.Received_NAL32 += (byte[] nal32) => {
                        Console.WriteLine("Received NALs from RTSP Client");
                        time_check = DateTime.Now;
                        // Pass this data into a H264 decoder
                    };

                    time_check = DateTime.Now;
                    current_mode = StreamMode.Play;

                    // busy-wait loop
                    // Check Connection Status (on RTSP Socket.
                    // Check when last NAL arrived
                    // Check User Intetface requests
                    while ((c.CurrentStatus == RtspStatus.Connecting
                            || c.CurrentStatus == RtspStatus.Connected)
                           && requested_mode != StreamMode.Stop) {

                        if (requested_mode == StreamMode.Play && current_mode != StreamMode.Play) {
                            c.Play();
                            time_check = DateTime.Now;
                            current_mode = StreamMode.Play;
                        }

                        if (requested_mode == StreamMode.Pause && current_mode != StreamMode.Pause) {
                            c.Pause();
                            current_mode = StreamMode.Pause;
                        }

                        if (current_mode == StreamMode.Play && (DateTime.Now - time_check).TotalSeconds > 10) {
                            // No NALs received in the last 10 seconds. Assume the stream has failed. Disconnect & Reconnect
                            current_mode = StreamMode.Stop;
                            c.Stop(); // Stop closes the RTSP Socket. Status will change to Disconnected
                        }

                        Thread.Sleep(250);
                    }

                    // User requested stop, or Status gives a connection error.
                    c.Stop();


                    // Wait 1 second before reconnecting, if we still want video
                    if (requested_mode != StreamMode.Stop) Thread.Sleep(1000);
                }
            });
            rtspTask.Start();

            // Wait for user to terminate programme
            // Check for null which is returned when running under some IDEs
            Console.WriteLine("Press 1 then ENTER to play");
            Console.WriteLine("Press 2 then ENTER to pause");
            Console.WriteLine("Press Q then ENTER to quit");

            String readline = null;
            while (requested_mode != StreamMode.Stop) {
                readline = Console.ReadLine();

                if (readline == null) {
                    // Avoid maxing out CPU on systems that instantly return null for ReadLine
                    Thread.Sleep(500);
                } else if (readline.Equals("1")) {
                    requested_mode = StreamMode.Play;
                } else if (readline.Equals("2")) {
                    requested_mode = StreamMode.Pause;
                } else if (readline.ToUpper().Equals("Q")) {
                    requested_mode = StreamMode.Stop;
                }
            }


            rtspTask.Wait(); // wait for Task to complete before exiting

        }
    }
}
